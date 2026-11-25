import * as THREE from 'three';
import { WORLD_LAYOUT } from './scene.js';

function createGlowSpriteTexture() {
  const canvas = document.createElement('canvas');
  canvas.width = 64;
  canvas.height = 64;
  const ctx = canvas.getContext('2d');
  const grad = ctx.createRadialGradient(32, 32, 2, 32, 32, 30);
  grad.addColorStop(0, 'rgba(255,255,255,1)');
  grad.addColorStop(0.28, 'rgba(255,255,255,0.88)');
  grad.addColorStop(0.62, 'rgba(255,255,255,0.28)');
  grad.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 64, 64);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  return tex;
}

// Merge multiple TubeGeometries sharing identical state & material into 1 draw call
function mergeTubeGeometries(geoms) {
  const parts = [];
  let totalVerts = 0;
  for (let i = 0; i < geoms.length; i++) {
    const g = geoms[i].index ? geoms[i].toNonIndexed() : geoms[i].clone();
    if (!g.attributes.normal) g.computeVertexNormals();
    totalVerts += g.attributes.position.count;
    parts.push(g);
    geoms[i].dispose();
  }
  const positions = new Float32Array(totalVerts * 3);
  const normals = new Float32Array(totalVerts * 3);
  let offset = 0;
  for (let i = 0; i < parts.length; i++) {
    const g = parts[i];
    const count = g.attributes.position.count;
    positions.set(g.attributes.position.array, offset * 3);
    normals.set(g.attributes.normal.array, offset * 3);
    offset += count;
    g.dispose();
  }
  const merged = new THREE.BufferGeometry();
  merged.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  merged.setAttribute('normal', new THREE.BufferAttribute(normals, 3));
  merged.computeBoundingSphere();
  return merged;
}

// Pre-sample CatmullRomCurve3 ONCE at startup into flat Float32Array lookup tables (positions + unit tangents)
function bakeCurveLUT(curve, samples = 128) {
  const pos = new Float32Array((samples + 1) * 3);
  const tan = new Float32Array((samples + 1) * 3);
  const tmpP = new THREE.Vector3();
  const tmpT = new THREE.Vector3();
  for (let i = 0; i <= samples; i++) {
    const t = i / samples;
    curve.getPointAt(t, tmpP);
    curve.getTangentAt(t, tmpT).normalize();
    const o = i * 3;
    pos[o] = tmpP.x;
    pos[o + 1] = tmpP.y;
    pos[o + 2] = tmpP.z;
    tan[o] = tmpT.x;
    tan[o + 1] = tmpT.y;
    tan[o + 2] = tmpT.z;
  }
  return { pos, tan, samples };
}

// Zero-allocation O(1) linear interpolation along a baked curve LUT
function sampleLUT(lut, t, outPos, outTan) {
  const f = t * lut.samples;
  const i0 = f | 0;
  const frac = f - i0;
  const i1 = i0 < lut.samples ? i0 + 1 : lut.samples;
  const o0 = i0 * 3;
  const o1 = i1 * 3;
  const p = lut.pos;
  outPos.x = p[o0] + (p[o1] - p[o0]) * frac;
  outPos.y = p[o0 + 1] + (p[o1 + 1] - p[o0 + 1]) * frac;
  outPos.z = p[o0 + 2] + (p[o1 + 2] - p[o0 + 2]) * frac;

  const n = lut.tan;
  const tx = n[o0] + (n[o1] - n[o0]) * frac;
  const ty = n[o0 + 1] + (n[o1 + 1] - n[o0 + 1]) * frac;
  const tz = n[o0 + 2] + (n[o1 + 2] - n[o0 + 2]) * frac;
  const invLen = 1 / (Math.hypot(tx, ty, tz) || 1);
  outTan.x = tx * invLen;
  outTan.y = ty * invLen;
  outTan.z = tz * invLen;
}

// Sleek dual-layer conduit (supports single curve or array of curves merged into 2 draw calls)
function makeDualConduit(
  curvesOrCurve,
  outerRadius,
  colorHex,
  {
    tubularSegments = 32,
    closed = false,
    jacketOpacity = 0.24,
    coreScale = 0.46,
    emissiveIntensity = 1.0,
  } = {}
) {
  const curves = Array.isArray(curvesOrCurve) ? curvesOrCurve : [curvesOrCurve];
  const group = new THREE.Group();

  const jacketMat = new THREE.MeshPhysicalMaterial({
    color: colorHex,
    roughness: 0.14,
    metalness: 0.10,
    clearcoat: 0.85,
    transparent: true,
    opacity: jacketOpacity,
    depthWrite: false,
  });
  const jacketGeom =
    curves.length === 1
      ? new THREE.TubeGeometry(curves[0], tubularSegments, outerRadius, 8, closed)
      : mergeTubeGeometries(curves.map((c) => new THREE.TubeGeometry(c, tubularSegments, outerRadius, 8, closed)));
  const jacket = new THREE.Mesh(jacketGeom, jacketMat);
  group.add(jacket);

  const coreMat = new THREE.MeshStandardMaterial({
    color: colorHex,
    emissive: colorHex,
    emissiveIntensity,
    roughness: 0.25,
    metalness: 0.2,
    transparent: true,
    opacity: 0.88,
  });
  const coreGeom =
    curves.length === 1
      ? new THREE.TubeGeometry(curves[0], tubularSegments, outerRadius * coreScale, 6, closed)
      : mergeTubeGeometries(
          curves.map((c) => new THREE.TubeGeometry(c, tubularSegments, outerRadius * coreScale, 6, closed))
        );
  const core = new THREE.Mesh(coreGeom, coreMat);
  group.add(core);

  return {
    group,
    jacketMat,
    coreMat,
    setState(active, color, intensity, jacketAlpha = jacketOpacity) {
      if (color !== null) {
        jacketMat.color.setHex(color);
        coreMat.color.setHex(color);
        coreMat.emissive.setHex(color);
      }
      jacketMat.opacity = active ? jacketAlpha : 0.06;
      coreMat.opacity = active ? 0.90 : 0.16;
      coreMat.emissiveIntensity = active ? intensity : 0.12;
    },
  };
}

