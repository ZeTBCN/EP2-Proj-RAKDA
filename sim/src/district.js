import * as THREE from 'three';
import { WORLD_LAYOUT } from './scene.js';

function tagInspectable(obj, key) {
  obj.userData.inspectKey = key;
  return obj;
}

function isNearVaultPlaza(x, z, minClear = 8.4) {
  const vaults = WORLD_LAYOUT.VAULT_POSITIONS;
  const minSq = minClear * minClear;
  for (let i = 0; i < vaults.length; i++) {
    const dx = x - vaults[i].x;
    const dz = z - vaults[i].z;
    if (dx * dx + dz * dz < minSq) return true;
  }
  return false;
}

function isNearRoadOrPark(r, angle) {
  if (Math.abs(r - 16) < 2.95 || Math.abs(r - 38) < 3.15) return true;
  const vaults = WORLD_LAYOUT.VAULT_POSITIONS;
  for (let i = 0; i < vaults.length; i++) {
    let diff = Math.abs(angle - vaults[i].angle);
    if (diff > Math.PI) diff = Math.PI * 2 - diff;
    if (diff < 0.115) return true;
  }
  const normA = ((angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
  if ((normA > 0.40 && normA < 0.74) || (normA > 3.53 && normA < 3.87)) return true;
  return false;
}

function seededRand(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

function configureTexture(canvas, { srgb = true, repeatX = 1, repeatY = 1, anisotropy = 4 } = {}) {
  const tex = new THREE.CanvasTexture(canvas);
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  tex.repeat.set(repeatX, repeatY);
  tex.anisotropy = anisotropy;
  tex.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
  tex.needsUpdate = true;
  return tex;
}

// Merge multiple static local-space primitives into a single BufferGeometry to minimize draw calls
function mergeStaticGeometries(entries) {
  const parts = [];
  let totalVerts = 0;
  let hasUV = true;
  const mat = new THREE.Matrix4();
  const quat = new THREE.Quaternion();
  const euler = new THREE.Euler();
  const pos = new THREE.Vector3();
  const scl = new THREE.Vector3();

  for (let i = 0; i < entries.length; i++) {
    const e = entries[i];
    const g = e.geom.index ? e.geom.toNonIndexed() : e.geom.clone();
    if (e.pos || e.rot || e.scale) {
      pos.set(e.pos ? e.pos[0] : 0, e.pos ? e.pos[1] : 0, e.pos ? e.pos[2] : 0);
      if (e.rot) {
        euler.set(e.rot[0], e.rot[1], e.rot[2]);
        quat.setFromEuler(euler);
      } else {
        quat.identity();
      }
      scl.set(e.scale ? e.scale[0] : 1, e.scale ? e.scale[1] : 1, e.scale ? e.scale[2] : 1);
      mat.compose(pos, quat, scl);
      g.applyMatrix4(mat);
    }
    if (!g.attributes.normal) g.computeVertexNormals();
    if (!g.attributes.uv) hasUV = false;
    totalVerts += g.attributes.position.count;
    parts.push(g);
    if (e.disposeSource !== false) e.geom.dispose();
  }

  const positions = new Float32Array(totalVerts * 3);
  const normals = new Float32Array(totalVerts * 3);
  const uvs = hasUV ? new Float32Array(totalVerts * 2) : null;

  let offset = 0;
  for (let i = 0; i < parts.length; i++) {
    const g = parts[i];
    const count = g.attributes.position.count;
    positions.set(g.attributes.position.array, offset * 3);
    normals.set(g.attributes.normal.array, offset * 3);
    if (uvs) uvs.set(g.attributes.uv.array, offset * 2);
    offset += count;
    g.dispose();
  }

  const merged = new THREE.BufferGeometry();
  merged.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  merged.setAttribute('normal', new THREE.BufferAttribute(normals, 3));
  if (uvs) merged.setAttribute('uv', new THREE.BufferAttribute(uvs, 2));
  merged.computeBoundingBox();
  merged.computeBoundingSphere();
  return merged;
}

// --- PROCEDURAL ARCHITECTURAL CANVAS TEXTURES (all <= 256x256 for snappy startup) ---

function createFacadeTextureSet({
  cols = 6,
  rows = 6,
  wallBase = '#d6cfc4',
  spandrel = '#8b6f54',
  finColor = '#6b513a',
  glassTop = '#2a4365',
  glassBot = '#15263d',
  litChance = 0.62,
  seed = 101,
  groundRetail = true,
  balconyLines = true,
} = {}) {
  const w = 256;
  const h = 256;
  const rng = seededRand(seed);

  const albedoCanvas = document.createElement('canvas');
  albedoCanvas.width = w;
  albedoCanvas.height = h;
  const actx = albedoCanvas.getContext('2d');

  const emissiveCanvas = document.createElement('canvas');
  emissiveCanvas.width = w;
  emissiveCanvas.height = h;
  const ectx = emissiveCanvas.getContext('2d');

  const roughCanvas = document.createElement('canvas');
  roughCanvas.width = w;
  roughCanvas.height = h;
  const rctx = roughCanvas.getContext('2d');

  actx.fillStyle = wallBase;
  actx.fillRect(0, 0, w, h);

  ectx.fillStyle = '#000000';
  ectx.fillRect(0, 0, w, h);

  rctx.fillStyle = '#cdcdcd';
  rctx.fillRect(0, 0, w, h);

  for (let i = 0; i < 64; i++) {
    const gx = Math.floor(rng() * w);
    actx.fillStyle = rng() > 0.5 ? 'rgba(255,255,255,0.045)' : 'rgba(0,0,0,0.045)';
    actx.fillRect(gx, 0, 2, h);
  }

  const cellW = w / cols;
  const cellH = h / rows;
  const warmPalette = ['#ffcf86', '#ffb75e', '#ffe4b5', '#ffd180', '#ffaa55'];

  for (let r = 0; r < rows; r++) {
    const y0 = r * cellH;
    actx.fillStyle = spandrel;
    actx.fillRect(0, y0, w, Math.max(3, cellH * 0.22));
    if (balconyLines) {
      actx.fillStyle = 'rgba(255,255,255,0.22)';
      actx.fillRect(0, y0 + cellH * 0.2, w, 1.5);
    }

    const isGround = groundRetail && r === rows - 1;
    for (let c = 0; c < cols; c++) {
      const x0 = c * cellW;
      const padX = cellW * (isGround ? 0.12 : 0.16);
      const padTop = cellH * (isGround ? 0.18 : 0.26);
      const padBot = cellH * (isGround ? 0.06 : 0.14);
      const winX = x0 + padX;
      const winY = y0 + padTop;
      const winW = cellW - padX * 2;
      const winH = cellH - padTop - padBot;

      actx.fillStyle = '#1e293b';
      actx.fillRect(winX - 1.5, winY - 1.5, winW + 3, winH + 3);

      const gGrad = actx.createLinearGradient(0, winY, 0, winY + winH);
      gGrad.addColorStop(0, glassTop);
      gGrad.addColorStop(0.55, glassBot);
      gGrad.addColorStop(1, '#1e3a5f');
      actx.fillStyle = gGrad;
      actx.fillRect(winX, winY, winW, winH);

      rctx.fillStyle = '#202020';
      rctx.fillRect(winX, winY, winW, winH);

      actx.fillStyle = '#1f2937';
      actx.fillRect(winX + winW * 0.5 - 0.75, winY, 1.5, winH);

      actx.fillStyle = finColor;
      actx.fillRect(x0, y0 + cellH * 0.22, Math.max(2, cellW * 0.08), cellH * 0.78);

      const litRoll = rng();
      if (litRoll < litChance || (isGround && litRoll < 0.8)) {
        const tone = warmPalette[Math.floor(rng() * warmPalette.length)];
        const wGrad = ectx.createLinearGradient(0, winY, 0, winY + winH);
        wGrad.addColorStop(0, tone);
        wGrad.addColorStop(1, '#c97a34');
        ectx.fillStyle = wGrad;
        ectx.fillRect(winX + 1, winY + 1, winW - 2, winH - 2);

        ectx.fillStyle = '#1a120b';
        ectx.fillRect(winX + winW * 0.5 - 0.8, winY, 1.6, winH);
      }
    }
  }

  return {
    map: configureTexture(albedoCanvas, { srgb: true }),
    emissiveMap: configureTexture(emissiveCanvas, { srgb: true }),
    roughnessMap: configureTexture(roughCanvas, { srgb: false }),
  };
}

function createRoofMembraneTexture(baseHex = '#94a3b8', seed = 202) {
  const c = document.createElement('canvas');
  c.width = 128;
  c.height = 128;
  const ctx = c.getContext('2d');
  const rng = seededRand(seed);
  ctx.fillStyle = baseHex;
  ctx.fillRect(0, 0, 128, 128);
  ctx.strokeStyle = 'rgba(15,23,42,0.16)';
  ctx.lineWidth = 1;
  for (let i = 0; i <= 128; i += 32) {
    ctx.beginPath();
    ctx.moveTo(i, 0);
    ctx.lineTo(i, 128);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(0, i);
    ctx.lineTo(128, i);
    ctx.stroke();
  }
  for (let i = 0; i < 320; i++) {
    ctx.fillStyle = rng() > 0.5 ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)';
    ctx.fillRect(rng() * 128, rng() * 128, 2, 2);
  }
  return configureTexture(c, { srgb: true });
}

function createSlateRoofTexture() {
  const c = document.createElement('canvas');
  c.width = 128;
  c.height = 128;
  const ctx = c.getContext('2d');
  const rng = seededRand(303);
  ctx.fillStyle = '#475569';
  ctx.fillRect(0, 0, 128, 128);
  const rows = 8;
  const rowH = 128 / rows;
  for (let r = 0; r < rows; r++) {
    const shade = 64 + Math.floor(rng() * 20);
    ctx.fillStyle = `rgb(${shade}, ${shade + 12}, ${shade + 24})`;
    ctx.fillRect(0, r * rowH, 128, rowH - 1);
    ctx.fillStyle = 'rgba(15,23,42,0.35)';
    ctx.fillRect(0, (r + 1) * rowH - 1.5, 128, 1.5);
  }
  ctx.strokeStyle = 'rgba(203,213,225,0.18)';
  ctx.lineWidth = 1.5;
  for (let x = 16; x < 128; x += 16) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, 128);
    ctx.stroke();
  }
  return configureTexture(c, { srgb: true });
}

