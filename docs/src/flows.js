import * as THREE from 'three';
import { WORLD_LAYOUT } from './scene.js';

function createGlowSpriteTexture() {
  const canvas = document.createElement('canvas');
  canvas.width = 64;
  canvas.height = 64;
  const ctx = canvas.getContext('2d');
  const grad = ctx.createRadialGradient(32, 32, 2, 32, 32, 30);
  grad.addColorStop(0, 'rgba(255,255,255,1)');
  grad.addColorStop(0.35, 'rgba(255,255,255,0.85)');
  grad.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 64, 64);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  return tex;
}

function makeParticleStream(curve, count, colorHex, size = 1.05, spriteTex) {
  const positions = new Float32Array(count * 3);
  const geom = new THREE.BufferGeometry();
  geom.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  const mat = new THREE.PointsMaterial({
    color: colorHex,
    size,
    map: spriteTex,
    transparent: true,
    opacity: 0.92,
    depthWrite: false,
    blending: THREE.AdditiveBlending,
  });
  const points = new THREE.Points(geom, mat);
  const offsets = Float32Array.from({ length: count }, (_, i) => i / count);

  return {
    points,
    mat,
    update(phase, active = true) {
      points.visible = active;
      if (!active) return;
      for (let i = 0; i < count; i++) {
        const t = (offsets[i] + phase) % 1;
        const p = curve.getPointAt(t < 0 ? t + 1 : t);
        positions[i * 3] = p.x;
        positions[i * 3 + 1] = p.y;
        positions[i * 3 + 2] = p.z;
      }
      geom.attributes.position.needsUpdate = true;
    },
  };
}