// Module-level shared geometries & scratch math objects (ZERO allocations inside update())
const SHARED_PACKET_GEOM = new THREE.CapsuleGeometry(0.14, 0.56, 4, 8);
const SHARED_RING_GEOM = new THREE.TorusGeometry(0.28, 0.055, 6, 16);
const UP_AXIS = new THREE.Vector3(0, 1, 0);
const Z_AXIS = new THREE.Vector3(0, 0, 1);
const SCRATCH_POS = new THREE.Vector3();
const SCRATCH_TAN = new THREE.Vector3();
const SCRATCH_QUAT = new THREE.Quaternion();
const SCRATCH_SCALE = new THREE.Vector3();
const SCRATCH_MAT4 = new THREE.Matrix4();
const ZERO_MAT4 = new THREE.Matrix4().makeScale(0, 0, 0);
const SCRATCH_COLOR = new THREE.Color();

/**
 * Batched multi-stream packet & additive halo pool for an entire flow loop.
 * Renders all packet streams in a loop in just 2 draw calls (1 InstancedMesh + 1 Points).
 */
function createPacketPool(parentGroup, streamSpecs, spriteTex, haloSize = 1.45) {
  let totalCount = 0;
  for (let i = 0; i < streamSpecs.length; i++) totalCount += streamSpecs[i].count;

  // HDR-tinted MeshBasicMaterial with per-instance colors -> triggers calibrated UnrealBloomPass
  const packetMat = new THREE.MeshBasicMaterial({ color: 0xffffff });
  const mesh = new THREE.InstancedMesh(SHARED_PACKET_GEOM, packetMat, totalCount);
  mesh.frustumCulled = false;
  parentGroup.add(mesh);

  const haloPositions = new Float32Array(totalCount * 3);
  const haloColors = new Float32Array(totalCount * 3);
  const haloGeom = new THREE.BufferGeometry();
  haloGeom.setAttribute('position', new THREE.BufferAttribute(haloPositions, 3));
  haloGeom.setAttribute('color', new THREE.BufferAttribute(haloColors, 3));
  const haloMat = new THREE.PointsMaterial({
    color: 0xffffff,
    vertexColors: true,
    size: haloSize,
    map: spriteTex,
    transparent: true,
    opacity: 0.84,
    depthWrite: false,
    blending: THREE.AdditiveBlending,
  });
  const halos = new THREE.Points(haloGeom, haloMat);
  halos.frustumCulled = false;
  parentGroup.add(halos);

  const handles = [];
  let cursor = 0;
  for (let s = 0; s < streamSpecs.length; s++) {
    const spec = streamSpecs[s];
    const startIdx = cursor;
    const count = spec.count;
    cursor += count;

    const offsets = new Float32Array(count);
    const sizeFactors = new Float32Array(count);
    const hdrGain = spec.hdrGain ?? 1.38;
    SCRATCH_COLOR.setHex(spec.colorHex);
    const hr = SCRATCH_COLOR.r;
    const hg = SCRATCH_COLOR.g;
    const hb = SCRATCH_COLOR.b;
    SCRATCH_COLOR.multiplyScalar(hdrGain);

    for (let i = 0; i < count; i++) {
      offsets[i] = i / count;
      sizeFactors[i] = i % 3 === 0 ? 1.30 : i % 3 === 1 ? 0.95 : 0.72;
      mesh.setMatrixAt(startIdx + i, ZERO_MAT4);
      mesh.setColorAt(startIdx + i, SCRATCH_COLOR);
      const co = (startIdx + i) * 3;
      haloColors[co] = hr;
      haloColors[co + 1] = hg;
      haloColors[co + 2] = hb;
      haloPositions[co + 1] = -9999;
    }

    handles.push({
      lut: spec.lut,
      startIdx,
      count,
      offsets,
      sizeFactors,
      packetScale: spec.packetScale ?? 1.0,
      reverse: Boolean(spec.reverse),
      hdrGain,
      currentHex: spec.colorHex,
      wasActive: true,
    });
  }
  mesh.instanceMatrix.needsUpdate = true;
  if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
  haloGeom.attributes.position.needsUpdate = true;
  haloGeom.attributes.color.needsUpdate = true;

  let colorDirty = false;

  function setStreamColor(handle, hex) {
    if (handle.currentHex === hex) return;
    handle.currentHex = hex;
    SCRATCH_COLOR.setHex(hex);
    const hr = SCRATCH_COLOR.r;
    const hg = SCRATCH_COLOR.g;
    const hb = SCRATCH_COLOR.b;
    SCRATCH_COLOR.multiplyScalar(handle.hdrGain);
    const end = handle.startIdx + handle.count;
    for (let idx = handle.startIdx; idx < end; idx++) {
      mesh.setColorAt(idx, SCRATCH_COLOR);
      const co = idx * 3;
      haloColors[co] = hr;
      haloColors[co + 1] = hg;
      haloColors[co + 2] = hb;
    }
    colorDirty = true;
  }

  function updateStream(handle, phase, active, scaleMult = 1.0) {
    const start = handle.startIdx;
    const count = handle.count;
    if (!active) {
      if (handle.wasActive) {
        for (let i = 0; i < count; i++) {
          const idx = start + i;
          mesh.setMatrixAt(idx, ZERO_MAT4);
          haloPositions[idx * 3 + 1] = -9999;
        }
        handle.wasActive = false;
      }
      return;
    }

    handle.wasActive = true;
    const lut = handle.lut;
    const offsets = handle.offsets;
    const sizeFactors = handle.sizeFactors;
    const baseScale = handle.packetScale * scaleMult;
    const rev = handle.reverse;

    for (let i = 0; i < count; i++) {
      let t = (offsets[i] + (rev ? -phase : phase)) % 1;
      if (t < 0) t += 1;

      sampleLUT(lut, t, SCRATCH_POS, SCRATCH_TAN);
      if (rev) SCRATCH_TAN.negate();

      SCRATCH_QUAT.setFromUnitVectors(UP_AXIS, SCRATCH_TAN);
      const s = baseScale * sizeFactors[i];
      SCRATCH_SCALE.set(s, s * 1.15, s);
      SCRATCH_MAT4.compose(SCRATCH_POS, SCRATCH_QUAT, SCRATCH_SCALE);

      const idx = start + i;
      mesh.setMatrixAt(idx, SCRATCH_MAT4);
      const po = idx * 3;
      haloPositions[po] = SCRATCH_POS.x;
      haloPositions[po + 1] = SCRATCH_POS.y;
      haloPositions[po + 2] = SCRATCH_POS.z;
    }
  }

  function flush() {
    mesh.instanceMatrix.needsUpdate = true;
    haloGeom.attributes.position.needsUpdate = true;
    if (colorDirty) {
      if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
      haloGeom.attributes.color.needsUpdate = true;
      colorDirty = false;
    }
  }

  return { handles, setStreamColor, updateStream, flush };
}