function createSolarPVTexture() {
  const w = 256;
  const h = 256;
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');

  ctx.fillStyle = '#94a3b8';
  ctx.fillRect(0, 0, w, h);
  ctx.fillStyle = '#091533';
  ctx.fillRect(5, 5, w - 10, h - 10);

  const cols = 6;
  const rows = 4;
  const pad = 6;
  const cw = (w - pad * 2) / cols;
  const ch = (h - pad * 2) / rows;

  for (let r = 0; r < rows; r++) {
    for (let col = 0; col < cols; col++) {
      const x = pad + col * cw + 1.5;
      const y = pad + r * ch + 1.5;
      const cellW = cw - 3;
      const cellH = ch - 3;

      const grad = ctx.createLinearGradient(x, y, x + cellW, y + cellH);
      grad.addColorStop(0, '#0f2557');
      grad.addColorStop(0.45, '#173882');
      grad.addColorStop(1, '#0b1b42');
      ctx.fillStyle = grad;
      ctx.fillRect(x, y, cellW, cellH);

      ctx.strokeStyle = 'rgba(186, 230, 253, 0.22)';
      ctx.lineWidth = 0.7;
      for (let fy = y + 6; fy < y + cellH - 3; fy += 6) {
        ctx.beginPath();
        ctx.moveTo(x, fy);
        ctx.lineTo(x + cellW, fy);
        ctx.stroke();
      }

      ctx.fillStyle = 'rgba(226, 232, 240, 0.82)';
      ctx.fillRect(x + cellW * 0.30, y, 1.4, cellH);
      ctx.fillRect(x + cellW * 0.70, y, 1.4, cellH);

      ctx.fillStyle = '#cbd5e1';
      ctx.fillRect(x - 1, y - 1, 2.5, 2.5);
    }
  }

  return configureTexture(c, { srgb: true });
}

function createRoadTexture({ withGuideway = true } = {}) {
  const w = 256;
  const h = 256;
  const rng = seededRand(404);
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');

  ctx.fillStyle = '#1b2432';
  ctx.fillRect(0, 0, w, h);

  for (let i = 0; i < 1400; i++) {
    const shade = 20 + Math.floor(rng() * 28);
    ctx.fillStyle = `rgba(${shade}, ${shade + 8}, ${shade + 18}, 0.45)`;
    ctx.fillRect(rng() * w, rng() * h, 2, 2);
  }

  ctx.fillStyle = '#475569';
  ctx.fillRect(0, 0, w, 16);
  ctx.fillRect(0, h - 16, w, 16);
  ctx.fillStyle = '#64748b';
  ctx.fillRect(0, 14, w, 3);
  ctx.fillRect(0, h - 17, w, 3);

  ctx.fillStyle = 'rgba(10, 15, 25, 0.24)';
  ctx.fillRect(0, 56, w, 46);
  ctx.fillRect(0, h - 102, w, 46);

  ctx.fillStyle = 'rgba(226, 232, 240, 0.72)';
  for (let x = 12; x < w; x += 64) {
    ctx.fillRect(x, h / 2 - 2.5, 34, 5);
  }

  if (withGuideway) {
    ctx.fillStyle = 'rgba(34, 211, 238, 0.55)';
    ctx.fillRect(0, 32, w, 3);
    ctx.fillRect(0, h - 35, w, 3);
    ctx.fillStyle = 'rgba(56, 189, 248, 0.42)';
    for (let x = 8; x < w; x += 32) {
      ctx.fillRect(x, 76, 10, 2);
      ctx.fillRect(x, h - 78, 10, 2);
    }
  }

  return configureTexture(c, { srgb: true });
}

function createPlazaStoneTexture() {
  const w = 256;
  const h = 256;
  const rng = seededRand(505);
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');

  ctx.fillStyle = '#475569';
  ctx.fillRect(0, 0, w, h);

  const tiles = 8;
  const step = w / tiles;
  for (let r = 0; r < tiles; r++) {
    const offset = (r % 2) * (step * 0.5);
    for (let col = -1; col <= tiles; col++) {
      const v = 78 + Math.floor(rng() * 24);
      ctx.fillStyle = `rgb(${v + 4}, ${v + 10}, ${v + 18})`;
      ctx.fillRect(col * step + offset + 1.5, r * step + 1.5, step - 3, step - 3);
    }
  }

  ctx.strokeStyle = 'rgba(226,232,240,0.16)';
  ctx.lineWidth = 2;
  ctx.strokeRect(4, 4, w - 8, h - 8);

  return configureTexture(c, { srgb: true, repeatX: 3, repeatY: 3 });
}

function createBessContainerTexture() {
  const w = 256;
  const h = 128;
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');

  ctx.fillStyle = '#253347';
  ctx.fillRect(0, 0, w, h);

  for (let x = 0; x < w; x += 8) {
    ctx.fillStyle = 'rgba(255,255,255,0.06)';
    ctx.fillRect(x, 12, 3, h - 24);
    ctx.fillStyle = 'rgba(0,0,0,0.22)';
    ctx.fillRect(x + 3, 12, 2, h - 24);
  }

  ctx.fillStyle = '#334155';
  ctx.fillRect(0, 0, w, 14);
  ctx.fillRect(0, h - 14, w, 14);

  for (const gx of [24, 176]) {
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(gx, 26, 56, 62);
    ctx.fillStyle = '#64748b';
    for (let ly = 30; ly < 84; ly += 6) {
      ctx.fillRect(gx + 3, ly, 50, 2.5);
    }
  }

  ctx.fillStyle = '#f59e0b';
  ctx.fillRect(0, h - 16, w, 3);

  return configureTexture(c, { srgb: true });
}

function createPolarRingGeometry(innerR, outerR, thetaSegments, repeatU) {
  const geom = new THREE.RingGeometry(innerR, outerR, thetaSegments, 1);
  const uv = geom.attributes.uv;
  for (let j = 0; j <= 1; j++) {
    for (let i = 0; i <= thetaSegments; i++) {
      const idx = j * (thetaSegments + 1) + i;
      uv.setXY(idx, (i / thetaSegments) * repeatU, j);
    }
  }
  uv.needsUpdate = true;
  return geom;
}

function createGabledRoofGeometry(w, d, ridgeH) {
  const halfW = w * 0.56;
  const shape = new THREE.Shape();
  shape.moveTo(-halfW, 0);
  shape.lineTo(halfW, 0);
  shape.lineTo(0, ridgeH);
  shape.closePath();

  const extrudeLen = d * 1.12;
  const geom = new THREE.ExtrudeGeometry(shape, {
    depth: extrudeLen,
    bevelEnabled: false,
    steps: 1,
  });
  geom.translate(0, 0, -extrudeLen / 2);
  geom.computeVertexNormals();
  return geom;
}

// Module-level reusable scratch objects (ZERO per-frame allocations in update())
const SCRATCH_DUMMY = new THREE.Object3D();
const SCRATCH_COLOR = new THREE.Color();
const HEAT_LOW = new THREE.Color(0x0ea5e9);
const HEAT_MID = new THREE.Color(0xf59e0b);
const HEAT_HIGH = new THREE.Color(0xff3b14);
const SCRATCH_HEAT = new THREE.Color();
const SCRATCH_ZONE_HEAT = new THREE.Color();
const ZONE_WEIGHTS = [0.24, 0.22, 0.20, 0.18, 0.16];