function makeConduitPipe(curve, radius, colorHex, opacity = 0.45) {
  const geom = new THREE.TubeGeometry(curve, 36, radius, 8, false);
  const mat = new THREE.MeshBasicMaterial({
    color: colorHex,
    transparent: true,
    opacity,
  });
  return new THREE.Mesh(geom, mat);
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

  // --- LOOP 1: 75°C Primary Microfluidic Cooling (Vaults <-> Thermal Store / Hub) ---
  const loop1Streams = WORLD_LAYOUT.VAULT_POSITIONS.map((vp, idx) => {
    const curve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(vp.x, vp.y + 0.4, vp.z),
      new THREE.Vector3(vp.x * 0.62, hubY + 1.4, vp.z * 0.62),
      new THREE.Vector3(-4.2, hubY + 1.8, -3.5),
    ]);
    const pipe = makeConduitPipe(curve, 0.22, 0xff5a1f, 0.36);
    loop1Group.add(pipe);
    const stream = makeParticleStream(curve, 14, 0xff6b2c, 1.35, spriteTex);
    loop1Group.add(stream.points);
    return { idx, pipe, stream, phase: idx * 0.2 };
  });

  // --- LOOP 2: 5GDHC 30 km Twin-Pipe Ring (15–25°C / 45°C) + Exergy Cascade ---
  const ringPts = [];
  for (let i = 0; i <= 24; i++) {
    const a = (i / 24) * Math.PI * 2;
    ringPts.push(new THREE.Vector3(Math.cos(a) * 38, -2.6, Math.sin(a) * 38));
  }
  const ringCurve = new THREE.CatmullRomCurve3(ringPts, true);
  loop2Group.add(makeConduitPipe(ringCurve, 0.26, 0x14b8a6, 0.32));
  const ringStream = makeParticleStream(ringCurve, 44, 0x2dd4bf, 1.45, spriteTex);
  loop2Group.add(ringStream.points);

  // Riser feed from central hub up to 5GDHC surface ring
  const hubToRingCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(-4.2, hubY + 1.5, -3.5),
    new THREE.Vector3(0, -8.5, 0),
    new THREE.Vector3(0, -2.6, 38),
  ]);
  loop2Group.add(makeConduitPipe(hubToRingCurve, 0.22, 0xf59e0b, 0.35));
  const hubToRingStream = makeParticleStream(hubToRingCurve, 18, 0xfbbf24, 1.35, spriteTex);
  loop2Group.add(hubToRingStream.points);

  // Exergy cascade internal link (Thermal Store -> TCES/PTES -> Micro-ORC)
  const cascadeCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(-4.2, hubY + 0.4, -3.5),
    new THREE.Vector3(0.2, hubY + 0.5, -6.4),
    new THREE.Vector3(1.2, hubY + 0.2, 4.8),
    new THREE.Vector3(6.2, hubY + 0.2, 4.4),
  ]);
  loop2Group.add(makeConduitPipe(cascadeCurve, 0.16, 0x10b981, 0.28));
  const cascadeStream = makeParticleStream(cascadeCurve, 18, 0x34d399, 1.25, spriteTex);
  loop2Group.add(cascadeStream.points);

  // --- LOOP 3: Rainwater Catchment -> 10,000 m³ Cistern -> Ultrapure -> Vaults + Falling Rain ---
  const cisternFeedCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(18, -0.4, -14),
    new THREE.Vector3(10, -7.5, -7),
    new THREE.Vector3(4.8, hubY + 1.5, -3.2),
  ]);
  loop3Group.add(makeConduitPipe(cisternFeedCurve, 0.2, 0x0ea5e9, 0.34));
  const cisternFeedStream = makeParticleStream(cisternFeedCurve, 16, 0x38bdf8, 1.25, spriteTex);
  loop3Group.add(cisternFeedStream.points);

  const ultrapureLoopStreams = WORLD_LAYOUT.VAULT_POSITIONS.map((vp, idx) => {
    const c = new THREE.CatmullRomCurve3([
      new THREE.Vector3(4.6, hubY - 0.6, -7.8),
      new THREE.Vector3(vp.x * 0.55, hubY - 0.8, vp.z * 0.55),
      new THREE.Vector3(vp.x, vp.y - 0.4, vp.z),
    ]);
    loop3Group.add(makeConduitPipe(c, 0.13, 0x38bdf8, 0.24));
    const s = makeParticleStream(c, 10, 0x7dd3fc, 1.0, spriteTex);
    loop3Group.add(s.points);
    return s;
  });

  // 3D Falling Rain Curtain (scaled by rainfallMultiplier)
  const rainCount = 520;
  const rainPositions = new Float32Array(rainCount * 2 * 3);
  const rainSeeds = Array.from({ length: rainCount }, (_, i) => {
    const a = ((i * 2.39996) % (Math.PI * 2));
    const r = Math.sqrt((i + 0.5) / rainCount) * 70;
    return {
      x: Math.cos(a) * r,
      y: (i * 7.13) % 52,
      z: Math.sin(a) * r,
      speed: 26 + (i % 7) * 2.5,
    };
  });
  const rainGeom = new THREE.BufferGeometry();
  rainGeom.setAttribute('position', new THREE.BufferAttribute(rainPositions, 3));
  const rainMat = new THREE.LineBasicMaterial({
    color: 0x7dd3fc,
    transparent: true,
    opacity: 0.42,
  });
  const rainLines = new THREE.LineSegments(rainGeom, rainMat);
  loop3Group.add(rainLines);

  // --- OPTICAL RING: 3.0 km NANF HCF + CPO Ring interconnecting all 5 vaults ---
  const ringHops = WORLD_LAYOUT.VAULT_POSITIONS.map((vp, i) => {
    const nextIdx = (i + 1) % 5;
    const nextVp = WORLD_LAYOUT.VAULT_POSITIONS[nextIdx];
    const midX = (vp.x + nextVp.x) * 0.54;
    const midZ = (vp.z + nextVp.z) * 0.54;
    const curve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(vp.x, vp.y + 0.25, vp.z),
      new THREE.Vector3(midX, vp.y + 0.4, midZ),
      new THREE.Vector3(nextVp.x, nextVp.y + 0.25, nextVp.z),
    ]);
    const pipe = makeConduitPipe(curve, 0.16, 0xa855f7, 0.55);
    opticalGroup.add(pipe);
    const stream = makeParticleStream(curve, 14, 0xd946ef, 1.3, spriteTex);
    opticalGroup.add(stream.points);
    return { fromIdx: i, toIdx: nextIdx, pipe, stream };
  });

  let phase1 = 0;
  let phase2 = 0;
  let phase3 = 0;
  let phaseOpt = 0;

  function update(sim, dt) {
    // Loop 1 (75°C primary cooling)
    const flowNorm = sim.thermal.primaryGrossFlowLmin / 4635.3;
    phase1 = (phase1 + dt * (0.28 + flowNorm * 0.45)) % 1;
    for (const ls of loop1Streams) {
      const vault = sim.vaults[ls.idx];
      const active = !vault.faulted && vault.itMw > 0.05;
      ls.pipe.material.opacity = active ? 0.38 : 0.08;
      ls.stream.update((phase1 + ls.idx * 0.15) % 1, active);
    }

    // Loop 2 (5GDHC ring & Exergy Cascade)
    const demandNorm = sim.thermal.districtDemandMw / 9.52;
    phase2 = (phase2 + dt * (0.14 + demandNorm * 0.26)) % 1;
    ringStream.update(phase2, true);
    hubToRingStream.update(phase2, sim.thermal.capturedHeatMw > 0.2);
    const cascadeActive = sim.thermal.surplusMw > 0.35;
    cascadeStream.update((phase2 * 1.4) % 1, cascadeActive);

    // Loop 3 (Rainwater & Ultrapure water)
    const rainMult = sim.hydrology.rainfallMultiplier;
    phase3 = (phase3 + dt * 0.32) % 1;
    cisternFeedStream.update(phase3, rainMult > 0.02);
    ultrapureLoopStreams.forEach((s, i) => {
      const active = !sim.vaults[i]?.faulted;
      s.update((phase3 + i * 0.18) % 1, active);
    });

    if (rainMult <= 0.01) {
      rainLines.visible = false;
    } else {
      rainLines.visible = true;
      rainMat.opacity = Math.min(0.65, 0.16 + rainMult * 0.28);
      const activeDrops = Math.min(rainCount, Math.floor(rainCount * Math.min(1, rainMult * 0.75)));
      rainGeom.setDrawRange(0, activeDrops * 2);
      for (let i = 0; i < activeDrops; i++) {
        const drop = rainSeeds[i];
        drop.y -= dt * drop.speed * (0.7 + rainMult * 0.35);
        if (drop.y < 0) drop.y += 52;
        const idx = i * 6;
        rainPositions[idx] = drop.x;
        rainPositions[idx + 1] = drop.y;
        rainPositions[idx + 2] = drop.z;
        rainPositions[idx + 3] = drop.x;
        rainPositions[idx + 4] = drop.y - 1.4;
        rainPositions[idx + 5] = drop.z;
      }
      rainGeom.attributes.position.needsUpdate = true;
    }

    // Optical Ring + SDN Rerouting
    const sdnBoost = sim.optical.sdnEnabled ? 0.55 : 0.25;
    phaseOpt = (phaseOpt + dt * sdnBoost) % 1;
    for (const hop of ringHops) {
      const fromFault = sim.vaults[hop.fromIdx]?.faulted;
      const toFault = sim.vaults[hop.toIdx]?.faulted;
      const linkFaulted = fromFault || toFault;
      if (linkFaulted) {
        hop.pipe.material.color.setHex(0xef4444);
        hop.pipe.material.opacity = 0.45;
        hop.stream.update(phaseOpt, false);
      } else {
        hop.pipe.material.color.setHex(sim.optical.sdnEnabled ? 0xa855f7 : 0x38bdf8);
        hop.pipe.material.opacity = 0.58;
        hop.stream.mat.color.setHex(sim.optical.sdnEnabled ? 0x22d3ee : 0xa855f7);
        hop.stream.update(phaseOpt, true);
      }
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