/**
 * Batched sliding collar-ring indicator pool (1 InstancedMesh draw call per loop).
 */
function createRingPool(parentGroup, ringSpecs) {
  let totalCount = 0;
  for (let i = 0; i < ringSpecs.length; i++) totalCount += ringSpecs[i].count;

  const mat = new THREE.MeshBasicMaterial({
    color: 0xffffff,
    transparent: true,
    opacity: 0.90,
  });
  const mesh = new THREE.InstancedMesh(SHARED_RING_GEOM, mat, totalCount);
  mesh.frustumCulled = false;
  parentGroup.add(mesh);

  const handles = [];
  let cursor = 0;
  for (let s = 0; s < ringSpecs.length; s++) {
    const spec = ringSpecs[s];
    const startIdx = cursor;
    const count = spec.count;
    cursor += count;

    const offsets = new Float32Array(count);
    SCRATCH_COLOR.setHex(spec.colorHex).multiplyScalar(1.35);
    for (let i = 0; i < count; i++) {
      offsets[i] = i / count;
      mesh.setMatrixAt(startIdx + i, ZERO_MAT4);
      mesh.setColorAt(startIdx + i, SCRATCH_COLOR);
    }

    handles.push({
      lut: spec.lut,
      startIdx,
      count,
      offsets,
      ringScale: spec.ringScale ?? 1.0,
      reverse: Boolean(spec.reverse),
      wasActive: true,
    });
  }
  mesh.instanceMatrix.needsUpdate = true;
  if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;

  function updateRing(handle, phase, active) {
    const start = handle.startIdx;
    const count = handle.count;
    if (!active) {
      if (handle.wasActive) {
        for (let i = 0; i < count; i++) mesh.setMatrixAt(start + i, ZERO_MAT4);
        handle.wasActive = false;
      }
      return;
    }

    handle.wasActive = true;
    const lut = handle.lut;
    const offsets = handle.offsets;
    const rev = handle.reverse;
    const sc = handle.ringScale;

    for (let i = 0; i < count; i++) {
      let t = (offsets[i] + (rev ? -phase : phase)) % 1;
      if (t < 0) t += 1;

      sampleLUT(lut, t, SCRATCH_POS, SCRATCH_TAN);
      if (rev) SCRATCH_TAN.negate();

      SCRATCH_QUAT.setFromUnitVectors(Z_AXIS, SCRATCH_TAN);
      SCRATCH_SCALE.set(sc, sc, sc);
      SCRATCH_MAT4.compose(SCRATCH_POS, SCRATCH_QUAT, SCRATCH_SCALE);
      mesh.setMatrixAt(start + i, SCRATCH_MAT4);
    }
  }

  function flush() {
    mesh.instanceMatrix.needsUpdate = true;
  }

  return { handles, updateRing, flush };
}