export function createDistrict(scene) {
  const group = new THREE.Group();
  group.name = 'surface_layer';
  scene.add(group);

  let heatmapVisible = false;

  // --- SHARED PROCEDURAL TEXTURES ---
  const midRiseTex = createFacadeTextureSet({
    cols: 6,
    rows: 7,
    wallBase: '#d9d2c5',
    spandrel: '#8c6d4e',
    finColor: '#6d5239',
    glassTop: '#2c4a70',
    glassBot: '#14243b',
    litChance: 0.64,
    seed: 11,
  });
  const lowRiseTex = createFacadeTextureSet({
    cols: 5,
    rows: 5,
    wallBase: '#cfd6dc',
    spandrel: '#7c6552',
    finColor: '#5c4a3a',
    glassTop: '#284566',
    glassBot: '#15253a',
    litChance: 0.60,
    seed: 22,
  });
  const townTex = createFacadeTextureSet({
    cols: 4,
    rows: 3,
    wallBase: '#c7b8a8',
    spandrel: '#7d5a47',
    finColor: '#5e4437',
    glassTop: '#2a405c',
    glassBot: '#162538',
    litChance: 0.58,
    seed: 33,
    groundRetail: false,
    balconyLines: false,
  });
  const detachedTex = createFacadeTextureSet({
    cols: 3,
    rows: 2,
    wallBase: '#ddd8ce',
    spandrel: '#96775a',
    finColor: '#755c44',
    glassTop: '#2d4b6e',
    glassBot: '#182b42',
    litChance: 0.56,
    seed: 44,
    groundRetail: false,
    balconyLines: false,
  });
  const civicTex = createFacadeTextureSet({
    cols: 7,
    rows: 4,
    wallBase: '#cbd5e1',
    spandrel: '#8b6e52',
    finColor: '#475569',
    glassTop: '#38bdf8',
    glassBot: '#1e3a5f',
    litChance: 0.72,
    seed: 55,
    groundRetail: true,
    balconyLines: true,
  });

  const roofMembraneTex = createRoofMembraneTexture('#9ca8b8', 71);
  const slateRoofTex = createSlateRoofTexture();
  const pvTex = createSolarPVTexture();
  const ringRoadTex = createRoadTexture({ withGuideway: true });
  const spokeRoadTex = createRoadTexture({ withGuideway: false });
  spokeRoadTex.repeat.set(10, 1);
  const plazaStoneTex = createPlazaStoneTexture();
  const bessTex = createBessContainerTexture();

  const vaults = WORLD_LAYOUT.VAULT_POSITIONS;
  const vaultCount = vaults.length;

  // --- 1. CONCENTRIC & RADIAL TRANSIT AVENUES + MERGED AUTONOMOUS GUIDEWAYS ---
  const ringRoadMat = new THREE.MeshStandardMaterial({
    map: ringRoadTex,
    roughness: 0.78,
    metalness: 0.08,
  });
  const spokeRoadMat = new THREE.MeshStandardMaterial({
    map: spokeRoadTex,
    roughness: 0.80,
    metalness: 0.08,
  });

  const ringRoadsGeom = mergeStaticGeometries([
    { geom: createPolarRingGeometry(14.3, 17.7, 96, 14), rot: [-Math.PI / 2, 0, 0] },
    { geom: createPolarRingGeometry(36.2, 39.8, 128, 30), rot: [-Math.PI / 2, 0, 0] },
  ]);
  const ringRoadsMesh = new THREE.Mesh(ringRoadsGeom, ringRoadMat);
  ringRoadsMesh.position.y = 0.052;
  ringRoadsMesh.receiveShadow = true;
  group.add(ringRoadsMesh);

  // Merge all 4 glowing cyan autonomous guideway rails into 1 draw call
  const guidewayMat = new THREE.MeshStandardMaterial({
    color: 0x22d3ee,
    emissive: 0x06b6d4,
    emissiveIntensity: 0.95,
    roughness: 0.25,
    metalness: 0.4,
    transparent: true,
    opacity: 0.82,
  });
  const guidewayEntries = [];
  for (const ringR of [16, 38]) {
    const laneOffset = ringR === 16 ? 0.85 : 0.95;
    for (const delta of [-laneOffset, laneOffset]) {
      guidewayEntries.push({
        geom: new THREE.RingGeometry(ringR + delta - 0.06, ringR + delta + 0.06, 112),
        rot: [-Math.PI / 2, 0, 0],
      });
    }
  }
  const guidewayMesh = new THREE.Mesh(mergeStaticGeometries(guidewayEntries), guidewayMat);
  guidewayMesh.position.y = 0.062;
  group.add(guidewayMesh);

  // 5 Radial Boulevards batched into 1 InstancedMesh
  const spokeLen = WORLD_LAYOUT.DISTRICT_RADIUS - 12.6;
  const midR = 12.6 + spokeLen * 0.5;
  const spokeMesh = new THREE.InstancedMesh(
    new THREE.PlaneGeometry(spokeLen, 2.9),
    spokeRoadMat,
    vaultCount
  );
  spokeMesh.receiveShadow = true;
  for (let i = 0; i < vaultCount; i++) {
    const vp = vaults[i];
    SCRATCH_DUMMY.position.set(Math.cos(vp.angle) * midR, 0.042, Math.sin(vp.angle) * midR);
    SCRATCH_DUMMY.rotation.set(-Math.PI / 2, 0, -vp.angle);
    SCRATCH_DUMMY.scale.set(1, 1, 1);
    SCRATCH_DUMMY.updateMatrix();
    spokeMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
  }
  spokeMesh.instanceMatrix.needsUpdate = true;
  group.add(spokeMesh);

  // --- 2. FIVE PUBLIC CIVIC PLAZAS (BATCHED ACROSS ALL 5 VAULTS -> 17 DRAW CALLS TOTAL) ---
  const plazaStoneMat = new THREE.MeshStandardMaterial({
    map: plazaStoneTex,
    color: 0xcbd5e1,
    roughness: 0.72,
    metalness: 0.12,
  });
  const plazaTrimMat = new THREE.MeshStandardMaterial({
    color: 0x334155,
    roughness: 0.55,
    metalness: 0.35,
  });
  const oculusGlassMat = new THREE.MeshPhysicalMaterial({
    color: 0x38bdf8,
    roughness: 0.08,
    metalness: 0.05,
    clearcoat: 1.0,
    clearcoatRoughness: 0.08,
    transparent: true,
    opacity: 0.34,
  });
  const pavilionGlassMat = new THREE.MeshPhysicalMaterial({
    color: 0x7dd3fc,
    roughness: 0.10,
    metalness: 0.12,
    clearcoat: 1.0,
    transparent: true,
    opacity: 0.28,
    side: THREE.DoubleSide,
  });
  const pavilionRibMat = new THREE.MeshStandardMaterial({
    color: 0xd6d3d1,
    roughness: 0.38,
    metalness: 0.65,
  });
  const benchTimberMat = new THREE.MeshStandardMaterial({
    color: 0x9a6b43,
    roughness: 0.72,
    metalness: 0.08,
  });
  const bollardBodyMat = new THREE.MeshStandardMaterial({
    color: 0x1e293b,
    roughness: 0.45,
    metalness: 0.65,
  });
  const bollardGlowMat = new THREE.MeshStandardMaterial({
    color: 0xffedd5,
    emissive: 0xffb347,
    emissiveIntensity: 0.9,
    roughness: 0.2,
  });
  const plazaOculusRingMat = new THREE.MeshStandardMaterial({
    color: 0x22d3ee,
    emissive: 0x06b6d4,
    emissiveIntensity: 1.05,
    roughness: 0.25,
  });

  const plazasGroup = tagInspectable(new THREE.Group(), 'civic_plaza');
  plazasGroup.position.y = 0.06;
  group.add(plazasGroup);

  // Merge terraced plinths per plaza (1 geometry, 5 instances)
  const plazaPlinthGeom = mergeStaticGeometries([
    { geom: new THREE.CylinderGeometry(6.65, 6.95, 0.16, 40), pos: [0, 0.08, 0] },
    { geom: new THREE.CylinderGeometry(5.6, 5.85, 0.12, 36), pos: [0, 0.20, 0] },
  ]);
  const plazaPlinthMesh = tagInspectable(
    new THREE.InstancedMesh(plazaPlinthGeom, plazaStoneMat, vaultCount),
    'civic_plaza'
  );
  plazaPlinthMesh.receiveShadow = true;

  // Merge plaza trim (oculus curb + 4 bench bases) per plaza -> 5 instances
  const plazaTrimEntries = [
    { geom: new THREE.TorusGeometry(2.48, 0.16, 10, 32), pos: [0, 0.30, 0], rot: [Math.PI / 2, 0, 0] },
  ];
  for (let b = 0; b < 4; b++) {
    const ba = b * (Math.PI / 2) + Math.PI / 4;
    plazaTrimEntries.push({
      geom: new THREE.TorusGeometry(4.35, 0.24, 8, 12, Math.PI * 0.24),
      pos: [0, 0.36, 0],
      rot: [Math.PI / 2, 0, ba],
    });
  }
  const plazaTrimMesh = tagInspectable(
    new THREE.InstancedMesh(mergeStaticGeometries(plazaTrimEntries), plazaTrimMat, vaultCount),
    'civic_plaza'
  );

  // Merge 4 curved timber bench seats per plaza -> 5 instances
  const plazaBenchEntries = [];
  for (let b = 0; b < 4; b++) {
    const ba = b * (Math.PI / 2) + Math.PI / 4;
    plazaBenchEntries.push({
      geom: new THREE.TorusGeometry(4.35, 0.20, 8, 12, Math.PI * 0.24),
      pos: [0, 0.48, 0],
      rot: [Math.PI / 2, 0, ba],
    });
  }
  const plazaBenchMesh = tagInspectable(
    new THREE.InstancedMesh(mergeStaticGeometries(plazaBenchEntries), benchTimberMat, vaultCount),
    'civic_plaza'
  );

  // Oculus laminated glass disc -> 5 instances
  const oculusGlassGeom = new THREE.CylinderGeometry(2.38, 2.38, 0.1, 28);
  oculusGlassGeom.translate(0, 0.24, 0);
  const plazaOculusMesh = tagInspectable(
    new THREE.InstancedMesh(oculusGlassGeom, oculusGlassMat, vaultCount),
    'civic_plaza'
  );

  // Pavilion skylight canopy glass (cone + cap merged) -> 5 instances
  const pavilionGlassGeom = mergeStaticGeometries([
    { geom: new THREE.CylinderGeometry(1.15, 2.75, 0.42, 24, 1, true), pos: [0, 1.66, 0] },
    { geom: new THREE.CylinderGeometry(1.15, 1.15, 0.06, 20), pos: [0, 1.87, 0] },
  ]);
  const plazaCanopyGlassMesh = tagInspectable(
    new THREE.InstancedMesh(pavilionGlassGeom, pavilionGlassMat, vaultCount),
    'civic_plaza'
  );

  // Pavilion ring + 6 structural ribs merged per plaza -> 5 instances
  const ribEntries = [
    { geom: new THREE.TorusGeometry(2.75, 0.09, 10, 32), pos: [0, 1.48, 0], rot: [Math.PI / 2, 0, 0] },
  ];
  for (let k = 0; k < 6; k++) {
    const a = (k / 6) * Math.PI * 2;
    ribEntries.push({
      geom: new THREE.BoxGeometry(0.11, 1.38, 0.18),
      pos: [Math.cos(a) * 2.72, 0.88, Math.sin(a) * 2.72],
      rot: [0, -a, -0.14],
    });
  }
  const plazaRibsMesh = tagInspectable(
    new THREE.InstancedMesh(mergeStaticGeometries(ribEntries), pavilionRibMat, vaultCount),
    'civic_plaza'
  );
  plazaRibsMesh.castShadow = true;

  // 8 bollard posts & 8 bollard lamps merged per plaza -> 5 instances each
  const bollardPostEntries = [];
  const bollardLampEntries = [];
  for (let l = 0; l < 8; l++) {
    const la = (l / 8) * Math.PI * 2 + Math.PI / 8;
    const bx = Math.cos(la) * 5.85;
    const bz = Math.sin(la) * 5.85;
    bollardPostEntries.push({
      geom: new THREE.CylinderGeometry(0.08, 0.10, 0.72, 8),
      pos: [bx, 0.56, bz],
    });
    bollardLampEntries.push({
      geom: new THREE.CylinderGeometry(0.085, 0.085, 0.18, 8),
      pos: [bx, 0.96, bz],
    });
  }
  const plazaBollardPostsMesh = tagInspectable(
    new THREE.InstancedMesh(mergeStaticGeometries(bollardPostEntries), bollardBodyMat, vaultCount),
    'civic_plaza'
  );
  const plazaBollardLampsMesh = tagInspectable(
    new THREE.InstancedMesh(mergeStaticGeometries(bollardLampEntries), bollardGlowMat, vaultCount),
    'civic_plaza'
  );

  // Shared status ring geometry (5 tiny meshes for individual fault pulsing)
  const sharedStatusRingGeom = new THREE.TorusGeometry(6.35, 0.085, 8, 44);
  const plazaVaultRings = [];

  for (let i = 0; i < vaultCount; i++) {
    const vp = vaults[i];
    SCRATCH_DUMMY.position.set(vp.x, 0, vp.z);
    SCRATCH_DUMMY.rotation.set(0, 0, 0);
    SCRATCH_DUMMY.scale.set(1, 1, 1);
    SCRATCH_DUMMY.updateMatrix();

    plazaPlinthMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaTrimMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaBenchMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaOculusMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaCanopyGlassMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaRibsMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaBollardPostsMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
    plazaBollardLampsMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);

    const statusRingMat = plazaOculusRingMat.clone();
    const rim = tagInspectable(new THREE.Mesh(sharedStatusRingGeom, statusRingMat), 'civic_plaza');
    rim.rotation.x = Math.PI / 2;
    rim.position.set(vp.x, 0.18, vp.z);
    plazasGroup.add(rim);
    plazaVaultRings.push(statusRingMat);
  }

  plazaPlinthMesh.instanceMatrix.needsUpdate = true;
  plazaTrimMesh.instanceMatrix.needsUpdate = true;
  plazaBenchMesh.instanceMatrix.needsUpdate = true;
  plazaOculusMesh.instanceMatrix.needsUpdate = true;
  plazaCanopyGlassMesh.instanceMatrix.needsUpdate = true;
  plazaRibsMesh.instanceMatrix.needsUpdate = true;
  plazaBollardPostsMesh.instanceMatrix.needsUpdate = true;
  plazaBollardLampsMesh.instanceMatrix.needsUpdate = true;

  plazasGroup.add(
    plazaPlinthMesh,
    plazaTrimMesh,
    plazaBenchMesh,
    plazaOculusMesh,
    plazaCanopyGlassMesh,
    plazaRibsMesh,
    plazaBollardPostsMesh,
    plazaBollardLampsMesh
  );

  // --- 3. CENTRAL CIVIC HUB, SCHOOLS & HEALTH CENTRE (inside r = 13) ---
  const civicHubGroup = tagInspectable(new THREE.Group(), 'civic_hub');
  group.add(civicHubGroup);

  const hubDeckGeom = mergeStaticGeometries([
    { geom: new THREE.CylinderGeometry(13.0, 13.3, 0.16, 56), pos: [0, 0.08, 0] },
    { geom: new THREE.CylinderGeometry(11.6, 11.9, 0.12, 48), pos: [0, 0.18, 0] },
  ]);
  const hubDeck = new THREE.Mesh(hubDeckGeom, plazaStoneMat);
  hubDeck.receiveShadow = true;
  civicHubGroup.add(hubDeck);

  const courtyardGreenMat = new THREE.MeshStandardMaterial({ color: 0x2f7d46, roughness: 0.88 });
  const sedumRoofMat = new THREE.MeshStandardMaterial({ color: 0x3b8c52, roughness: 0.86 });
  const waterReflectMat = new THREE.MeshPhysicalMaterial({
    color: 0x0c4a6e,
    roughness: 0.06,
    metalness: 0.18,
    clearcoat: 1.0,
    clearcoatRoughness: 0.06,
    transparent: true,
    opacity: 0.88,
  });

  const courtyardLawn = new THREE.Mesh(new THREE.CylinderGeometry(3.3, 3.4, 0.10, 32), courtyardGreenMat);
  courtyardLawn.position.y = 0.28;
  civicHubGroup.add(courtyardLawn);

  const basinRim = new THREE.Mesh(new THREE.TorusGeometry(1.95, 0.14, 10, 32), plazaTrimMat);
  basinRim.rotation.x = Math.PI / 2;
  basinRim.position.y = 0.36;
  civicHubGroup.add(basinRim);

  const basinWater = new THREE.Mesh(new THREE.CylinderGeometry(1.88, 1.88, 0.08, 28), waterReflectMat);
  basinWater.position.y = 0.33;
  civicHubGroup.add(basinWater);

  const hubSpireRing = new THREE.Mesh(
    new THREE.TorusGeometry(0.95, 0.07, 10, 28),
    new THREE.MeshStandardMaterial({
      color: 0x38bdf8,
      emissive: 0x0ea5e9,
      emissiveIntensity: 1.25,
      roughness: 0.2,
    })
  );
  hubSpireRing.rotation.x = Math.PI / 2;
  hubSpireRing.position.y = 0.52;
  civicHubGroup.add(hubSpireRing);

  const civicFacadeMat = new THREE.MeshStandardMaterial({
    map: civicTex.map,
    emissive: new THREE.Color(0xffcf88),
    emissiveMap: civicTex.emissiveMap,
    emissiveIntensity: 0.12,
    roughnessMap: civicTex.roughnessMap,
    roughness: 0.9,
    metalness: 0.18,
  });
  const civicRoofMat = new THREE.MeshStandardMaterial({
    map: roofMembraneTex,
    color: 0x94a3b8,
    roughness: 0.72,
    metalness: 0.15,
  });
  const civicDarkTrimMat = new THREE.MeshStandardMaterial({
    color: 0x1e293b,
    roughness: 0.45,
    metalness: 0.5,
  });
  const civicBoxMats = [
    civicFacadeMat,
    civicFacadeMat,
    civicRoofMat,
    civicDarkTrimMat,
    civicFacadeMat,
    civicFacadeMat,
  ];

  const civicSpecs = [
    { x: -5.4, z: -4.2, w: 5.4, h: 3.4, d: 3.6, rotY: 0.18, upperScale: 0.72 },
    { x: 5.3, z: -4.0, w: 5.6, h: 3.0, d: 3.5, rotY: -0.22, upperScale: 0.76 },
    { x: -5.0, z: 4.8, w: 4.8, h: 2.8, d: 4.0, rotY: -0.15, upperScale: 0.70 },
    { x: 5.0, z: 4.9, w: 5.0, h: 3.9, d: 3.6, rotY: 0.24, upperScale: 0.68 },
  ];

  // Batch civic podiums/uppers (8 boxes), sedum decks (4), and parapets (4) into 3 InstancedMeshes
  const unitBoxGeom = new THREE.BoxGeometry(1, 1, 1);
  const civicVolumesMesh = new THREE.InstancedMesh(unitBoxGeom, civicBoxMats, civicSpecs.length * 2);
  civicVolumesMesh.castShadow = true;
  civicVolumesMesh.receiveShadow = true;
  const civicSedumMesh = new THREE.InstancedMesh(unitBoxGeom, sedumRoofMat, civicSpecs.length);
  const civicParapetMesh = new THREE.InstancedMesh(unitBoxGeom, civicDarkTrimMat, civicSpecs.length);

  const pvRecords = [];
  for (let i = 0; i < civicSpecs.length; i++) {
    const cs = civicSpecs[i];
    const cosR = Math.cos(cs.rotY);
    const sinR = Math.sin(cs.rotY);
    const podH = cs.h * 0.56;

    // Lower podium
    SCRATCH_DUMMY.position.set(cs.x, 0.24 + podH / 2, cs.z);
    SCRATCH_DUMMY.rotation.set(0, cs.rotY, 0);
    SCRATCH_DUMMY.scale.set(cs.w, podH, cs.d);
    SCRATCH_DUMMY.updateMatrix();
    civicVolumesMesh.setMatrixAt(i * 2, SCRATCH_DUMMY.matrix);

    // Sedum green roof deck
    SCRATCH_DUMMY.position.set(cs.x, 0.24 + podH + 0.06, cs.z);
    SCRATCH_DUMMY.scale.set(cs.w * 0.94, 0.12, cs.d * 0.94);
    SCRATCH_DUMMY.updateMatrix();
    civicSedumMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);

    // Cantilevered upper glazed volume
    const upW = cs.w * cs.upperScale;
    const upD = cs.d * 0.78;
    const upH = cs.h - podH;
    const lx = cs.w * 0.08;
    const lz = -cs.d * 0.06;
    const ux = cs.x + lx * cosR + lz * sinR;
    const uz = cs.z - lx * sinR + lz * cosR;

    SCRATCH_DUMMY.position.set(ux, 0.24 + podH + upH / 2, uz);
    SCRATCH_DUMMY.scale.set(upW, upH, upD);
    SCRATCH_DUMMY.updateMatrix();
    civicVolumesMesh.setMatrixAt(i * 2 + 1, SCRATCH_DUMMY.matrix);

    // Parapet collar
    SCRATCH_DUMMY.position.set(ux, 0.24 + cs.h + 0.08, uz);
    SCRATCH_DUMMY.scale.set(upW + 0.18, 0.18, upD + 0.18);
    SCRATCH_DUMMY.updateMatrix();
    civicParapetMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);

    pvRecords.push({
      x: ux,
      y: 0.24 + cs.h + 0.34,
      z: uz,
      rotY: cs.rotY,
      w: upW * 0.82,
      d: upD * 0.72,
    });
  }
  civicVolumesMesh.instanceMatrix.needsUpdate = true;
  civicSedumMesh.instanceMatrix.needsUpdate = true;
  civicParapetMesh.instanceMatrix.needsUpdate = true;
  civicHubGroup.add(civicVolumesMesh, civicSedumMesh, civicParapetMesh);

  // --- 4. 8 MW / 32 MWh LFP BESS YARD (BATCHED -> 11 DRAW CALLS TOTAL) ---
  const bessGroup = tagInspectable(new THREE.Group(), 'bess_yard');
  bessGroup.position.set(-48, 0.08, 22);
  group.add(bessGroup);

  const bessPad = new THREE.Mesh(new THREE.BoxGeometry(12.6, 0.22, 9.6), plazaStoneMat);
  bessPad.position.y = 0.11;
  bessPad.receiveShadow = true;
  bessGroup.add(bessPad);

  const railPostMat = new THREE.MeshStandardMaterial({ color: 0xf59e0b, roughness: 0.45, metalness: 0.55 });
  const railBarMat = new THREE.MeshStandardMaterial({ color: 0x64748b, roughness: 0.4, metalness: 0.7 });
  const padHalfX = 6.1;
  const padHalfZ = 4.6;
  const railY = 0.22;
  const railSegments = [
    { x: 0, z: -padHalfZ, len: padHalfX * 2, rotY: 0 },
    { x: 0, z: padHalfZ, len: padHalfX * 2, rotY: 0 },
    { x: -padHalfX, z: 0, len: padHalfZ * 2, rotY: Math.PI / 2 },
    { x: padHalfX, z: 0, len: padHalfZ * 2, rotY: Math.PI / 2 },
  ];

  // Merge all perimeter railing horizontal bars + transformer cooling fins into 1 mesh
  const railBarEntries = [];
  const railPostEntries = [];
  for (const rs of railSegments) {
    for (const hOff of [0.42, 0.78]) {
      railBarEntries.push({
        geom: new THREE.BoxGeometry(rs.len, 0.055, 0.055),
        pos: [rs.x, railY + hOff, rs.z],
        rot: [0, rs.rotY, 0],
      });
    }
    const postCount = Math.max(3, Math.floor(rs.len / 2.2));
    for (let p = 0; p <= postCount; p++) {
      const frac = -0.5 + p / postCount;
      railPostEntries.push({
        geom: new THREE.BoxGeometry(0.08, 0.85, 0.08),
        pos:
          rs.rotY === 0
            ? [rs.x + frac * rs.len, railY + 0.42, rs.z]
            : [rs.x, railY + 0.42, rs.z + frac * rs.len],
      });
    }
  }
  for (let f = 0; f < 5; f++) {
    railBarEntries.push({
      geom: new THREE.BoxGeometry(0.08, 1.28, 2.55),
      pos: [3.85 + f * 0.35, 0.22 + 0.82, -1.1],
    });
  }
  bessGroup.add(new THREE.Mesh(mergeStaticGeometries(railBarEntries), railBarMat));
  bessGroup.add(new THREE.Mesh(mergeStaticGeometries(railPostEntries), railPostMat));

  // 8 Containerized LFP Battery Units + Switchgear (1 InstancedMesh) + Rooftop Chillers & Transformer (1 merged mesh)
  const bessContainerMat = new THREE.MeshStandardMaterial({
    map: bessTex,
    roughness: 0.46,
    metalness: 0.55,
  });
  const bessRoofMat = new THREE.MeshStandardMaterial({
    color: 0x334155,
    roughness: 0.52,
    metalness: 0.62,
  });

  const bessContainersMesh = new THREE.InstancedMesh(new THREE.BoxGeometry(1.85, 1.48, 3.1), bessContainerMat, 9);
  bessContainersMesh.castShadow = true;
  bessContainersMesh.receiveShadow = true;

  const bessMetalEntries = [
    { geom: new THREE.BoxGeometry(1.75, 1.65, 2.35), pos: [4.55, 0.22 + 0.825, -1.1] },
  ];

  // 5 shared LED bar materials (tiers 0..4), 1 InstancedMesh per tier (5 draw calls instead of 40)
  const ledBarGeom = new THREE.BoxGeometry(1.18, 0.11, 0.06);
  const bessBarMats = [];
  const bessBarMeshes = [];
  for (let tier = 0; tier < 5; tier++) {
    const mat = new THREE.MeshStandardMaterial({
      color: 0x10b981,
      emissive: 0x10b981,
      emissiveIntensity: 1.2,
      roughness: 0.2,
    });
    bessBarMats.push(mat);
    const m = new THREE.InstancedMesh(ledBarGeom, mat, 8);
    bessBarMeshes.push(m);
    bessGroup.add(m);
  }

  let unitIdx = 0;
  for (let r = 0; r < 2; r++) {
    for (let c = 0; c < 4; c++) {
      const cx = -4.2 + c * 2.2;
      const cz = r === 0 ? -2.05 : 2.05;

      SCRATCH_DUMMY.position.set(cx, 0.22 + 0.74, cz);
      SCRATCH_DUMMY.rotation.set(0, 0, 0);
      SCRATCH_DUMMY.scale.set(1, 1, 1);
      SCRATCH_DUMMY.updateMatrix();
      bessContainersMesh.setMatrixAt(unitIdx, SCRATCH_DUMMY.matrix);

      bessMetalEntries.push({
        geom: new THREE.BoxGeometry(1.35, 0.24, 1.6),
        pos: [cx, 0.22 + 1.48 + 0.12, cz - 0.35],
      });

      const faceZ = r === 0 ? cz - 1.57 : cz + 1.57;
      for (let tier = 0; tier < 5; tier++) {
        SCRATCH_DUMMY.position.set(cx, 0.52 + tier * 0.21, faceZ);
        SCRATCH_DUMMY.updateMatrix();
        bessBarMeshes[tier].setMatrixAt(unitIdx, SCRATCH_DUMMY.matrix);
      }
      unitIdx++;
    }
  }
  // 9th instance = MV switchgear cabinet
  SCRATCH_DUMMY.position.set(4.55, 0.22 + 0.71, 1.8);
  SCRATCH_DUMMY.scale.set(1.6 / 1.85, 1.42 / 1.48, 2.1 / 3.1);
  SCRATCH_DUMMY.updateMatrix();
  bessContainersMesh.setMatrixAt(8, SCRATCH_DUMMY.matrix);
  bessContainersMesh.instanceMatrix.needsUpdate = true;
  bessGroup.add(bessContainersMesh);

  for (let tier = 0; tier < 5; tier++) bessBarMeshes[tier].instanceMatrix.needsUpdate = true;

  const bessMetalMesh = new THREE.Mesh(mergeStaticGeometries(bessMetalEntries), bessRoofMat);
  bessMetalMesh.castShadow = true;
  bessGroup.add(bessMetalMesh);

  // 3 HV ceramic bushings merged into 1 mesh
  const bushingMat = new THREE.MeshStandardMaterial({ color: 0xe2e8f0, roughness: 0.2, metalness: 0.15 });
  const bushingEntries = [];
  for (let i = 0; i < 3; i++) {
    bushingEntries.push({
      geom: new THREE.CylinderGeometry(0.09, 0.14, 0.55, 10),
      pos: [4.55, 0.22 + 1.65 + 0.27, -1.7 + i * 0.6],
    });
  }
  bessGroup.add(new THREE.Mesh(mergeStaticGeometries(bushingEntries), bushingMat));

  // --- 5. 25 ha BLUE-GREEN PARKS, RETENTION PONDS & ORGANIC MULTI-TIER TREES ---
  const parkLawnMat = new THREE.MeshStandardMaterial({
    color: 0x1a5c32,
    roughness: 0.92,
    metalness: 0.02,
  });
  const parkPathMat = new THREE.MeshStandardMaterial({
    color: 0x94a3b8,
    roughness: 0.82,
    metalness: 0.05,
  });
  const pondEmbankMat = new THREE.MeshStandardMaterial({
    color: 0x334155,
    roughness: 0.85,
  });
  const pondWaterMat = new THREE.MeshPhysicalMaterial({
    color: 0x0e7490,
    emissive: 0x083344,
    emissiveIntensity: 0.18,
    roughness: 0.06,
    metalness: 0.16,
    clearcoat: 1.0,
    clearcoatRoughness: 0.05,
    transparent: true,
    opacity: 0.90,
  });

  const pondCenters = [];
  const wedgeEntries = [];
  const pathEntries = [];
  const pondSpecs = [];

  for (const parkAngle of [0.57, 3.70]) {
    wedgeEntries.push({
      geom: new THREE.RingGeometry(18.4, 68.5, 24, 1, -parkAngle - 0.165, 0.33),
      pos: [0, 0.030, 0],
      rot: [-Math.PI / 2, 0, 0],
    });
    pathEntries.push({
      geom: new THREE.PlaneGeometry(46, 1.15),
      pos: [Math.cos(parkAngle) * 43, 0.038, Math.sin(parkAngle) * 43],
      rot: [-Math.PI / 2, 0, -parkAngle],
    });

    for (const [rPond, aOff, rx, rz] of [
      [28.5, -0.045, 3.6, 2.2],
      [48.5, 0.04, 5.2, 2.8],
      [60.5, -0.03, 3.8, 2.1],
    ]) {
      const pa = parkAngle + aOff;
      const px = Math.cos(pa) * rPond;
      const pz = Math.sin(pa) * rPond;
      pondCenters.push({ x: px, z: pz, r: Math.max(rx, rz) + 1.2 });
      pondSpecs.push({ px, pz, pa, rx, rz });
    }
  }

  const parkWedgesMesh = new THREE.Mesh(mergeStaticGeometries(wedgeEntries), parkLawnMat);
  parkWedgesMesh.receiveShadow = true;
  group.add(parkWedgesMesh);
  group.add(new THREE.Mesh(mergeStaticGeometries(pathEntries), parkPathMat));

  // Batch all 6 pond embankments & 6 pond water surfaces into 2 InstancedMeshes
  const pondBankMesh = new THREE.InstancedMesh(
    new THREE.CylinderGeometry(1, 1.06, 0.06, 20),
    pondEmbankMat,
    pondSpecs.length
  );
  const pondWaterMesh = new THREE.InstancedMesh(
    new THREE.CylinderGeometry(1, 1, 0.06, 20),
    pondWaterMat,
    pondSpecs.length
  );
  for (let i = 0; i < pondSpecs.length; i++) {
    const ps = pondSpecs[i];
    SCRATCH_DUMMY.rotation.set(0, -ps.pa, 0);
    SCRATCH_DUMMY.position.set(ps.px, 0.036, ps.pz);
    SCRATCH_DUMMY.scale.set(ps.rx + 0.45, 1, ps.rz + 0.45);
    SCRATCH_DUMMY.updateMatrix();
    pondBankMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);

    SCRATCH_DUMMY.position.set(ps.px, 0.052, ps.pz);
    SCRATCH_DUMMY.scale.set(ps.rx, 1, ps.rz);
    SCRATCH_DUMMY.updateMatrix();
    pondWaterMesh.setMatrixAt(i, SCRATCH_DUMMY.matrix);
  }
  pondBankMesh.instanceMatrix.needsUpdate = true;
  pondWaterMesh.instanceMatrix.needsUpdate = true;
  group.add(pondBankMesh, pondWaterMesh);

  function isInPond(x, z) {
    for (let i = 0; i < pondCenters.length; i++) {
      const pc = pondCenters[i];
      const dx = x - pc.x;
      const dz = z - pc.z;
      if (dx * dx + dz * dz < pc.r * pc.r) return true;
    }
    return false;
  }

  // Organic Multi-Tier Instanced Trees
  const deciduousTrees = [];
  const coniferTrees = [];
  const treeRng = seededRand(808);

  for (let i = 0; i < 6; i++) {
    const a = (i / 6) * Math.PI * 2 + 0.35;
    deciduousTrees.push({
      x: Math.cos(a) * 2.85,
      z: Math.sin(a) * 2.85,
      yBase: 0.28,
      scale: 0.65 + treeRng() * 0.2,
      tint: treeRng(),
    });
  }

  for (let i = 0; i < 420; i++) {
    const a = (i * 2.399963) % (Math.PI * 2);
    const r = 18.8 + ((i * 19.73) % 49.0);
    const x = Math.cos(a) * r;
    const z = Math.sin(a) * r;
    if (isNearVaultPlaza(x, z, 7.6)) continue;
    if (Math.hypot(x + 48, z - 22) < 9.2) continue;
    if (Math.abs(r - 16) < 2.5 || Math.abs(r - 38) < 2.75) continue;
    if (isInPond(x, z)) continue;

    let nearSpoke = false;
    for (let v = 0; v < vaultCount; v++) {
      let diff = Math.abs(a - vaults[v].angle);
      if (diff > Math.PI) diff = Math.PI * 2 - diff;
      if (diff < 0.068) {
        nearSpoke = true;
        break;
      }
    }
    if (nearSpoke) continue;

    const normA = ((a % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
    const inPark = (normA > 0.405 && normA < 0.735) || (normA > 3.535 && normA < 3.865);

    if (inPark || i % 3 === 0) {
      const rec = {
        x,
        z,
        yBase: 0.04,
        scale: (inPark ? 0.85 : 0.70) + treeRng() * 0.45,
        tint: treeRng(),
      };
      if (i % 3 === 0) coniferTrees.push(rec);
      else deciduousTrees.push(rec);
    }
  }

  const trunkMat = new THREE.MeshStandardMaterial({ color: 0x5c4033, roughness: 0.92 });
  const foliageMat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.84, metalness: 0.02 });

  // Merge both deciduous crown tiers into 1 geometry & both conifer tiers into 1 geometry -> saves 2 draw calls!
  const totalTrees = deciduousTrees.length + coniferTrees.length;
  if (totalTrees > 0) {
    const allTrunks = new THREE.InstancedMesh(
      new THREE.CylinderGeometry(0.11, 0.18, 1.12, 6),
      trunkMat,
      totalTrees
    );
    let tIdx = 0;

    if (deciduousTrees.length > 0) {
      const decCrownGeom = mergeStaticGeometries([
        { geom: new THREE.IcosahedronGeometry(0.95, 1), pos: [0, 1.55, 0], scale: [1.05, 0.85, 1.0] },
        { geom: new THREE.IcosahedronGeometry(0.68, 1), pos: [0.22, 2.20, 0.12], scale: [0.92, 0.82, 0.92] },
      ]);
      const decCrowns = new THREE.InstancedMesh(decCrownGeom, foliageMat, deciduousTrees.length);
      decCrowns.castShadow = true;
      decCrowns.receiveShadow = true;
      const decPalette = [0x1f7a3e, 0x2d8a4e, 0x3f8f45, 0x256d42, 0x4b9452];

      for (let i = 0; i < deciduousTrees.length; i++) {
        const t = deciduousTrees[i];
        SCRATCH_DUMMY.rotation.set(0, t.tint * Math.PI * 2, 0);
        SCRATCH_DUMMY.scale.setScalar(t.scale);

        SCRATCH_DUMMY.position.set(t.x, t.yBase + 0.55 * t.scale, t.z);
        SCRATCH_DUMMY.updateMatrix();
        allTrunks.setMatrixAt(tIdx, SCRATCH_DUMMY.matrix);
        allTrunks.setColorAt(tIdx, SCRATCH_COLOR.setHex(0x6b4c3b));
        tIdx++;

        SCRATCH_DUMMY.position.set(t.x, t.yBase, t.z);
        SCRATCH_DUMMY.updateMatrix();
        decCrowns.setMatrixAt(i, SCRATCH_DUMMY.matrix);
        decCrowns.setColorAt(i, SCRATCH_COLOR.setHex(decPalette[i % decPalette.length]));
      }
      decCrowns.instanceMatrix.needsUpdate = true;
      if (decCrowns.instanceColor) decCrowns.instanceColor.needsUpdate = true;
      group.add(decCrowns);
    }

    if (coniferTrees.length > 0) {
      const conCrownGeom = mergeStaticGeometries([
        { geom: new THREE.ConeGeometry(0.88, 1.75, 7), pos: [0, 1.48, 0] },
        { geom: new THREE.ConeGeometry(0.62, 1.55, 7), pos: [0, 2.38, 0] },
      ]);
      const conCrowns = new THREE.InstancedMesh(conCrownGeom, foliageMat, coniferTrees.length);
      conCrowns.castShadow = true;
      const conPalette = [0x14532d, 0x166534, 0x1b5e3a, 0x11492b];

      for (let i = 0; i < coniferTrees.length; i++) {
        const t = coniferTrees[i];
        SCRATCH_DUMMY.rotation.set(0, t.tint * Math.PI * 2, 0);
        SCRATCH_DUMMY.scale.setScalar(t.scale);

        SCRATCH_DUMMY.position.set(t.x, t.yBase + 0.50 * t.scale, t.z);
        SCRATCH_DUMMY.updateMatrix();
        allTrunks.setMatrixAt(tIdx, SCRATCH_DUMMY.matrix);
        allTrunks.setColorAt(tIdx, SCRATCH_COLOR.setHex(0x523a2c));
        tIdx++;

        SCRATCH_DUMMY.position.set(t.x, t.yBase, t.z);
        SCRATCH_DUMMY.updateMatrix();
        conCrowns.setMatrixAt(i, SCRATCH_DUMMY.matrix);
        conCrowns.setColorAt(i, SCRATCH_COLOR.setHex(conPalette[i % conPalette.length]));
      }
      conCrowns.instanceMatrix.needsUpdate = true;
      if (conCrowns.instanceColor) conCrowns.instanceColor.needsUpdate = true;
      group.add(conCrowns);
    }

    allTrunks.instanceMatrix.needsUpdate = true;
    if (allTrunks.instanceColor) allTrunks.instanceColor.needsUpdate = true;
    group.add(allTrunks);
  }

  // --- 6. FOUR RESIDENTIAL TYPOLOGIES (INSTANCEDMESHES) + 12 MW_p ROOFTOP PV ARRAYS ---
  const buildingRecords = [];
  const facadeMaterials = [civicFacadeMat];

  function sampleRing(count, rMin, rMax, seedOffset, minSpacing = 2.6) {
    const pts = [];
    const minSq = minSpacing * minSpacing;
    let attempt = 0;
    while (pts.length < count && attempt < count * 14) {
      attempt++;
      const a = (attempt * 2.399963 + seedOffset) % (Math.PI * 2);
      const frac = (attempt * 0.6180339) % 1;
      const r = rMin + frac * (rMax - rMin);
      const x = Math.cos(a) * r;
      const z = Math.sin(a) * r;
      if (isNearVaultPlaza(x, z, 8.7)) continue;
      if (isNearRoadOrPark(r, a)) continue;
      if (Math.hypot(x + 48, z - 22) < 10.2) continue;

      let overlap = false;
      for (let j = 0; j < pts.length; j++) {
        const dx = x - pts[j].x;
        const dz = z - pts[j].z;
        if (dx * dx + dz * dz < minSq) {
          overlap = true;
          break;
        }
      }
      if (overlap) continue;
      pts.push({ x, z, angle: a });
    }
    return pts;
  }

  const midRisePts = sampleRing(55, 19.6, 34.4, 0.1, 3.6);
  const lowRisePts = sampleRing(40, 30.8, 41.6, 1.2, 3.2);
  const townhousePts = sampleRing(165, 42.6, 54.6, 2.4, 2.35);
  const detachedPts = sampleRing(150, 56.2, 68.0, 3.7, 2.25);

  const flatRoofMat = new THREE.MeshStandardMaterial({
    map: roofMembraneTex,
    color: 0xffffff,
    roughness: 0.78,
    metalness: 0.10,
  });
  const bottomDarkMat = new THREE.MeshStandardMaterial({ color: 0x1e293b, roughness: 0.9 });
  const parapetMat = new THREE.MeshStandardMaterial({
    color: 0xffffff,
    roughness: 0.62,
    metalness: 0.24,
  });
  const gabledRoofMat = new THREE.MeshStandardMaterial({
    map: slateRoofTex,
    color: 0xffffff,
    roughness: 0.62,
    metalness: 0.18,
  });

  function makeFacadeMaterial(texSet) {
    const mat = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      map: texSet.map,
      emissive: new THREE.Color(0xffc978),
      emissiveMap: texSet.emissiveMap,
      emissiveIntensity: 0.06,
      roughnessMap: texSet.roughnessMap,
      roughness: 0.92,
      metalness: 0.14,
    });
    facadeMaterials.push(mat);
    return mat;
  }

  function buildTypology({
    pts,
    w,
    hMin,
    hVar,
    d,
    texSet,
    paletteHexes,
    addPv = false,
    pvEvery = 1,
    roofStyle = 'parapet',
  }) {
    if (pts.length === 0) return;

    const facadeMat = makeFacadeMaterial(texSet);
    const boxMats = [facadeMat, facadeMat, flatRoofMat, bottomDarkMat, facadeMat, facadeMat];

    const mesh = tagInspectable(
      new THREE.InstancedMesh(new THREE.BoxGeometry(w, 1, d), boxMats, pts.length),
      'residential_district'
    );
    mesh.castShadow = true;
    mesh.receiveShadow = true;

    const baseColors = new Array(pts.length);
    const zoneIndices = new Uint8Array(pts.length);

    let roofTopDetailMesh = null;
    let roofBaseColors = null;
    const roofPalette = [0x64748b, 0x526277, 0x717d8c, 0x4b5563];

    if (roofStyle === 'parapet') {
      // Merge parapet rim + penthouse mechanical core into 1 InstancedMesh per typology (halves rooftop draw calls)
      const parapetPenthouseGeom = mergeStaticGeometries([
        { geom: new THREE.BoxGeometry(w + 0.16, 0.22, d + 0.16), pos: [0, 0.09, 0] },
        { geom: new THREE.BoxGeometry(w * 0.34, 0.62, d * 0.42), pos: [w * 0.24, 0.38, 0] },
      ]);
      roofTopDetailMesh = tagInspectable(
        new THREE.InstancedMesh(parapetPenthouseGeom, parapetMat, pts.length),
        'residential_district'
      );
      roofTopDetailMesh.castShadow = true;
      roofBaseColors = new Array(pts.length);
    } else if (roofStyle === 'pitched') {
      roofTopDetailMesh = tagInspectable(
        new THREE.InstancedMesh(createGabledRoofGeometry(w, d, 0.72), gabledRoofMat, pts.length),
        'residential_district'
      );
      roofTopDetailMesh.castShadow = true;
      roofTopDetailMesh.receiveShadow = true;
      roofBaseColors = new Array(pts.length);
    }

    for (let idx = 0; idx < pts.length; idx++) {
      const pt = pts[idx];
      const h = hMin + (((idx * 37 + 13) % 100) / 100) * hVar;
      const rotY = -pt.angle + Math.PI / 2;

      SCRATCH_DUMMY.position.set(pt.x, h / 2 + 0.04, pt.z);
      SCRATCH_DUMMY.rotation.set(0, rotY, 0);
      SCRATCH_DUMMY.scale.set(1, h, 1);
      SCRATCH_DUMMY.updateMatrix();
      mesh.setMatrixAt(idx, SCRATCH_DUMMY.matrix);

      const baseCol = new THREE.Color(paletteHexes[idx % paletteHexes.length]);
      mesh.setColorAt(idx, baseCol);
      baseColors[idx] = baseCol;

      let bestZone = 0;
      let bestDist = Infinity;
      for (let v = 0; v < vaultCount; v++) {
        const vp = vaults[v];
        const distSq = (pt.x - vp.x) ** 2 + (pt.z - vp.z) ** 2;
        if (distSq < bestDist) {
          bestDist = distSq;
          bestZone = v;
        }
      }
      zoneIndices[idx] = bestZone;

      if (roofStyle === 'parapet') {
        const flip = idx % 2 === 0 ? 0 : Math.PI;
        SCRATCH_DUMMY.position.set(pt.x, h + 0.04, pt.z);
        SCRATCH_DUMMY.rotation.set(0, rotY + flip, 0);
        SCRATCH_DUMMY.scale.set(1, 1, 1);
        SCRATCH_DUMMY.updateMatrix();
        roofTopDetailMesh.setMatrixAt(idx, SCRATCH_DUMMY.matrix);
        const pCol = new THREE.Color(0x94a3b8);
        roofTopDetailMesh.setColorAt(idx, pCol);
        roofBaseColors[idx] = pCol;
      } else if (roofStyle === 'pitched') {
        SCRATCH_DUMMY.position.set(pt.x, h + 0.04, pt.z);
        SCRATCH_DUMMY.rotation.set(0, rotY, 0);
        SCRATCH_DUMMY.scale.set(1, 1, 1);
        SCRATCH_DUMMY.updateMatrix();
        roofTopDetailMesh.setMatrixAt(idx, SCRATCH_DUMMY.matrix);
        const rCol = new THREE.Color(roofPalette[idx % roofPalette.length]);
        roofTopDetailMesh.setColorAt(idx, rCol);
        roofBaseColors[idx] = rCol;
      }

      if (addPv && idx % pvEvery === 0) {
        const cosR = Math.cos(rotY);
        const sinR = Math.sin(rotY);
        const pvOff = roofStyle === 'parapet' ? -w * 0.14 * (idx % 2 === 0 ? 1 : -1) : 0;
        pvRecords.push({
          x: pt.x + pvOff * cosR,
          y: h + (roofStyle === 'parapet' ? 0.34 : 0.46),
          z: pt.z - pvOff * sinR,
          rotY,
          w: w * (roofStyle === 'parapet' ? 0.56 : 0.62),
          d: d * 0.64,
        });
      }
    }

    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
    group.add(mesh);
    buildingRecords.push({ mesh, count: pts.length, baseColors, zoneIndices });

    if (roofTopDetailMesh) {
      roofTopDetailMesh.instanceMatrix.needsUpdate = true;
      if (roofTopDetailMesh.instanceColor) roofTopDetailMesh.instanceColor.needsUpdate = true;
      group.add(roofTopDetailMesh);
      buildingRecords.push({ mesh: roofTopDetailMesh, count: pts.length, baseColors: roofBaseColors, zoneIndices });
    }
  }

  buildTypology({
    pts: midRisePts,
    w: 3.7,
    hMin: 5.4,
    hVar: 2.5,
    d: 2.5,
    texSet: midRiseTex,
    paletteHexes: [0xf5f0e6, 0xebe6dd, 0xe5ebf2, 0xf2ece1],
    addPv: true,
    pvEvery: 1,
    roofStyle: 'parapet',
  });
  buildTypology({
    pts: lowRisePts,
    w: 3.25,
    hMin: 3.3,
    hVar: 1.3,
    d: 2.25,
    texSet: lowRiseTex,
    paletteHexes: [0xeae5dc, 0xe2e8f0, 0xdfd8cc, 0xdce5ee],
    addPv: true,
    pvEvery: 1,
    roofStyle: 'parapet',
  });
  buildTypology({
    pts: townhousePts,
    w: 2.15,
    hMin: 2.0,
    hVar: 0.72,
    d: 1.55,
    texSet: townTex,
    paletteHexes: [0xe6d8c9, 0xdccbb8, 0xd8dee6, 0xe5d6c5],
    addPv: true,
    pvEvery: 3,
    roofStyle: 'parapet',
  });
  buildTypology({
    pts: detachedPts,
    w: 1.75,
    hMin: 1.55,
    hVar: 0.52,
    d: 1.55,
    texSet: detachedTex,
    paletteHexes: [0xf0ece3, 0xe8e2d8, 0xe2e8ee, 0xeee6da],
    addPv: false,
    roofStyle: 'pitched',
  });

  // 12 MW_p Rooftop Solar PV Arrays + Angled Mounting Frames
  const pvMat = new THREE.MeshStandardMaterial({
    map: pvTex,
    color: 0xdbeafe,
    emissive: 0x1d4ed8,
    emissiveIntensity: 0.16,
    roughness: 0.14,
    metalness: 0.76,
  });
  const pvFrameMat = new THREE.MeshStandardMaterial({
    color: 0x475569,
    roughness: 0.45,
    metalness: 0.78,
  });

  if (pvRecords.length > 0) {
    const pvMesh = tagInspectable(
      new THREE.InstancedMesh(new THREE.BoxGeometry(1, 0.08, 1), pvMat, pvRecords.length),
      'rooftop_pv'
    );
    const pvFrames = tagInspectable(
      new THREE.InstancedMesh(new THREE.BoxGeometry(0.92, 0.16, 0.65), pvFrameMat, pvRecords.length),
      'rooftop_pv'
    );

    const pvWhite = new THREE.Color(0xffffff);
    const frameCol = new THREE.Color(0x64748b);
    for (let idx = 0; idx < pvRecords.length; idx++) {
      const p = pvRecords[idx];
      SCRATCH_DUMMY.rotation.order = 'XYZ';
      SCRATCH_DUMMY.position.set(p.x, p.y - 0.10, p.z);
      SCRATCH_DUMMY.rotation.set(0, p.rotY, 0);
      SCRATCH_DUMMY.scale.set(p.w, 1, p.d);
      SCRATCH_DUMMY.updateMatrix();
      pvFrames.setMatrixAt(idx, SCRATCH_DUMMY.matrix);
      pvFrames.setColorAt(idx, frameCol);

      SCRATCH_DUMMY.rotation.order = 'YXZ';
      SCRATCH_DUMMY.position.set(p.x, p.y, p.z);
      SCRATCH_DUMMY.rotation.set(0.24, p.rotY, 0);
      SCRATCH_DUMMY.scale.set(p.w, 1, p.d);
      SCRATCH_DUMMY.updateMatrix();
      pvMesh.setMatrixAt(idx, SCRATCH_DUMMY.matrix);
      pvMesh.setColorAt(idx, pvWhite);
    }
    SCRATCH_DUMMY.rotation.order = 'XYZ';
    pvMesh.instanceMatrix.needsUpdate = true;
    if (pvMesh.instanceColor) pvMesh.instanceColor.needsUpdate = true;
    pvFrames.instanceMatrix.needsUpdate = true;
    if (pvFrames.instanceColor) pvFrames.instanceColor.needsUpdate = true;
    group.add(pvFrames, pvMesh);
  }

  // --- 7. 12 STREAMLINED AUTONOMOUS 6G V2X PODS (BATCHED TO 6 INSTANCEDMESH DRAW CALLS) ---
  const podsGroup = tagInspectable(new THREE.Group(), 'autonomous_pods');
  group.add(podsGroup);

  const podCount = 12;
  const podHullMat = new THREE.MeshStandardMaterial({
    color: 0xf1f5f9,
    roughness: 0.22,
    metalness: 0.42,
  });
  const podSkirtMat = new THREE.MeshStandardMaterial({
    color: 0x0f172a,
    roughness: 0.55,
    metalness: 0.50,
  });
  const podGlassMat = new THREE.MeshPhysicalMaterial({
    color: 0x0f172a,
    roughness: 0.08,
    metalness: 0.20,
    clearcoat: 1.0,
    clearcoatRoughness: 0.05,
  });
  const podHeadlightMat = new THREE.MeshStandardMaterial({
    color: 0xffffff,
    emissive: 0xf8fafc,
    emissiveIntensity: 1.9,
    roughness: 0.15,
  });
  const podTaillightMat = new THREE.MeshStandardMaterial({
    color: 0xff2244,
    emissive: 0xff1e3c,
    emissiveIntensity: 1.85,
    roughness: 0.2,
  });
  const podLidarMat = new THREE.MeshStandardMaterial({
    color: 0x22d3ee,
    emissive: 0x22d3ee,
    emissiveIntensity: 1.75,
    roughness: 0.15,
  });

  // Merge each pod material's static sub-parts into 1 rigid geometry -> 6 InstancedMeshes total (down from 168 draw calls)
  const podSkirtGeom = mergeStaticGeometries([
    { geom: new THREE.BoxGeometry(0.94, 0.22, 1.72), pos: [0, 0.21, 0] },
    { geom: new THREE.CylinderGeometry(0.16, 0.20, 0.10, 12), pos: [0, 0.96, 0.12] },
  ]);
  const podHullGeom = mergeStaticGeometries([
    { geom: new THREE.BoxGeometry(0.98, 0.34, 1.62), pos: [0, 0.44, 0] },
    { geom: new THREE.CylinderGeometry(0.46, 0.49, 0.34, 12), pos: [0, 0.44, 0.80], scale: [1.0, 1.0, 0.58] },
    { geom: new THREE.CylinderGeometry(0.46, 0.49, 0.34, 12), pos: [0, 0.44, -0.80], scale: [1.0, 1.0, 0.58] },
    { geom: new THREE.BoxGeometry(0.76, 0.08, 1.18), pos: [0, 0.89, 0] },
  ]);
  const podGlassGeom = mergeStaticGeometries([
    { geom: new THREE.BoxGeometry(0.88, 0.30, 1.24), pos: [0, 0.72, 0] },
    { geom: new THREE.CylinderGeometry(0.40, 0.44, 0.29, 12), pos: [0, 0.72, 0.60], scale: [1.0, 1.0, 0.52] },
    { geom: new THREE.CylinderGeometry(0.40, 0.44, 0.29, 12), pos: [0, 0.72, -0.60], scale: [1.0, 1.0, 0.52] },
  ]);
  const podHeadlightGeom = mergeStaticGeometries([
    { geom: new THREE.BoxGeometry(0.24, 0.08, 0.08), pos: [-0.28, 0.45, 1.05] },
    { geom: new THREE.BoxGeometry(0.24, 0.08, 0.08), pos: [0.28, 0.45, 1.05] },
  ]);
  const podTaillightGeom = new THREE.BoxGeometry(0.74, 0.075, 0.08);
  podTaillightGeom.translate(0, 0.46, -1.05);
  podTaillightGeom.computeBoundingBox();
  const podLidarGeom = mergeStaticGeometries([
    { geom: new THREE.CylinderGeometry(0.15, 0.15, 0.07, 12), pos: [0, 1.02, 0.12] },
    { geom: new THREE.BoxGeometry(0.03, 0.045, 1.15), pos: [-0.495, 0.55, 0] },
    { geom: new THREE.BoxGeometry(0.03, 0.045, 1.15), pos: [0.495, 0.55, 0] },
  ]);

  const podMeshes = [
    tagInspectable(new THREE.InstancedMesh(podSkirtGeom, podSkirtMat, podCount), 'autonomous_pods'),
    tagInspectable(new THREE.InstancedMesh(podHullGeom, podHullMat, podCount), 'autonomous_pods'),
    tagInspectable(new THREE.InstancedMesh(podGlassGeom, podGlassMat, podCount), 'autonomous_pods'),
    tagInspectable(new THREE.InstancedMesh(podHeadlightGeom, podHeadlightMat, podCount), 'autonomous_pods'),
    tagInspectable(new THREE.InstancedMesh(podTaillightGeom, podTaillightMat, podCount), 'autonomous_pods'),
    tagInspectable(new THREE.InstancedMesh(podLidarGeom, podLidarMat, podCount), 'autonomous_pods'),
  ];
  podMeshes[1].castShadow = true;
  for (let i = 0; i < podMeshes.length; i++) {
    podMeshes[i].frustumCulled = false;
    podsGroup.add(podMeshes[i]);
  }

  const pods = Array.from({ length: podCount }, (_, i) => {
    const isInner = i % 2 === 0;
    return {
      ringRadius: (isInner ? 16.0 : 38.0) + (isInner ? (i % 4 === 0 ? 0.65 : -0.65) : (i % 4 === 1 ? 0.72 : -0.72)),
      baseAngle: (i / podCount) * Math.PI * 2,
      speed: isInner ? 0.23 : -0.14,
    };
  });

  // Dual-Glow V2X Optical Beams + Traveling Data Pulses
  const v2xCorePositions = new Float32Array(podCount * 2 * 3);
  const v2xCoreGeom = new THREE.BufferGeometry();
  v2xCoreGeom.setAttribute('position', new THREE.BufferAttribute(v2xCorePositions, 3));
  const v2xCoreLines = new THREE.LineSegments(
    v2xCoreGeom,
    new THREE.LineBasicMaterial({
      color: 0x22d3ee,
      transparent: true,
      opacity: 0.85,
      blending: THREE.AdditiveBlending,
      depthWrite: false,
    })
  );
  v2xCoreLines.frustumCulled = false;
  podsGroup.add(v2xCoreLines);

  const v2xAuraPositions = new Float32Array(podCount * 2 * 3);
  const v2xAuraGeom = new THREE.BufferGeometry();
  v2xAuraGeom.setAttribute('position', new THREE.BufferAttribute(v2xAuraPositions, 3));
  const v2xAuraLines = new THREE.LineSegments(
    v2xAuraGeom,
    new THREE.LineBasicMaterial({
      color: 0xa855f7,
      transparent: true,
      opacity: 0.55,
      blending: THREE.AdditiveBlending,
      depthWrite: false,
    })
  );
  v2xAuraLines.frustumCulled = false;
  podsGroup.add(v2xAuraLines);

  const v2xPulseCount = podCount * 2;
  const v2xPulseMesh = new THREE.InstancedMesh(
    new THREE.SphereGeometry(0.18, 8, 8),
    new THREE.MeshBasicMaterial({
      color: 0x67e8f9,
      transparent: true,
      opacity: 0.95,
      blending: THREE.AdditiveBlending,
      depthWrite: false,
    }),
    v2xPulseCount
  );
  v2xPulseMesh.frustumCulled = false;
  podsGroup.add(v2xPulseMesh);

  // --- LIVE UPDATE STATE (ZERO PER-FRAME ALLOCATIONS) ---
  let lastHeatmapVisible = null;
  let lastDemandBucket = -1;

  function setHeatmapVisible(visible) {
    heatmapVisible = Boolean(visible);
    lastHeatmapVisible = null;
  }

  function update(sim, dt, elapsed = 0) {
    if (!group.visible) return;

    const tod = sim.inputs?.timeOfDay ?? 13.5;
    const daylight = Math.max(0, Math.sin(Math.max(0, Math.min(1, (tod - 6) / 12)) * Math.PI));
    const nightFactor = Math.min(1, Math.max(0, 1 - daylight * 1.35));

    // Update architectural window glow (suppressed cleanly in thermal heatmap mode)
    const windowGlow = heatmapVisible ? 0.0 : 0.05 + nightFactor * 1.25;
    for (let i = 0; i < facadeMaterials.length; i++) {
      facadeMaterials[i].emissiveIntensity = windowGlow;
    }

    bollardGlowMat.emissiveIntensity = 0.55 + nightFactor * 1.45;
    guidewayMat.emissiveIntensity = 0.65 + nightFactor * 0.75 + 0.12 * Math.sin(elapsed * 2.5);

    // Dirty-checked building instanceColor updates (zero string allocations, zero redundant GPU uploads)
    const demandNorm = Math.min(1, Math.max(0, (sim.thermal.districtDemandMw - 1.45) / (9.52 - 1.45)));
    const demandBucket = heatmapVisible ? Math.round(demandNorm * 100) : -1;
    if (heatmapVisible !== lastHeatmapVisible || demandBucket !== lastDemandBucket) {
      lastHeatmapVisible = heatmapVisible;
      lastDemandBucket = demandBucket;

      if (heatmapVisible) {
        if (demandNorm < 0.5) {
          SCRATCH_HEAT.lerpColors(HEAT_LOW, HEAT_MID, demandNorm * 2);
        } else {
          SCRATCH_HEAT.lerpColors(HEAT_MID, HEAT_HIGH, (demandNorm - 0.5) * 2);
        }
        for (let r = 0; r < buildingRecords.length; r++) {
          const rec = buildingRecords[r];
          const zones = rec.zoneIndices;
          for (let i = 0; i < rec.count; i++) {
            const zw = ZONE_WEIGHTS[zones[i]] ?? 0.2;
            const zoneShift = (zw - 0.2) * 1.4;
            SCRATCH_ZONE_HEAT.copy(SCRATCH_HEAT).offsetHSL(-zoneShift * 0.06, 0.05, zoneShift * 0.12);
            rec.mesh.setColorAt(i, SCRATCH_ZONE_HEAT);
          }
          if (rec.mesh.instanceColor) rec.mesh.instanceColor.needsUpdate = true;
        }
      } else {
        for (let r = 0; r < buildingRecords.length; r++) {
          const rec = buildingRecords[r];
          const cols = rec.baseColors;
          for (let i = 0; i < rec.count; i++) {
            rec.mesh.setColorAt(i, cols[i]);
          }
          if (rec.mesh.instanceColor) rec.mesh.instanceColor.needsUpdate = true;
        }
      }
    }

    // Civic Plaza status rings
    const faultSin = Math.abs(Math.sin(elapsed * 5));
    for (let idx = 0; idx < plazaVaultRings.length; idx++) {
      const ringMat = plazaVaultRings[idx];
      if (sim.vaults[idx]?.faulted) {
        ringMat.color.setHex(0xef4444);
        ringMat.emissive.setHex(0xef4444);
        ringMat.emissiveIntensity = 0.8 + 0.9 * faultSin;
      } else {
        ringMat.color.setHex(0x22d3ee);
        ringMat.emissive.setHex(0x06b6d4);
        ringMat.emissiveIntensity = 0.75 + nightFactor * 0.55;
      }
    }

    // Rooftop PV shimmer
    const pvFrac = Math.min(1, Math.max(0, sim.electrical.pvOutputMw / 12.0));
    pvMat.emissiveIntensity = 0.06 + pvFrac * 0.85;

    // Multi-bar BESS SOC LEDs
    const isDischarging = sim.electrical.bessDischargeMw > 0.05;
    const isCharging = sim.electrical.bessChargeMw > 0.05;
    const activeBars = isDischarging ? 4 : isCharging ? 5 : 3;
    const pulseBar = Math.floor(elapsed * 2.5) % activeBars;

    for (let tier = 0; tier < 5; tier++) {
      const mat = bessBarMats[tier];
      if (tier >= activeBars) {
        mat.color.setHex(0x334155);
        mat.emissive.setHex(0x1e293b);
        mat.emissiveIntensity = 0.25;
      } else if (isDischarging) {
        mat.color.setHex(0x10b981);
        mat.emissive.setHex(0x10b981);
        mat.emissiveIntensity = tier === activeBars - 1 - (pulseBar % activeBars) ? 1.85 : 1.15;
      } else if (isCharging) {
        mat.color.setHex(0x22d3ee);
        mat.emissive.setHex(0x06b6d4);
        mat.emissiveIntensity = tier === pulseBar ? 1.9 : 1.1;
      } else {
        mat.color.setHex(0x64748b);
        mat.emissive.setHex(0x38bdf8);
        mat.emissiveIntensity = 0.35;
      }
    }

    // Check if any healthy vault exists (zero-allocation loop)
    let hasHealthyVault = false;
    for (let v = 0; v < vaultCount; v++) {
      if (!sim.vaults[v]?.faulted) {
        hasHealthyVault = true;
        break;
      }
    }

    // Animate Autonomous 6G V2X Pods + Dual-Glow Optical Beams
    for (let idx = 0; idx < podCount; idx++) {
      const p = pods[idx];
      const ang = p.baseAngle + elapsed * p.speed;
      const cosA = Math.cos(ang);
      const sinA = Math.sin(ang);
      const px = cosA * p.ringRadius;
      const pz = sinA * p.ringRadius;

      const dx = -sinA * p.speed;
      const dz = cosA * p.speed;
      SCRATCH_DUMMY.position.set(px, 0.06, pz);
      SCRATCH_DUMMY.rotation.set(0, Math.atan2(dx, dz), 0);
      SCRATCH_DUMMY.scale.set(1, 1, 1);
      SCRATCH_DUMMY.updateMatrix();

      for (let m = 0; m < podMeshes.length; m++) {
        podMeshes[m].setMatrixAt(idx, SCRATCH_DUMMY.matrix);
      }

      // Find nearest healthy vault with zero array allocations
      let nearest = vaults[0];
      let bestDistSq = Infinity;
      for (let v = 0; v < vaultCount; v++) {
        if (hasHealthyVault && sim.vaults[v]?.faulted) continue;
        const vp = vaults[v];
        const ddx = px - vp.x;
        const ddz = pz - vp.z;
        const dSq = ddx * ddx + ddz * ddz;
        if (dSq < bestDistSq) {
          bestDistSq = dSq;
          nearest = vp;
        }
      }

      const startY = 1.06;
      const endY = nearest.y + 0.6;
      const offset = idx * 6;
      v2xCorePositions[offset] = px;
      v2xCorePositions[offset + 1] = startY;
      v2xCorePositions[offset + 2] = pz;
      v2xCorePositions[offset + 3] = nearest.x;
      v2xCorePositions[offset + 4] = endY;
      v2xCorePositions[offset + 5] = nearest.z;

      v2xAuraPositions[offset] = px;
      v2xAuraPositions[offset + 1] = startY + 0.14;
      v2xAuraPositions[offset + 2] = pz;
      v2xAuraPositions[offset + 3] = nearest.x;
      v2xAuraPositions[offset + 4] = endY + 0.22;
      v2xAuraPositions[offset + 5] = nearest.z;

      const tDown = (elapsed * 1.15 + idx * 0.19) % 1;
      const tUp = 1 - ((elapsed * 1.35 + idx * 0.31) % 1);
      const spanX = nearest.x - px;
      const spanY = endY - startY;
      const spanZ = nearest.z - pz;

      SCRATCH_DUMMY.rotation.set(0, 0, 0);
      SCRATCH_DUMMY.scale.setScalar(1.0);
      SCRATCH_DUMMY.position.set(px + spanX * tDown, startY + spanY * tDown, pz + spanZ * tDown);
      SCRATCH_DUMMY.updateMatrix();
      v2xPulseMesh.setMatrixAt(idx * 2, SCRATCH_DUMMY.matrix);

      SCRATCH_DUMMY.scale.setScalar(0.8);
      SCRATCH_DUMMY.position.set(px + spanX * tUp, startY + spanY * tUp, pz + spanZ * tUp);
      SCRATCH_DUMMY.updateMatrix();
      v2xPulseMesh.setMatrixAt(idx * 2 + 1, SCRATCH_DUMMY.matrix);
    }

    for (let m = 0; m < podMeshes.length; m++) {
      podMeshes[m].instanceMatrix.needsUpdate = true;
    }
    v2xCoreGeom.attributes.position.needsUpdate = true;
    v2xAuraGeom.attributes.position.needsUpdate = true;
    v2xPulseMesh.instanceMatrix.needsUpdate = true;
  }

  return { group, setHeatmapVisible, update };
}