export function createFlows(scene) {
  const spriteTex = createGlowSpriteTexture();

  const loop1Group = new THREE.Group();
  loop1Group.name = 'loop1_primary_75c';
  const loop2Group = new THREE.Group();
  loop2Group.name = 'loop2_5gdhc_exergy';
  const loop3Group = new THREE.Group();
  loop3Group.name = 'loop3_water_rain';
  const opticalGroup = new THREE.Group();
  opticalGroup.name = 'optical_sdn_ring';

  scene.add(loop1Group, loop2Group, loop3Group, opticalGroup);

  const hubY = WORLD_LAYOUT.VAULT_Y + 0.8;
  const vaults = WORLD_LAYOUT.VAULT_POSITIONS;

  // ============================================================================
  // LOOP 1: 75°C Primary Microfluidic Cooling (Vaults -> Hub + 50°C Return Manifold)
  // ============================================================================
  const loop1PacketSpecs = [];
  const loop1RingSpecs = [];
  const loop1Conduits = vaults.map((vp, idx) => {
    const supplyCurve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(vp.x, vp.y + 0.45, vp.z),
      new THREE.Vector3(vp.x * 0.62, hubY + 1.55, vp.z * 0.62),
      new THREE.Vector3(-4.2, hubY + 1.95, -3.5),
    ]);
    const supplyLUT = bakeCurveLUT(supplyCurve, 96);
    const supplyConduit = makeDualConduit(supplyCurve, 0.25, 0xff521b, {
      tubularSegments: 28,
      jacketOpacity: 0.26,
      emissiveIntensity: 1.15,
    });
    loop1Group.add(supplyConduit.group);

    const perpX = -Math.sin(vp.angle) * 0.95;
    const perpZ = Math.cos(vp.angle) * 0.95;
    const returnCurve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(-4.2 + perpX * 0.4, hubY + 0.45, -3.5 + perpZ * 0.4),
      new THREE.Vector3(vp.x * 0.62 + perpX, hubY + 0.25, vp.z * 0.62 + perpZ),
      new THREE.Vector3(vp.x + perpX * 0.5, vp.y - 0.15, vp.z + perpZ * 0.5),
    ]);
    const returnLUT = bakeCurveLUT(returnCurve, 96);
    const returnConduit = makeDualConduit(returnCurve, 0.17, 0xf59e0b, {
      tubularSegments: 24,
      jacketOpacity: 0.20,
      emissiveIntensity: 0.78,
    });
    loop1Group.add(returnConduit.group);

    const supplyPktIdx = loop1PacketSpecs.length;
    loop1PacketSpecs.push({ lut: supplyLUT, count: 10, colorHex: 0xff7733, packetScale: 1.0, hdrGain: 1.42 });
    const returnPktIdx = loop1PacketSpecs.length;
    loop1PacketSpecs.push({ lut: returnLUT, count: 7, colorHex: 0xfbbf24, packetScale: 0.72, hdrGain: 1.25 });

    const supplyRingIdx = loop1RingSpecs.length;
    loop1RingSpecs.push({ lut: supplyLUT, count: 4, colorHex: 0xff9a3c, ringScale: 1.05 });

    return { idx, supplyConduit, returnConduit, supplyPktIdx, returnPktIdx, supplyRingIdx };
  });

  const loop1Packets = createPacketPool(loop1Group, loop1PacketSpecs, spriteTex, 1.55);
  const loop1Rings = createRingPool(loop1Group, loop1RingSpecs);

  // ============================================================================
  // LOOP 2: 5GDHC 30 km Twin-Pipe Ring + Radial Spurs + Exergy Cascade & Deficit Discharge
  // ============================================================================
  function makeHorizontalRingCurve(radius, y, segments = 32) {
    const pts = [];
    for (let i = 0; i < segments; i++) {
      const a = (i / segments) * Math.PI * 2;
      pts.push(new THREE.Vector3(Math.cos(a) * radius, y, Math.sin(a) * radius));
    }
    return new THREE.CatmullRomCurve3(pts, true);
  }

  const ringWarmCurve = makeHorizontalRingCurve(37.2, -2.4, 36);
  const ringWarmLUT = bakeCurveLUT(ringWarmCurve, 160);
  const ringWarmConduit = makeDualConduit(ringWarmCurve, 0.26, 0xf59e0b, {
    tubularSegments: 72,
    closed: true,
    jacketOpacity: 0.24,
    emissiveIntensity: 0.95,
  });
  loop2Group.add(ringWarmConduit.group);

  const ringCoolCurve = makeHorizontalRingCurve(38.8, -2.6, 36);
  const ringCoolLUT = bakeCurveLUT(ringCoolCurve, 160);
  const ringCoolConduit = makeDualConduit(ringCoolCurve, 0.24, 0x14b8a6, {
    tubularSegments: 72,
    closed: true,
    jacketOpacity: 0.24,
    emissiveIntensity: 0.90,
  });
  loop2Group.add(ringCoolConduit.group);

  const innerRingCurve = makeHorizontalRingCurve(16.4, -2.4, 24);
  const innerRingLUT = bakeCurveLUT(innerRingCurve, 128);
  const innerRingConduit = makeDualConduit(innerRingCurve, 0.18, 0x2dd4bf, {
    tubularSegments: 48,
    closed: true,
    jacketOpacity: 0.20,
    emissiveIntensity: 0.85,
  });
  loop2Group.add(innerRingConduit.group);

  // Merge all 5 radial zone spurs into ONE dual conduit (2 draw calls instead of 10!)
  const spurCurves = vaults.map(
    (vp) =>
      new THREE.CatmullRomCurve3([
        new THREE.Vector3(Math.cos(vp.angle) * 16.4, -2.4, Math.sin(vp.angle) * 16.4),
        new THREE.Vector3(Math.cos(vp.angle) * 37.2, -2.4, Math.sin(vp.angle) * 37.2),
        new THREE.Vector3(Math.cos(vp.angle) * 61.0, -2.2, Math.sin(vp.angle) * 61.0),
      ])
  );
  const spurLUTs = spurCurves.map((c) => bakeCurveLUT(c, 80));
  const radialSpursConduit = makeDualConduit(spurCurves, 0.15, 0x14b8a6, {
    tubularSegments: 20,
    jacketOpacity: 0.20,
    emissiveIntensity: 0.80,
  });
  loop2Group.add(radialSpursConduit.group);

  // Main Hub Riser from Thermal Store up to 5GDHC twin-pipe ring
  const hubToRingCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(-4.2, hubY + 1.6, -3.5),
    new THREE.Vector3(-1.2, -8.8, 4.5),
    new THREE.Vector3(0, -2.4, 37.2),
  ]);
  const hubToRingLUT = bakeCurveLUT(hubToRingCurve, 96);
  const hubToRingConduit = makeDualConduit(hubToRingCurve, 0.25, 0xf59e0b, {
    tubularSegments: 28,
    jacketOpacity: 0.26,
    emissiveIntensity: 1.1,
  });
  loop2Group.add(hubToRingConduit.group);

  // Summer Exergy-Priority Cascade Branches
  const tsOrigin = new THREE.Vector3(-4.2, hubY - 0.6, -3.5);
  function makeCascadeConduit(midPt, endPt, colorHex) {
    const curve = new THREE.CatmullRomCurve3([tsOrigin.clone(), midPt, endPt]);
    const lut = bakeCurveLUT(curve, 64);
    const conduit = makeDualConduit(curve, 0.15, colorHex, {
      tubularSegments: 20,
      jacketOpacity: 0.22,
      emissiveIntensity: 0.9,
    });
    loop2Group.add(conduit.group);
    return { lut, conduit };
  }

  const tcesBranch = makeCascadeConduit(
    new THREE.Vector3(-1.5, hubY - 0.4, 1.2),
    new THREE.Vector3(1.2, hubY - 1.2, 4.8),
    0xfbbf24
  );
  const ptesBranch = makeCascadeConduit(
    new THREE.Vector3(1.6, hubY - 0.2, 0.8),
    new THREE.Vector3(6.2, hubY - 1.0, 4.4),
    0xf97316
  );
  const orcBranch = makeCascadeConduit(
    new THREE.Vector3(-1.8, hubY - 0.3, -5.6),
    new THREE.Vector3(0.2, hubY - 1.4, -6.4),
    0x10b981
  );
  const mofBranch = makeCascadeConduit(
    new THREE.Vector3(-6.8, hubY - 0.5, -2.4),
    new THREE.Vector3(-8.6, hubY - 1.4, -0.4),
    0x2dd4bf
  );
  const dryCoolerBranch = makeCascadeConduit(
    new THREE.Vector3(3.2, hubY + 0.1, -1.6),
    new THREE.Vector3(9.4, hubY - 1.6, 0.2),
    0xf43f5e
  );

  // Winter Deficit Reverse Discharge (HTHP Bank + Thermal Store -> 5GDHC Riser)
  const hthpBoostCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(-5.6, hubY - 1.2, 4.4),
    new THREE.Vector3(-3.6, -11.5, 4.2),
    new THREE.Vector3(-1.2, -8.8, 4.5),
    new THREE.Vector3(0, -2.4, 37.2),
  ]);
  const hthpBoostLUT = bakeCurveLUT(hthpBoostCurve, 96);
  const hthpBoostConduit = makeDualConduit(hthpBoostCurve, 0.22, 0xff5a1f, {
    tubularSegments: 28,
    jacketOpacity: 0.26,
    emissiveIntensity: 1.35,
  });
  loop2Group.add(hthpBoostConduit.group);

  // Batch ALL 15 Loop 2 packet streams into 1 InstancedMesh + 1 Points (2 draw calls!)
  const loop2PacketSpecs = [
    { lut: ringWarmLUT, count: 26, colorHex: 0xfbbf24, packetScale: 1.05, hdrGain: 1.38 },
    { lut: ringCoolLUT, count: 24, colorHex: 0x2dd4bf, packetScale: 0.92, reverse: true, hdrGain: 1.32 },
    { lut: innerRingLUT, count: 14, colorHex: 0x5eead4, packetScale: 0.78, hdrGain: 1.28 },
    ...spurLUTs.map((lut) => ({ lut, count: 8, colorHex: 0x38e8d2, packetScale: 0.72, hdrGain: 1.25 })),
    { lut: hubToRingLUT, count: 14, colorHex: 0xfbbf24, packetScale: 1.0, hdrGain: 1.42 },
    { lut: tcesBranch.lut, count: 7, colorHex: 0xfde047, packetScale: 0.75, hdrGain: 1.38 },
    { lut: ptesBranch.lut, count: 7, colorHex: 0xfb923c, packetScale: 0.75, hdrGain: 1.38 },
    { lut: orcBranch.lut, count: 7, colorHex: 0x34d399, packetScale: 0.75, hdrGain: 1.38 },
    { lut: mofBranch.lut, count: 7, colorHex: 0x5eead4, packetScale: 0.75, hdrGain: 1.38 },
    { lut: dryCoolerBranch.lut, count: 7, colorHex: 0xfb7185, packetScale: 0.75, hdrGain: 1.38 },
    { lut: hthpBoostLUT, count: 14, colorHex: 0xff7849, packetScale: 1.05, hdrGain: 1.48 },
  ];
  const loop2Packets = createPacketPool(loop2Group, loop2PacketSpecs, spriteTex, 1.5);
  const L2_WARM = 0;
  const L2_COOL = 1;
  const L2_INNER = 2;
  const L2_SPUR0 = 3;
  const L2_RISER = 8;
  const L2_TCES = 9;
  const L2_PTES = 10;
  const L2_ORC = 11;
  const L2_MOF = 12;
  const L2_DRY = 13;
  const L2_HTHP = 14;

  const loop2Rings = createRingPool(loop2Group, [
    { lut: ringWarmLUT, count: 10, colorHex: 0xfde047, ringScale: 1.1 },
    { lut: hubToRingLUT, count: 5, colorHex: 0xfef08a, ringScale: 1.05 },
    { lut: hthpBoostLUT, count: 5, colorHex: 0xffa94d, ringScale: 1.15 },
  ]);

  // ============================================================================
  // LOOP 3: Roof Catchment Downspouts -> 10,000 m³ Cistern -> Ultrapure -> Vaults + 3D Rain
  // ============================================================================
  const cisternTop = new THREE.Vector3(4.8, hubY + 0.9, -3.2);
  const catchmentCurves = [
    new THREE.Vector3(24, -0.3, -18),
    new THREE.Vector3(22, -0.3, 14),
    new THREE.Vector3(-14, -0.3, -24),
    new THREE.Vector3(10, -0.3, -6),
  ].map((startPt, idx) => {
    const midPt = new THREE.Vector3(startPt.x * 0.45 + 3.0, -7.8 - idx * 0.4, startPt.z * 0.45 - 1.8);
    return new THREE.CatmullRomCurve3([startPt, midPt, cisternTop]);
  });
  const catchmentLUTs = catchmentCurves.map((c) => bakeCurveLUT(c, 80));
  // Merge all 4 catchment downspouts into ONE dual conduit (2 draw calls instead of 8!)
  const catchmentConduit = makeDualConduit(catchmentCurves, 0.16, 0x0ea5e9, {
    tubularSegments: 22,
    jacketOpacity: 0.24,
    emissiveIntensity: 0.95,
  });
  loop3Group.add(catchmentConduit.group);

  const cisternToUltrapureCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(4.8, hubY - 1.2, -5.2),
    new THREE.Vector3(4.7, hubY - 1.6, -6.5),
    new THREE.Vector3(4.6, hubY - 1.4, -7.8),
  ]);
  const cisternToUpLUT = bakeCurveLUT(cisternToUltrapureCurve, 48);
  const cisternToUpConduit = makeDualConduit(cisternToUltrapureCurve, 0.18, 0x6366f1, {
    tubularSegments: 14,
    jacketOpacity: 0.25,
    emissiveIntensity: 1.15,
  });
  loop3Group.add(cisternToUpConduit.group);

  const ultrapureConduits = vaults.map((vp, idx) => {
    const c = new THREE.CatmullRomCurve3([
      new THREE.Vector3(4.6, hubY - 1.4, -7.8),
      new THREE.Vector3(vp.x * 0.55, hubY - 1.1, vp.z * 0.55),
      new THREE.Vector3(vp.x, vp.y - 0.55, vp.z),
    ]);
    const lut = bakeCurveLUT(c, 80);
    const conduit = makeDualConduit(c, 0.13, 0x38bdf8, {
      tubularSegments: 20,
      jacketOpacity: 0.22,
      emissiveIntensity: 0.85,
    });
    loop3Group.add(conduit.group);
    return { idx, lut, conduit };
  });

  // Batch ALL 10 Loop 3 packet streams into 1 InstancedMesh + 1 Points (2 draw calls!)
  const loop3Packets = createPacketPool(
    loop3Group,
    [
      ...catchmentLUTs.map((lut) => ({ lut, count: 9, colorHex: 0x38bdf8, packetScale: 0.78, hdrGain: 1.35 })),
      { lut: cisternToUpLUT, count: 6, colorHex: 0x818cf8, packetScale: 0.8, hdrGain: 1.42 },
      ...ultrapureConduits.map((u) => ({ lut: u.lut, count: 8, colorHex: 0x7dd3fc, packetScale: 0.65, hdrGain: 1.30 })),
    ],
    spriteTex,
    1.25
  );
  const L3_UP_FEED = 4;
  const L3_VAULT0 = 5;

  // Enhanced 3D Wind-Angled Falling Rain Streaks + Ground Splash Shimmer
  const rainCount = 820;
  const rainPositions = new Float32Array(rainCount * 2 * 3);
  const rainSeeds = Array.from({ length: rainCount }, (_, i) => {
    const a = (i * 2.399963) % (Math.PI * 2);
    const r = Math.sqrt((i + 0.5) / rainCount) * 71;
    return {
      x: Math.cos(a) * r,
      y: (i * 7.13) % 56,
      z: Math.sin(a) * r,
      speed: 28 + (i % 9) * 2.6,
      len: 0.90 + (i % 5) * 0.20,
    };
  });
  const rainGeom = new THREE.BufferGeometry();
  rainGeom.setAttribute('position', new THREE.BufferAttribute(rainPositions, 3));
  const rainMat = new THREE.LineBasicMaterial({
    color: 0x7dd3fc,
    transparent: true,
    opacity: 0.21,
    depthWrite: false,
  });
  const rainLines = new THREE.LineSegments(rainGeom, rainMat);
  rainLines.frustumCulled = false;
  loop3Group.add(rainLines);

  const splashCount = 140;
  const splashPositions = new Float32Array(splashCount * 3);
  for (let i = 0; i < splashCount; i++) {
    const a = (i * 2.399963) % (Math.PI * 2);
    const r = Math.sqrt((i + 0.5) / splashCount) * 68;
    splashPositions[i * 3] = Math.cos(a) * r;
    splashPositions[i * 3 + 1] = 0.14;
    splashPositions[i * 3 + 2] = Math.sin(a) * r;
  }
  const splashGeom = new THREE.BufferGeometry();
  splashGeom.setAttribute('position', new THREE.BufferAttribute(splashPositions, 3));
  const splashMat = new THREE.PointsMaterial({
    color: 0x7dd3fc,
    size: 0.9,
    map: spriteTex,
    transparent: true,
    opacity: 0.22,
    depthWrite: false,
    blending: THREE.AdditiveBlending,
  });
  const rainSplashes = new THREE.Points(splashGeom, splashMat);
  rainSplashes.frustumCulled = false;
  loop3Group.add(rainSplashes);

  // ============================================================================
  // OPTICAL RING: 3.0 km NANF HCF + CPO Ring + Bidirectional SDN Packets + Bypass Arcs
  // ============================================================================
  const optPacketSpecs = [];
  const optRingSpecs = [];

  const ringHops = vaults.map((vp, i) => {
    const nextIdx = (i + 1) % 5;
    const nextVp = vaults[nextIdx];
    const midX = (vp.x + nextVp.x) * 0.56;
    const midZ = (vp.z + nextVp.z) * 0.56;
    const curve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(vp.x, vp.y + 0.32, vp.z),
      new THREE.Vector3(midX, vp.y + 0.55, midZ),
      new THREE.Vector3(nextVp.x, nextVp.y + 0.32, nextVp.z),
    ]);
    const lut = bakeCurveLUT(curve, 96);
    const conduit = makeDualConduit(curve, 0.18, 0xa855f7, {
      tubularSegments: 24,
      jacketOpacity: 0.30,
      emissiveIntensity: 1.25,
    });
    opticalGroup.add(conduit.group);

    const fwdIdx = optPacketSpecs.length;
    optPacketSpecs.push({ lut, count: 9, colorHex: 0x22d3ee, packetScale: 0.85, hdrGain: 1.45 });
    const revIdx = optPacketSpecs.length;
    optPacketSpecs.push({ lut, count: 7, colorHex: 0xe879f9, packetScale: 0.72, reverse: true, hdrGain: 1.40 });

    return { fromIdx: i, toIdx: nextIdx, conduit, fwdIdx, revIdx };
  });

  const bypassLinks = vaults.map((_, faultIdx) => {
    const prevIdx = (faultIdx + 4) % 5;
    const nextIdx = (faultIdx + 1) % 5;
    const prevVp = vaults[prevIdx];
    const nextVp = vaults[nextIdx];
    const curve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(prevVp.x, prevVp.y + 0.65, prevVp.z),
      new THREE.Vector3((prevVp.x + nextVp.x) * 0.32, prevVp.y + 1.85, (prevVp.z + nextVp.z) * 0.32),
      new THREE.Vector3(nextVp.x, nextVp.y + 0.65, nextVp.z),
    ]);
    const lut = bakeCurveLUT(curve, 96);
    const conduit = makeDualConduit(curve, 0.16, 0x22d3ee, {
      tubularSegments: 24,
      jacketOpacity: 0.32,
      emissiveIntensity: 1.5,
    });
    conduit.group.visible = false;
    opticalGroup.add(conduit.group);

    const pktIdx = optPacketSpecs.length;
    optPacketSpecs.push({ lut, count: 12, colorHex: 0x67e8f9, packetScale: 0.92, hdrGain: 1.50 });
    const ringIdx = optRingSpecs.length;
    optRingSpecs.push({ lut, count: 4, colorHex: 0x22d3ee, ringScale: 0.9 });

    return { faultIdx, prevIdx, nextIdx, conduit, pktIdx, ringIdx };
  });

  const optPackets = createPacketPool(opticalGroup, optPacketSpecs, spriteTex, 1.5);
  const optRings = createRingPool(opticalGroup, optRingSpecs);

  let phase1 = 0;
  let phase2 = 0;
  let phase3 = 0;
  let phaseOpt = 0;
  let internalElapsed = 0;

  function update(sim, dt, elapsed) {
    internalElapsed = typeof elapsed === 'number' ? elapsed : internalElapsed + dt;
    const pulse = 0.88 + 0.18 * Math.sin(internalElapsed * 3.4);

    // Advance scalar phases smoothly regardless of layer visibility
    const flowNorm = Math.min(1.2, sim.thermal.primaryGrossFlowLmin / 4590.0);
    phase1 = (phase1 + dt * (0.24 + flowNorm * 0.52)) % 1;

    const demandNorm = Math.min(1.2, sim.thermal.districtDemandMw / 9.52);
    phase2 = (phase2 + dt * (0.12 + demandNorm * 0.32)) % 1;

    const rainMult = sim.hydrology.rainfallMultiplier;
    phase3 = (phase3 + dt * (0.28 + Math.min(0.45, rainMult * 0.22))) % 1;

    const sdnOn = Boolean(sim.optical.sdnEnabled);
    phaseOpt = (phaseOpt + dt * (sdnOn ? 0.62 : 0.26)) % 1;

    // --- LOOP 1 (75°C primary cooling + 50°C return manifold) ---
    if (loop1Group.visible) {
      for (let i = 0; i < loop1Conduits.length; i++) {
        const ls = loop1Conduits[i];
        const vault = sim.vaults[ls.idx];
        const active = !vault.faulted && vault.itMw > 0.05;
        const vLoad = Math.min(1, vault.itMw / 2.0);
        ls.supplyConduit.setState(active, null, (0.75 + vLoad * 0.85) * pulse, 0.20 + vLoad * 0.14);
        ls.returnConduit.setState(active, null, (0.50 + vLoad * 0.55) * pulse, 0.16 + vLoad * 0.10);

        const pShift = (phase1 + ls.idx * 0.17) % 1;
        loop1Packets.updateStream(loop1Packets.handles[ls.supplyPktIdx], pShift, active, 0.78 + vLoad * 0.35);
        loop1Rings.updateRing(loop1Rings.handles[ls.supplyRingIdx], pShift, active);
        loop1Packets.updateStream(
          loop1Packets.handles[ls.returnPktIdx],
          (phase1 * 0.88 + ls.idx * 0.21) % 1,
          active,
          0.72 + vLoad * 0.28
        );
      }
      loop1Packets.flush();
      loop1Rings.flush();
    }

    // --- LOOP 2 (5GDHC Twin-Pipe Ring + Radial Spurs + Exergy Cascade / Winter Deficit) ---
    if (loop2Group.visible) {
      ringWarmConduit.setState(true, null, (0.75 + demandNorm * 0.65) * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_WARM], phase2, true, 0.85 + demandNorm * 0.30);
      loop2Rings.updateRing(loop2Rings.handles[0], phase2, true);

      ringCoolConduit.setState(true, null, (0.65 + demandNorm * 0.55) * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_COOL], phase2 * 0.95, true, 0.82 + demandNorm * 0.25);

      innerRingConduit.setState(true, null, (0.65 + demandNorm * 0.50) * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_INNER], (phase2 * 1.25) % 1, true, 0.85 + demandNorm * 0.25);

      radialSpursConduit.setState(true, null, (0.60 + demandNorm * 0.55) * pulse);
      for (let i = 0; i < 5; i++) {
        const zWeight = sim.vaults[i]?.zoneWeight ?? 0.2;
        const zScale = 0.75 + zWeight * 2.0;
        loop2Packets.updateStream(
          loop2Packets.handles[L2_SPUR0 + i],
          (phase2 * (0.9 + zWeight * 1.8) + i * 0.19) % 1,
          true,
          zScale * 0.85
        );
      }

      const isDeficit =
        sim.thermal.deficitMw > 0.08 ||
        sim.thermal.hthpThermalOutputMw > 0.05 ||
        sim.thermal.thermalStoreDischargeMw > 0.05;
      const riserActive = sim.thermal.capturedHeatMw > 0.2 || isDeficit;
      const riserPhase = (phase2 * 1.35) % 1;
      hubToRingConduit.setState(riserActive, isDeficit ? 0xff5a1f : 0xf59e0b, (isDeficit ? 1.45 : 1.05) * pulse);
      loop2Packets.setStreamColor(loop2Packets.handles[L2_RISER], isDeficit ? 0xff7849 : 0xfbbf24);
      loop2Packets.updateStream(loop2Packets.handles[L2_RISER], riserPhase, riserActive, isDeficit ? 1.2 : 1.0);
      loop2Rings.updateRing(loop2Rings.handles[1], riserPhase, riserActive);

      hthpBoostConduit.group.visible = isDeficit;
      if (isDeficit) {
        hthpBoostConduit.setState(true, null, 1.55 * pulse, 0.30);
      }
      const hthpPhase = (phase2 * 1.65) % 1;
      loop2Packets.updateStream(loop2Packets.handles[L2_HTHP], hthpPhase, isDeficit, 1.15);
      loop2Rings.updateRing(loop2Rings.handles[2], hthpPhase, isDeficit);

      const tcesOn = sim.thermal.tcesChargeMw > 0.05;
      tcesBranch.conduit.setState(tcesOn, null, 1.2 * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_TCES], (phase2 * 1.45) % 1, tcesOn);

      const ptesOn = sim.thermal.tiPtesChargeMw > 0.05;
      ptesBranch.conduit.setState(ptesOn, null, 1.2 * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_PTES], (phase2 * 1.35 + 0.25) % 1, ptesOn);

      const orcOn = sim.thermal.orcOutputKw > 10;
      orcBranch.conduit.setState(orcOn, null, 1.35 * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_ORC], (phase2 * 1.6 + 0.5) % 1, orcOn);

      const mofOn = (sim.thermal.commercialHeatSalesMw + sim.thermal.mofChillerMw) > 0.05;
      mofBranch.conduit.setState(mofOn, null, 1.25 * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_MOF], (phase2 * 1.5 + 0.7) % 1, mofOn);

      const dryOn = sim.thermal.dryCoolerRejectionMw > 0.05;
      dryCoolerBranch.conduit.setState(dryOn, null, 1.25 * pulse);
      loop2Packets.updateStream(loop2Packets.handles[L2_DRY], (phase2 * 1.4 + 0.1) % 1, dryOn);

      loop2Packets.flush();
      loop2Rings.flush();
    }

    // --- LOOP 3 (Rainwater Downspouts + Cistern -> Ultrapure -> Vaults + 3D Falling Rain) ---
    if (loop3Group.visible) {
      const isRaining = rainMult > 0.02;
      catchmentConduit.setState(isRaining, null, (0.75 + Math.min(0.8, rainMult * 0.45)) * pulse);
      const rainPktScale = 0.8 + Math.min(0.4, rainMult * 0.25);
      for (let i = 0; i < 4; i++) {
        loop3Packets.updateStream(loop3Packets.handles[i], (phase3 + i * 0.23) % 1, isRaining, rainPktScale);
      }

      const upActive = sim.electrical.activeVaultCount > 0;
      cisternToUpConduit.setState(upActive, null, 1.2 * pulse);
      loop3Packets.updateStream(loop3Packets.handles[L3_UP_FEED], (phase3 * 1.2) % 1, upActive);

      for (let i = 0; i < ultrapureConduits.length; i++) {
        const u = ultrapureConduits[i];
        const active = !sim.vaults[u.idx]?.faulted;
        u.conduit.setState(active, null, 0.9 * pulse);
        loop3Packets.updateStream(loop3Packets.handles[L3_VAULT0 + i], (phase3 + i * 0.19) % 1, active);
      }
      loop3Packets.flush();

      if (rainMult <= 0.01) {
        rainLines.visible = false;
        rainSplashes.visible = false;
      } else {
        rainLines.visible = true;
        rainMat.opacity = Math.min(0.40, 0.06 + rainMult * 0.15);
        const activeDrops = Math.min(rainCount, Math.floor(rainCount * Math.min(1, 0.12 + rainMult * 0.46)));
        rainGeom.setDrawRange(0, activeDrops * 2);

        const windX = 0.28 + rainMult * 0.16;
        const windZ = 0.14;
        const fallMult = dt * (0.75 + rainMult * 0.32);

        for (let i = 0; i < activeDrops; i++) {
          const drop = rainSeeds[i];
          drop.y -= fallMult * drop.speed;
          if (drop.y < 0) drop.y += 56;
          const idx = i * 6;
          rainPositions[idx] = drop.x;
          rainPositions[idx + 1] = drop.y;
          rainPositions[idx + 2] = drop.z;
          rainPositions[idx + 3] = drop.x + windX * drop.len;
          rainPositions[idx + 4] = drop.y - drop.len;
          rainPositions[idx + 5] = drop.z + windZ * drop.len;
        }
        rainGeom.attributes.position.needsUpdate = true;

        rainSplashes.visible = rainMult > 0.12;
        splashMat.opacity = Math.min(0.30, rainMult * 0.14) * (0.75 + 0.25 * Math.sin(internalElapsed * 9));
      }
    }

    // --- OPTICAL RING: 3.0 km NANF HCF + SDN Bidirectional Shifting + Fault Bypass ---
    if (opticalGroup.visible) {
      const faultPulse = 0.65 + 0.95 * Math.abs(Math.sin(internalElapsed * 6.5));
      const fwdColor = sdnOn ? 0x22d3ee : 0x60a5fa;
      const revColor = sdnOn ? 0xe879f9 : 0xa855f7;

      for (let i = 0; i < ringHops.length; i++) {
        const hop = ringHops[i];
        const vFrom = sim.vaults[hop.fromIdx];
        const vTo = sim.vaults[hop.toIdx];
        const linkFaulted = Boolean(vFrom?.faulted || vTo?.faulted);
        const fwdH = optPackets.handles[hop.fwdIdx];
        const revH = optPackets.handles[hop.revIdx];

        if (linkFaulted) {
          hop.conduit.setState(true, 0xef4444, faultPulse * 1.45, 0.34);
          optPackets.updateStream(fwdH, phaseOpt, false);
          optPackets.updateStream(revH, phaseOpt, false);
        } else {
          const loadDiff = sdnOn ? vTo.itMw - vFrom.itMw : 0;
          const fwdBias = sdnOn ? Math.max(0.65, 1.0 + loadDiff * 0.8) : 0.9;
          const revBias = sdnOn ? Math.max(0.65, 1.0 - loadDiff * 0.8) : 0.85;

          hop.conduit.setState(true, sdnOn ? 0xa855f7 : 0x38bdf8, (sdnOn ? 1.32 : 0.88) * pulse, 0.28);
          optPackets.setStreamColor(fwdH, fwdColor);
          optPackets.setStreamColor(revH, revColor);
          optPackets.updateStream(fwdH, (phaseOpt * (sdnOn ? 1.15 : 0.85) + hop.fromIdx * 0.13) % 1, true, fwdBias);
          optPackets.updateStream(revH, (phaseOpt * (sdnOn ? 1.05 : 0.80) + hop.toIdx * 0.17) % 1, true, revBias);
        }
      }

      const bpPhase = (phaseOpt * 1.45) % 1;
      for (let i = 0; i < bypassLinks.length; i++) {
        const bp = bypassLinks[i];
        const showBypass =
          Boolean(sim.vaults[bp.faultIdx]?.faulted) &&
          !sim.vaults[bp.prevIdx]?.faulted &&
          !sim.vaults[bp.nextIdx]?.faulted;
        bp.conduit.group.visible = showBypass;
        if (showBypass) {
          bp.conduit.setState(true, 0x22d3ee, 1.6 * pulse, 0.34);
        }
        optPackets.updateStream(optPackets.handles[bp.pktIdx], bpPhase, showBypass, 1.1);
        optRings.updateRing(optRings.handles[bp.ringIdx], bpPhase, showBypass);
      }

      optPackets.flush();
      optRings.flush();
    }
  }

  return {
    groups: {
      loop1: loop1Group,
      loop2: loop2Group,
      loop3: loop3Group,
      optical: opticalGroup,
    },
    update,
  };
}
