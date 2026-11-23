import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { WORLD_LAYOUT } from './scene.js';

function tagInspectable(obj, key) {
  obj.userData.inspectKey = key;
  return obj;
}

// ---------------------------------------------------------------------------
// Geometry Merging / Static Batcher Helpers (slash draw calls, zero leaks)
// ---------------------------------------------------------------------------

const _mat4 = new THREE.Matrix4();
const _pos = new THREE.Vector3();
const _quat = new THREE.Quaternion();
const _euler = new THREE.Euler();
const _scale = new THREE.Vector3();
const _up = new THREE.Vector3(0, 1, 0);
const _dir = new THREE.Vector3();
const _mid = new THREE.Vector3();

function mergeNormalized(geoms) {
  if (geoms.length === 1) return geoms[0];
  let allIndexed = true;
  for (let i = 0; i < geoms.length; i++) {
    if (!geoms[i].index) {
      allIndexed = false;
      break;
    }
  }
  const prepared = allIndexed
    ? geoms
    : geoms.map((g) => (g.index ? g.toNonIndexed() : g));
  const merged = mergeGeometries(prepared, false);
  for (let i = 0; i < geoms.length; i++) geoms[i].dispose();
  if (!allIndexed) {
    for (let i = 0; i < prepared.length; i++) {
      if (prepared[i] !== geoms[i]) prepared[i].dispose();
    }
  }
  if (!merged) {
    throw new Error('mergeGeometries returned null in vaults.js');
  }
  return merged;
}

function createStaticBatcher() {
  const buckets = new Map();

  function add(sourceGeom, mat, opts = {}) {
    const renderOrder = opts.renderOrder || 0;
    const key = `${mat.uuid}|${renderOrder}`;
    let bucket = buckets.get(key);
    if (!bucket) {
      bucket = {
        mat,
        renderOrder,
        castShadow: false,
        receiveShadow: false,
        geoms: [],
      };
      buckets.set(key, bucket);
    }
    if (opts.castShadow) bucket.castShadow = true;
    if (opts.receiveShadow) bucket.receiveShadow = true;

    const g = sourceGeom.clone();
    if (opts.uvRepeat) {
      const uv = g.attributes.uv;
      if (uv) {
        const [rx, ry] = opts.uvRepeat;
        for (let i = 0; i < uv.count; i++) {
          uv.setXY(i, uv.getX(i) * rx, uv.getY(i) * ry);
        }
        uv.needsUpdate = true;
      }
    }

    if (opts.matrix) {
      g.applyMatrix4(opts.matrix);
    } else {
      const p = opts.pos || [0, 0, 0];
      const r = opts.rot || [0, 0, 0];
      const s = opts.scale || [1, 1, 1];
      _pos.set(p[0], p[1], p[2]);
      _euler.set(r[0], r[1], r[2]);
      _quat.setFromEuler(_euler);
      _scale.set(s[0], s[1], s[2]);
      _mat4.compose(_pos, _quat, _scale);
      g.applyMatrix4(_mat4);
    }
    bucket.geoms.push(g);
  }

  function addPipe(p1, p2, radius, mat, radialSegments = 8) {
    _dir.subVectors(p2, p1);
    const len = _dir.length();
    if (len < 1e-4) return;
    _mid.copy(p1).add(p2).multiplyScalar(0.5);
    _quat.setFromUnitVectors(_up, _dir.normalize());
    _scale.set(1, 1, 1);
    _mat4.compose(_mid, _quat, _scale);
    const cyl = new THREE.CylinderGeometry(radius, radius, len, radialSegments);
    add(cyl, mat, { matrix: _mat4 });
    cyl.dispose();
  }

  function buildTemplates() {
    const out = [];
    for (const bucket of buckets.values()) {
      out.push({
        geom: mergeNormalized(bucket.geoms),
        mat: bucket.mat,
        renderOrder: bucket.renderOrder,
        castShadow: bucket.castShadow,
        receiveShadow: bucket.receiveShadow,
      });
    }
    buckets.clear();
    return out;
  }

  function flushInto(targetGroup) {
    const templates = buildTemplates();
    for (let i = 0; i < templates.length; i++) {
      const t = templates[i];
      const mesh = new THREE.Mesh(t.geom, t.mat);
      if (t.renderOrder) mesh.renderOrder = t.renderOrder;
      if (t.castShadow) mesh.castShadow = true;
      if (t.receiveShadow) mesh.receiveShadow = true;
      targetGroup.add(mesh);
    }
    return templates.length;
  }

  return { add, addPipe, buildTemplates, flushInto };
}

function buildMergedFanGeometry(bladeCount, radius) {
  const geoms = [];
  const hub = new THREE.CylinderGeometry(radius * 0.2, radius * 0.24, 0.14, 12);
  geoms.push(hub);

  const bladeSrc = new THREE.BoxGeometry(radius * 0.82, 0.035, radius * 0.24);
  const rotY = new THREE.Matrix4();
  const localMat = new THREE.Matrix4();
  const finalMat = new THREE.Matrix4();

  _pos.set(radius * 0.48, 0, 0);
  _euler.set(0.28, 0, 0); // aerodynamic pitch
  _quat.setFromEuler(_euler);
  _scale.set(1, 1, 1);
  localMat.compose(_pos, _quat, _scale);

  for (let i = 0; i < bladeCount; i++) {
    const angle = (i / bladeCount) * Math.PI * 2;
    rotY.makeRotationY(angle);
    finalMat.multiplyMatrices(rotY, localMat);
    const b = bladeSrc.clone();
    b.applyMatrix4(finalMat);
    geoms.push(b);
  }
  bladeSrc.dispose();
  return mergeNormalized(geoms);
}

// ---------------------------------------------------------------------------
// Procedural Canvas PBR Texture Generators (right-sized <= 512, zero external files)
// ---------------------------------------------------------------------------

function makeCanvas(width, height) {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  return { canvas, ctx: canvas.getContext('2d') };
}

function finalizeTexture(canvas, { repeatX = 1, repeatY = 1, srgb = true } = {}) {
  const tex = new THREE.CanvasTexture(canvas);
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  tex.repeat.set(repeatX, repeatY);
  tex.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
  tex.anisotropy = 4;
  tex.needsUpdate = true;
  return tex;
}

function createConcreteTextures() {
  const size = 256;
  const { canvas: cColor, ctx: ctxC } = makeCanvas(size, size);
  const { canvas: cBump, ctx: ctxB } = makeCanvas(size, size);

  ctxC.fillStyle = '#364558';
  ctxC.fillRect(0, 0, size, size);
  ctxB.fillStyle = '#808080';
  ctxB.fillRect(0, 0, size, size);

  const cols = 4;
  const rows = 2;
  const pw = size / cols;
  const ph = size / rows;
  const panelShades = [
    '#39495d', '#334254', '#3b4b60', '#354457',
    '#344355', '#3a4a5e', '#324153', '#38485c',
  ];

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      ctxC.fillStyle = panelShades[r * cols + c];
      ctxC.fillRect(c * pw + 1, r * ph + 1, pw - 2, ph - 2);
    }
  }

  let seed = 1337;
  const rand = () => {
    seed = (seed * 16807) % 2147483647;
    return (seed - 1) / 2147483646;
  };
  for (let i = 0; i < 1800; i++) {
    const x = Math.floor(rand() * size);
    const y = Math.floor(rand() * size);
    const v = rand() > 0.5 ? 255 : 0;
    const a = 0.025 + rand() * 0.04;
    ctxC.fillStyle = `rgba(${v},${v},${v},${a.toFixed(3)})`;
    ctxC.fillRect(x, y, 2, 2);
    const bv = Math.floor(116 + rand() * 24);
    ctxB.fillStyle = `rgb(${bv},${bv},${bv})`;
    ctxB.fillRect(x, y, 2, 2);
  }

  ctxC.strokeStyle = '#1b2533';
  ctxC.lineWidth = 2;
  ctxB.strokeStyle = '#262626';
  ctxB.lineWidth = 2.5;

  for (let c = 0; c <= cols; c++) {
    const x = c * pw;
    ctxC.beginPath();
    ctxC.moveTo(x, 0);
    ctxC.lineTo(x, size);
    ctxC.stroke();
    ctxB.beginPath();
    ctxB.moveTo(x, 0);
    ctxB.lineTo(x, size);
    ctxB.stroke();
  }
  for (let r = 0; r <= rows; r++) {
    const y = r * ph;
    ctxC.beginPath();
    ctxC.moveTo(0, y);
    ctxC.lineTo(size, y);
    ctxC.stroke();
    ctxB.beginPath();
    ctxB.moveTo(0, y);
    ctxB.lineTo(size, y);
    ctxB.stroke();
  }

  ctxC.strokeStyle = 'rgba(148,163,184,0.16)';
  ctxC.lineWidth = 1;
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      ctxC.strokeRect(c * pw + 1.5, r * ph + 1.5, pw - 3, ph - 3);
    }
  }

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const xs = [c * pw + 11, (c + 1) * pw - 11];
      const ys = [r * ph + 16, (r + 1) * ph - 16];
      for (const tx of xs) {
        for (const ty of ys) {
          ctxC.fillStyle = '#16202e';
          ctxC.beginPath();
          ctxC.arc(tx, ty, 2.2, 0, Math.PI * 2);
          ctxC.fill();
          ctxC.strokeStyle = 'rgba(148,163,184,0.28)';
          ctxC.lineWidth = 0.8;
          ctxC.stroke();

          ctxB.fillStyle = '#1e1e1e';
          ctxB.beginPath();
          ctxB.arc(tx, ty, 2.5, 0, Math.PI * 2);
          ctxB.fill();
        }
      }
    }
  }

  return {
    map: finalizeTexture(cColor, { repeatX: 2, repeatY: 1, srgb: true }),
    bumpMap: finalizeTexture(cBump, { repeatX: 2, repeatY: 1, srgb: false }),
  };
}

function createHazardStripeTexture() {
  const size = 128;
  const { canvas, ctx } = makeCanvas(size, size);
  ctx.fillStyle = '#f59e0b';
  ctx.fillRect(0, 0, size, size);

  ctx.fillStyle = '#111827';
  const stripeW = 16;
  for (let offset = -size; offset < size * 2; offset += stripeW * 2) {
    ctx.beginPath();
    ctx.moveTo(offset, 0);
    ctx.lineTo(offset + stripeW, 0);
    ctx.lineTo(offset + stripeW - size, size);
    ctx.lineTo(offset - size, size);
    ctx.closePath();
    ctx.fill();
  }

  ctx.fillStyle = 'rgba(255,255,255,0.06)';
  for (let y = 0; y < size; y += 4) {
    ctx.fillRect(0, y, size, 1);
  }
  ctx.fillStyle = '#0b1120';
  ctx.fillRect(0, 0, size, 5);
  ctx.fillRect(0, size - 5, size, 5);

  return finalizeTexture(canvas, { repeatX: 1, repeatY: 1, srgb: true });
}

function createServerRackTextures() {
  const w = 256;
  const h = 512;
  const { canvas: cColor, ctx: ctxC } = makeCanvas(w, h);
  const { canvas: cEmissive, ctx: ctxE } = makeCanvas(w, h);

  ctxC.fillStyle = '#0c1322';
  ctxC.fillRect(0, 0, w, h);
  ctxE.fillStyle = '#000000';
  ctxE.fillRect(0, 0, w, h);

  ctxC.strokeStyle = '#334155';
  ctxC.lineWidth = 6;
  ctxC.strokeRect(5, 5, w - 10, h - 10);

  ctxC.fillStyle = '#152033';
  ctxC.fillRect(14, 14, w - 28, 28);
  ctxE.fillStyle = '#ffffff';
  ctxE.fillRect(26, 24, 90, 6);
  ctxE.fillStyle = '#a5f3fc';
  ctxE.fillRect(w - 64, 24, 34, 6);

  const trays = 10;
  const topOffset = 52;
  const trayH = 44;

  for (let i = 0; i < trays; i++) {
    const y = topOffset + i * trayH;

    ctxC.fillStyle = i % 2 === 0 ? '#111c2f' : '#0f1829';
    ctxC.fillRect(16, y + 2, w - 32, trayH - 6);

    ctxC.fillStyle = '#26354d';
    ctxC.fillRect(14, y, w - 28, 2);

    ctxC.fillStyle = '#050912';
    for (let px = 26; px < w - 74; px += 8) {
      for (let py = y + 8; py < y + trayH - 10; py += 7) {
        ctxC.fillRect(px, py, 4, 4);
      }
    }

    ctxC.fillStyle = '#384b66';
    ctxC.fillRect(18, y + 6, 4, trayH - 14);
    ctxC.fillRect(w - 22, y + 6, 4, trayH - 14);

    const ledY = y + 14;
    ctxC.fillStyle = '#1e293b';
    ctxC.fillRect(w - 66, ledY - 3, 38, 16);

    ctxE.fillStyle = '#ffffff';
    ctxE.fillRect(w - 62, ledY, 6, 4);
    ctxE.fillRect(w - 52, ledY, 6, 4);
    ctxE.fillStyle = i % 3 === 0 ? '#ffffff' : '#94a3b8';
    ctxE.fillRect(w - 42, ledY, 10, 4);

    const barW = 14 + ((i * 7) % 18);
    ctxE.fillStyle = '#d4d4d8';
    ctxE.fillRect(w - 62, ledY + 7, barW, 3);
  }

  return {
    map: finalizeTexture(cColor, { repeatX: 1, repeatY: 1, srgb: true }),
    emissiveMap: finalizeTexture(cEmissive, { repeatX: 1, repeatY: 1, srgb: true }),
  };
}

function createBrushedMetalTexture() {
  const size = 128;
  const { canvas, ctx } = makeCanvas(size, size);
  ctx.fillStyle = '#94a3b8';
  ctx.fillRect(0, 0, size, size);

  let seed = 4099;
  const rand = () => {
    seed = (seed * 16807) % 2147483647;
    return (seed - 1) / 2147483646;
  };
  for (let y = 0; y < size; y++) {
    const shade = Math.floor(132 + rand() * 34);
    ctx.fillStyle = `rgb(${shade},${shade + 6},${shade + 14})`;
    ctx.fillRect(0, y, size, 1);
  }
  ctx.fillStyle = 'rgba(15,23,42,0.35)';
  ctx.fillRect(0, size / 2 - 1, size, 2);
  for (let x = 8; x < size; x += 16) {
    ctx.beginPath();
    ctx.arc(x, size / 2 - 4, 1.4, 0, Math.PI * 2);
    ctx.arc(x, size / 2 + 4, 1.4, 0, Math.PI * 2);
    ctx.fill();
  }
  return finalizeTexture(canvas, { repeatX: 2, repeatY: 1, srgb: true });
}

function createHeatExchangerFinTextures() {
  const size = 128;
  const { canvas: cColor, ctx: ctxC } = makeCanvas(size, size);
  const { canvas: cBump, ctx: ctxB } = makeCanvas(size, size);

  ctxC.fillStyle = '#1e293b';
  ctxC.fillRect(0, 0, size, size);
  ctxB.fillStyle = '#202020';
  ctxB.fillRect(0, 0, size, size);

  for (let y = 0; y < size; y += 4) {
    ctxC.fillStyle = '#64748b';
    ctxC.fillRect(0, y, size, 2);
    ctxC.fillStyle = '#94a3b8';
    ctxC.fillRect(0, y, size, 1);
    ctxC.fillStyle = '#0f172a';
    ctxC.fillRect(0, y + 2, size, 2);

    ctxB.fillStyle = '#e5e5e5';
    ctxB.fillRect(0, y, size, 2);
    ctxB.fillStyle = '#181818';
    ctxB.fillRect(0, y + 2, size, 2);
  }

  ctxC.fillStyle = '#334155';
  ctxC.fillRect(0, 0, 4, size);
  ctxC.fillRect(size / 2 - 2, 0, 4, size);
  ctxC.fillRect(size - 4, 0, 4, size);

  return {
    map: finalizeTexture(cColor, { repeatX: 1, repeatY: 1, srgb: true }),
    bumpMap: finalizeTexture(cBump, { repeatX: 1, repeatY: 1, srgb: false }),
  };
}

function createThermalStratificationTexture() {
  const w = 64;
  const h = 128;
  const { canvas, ctx } = makeCanvas(w, h);

  const grad = ctx.createLinearGradient(0, 0, 0, h);
  grad.addColorStop(0.0, '#ff3b14');
  grad.addColorStop(0.28, '#ff6824');
  grad.addColorStop(0.50, '#f59e0b');
  grad.addColorStop(0.68, '#14b8a6');
  grad.addColorStop(1.0, '#0e7490');
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, w, h);

  for (let y = 6; y < h; y += 8) {
    ctx.fillStyle = 'rgba(255,255,255,0.09)';
    ctx.fillRect(0, y, w, 1);
  }

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.ClampToEdgeWrapping;
  tex.anisotropy = 4;
  tex.needsUpdate = true;
  return tex;
}

function createWaterCausticTextures() {
  const size = 128;
  const { canvas: cColor, ctx: ctxC } = makeCanvas(size, size);
  const { canvas: cBump, ctx: ctxB } = makeCanvas(size, size);

  const imgC = ctxC.createImageData(size, size);
  const imgB = ctxB.createImageData(size, size);
  const tau = Math.PI * 2;

  for (let y = 0; y < size; y++) {
    const ny = (y / size) * tau;
    for (let x = 0; x < size; x++) {
      const nx = (x / size) * tau;
      const w1 = Math.sin(nx * 3 + Math.cos(ny * 2));
      const w2 = Math.cos(ny * 4 - Math.sin(nx * 3));
      const w3 = Math.sin((nx + ny) * 2.0) * 0.6;
      const ridge = 1.0 - Math.min(1.0, Math.abs(w1 + w2 + w3) * 0.48);
      const caustic = Math.pow(Math.max(0, ridge), 2.4);

      const idx = (y * size + x) * 4;
      imgC.data[idx] = Math.min(255, Math.floor(14 + caustic * 150));
      imgC.data[idx + 1] = Math.min(255, Math.floor(138 + caustic * 115));
      imgC.data[idx + 2] = Math.min(255, Math.floor(205 + caustic * 50));
      imgC.data[idx + 3] = 255;

      const bVal = Math.min(255, Math.floor(105 + caustic * 140));
      imgB.data[idx] = bVal;
      imgB.data[idx + 1] = bVal;
      imgB.data[idx + 2] = bVal;
      imgB.data[idx + 3] = 255;
    }
  }
  ctxC.putImageData(imgC, 0, 0);
  ctxB.putImageData(imgB, 0, 0);

  return {
    map: finalizeTexture(cColor, { repeatX: 2.5, repeatY: 2.0, srgb: true }),
    bumpMap: finalizeTexture(cBump, { repeatX: 2.5, repeatY: 2.0, srgb: false }),
  };
}

function createCisternGaugeTexture() {
  const w = 256;
  const h = 256;
  const { canvas, ctx } = makeCanvas(w, h);
  ctx.clearRect(0, 0, w, h);

  ctx.strokeStyle = 'rgba(125,211,252,0.72)';
  ctx.fillStyle = 'rgba(186,230,253,0.92)';
  ctx.font = 'bold 13px monospace';

  const marks = [
    { pct: 100, label: '10,000 m³' },
    { pct: 80, label: '80%' },
    { pct: 60, label: '60%' },
    { pct: 40, label: '40%' },
    { pct: 20, label: '20%' },
  ];

  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(16, 16);
  ctx.lineTo(16, h - 16);
  ctx.stroke();

  for (let p = 10; p <= 100; p += 10) {
    const y = h - 18 - (p / 100) * (h - 36);
    const major = p % 20 === 0;
    ctx.beginPath();
    ctx.moveTo(16, y);
    ctx.lineTo(major ? 28 : 22, y);
    ctx.stroke();

    if (major) {
      ctx.save();
      ctx.strokeStyle = 'rgba(56,189,248,0.18)';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(30, y);
      ctx.lineTo(w - 12, y);
      ctx.stroke();
      ctx.restore();
    }
  }

  for (const m of marks) {
    const y = h - 18 - (m.pct / 100) * (h - 36);
    ctx.fillText(m.label, 32, y + 4);
  }

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 4;
  tex.needsUpdate = true;
  return tex;
}

function createHubFloorTexture() {
  const size = 512;
  const { canvas, ctx } = makeCanvas(size, size);
  const cx = size / 2;
  const cy = size / 2;

  ctx.fillStyle = '#192334';
  ctx.fillRect(0, 0, size, size);

  ctx.strokeStyle = 'rgba(56,189,248,0.14)';
  ctx.lineWidth = 2;
  for (const r of [72, 148, 212, 240]) {
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.stroke();
  }

  ctx.strokeStyle = 'rgba(148,163,184,0.12)';
  ctx.lineWidth = 1.5;
  for (let i = 0; i < 12; i++) {
    const a = (i / 12) * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(cx + Math.cos(a) * 48, cy + Math.sin(a) * 48);
    ctx.lineTo(cx + Math.cos(a) * 246, cy + Math.sin(a) * 246);
    ctx.stroke();
  }

  ctx.strokeStyle = 'rgba(245,158,11,0.45)';
  ctx.lineWidth = 5;
  ctx.setLineDash([14, 10]);
  ctx.beginPath();
  ctx.arc(cx, cy, 246, 0, Math.PI * 2);
  ctx.stroke();
  ctx.setLineDash([]);

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 4;
  tex.needsUpdate = true;
  return tex;
}

// ---------------------------------------------------------------------------
// Main Export
// ---------------------------------------------------------------------------

export function createVaults(scene) {
  const group = new THREE.Group();
  group.name = 'subterranean_layer';
  scene.add(group);

  // Shared procedural PBR textures (created ONCE)
  const concreteTex = createConcreteTextures();
  const hazardTex = createHazardStripeTexture();
  const rackTex = createServerRackTextures();
  const brushedSteelTex = createBrushedMetalTexture();
  const finTex = createHeatExchangerFinTextures();
  const thermalStratTex = createThermalStratificationTexture();
  const waterCausticTex = createWaterCausticTextures();
  const cisternGaugeTex = createCisternGaugeTexture();
  const hubFloorTex = createHubFloorTexture();

  // Shared PBR Materials calibrated for IBL (RoomEnvironment) + UnrealBloomPass
  const concreteMat = new THREE.MeshStandardMaterial({
    color: 0x9fb0c7,
    map: concreteTex.map,
    bumpMap: concreteTex.bumpMap,
    bumpScale: 0.045,
    roughness: 0.84,
    metalness: 0.08,
  });

  const slabMat = new THREE.MeshStandardMaterial({
    color: 0x334155,
    map: concreteTex.map,
    bumpMap: concreteTex.bumpMap,
    bumpScale: 0.025,
    roughness: 0.72,
    metalness: 0.22,
  });

  const darkSteelMat = new THREE.MeshStandardMaterial({
    color: 0x1e293b,
    roughness: 0.42,
    metalness: 0.78,
  });

  const brushedSteelMat = new THREE.MeshStandardMaterial({
    color: 0xcbd5e1,
    map: brushedSteelTex,
    roughness: 0.30,
    metalness: 0.85,
  });

  const brushedAlumMat = new THREE.MeshStandardMaterial({
    color: 0xe2e8f0,
    map: brushedSteelTex,
    roughness: 0.26,
    metalness: 0.88,
  });

  const finCoilMat = new THREE.MeshStandardMaterial({
    color: 0xb8c7d9,
    map: finTex.map,
    bumpMap: finTex.bumpMap,
    bumpScale: 0.06,
    roughness: 0.42,
    metalness: 0.72,
  });

  const trenchMat = new THREE.MeshStandardMaterial({
    color: 0x070c16,
    roughness: 0.96,
    metalness: 0.05,
  });

  const hazardMat = new THREE.MeshStandardMaterial({
    map: hazardTex,
    roughness: 0.48,
    metalness: 0.15,
  });

  const elastomerMat = new THREE.MeshStandardMaterial({
    color: 0xf97316,
    roughness: 0.65,
    metalness: 0.1,
  });

  const rackBodyMat = new THREE.MeshStandardMaterial({
    color: 0x111827,
    roughness: 0.40,
    metalness: 0.72,
  });

  const coolPipeMat = new THREE.MeshStandardMaterial({
    color: 0x38bdf8,
    emissive: 0x0369a1,
    emissiveIntensity: 0.35,
    roughness: 0.25,
    metalness: 0.78,
  });

  const brassMat = new THREE.MeshStandardMaterial({
    color: 0xd97706,
    roughness: 0.32,
    metalness: 0.82,
  });

  const glassTankMat = new THREE.MeshPhysicalMaterial({
    color: 0xcbe4ff,
    roughness: 0.08,
    metalness: 0.04,
    clearcoat: 1.0,
    clearcoatRoughness: 0.06,
    transparent: true,
    opacity: 0.26,
    depthWrite: false,
  });

  const glassDoubleMat = new THREE.MeshPhysicalMaterial({
    color: 0xdbeafe,
    roughness: 0.08,
    metalness: 0.04,
    clearcoat: 1.0,
    clearcoatRoughness: 0.06,
    transparent: true,
    opacity: 0.28,
    side: THREE.DoubleSide,
    depthWrite: false,
  });

  const coveLightMat = new THREE.MeshStandardMaterial({
    color: 0xe0f2fe,
    emissive: 0x7dd3fc,
    emissiveIntensity: 1.15,
    roughness: 0.2,
  });

  const insulatorMat = new THREE.MeshPhysicalMaterial({
    color: 0x7dd3fc,
    roughness: 0.14,
    metalness: 0.1,
    clearcoat: 1.0,
  });

  const shaftGlassMat = new THREE.MeshPhysicalMaterial({
    color: 0x64748b,
    roughness: 0.22,
    metalness: 0.2,
    clearcoat: 0.8,
    transparent: true,
    opacity: 0.26,
    depthWrite: false,
  });

  const fanBladeMat = new THREE.MeshStandardMaterial({
    color: 0xcbd5e1,
    roughness: 0.28,
    metalness: 0.84,
  });

  // =========================================================================
  // 1. SHARED MERGED VAULT GEOMETRY TEMPLATES (built ONCE, shared by all 5 vaults)
  // =========================================================================
  const vaultStaticBatcher = createStaticBatcher();
  const dynRackDoorsBatcher = createStaticBatcher();
  const dynHotBatcher = createStaticBatcher();
  const dynHtsBatcher = createStaticBatcher();
  const dynFluidBatcher = createStaticBatcher();
  const dummyDynMat = new THREE.MeshBasicMaterial();

  const tempGeoms = [];
  const track = (g) => {
    tempGeoms.push(g);
    return g;
  };

  // --- 1.0 m Perimeter Vibration-Isolation Trench ---
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(11.9, 0.26, 9.9)), trenchMat, { pos: [0, -3.86, 0] });

  const curbFB = track(new THREE.BoxGeometry(11.9, 0.42, 0.38));
  const curbLR = track(new THREE.BoxGeometry(0.38, 0.42, 9.14));
  vaultStaticBatcher.add(curbFB, concreteMat, { pos: [0, -3.56, 4.76] });
  vaultStaticBatcher.add(curbFB, concreteMat, { pos: [0, -3.56, -4.76] });
  vaultStaticBatcher.add(curbLR, concreteMat, { pos: [-5.76, -3.56, 0] });
  vaultStaticBatcher.add(curbLR, concreteMat, { pos: [5.76, -3.56, 0] });

  // Yellow/black industrial safety striping along trench rim (single merged hazardMat)
  const trimFB = track(new THREE.BoxGeometry(11.92, 0.05, 0.34));
  const trimLR = track(new THREE.BoxGeometry(0.34, 0.05, 9.12));
  vaultStaticBatcher.add(trimFB, hazardMat, { pos: [0, -3.33, 4.76], uvRepeat: [9, 1] });
  vaultStaticBatcher.add(trimFB, hazardMat, { pos: [0, -3.33, -4.76], uvRepeat: [9, 1] });
  vaultStaticBatcher.add(trimLR, hazardMat, { pos: [-5.76, -3.33, 0], uvRepeat: [1, 7.5] });
  vaultStaticBatcher.add(trimLR, hazardMat, { pos: [5.76, -3.33, 0], uvRepeat: [1, 7.5] });

  // Visible Spring / Elastomer Acoustic Isolators beneath floating RC slab
  const isolatorBaseGeom = track(new THREE.CylinderGeometry(0.22, 0.25, 0.08, 10));
  const isolatorSpringGeom = track(new THREE.CylinderGeometry(0.14, 0.14, 0.22, 10));
  const isolatorPadGeom = track(new THREE.CylinderGeometry(0.19, 0.19, 0.08, 10));
  const isolatorCoords = [
    [-4.2, 3.3], [-1.4, 3.3], [1.4, 3.3], [4.2, 3.3],
    [-4.2, -3.3], [-1.4, -3.3], [1.4, -3.3], [4.2, -3.3],
    [-4.2, 0], [4.2, 0],
  ];
  for (const [ix, iz] of isolatorCoords) {
    vaultStaticBatcher.add(isolatorBaseGeom, darkSteelMat, { pos: [ix, -3.69, iz] });
    vaultStaticBatcher.add(isolatorSpringGeom, brushedSteelMat, { pos: [ix, -3.54, iz] });
    vaultStaticBatcher.add(isolatorPadGeom, elastomerMat, { pos: [ix, -3.40, iz] });
  }

  // Floating Acoustic RC Slab + steel edge trim
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(9.8, 0.38, 7.8)), slabMat, {
    pos: [0, -3.18, 0],
    receiveShadow: true,
  });
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(9.86, 0.14, 0.08)), darkSteelMat, { pos: [0, -3.12, 3.91] });

  // Structural Ribbed Concrete Walls + Buttress Pilasters
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(9.8, 3.8, 0.34)), concreteMat, {
    pos: [0, -1.09, -3.73],
    castShadow: true,
    receiveShadow: true,
  });
  const sideWallGeom = track(new THREE.BoxGeometry(0.34, 3.8, 7.8));
  vaultStaticBatcher.add(sideWallGeom, concreteMat, {
    pos: [-4.73, -1.09, 0],
    castShadow: true,
    receiveShadow: true,
  });
  vaultStaticBatcher.add(sideWallGeom, concreteMat, {
    pos: [4.73, -1.09, 0],
    castShadow: true,
    receiveShadow: true,
  });

  const pilasterBackGeom = track(new THREE.BoxGeometry(0.32, 3.82, 0.28));
  const pilasterSideGeom = track(new THREE.BoxGeometry(0.28, 3.82, 0.32));
  for (const px of [-3.6, -1.8, 0, 1.8, 3.6]) {
    vaultStaticBatcher.add(pilasterBackGeom, concreteMat, { pos: [px, -1.09, -3.50] });
  }
  for (const pz of [-2.4, 0, 2.4]) {
    vaultStaticBatcher.add(pilasterSideGeom, concreteMat, { pos: [-4.50, -1.09, pz] });
    vaultStaticBatcher.add(pilasterSideGeom, concreteMat, { pos: [4.50, -1.09, pz] });
  }

  // Steel Cap Beams & Overhead Gantry Tie Beams
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(9.92, 0.16, 0.46)), darkSteelMat, { pos: [0, 0.85, -3.73] });
  const capSideGeom = track(new THREE.BoxGeometry(0.46, 0.16, 7.92));
  vaultStaticBatcher.add(capSideGeom, darkSteelMat, { pos: [-4.73, 0.85, 0] });
  vaultStaticBatcher.add(capSideGeom, darkSteelMat, { pos: [4.73, 0.85, 0] });

  const tieBeamGeom = track(new THREE.BoxGeometry(9.2, 0.12, 0.16));
  for (const tz of [-1.45, 1.15]) {
    vaultStaticBatcher.add(tieBeamGeom, darkSteelMat, { pos: [0, 0.82, tz] });
  }

  // Recessed architectural wall LED lighting coves
  vaultStaticBatcher.add(track(new THREE.BoxGeometry(8.8, 0.06, 0.06)), coveLightMat, { pos: [0, 0.62, -3.48] });
  const coveSideGeom = track(new THREE.BoxGeometry(0.06, 0.06, 6.8));
  vaultStaticBatcher.add(coveSideGeom, coveLightMat, { pos: [-4.48, 0.62, 0] });
  vaultStaticBatcher.add(coveSideGeom, coveLightMat, { pos: [4.48, 0.62, 0] });

  // 8 Liquid-Cooled AI Server Racks + Dual Overhead Manifolds + HTS Busways
  const rackCaseGeom = track(new THREE.BoxGeometry(0.96, 2.36, 1.08));
  const rackDoorGeom = track(new THREE.PlaneGeometry(0.88, 2.24));
  const rackTopTrimGeom = track(new THREE.BoxGeometry(0.98, 0.06, 1.12));
  const rowHeaderGeom = track(new THREE.CylinderGeometry(0.075, 0.075, 5.4, 10));
  const dropPipeGeom = track(new THREE.CylinderGeometry(0.032, 0.032, 0.36, 8));
  const couplingGeom = track(new THREE.CylinderGeometry(0.052, 0.052, 0.06, 8));
  const buswayGeom = track(new THREE.CylinderGeometry(0.095, 0.095, 6.2, 12));
  const htsClampGeom = track(new THREE.CylinderGeometry(0.145, 0.145, 0.12, 12));
  const hangerRodGeom = track(new THREE.CylinderGeometry(0.022, 0.022, 0.96, 6));
  const insulatorDiscGeom = track(new THREE.CylinderGeometry(0.085, 0.085, 0.06, 10));

  const rowZs = [-1.45, 1.15];
  const colXs = [-2.55, -1.15, 0.25, 1.65];

  for (const rz of rowZs) {
    dynHotBatcher.add(rowHeaderGeom, dummyDynMat, { pos: [-0.2, -0.36, rz - 0.18], rot: [0, 0, Math.PI / 2] });
    vaultStaticBatcher.add(rowHeaderGeom, coolPipeMat, { pos: [-0.2, -0.36, rz + 0.18], rot: [0, 0, Math.PI / 2] });

    for (const rx of colXs) {
      vaultStaticBatcher.add(rackCaseGeom, rackBodyMat, { pos: [rx, -1.81, rz], castShadow: true });
      vaultStaticBatcher.add(rackTopTrimGeom, darkSteelMat, { pos: [rx, -0.60, rz] });
      dynRackDoorsBatcher.add(rackDoorGeom, dummyDynMat, { pos: [rx, -1.81, rz + 0.545] });

      dynHotBatcher.add(dropPipeGeom, dummyDynMat, { pos: [rx - 0.12, -0.52, rz - 0.18] });
      vaultStaticBatcher.add(dropPipeGeom, coolPipeMat, { pos: [rx + 0.12, -0.52, rz + 0.18] });
      vaultStaticBatcher.add(couplingGeom, brushedSteelMat, { pos: [rx - 0.12, -0.61, rz - 0.18] });
      vaultStaticBatcher.add(couplingGeom, brushedSteelMat, { pos: [rx + 0.12, -0.61, rz + 0.18] });
    }

    dynHtsBatcher.add(buswayGeom, dummyDynMat, { pos: [-0.6, -0.10, rz], rot: [0, 0, Math.PI / 2] });

    for (const hx of [-2.9, -0.5, 1.8]) {
      vaultStaticBatcher.add(htsClampGeom, brushedSteelMat, { pos: [hx, -0.10, rz], rot: [0, 0, Math.PI / 2] });
      vaultStaticBatcher.add(hangerRodGeom, brushedSteelMat, { pos: [hx, 0.36, rz] });
      vaultStaticBatcher.add(insulatorDiscGeom, insulatorMat, { pos: [hx, 0.22, rz] });
      vaultStaticBatcher.add(insulatorDiscGeom, insulatorMat, { pos: [hx, 0.34, rz] });
    }
  }

  // Cross-manifold connecting both rows' hot headers into the 43.4 m³ buffer tank
  dynHotBatcher.add(track(new THREE.CylinderGeometry(0.085, 0.085, 2.75, 10)), dummyDynMat, {
    pos: [2.45, -0.36, -0.15],
    rot: [Math.PI / 2, 0, 0],
  });
  dynHotBatcher.add(track(new THREE.CylinderGeometry(0.085, 0.085, 1.05, 10)), dummyDynMat, {
    pos: [2.96, -0.36, -0.2],
    rot: [0, 0, Math.PI / 2],
  });

  // 800 VDC SST Power Cabinets (Left Wall)
  const sstCabGeom = track(new THREE.BoxGeometry(0.72, 2.28, 1.45));
  const sstLouverGeom = track(new THREE.PlaneGeometry(1.18, 1.65));
  const sstLedGeom = track(new THREE.BoxGeometry(0.05, 0.14, 1.15));
  const sstRiserGeom = track(new THREE.CylinderGeometry(0.075, 0.075, 0.65, 10));
  for (const sz of [-1.45, 1.15]) {
    vaultStaticBatcher.add(sstCabGeom, brushedSteelMat, { pos: [-3.98, -1.85, sz] });
    vaultStaticBatcher.add(sstLouverGeom, finCoilMat, { pos: [-3.61, -1.92, sz], rot: [0, Math.PI / 2, 0] });
    dynHtsBatcher.add(sstLedGeom, dummyDynMat, { pos: [-3.60, -0.92, sz] });
    dynHtsBatcher.add(sstRiserGeom, dummyDynMat, { pos: [-3.75, -0.40, sz] });
  }

  // Detailed 43.4 m³ 15-min Cylindrical Thermal Buffer Tank
  const tankX = 3.55;
  const tankZ = -0.2;
  vaultStaticBatcher.add(track(new THREE.CylinderGeometry(1.02, 1.10, 0.28, 20)), darkSteelMat, {
    pos: [tankX, -2.85, tankZ],
  });
  vaultStaticBatcher.add(
    track(new THREE.SphereGeometry(0.98, 20, 10, 0, Math.PI * 2, Math.PI * 0.5, Math.PI * 0.5)),
    brushedSteelMat,
    { pos: [tankX, -2.62, tankZ], scale: [1, 0.42, 1] }
  );
  vaultStaticBatcher.add(
    track(new THREE.SphereGeometry(0.98, 20, 10, 0, Math.PI * 2, 0, Math.PI * 0.5)),
    brushedSteelMat,
    { pos: [tankX, -0.60, tankZ], scale: [1, 0.42, 1] }
  );
  vaultStaticBatcher.add(track(new THREE.CylinderGeometry(0.18, 0.22, 0.34, 12)), brushedSteelMat, {
    pos: [tankX, -0.26, tankZ],
  });
  const tankFlangeGeom = track(new THREE.TorusGeometry(1.02, 0.055, 8, 24));
  for (const fy of [-2.60, -1.61, -0.62]) {
    vaultStaticBatcher.add(tankFlangeGeom, darkSteelMat, { pos: [tankX, fy, tankZ], rot: [Math.PI / 2, 0, 0] });
  }

  dynFluidBatcher.add(track(new THREE.CylinderGeometry(0.84, 0.84, 1.92, 20)), dummyDynMat, {
    pos: [tankX, -1.62, tankZ],
  });
  vaultStaticBatcher.add(track(new THREE.CylinderGeometry(0.99, 0.99, 2.02, 22)), glassTankMat, {
    pos: [tankX, -1.61, tankZ],
    renderOrder: 2,
  });

  // External Front Sight-Glass Column with Brass Valve Fittings
  const sgZ = tankZ + 1.07;
  vaultStaticBatcher.add(track(new THREE.CylinderGeometry(0.065, 0.065, 1.72, 10)), glassTankMat, {
    pos: [tankX, -1.61, sgZ],
    renderOrder: 2,
  });
  dynFluidBatcher.add(track(new THREE.CylinderGeometry(0.038, 0.038, 1.56, 8)), dummyDynMat, {
    pos: [tankX, -1.64, sgZ],
  });
  const sgValveGeom = track(new THREE.CylinderGeometry(0.08, 0.08, 0.14, 10));
  for (const vy of [-2.44, -0.78]) {
    vaultStaticBatcher.add(sgValveGeom, brassMat, { pos: [tankX, vy, sgZ - 0.06], rot: [Math.PI / 2, 0, 0] });
  }

  // Water-Mist Fire-Suppression Ring (< 110 dB)
  vaultStaticBatcher.add(track(new THREE.TorusGeometry(2.65, 0.042, 8, 32)), brushedSteelMat, {
    pos: [-0.2, 0.66, -0.15],
    rot: [Math.PI / 2, 0, 0],
  });
  const mistNozzleGeom = track(new THREE.CylinderGeometry(0.025, 0.045, 0.11, 8));
  for (let m = 0; m < 8; m++) {
    const ma = (m / 8) * Math.PI * 2;
    vaultStaticBatcher.add(mistNozzleGeom, brassMat, {
      pos: [-0.2 + Math.cos(ma) * 2.65, 0.59, -0.15 + Math.sin(ma) * 2.65],
    });
  }

  // Vertical Riser Shaft to Surface Plaza (with Service Ring Collars)
  vaultStaticBatcher.add(track(new THREE.CylinderGeometry(0.76, 0.84, 14.8, 18, 1, true)), shaftGlassMat, {
    pos: [0, 8.25, 0],
    renderOrder: 2,
  });
  const riserPipeGeom = track(new THREE.CylinderGeometry(0.11, 0.11, 14.6, 8));
  dynHotBatcher.add(riserPipeGeom, dummyDynMat, { pos: [-0.22, 8.25, -0.12] });
  vaultStaticBatcher.add(riserPipeGeom, coolPipeMat, { pos: [0.22, 8.25, -0.12] });
  dynHtsBatcher.add(track(new THREE.CylinderGeometry(0.065, 0.065, 14.6, 8)), dummyDynMat, {
    pos: [0, 8.25, 0.24],
  });
  const shaftCollarGeom = track(new THREE.TorusGeometry(0.84, 0.075, 8, 24));
  for (const cy of [1.2, 4.4, 7.6, 10.8, 14.0]) {
    vaultStaticBatcher.add(shaftCollarGeom, darkSteelMat, { pos: [0, cy, 0], rot: [Math.PI / 2, 0, 0] });
  }

  // Finalize shared merged vault geometries & dispose temporary primitives
  const vaultStaticTemplates = vaultStaticBatcher.buildTemplates();
  const vaultRackDoorsGeom = dynRackDoorsBatcher.buildTemplates()[0].geom;
  const vaultHotHeaderGeom = dynHotBatcher.buildTemplates()[0].geom;
  const vaultHtsGeom = dynHtsBatcher.buildTemplates()[0].geom;
  const vaultBufferFluidGeom = dynFluidBatcher.buildTemplates()[0].geom;
  dummyDynMat.dispose();
  for (let i = 0; i < tempGeoms.length; i++) tempGeoms[i].dispose();
  tempGeoms.length = 0;

  // Shared fault geometries
  const faultBeaconGeom = new THREE.TorusGeometry(5.6, 0.16, 12, 44);
  const faultHaloGeom = new THREE.RingGeometry(5.8, 6.5, 44);

  // Instantiate 5 Vaults reusing the exact same merged BufferGeometries
  const vaultNodes = WORLD_LAYOUT.VAULT_POSITIONS.map((vp, idx) => {
    const vGroup = tagInspectable(new THREE.Group(), `vault_${idx}`);
    vGroup.position.set(vp.x, vp.y, vp.z);
    vGroup.rotation.y = -vp.angle + Math.PI / 2;
    group.add(vGroup);

    for (let i = 0; i < vaultStaticTemplates.length; i++) {
      const t = vaultStaticTemplates[i];
      const mesh = new THREE.Mesh(t.geom, t.mat);
      if (t.renderOrder) mesh.renderOrder = t.renderOrder;
      if (t.castShadow) mesh.castShadow = true;
      if (t.receiveShadow) mesh.receiveShadow = true;
      vGroup.add(mesh);
    }

    // Per-vault dynamic materials (only 4 active + 2 fault materials per vault)
    const rackLedMat = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      map: rackTex.map,
      emissive: 0x22d3ee,
      emissiveMap: rackTex.emissiveMap,
      emissiveIntensity: 1.45,
      roughness: 0.36,
      metalness: 0.62,
    });

    const hotHeaderMat = new THREE.MeshStandardMaterial({
      color: 0xff5a1f,
      emissive: 0xea580c,
      emissiveIntensity: 0.85,
      roughness: 0.28,
      metalness: 0.72,
    });

    const htsMat = new THREE.MeshPhysicalMaterial({
      color: 0x38bdf8,
      emissive: 0x0ea5e9,
      emissiveIntensity: 1.4,
      roughness: 0.14,
      metalness: 0.82,
      clearcoat: 1.0,
      clearcoatRoughness: 0.1,
    });

    const bufferFluidMat = new THREE.MeshStandardMaterial({
      color: 0xff5a1f,
      emissive: 0xea580c,
      emissiveIntensity: 0.95,
      roughness: 0.22,
      metalness: 0.1,
    });

    vGroup.add(new THREE.Mesh(vaultRackDoorsGeom, rackLedMat));
    vGroup.add(new THREE.Mesh(vaultHotHeaderGeom, hotHeaderMat));
    vGroup.add(new THREE.Mesh(vaultHtsGeom, htsMat));

    const fluidMesh = new THREE.Mesh(vaultBufferFluidGeom, bufferFluidMat);
    fluidMesh.renderOrder = 1;
    vGroup.add(fluidMesh);

    const beaconMat = new THREE.MeshStandardMaterial({
      color: 0xef4444,
      emissive: 0xff2222,
      emissiveIntensity: 2.3,
      transparent: true,
      opacity: 0.0,
      depthWrite: false,
    });
    const faultBeacon = new THREE.Mesh(faultBeaconGeom, beaconMat);
    faultBeacon.rotation.x = Math.PI / 2;
    faultBeacon.position.y = 1.05;
    faultBeacon.visible = false;
    vGroup.add(faultBeacon);

    const faultHaloMat = new THREE.MeshBasicMaterial({
      color: 0xef4444,
      transparent: true,
      opacity: 0.0,
      side: THREE.DoubleSide,
      depthWrite: false,
    });
    const faultHalo = new THREE.Mesh(faultHaloGeom, faultHaloMat);
    faultHalo.rotation.x = -Math.PI / 2;
    faultHalo.position.y = 1.05;
    faultHalo.visible = false;
    vGroup.add(faultHalo);

    return {
      group: vGroup,
      rackLedMat,
      hotHeaderMat,
      htsMat,
      bufferFluidMat,
      faultBeacon,
      beaconMat,
      faultHalo,
      faultHaloMat,
      lastFaulted: null,
      lastLoadFactor: -1,
    };
  });

  // =========================================================================
  // 2. CENTRAL UTILITY HUB (x=0, z=0, y=-16) — Batched per Inspectable Group
  // =========================================================================
  const hubGroup = new THREE.Group();
  hubGroup.position.set(0, WORLD_LAYOUT.VAULT_Y, 0);
  group.add(hubGroup);

  // Multi-tiered architectural engineering plinth (2 clean meshes instead of 3-group multi-mat)
  const hubFloorMat = new THREE.MeshStandardMaterial({
    map: hubFloorTex,
    roughness: 0.72,
    metalness: 0.25,
  });
  const hubPlinthBody = new THREE.Mesh(new THREE.CylinderGeometry(13.4, 14.0, 0.52, 56), slabMat);
  hubPlinthBody.position.y = -3.38;
  hubPlinthBody.receiveShadow = true;
  hubGroup.add(hubPlinthBody);

  const hubPlinthTop = new THREE.Mesh(new THREE.CircleGeometry(13.4, 56), hubFloorMat);
  hubPlinthTop.rotation.x = -Math.PI / 2;
  hubPlinthTop.position.y = -3.115;
  hubPlinthTop.receiveShadow = true;
  hubGroup.add(hubPlinthTop);

  const hubCurbRing = new THREE.Mesh(new THREE.TorusGeometry(13.42, 0.12, 8, 64), darkSteelMat);
  hubCurbRing.rotation.x = Math.PI / 2;
  hubCurbRing.position.y = -3.10;
  hubGroup.add(hubCurbRing);

  const hubAccentRing = new THREE.Mesh(
    new THREE.TorusGeometry(13.78, 0.045, 8, 64),
    new THREE.MeshStandardMaterial({
      color: 0x22d3ee,
      emissive: 0x0ea5e9,
      emissiveIntensity: 1.05,
      roughness: 0.2,
    })
  );
  hubAccentRing.rotation.x = Math.PI / 2;
  hubAccentRing.position.y = -3.38;
  hubGroup.add(hubAccentRing);

  // -------------------------------------------------------------------------
  // 2A. 2,500 m³ Stratified Thermal Store (thermal_store)
  // -------------------------------------------------------------------------
  const thermalStoreGroup = tagInspectable(new THREE.Group(), 'thermal_store');
  thermalStoreGroup.position.set(-4.2, -3.1, -3.5);
  hubGroup.add(thermalStoreGroup);

  const tsBatcher = createStaticBatcher();
  const cutawayCenterAngle = Math.PI * 0.25;
  const windowArc = Math.PI * 0.68;
  const jacketStart = cutawayCenterAngle + windowArc / 2;
  const jacketLen = Math.PI * 2 - windowArc;

  const tsJacketMat = new THREE.MeshStandardMaterial({
    color: 0x94a3b8,
    map: brushedSteelTex,
    roughness: 0.36,
    metalness: 0.78,
    side: THREE.DoubleSide,
  });

  const tsPadG = new THREE.CylinderGeometry(2.95, 3.05, 0.24, 32);
  tsBatcher.add(tsPadG, concreteMat, { pos: [0, 0.12, 0] });
  tsPadG.dispose();

  const tsJacketG = new THREE.CylinderGeometry(2.68, 2.68, 6.2, 32, 1, true, jacketStart, jacketLen);
  tsBatcher.add(tsJacketG, tsJacketMat, { pos: [0, 3.34, 0], castShadow: true });
  tsJacketG.dispose();

  const ribGeom = new THREE.BoxGeometry(0.10, 6.22, 0.16);
  for (let i = 0; i <= 10; i++) {
    const a = jacketStart + (i / 10) * jacketLen;
    tsBatcher.add(ribGeom, darkSteelMat, {
      pos: [Math.sin(a) * 2.72, 3.34, Math.cos(a) * 2.72],
      rot: [0, a, 0],
    });
  }
  ribGeom.dispose();

  const hoopGeom = new THREE.TorusGeometry(2.72, 0.06, 8, 36);
  for (const hy of [0.35, 2.25, 4.35, 6.38]) {
    tsBatcher.add(hoopGeom, darkSteelMat, { pos: [0, hy, 0], rot: [Math.PI / 2, 0, 0] });
  }
  hoopGeom.dispose();

  const tsGlassG = new THREE.CylinderGeometry(2.66, 2.66, 6.1, 20, 1, true, cutawayCenterAngle - windowArc / 2, windowArc);
  tsBatcher.add(tsGlassG, glassDoubleMat, { pos: [0, 3.34, 0], renderOrder: 2 });
  tsGlassG.dispose();

  const tsDomeG = new THREE.SphereGeometry(2.70, 28, 12, 0, Math.PI * 2, 0, Math.PI * 0.5);
  tsBatcher.add(tsDomeG, brushedSteelMat, { pos: [0, 6.42, 0], scale: [1, 0.36, 1] });
  tsDomeG.dispose();

  const tsTopManG = new THREE.CylinderGeometry(0.48, 0.56, 0.55, 16);
  tsBatcher.add(tsTopManG, darkSteelMat, { pos: [0, 7.45, 0] });
  tsTopManG.dispose();

  const tsManPipeG = new THREE.CylinderGeometry(0.14, 0.14, 6.8, 10);
  tsBatcher.add(tsManPipeG, brushedSteelMat, { pos: [-2.98, 3.6, 0.4] });
  tsManPipeG.dispose();

  const catDeckG = new THREE.TorusGeometry(3.15, 0.24, 6, 36);
  tsBatcher.add(catDeckG, darkSteelMat, { pos: [0, 5.55, 0], rot: [Math.PI / 2, 0, 0], scale: [1, 1, 0.25] });
  catDeckG.dispose();

  const railTopG = new THREE.TorusGeometry(3.36, 0.032, 6, 36);
  tsBatcher.add(railTopG, brushedAlumMat, { pos: [0, 6.28, 0], rot: [Math.PI / 2, 0, 0] });
  railTopG.dispose();

  const railMidG = new THREE.TorusGeometry(3.36, 0.024, 6, 36);
  tsBatcher.add(railMidG, brushedAlumMat, { pos: [0, 5.92, 0], rot: [Math.PI / 2, 0, 0] });
  railMidG.dispose();

  const postGeom = new THREE.CylinderGeometry(0.025, 0.025, 0.78, 6);
  for (let i = 0; i < 12; i++) {
    const a = (i / 12) * Math.PI * 2;
    tsBatcher.add(postGeom, brushedSteelMat, { pos: [Math.cos(a) * 3.36, 5.92, Math.sin(a) * 3.36] });
  }
  postGeom.dispose();

  tsBatcher.flushInto(thermalStoreGroup);

  // Live Stratified Thermal Fluid Core (animated SOC scale)
  const tsFluidMat = new THREE.MeshStandardMaterial({
    color: 0xe2e8f0,
    map: thermalStratTex,
    emissive: 0xffffff,
    emissiveMap: thermalStratTex,
    emissiveIntensity: 0.42,
    roughness: 0.36,
    metalness: 0.04,
  });
  const tsFluidHeight = 5.85;
  const tsFluid = new THREE.Mesh(new THREE.CylinderGeometry(2.42, 2.42, tsFluidHeight, 28), tsFluidMat);
  tsFluid.position.y = 0.26 + tsFluidHeight / 2;
  tsFluid.renderOrder = 1;
  thermalStoreGroup.add(tsFluid);

  const tsMeniscusMat = new THREE.MeshStandardMaterial({
    color: 0xff6b2c,
    emissive: 0xff4518,
    emissiveIntensity: 0.85,
    roughness: 0.28,
  });
  const tsMeniscus = new THREE.Mesh(new THREE.CylinderGeometry(2.43, 2.43, 0.14, 28), tsMeniscusMat);
  tsMeniscus.position.y = 0.26 + tsFluidHeight;
  thermalStoreGroup.add(tsMeniscus);

  // -------------------------------------------------------------------------
  // 2B. 10,000 m³ RC Drought Rainwater Cistern (rainwater_cistern)
  // -------------------------------------------------------------------------
  const cisternGroup = tagInspectable(new THREE.Group(), 'rainwater_cistern');
  cisternGroup.position.set(4.8, -3.1, -3.2);
  hubGroup.add(cisternGroup);

  const cW = 7.4;
  const cH = 4.7;
  const cD = 5.6;
  const wallT = 0.28;

  const cisternBatcher = createStaticBatcher();
  const cFloorG = new THREE.BoxGeometry(cW, 0.24, cD);
  cisternBatcher.add(cFloorG, concreteMat, { pos: [0, 0.12, 0] });
  cFloorG.dispose();

  const cBackG = new THREE.BoxGeometry(cW, cH, wallT);
  cisternBatcher.add(cBackG, concreteMat, { pos: [0, cH / 2 + 0.12, -cD / 2 + wallT / 2] });
  cBackG.dispose();

  const cSideG = new THREE.BoxGeometry(wallT, cH, cD - wallT);
  cisternBatcher.add(cSideG, concreteMat, { pos: [-cW / 2 + wallT / 2, cH / 2 + 0.12, wallT / 2] });
  cisternBatcher.add(cSideG, concreteMat, { pos: [cW / 2 - wallT / 2, cH / 2 + 0.12, wallT / 2] });
  cSideG.dispose();

  const cRibG = new THREE.BoxGeometry(0.26, cH, 0.26);
  for (const bx of [-2.4, 0, 2.4]) {
    cisternBatcher.add(cRibG, concreteMat, { pos: [bx, cH / 2 + 0.12, -cD / 2 - 0.10] });
  }
  cRibG.dispose();

  const cBeamG = new THREE.BoxGeometry(0.28, 0.26, cD);
  for (const rx of [-2.3, 0, 2.3]) {
    cisternBatcher.add(cBeamG, concreteMat, { pos: [rx, cH + 0.10, 0] });
  }
  cBeamG.dispose();

  const frontTopBeamG = new THREE.BoxGeometry(cW, 0.28, 0.28);
  cisternBatcher.add(frontTopBeamG, darkSteelMat, { pos: [0, cH + 0.10, cD / 2 - 0.14] });
  frontTopBeamG.dispose();

  // 6 Interior Structural RC Support Columns
  const colGeom = new THREE.CylinderGeometry(0.22, 0.26, cH - 0.15, 12);
  const colCapGeom = new THREE.BoxGeometry(0.68, 0.16, 0.68);
  for (const cx of [-2.1, 0, 2.1]) {
    for (const cz of [-1.1, 1.1]) {
      cisternBatcher.add(colGeom, concreteMat, { pos: [cx, cH / 2 + 0.14, cz] });
      cisternBatcher.add(colCapGeom, concreteMat, { pos: [cx, cH - 0.08, cz] });
    }
  }
  colGeom.dispose();
  colCapGeom.dispose();

  // Vertical steel window mullions
  const mullionG = new THREE.BoxGeometry(0.10, cH, 0.12);
  for (const mx of [-2.3, 0, 2.3]) {
    cisternBatcher.add(mullionG, darkSteelMat, { pos: [mx, cH / 2 + 0.12, cD / 2 - 0.06] });
  }
  mullionG.dispose();

  cisternBatcher.flushInto(cisternGroup);

  // Live Translucent Cistern Water Volume + Animated Rippling Surface
  const cisternWaterMat = new THREE.MeshPhysicalMaterial({
    color: 0x0284c7,
    emissive: 0x0369a1,
    emissiveIntensity: 0.42,
    roughness: 0.15,
    metalness: 0.04,
    transparent: true,
    opacity: 0.64,
    depthWrite: false,
  });
  const maxWaterH = 4.25;
  const cisternWater = new THREE.Mesh(
    new THREE.BoxGeometry(cW - wallT * 2 - 0.08, maxWaterH, cD - wallT * 2 - 0.08),
    cisternWaterMat
  );
  cisternWater.position.set(0, 0.25 + maxWaterH / 2, 0);
  cisternWater.renderOrder = 1;
  cisternGroup.add(cisternWater);

  const waterSurfaceMat = new THREE.MeshPhysicalMaterial({
    color: 0x38bdf8,
    map: waterCausticTex.map,
    bumpMap: waterCausticTex.bumpMap,
    bumpScale: 0.08,
    emissive: 0x0ea5e9,
    emissiveMap: waterCausticTex.map,
    emissiveIntensity: 0.62,
    roughness: 0.10,
    metalness: 0.08,
    clearcoat: 1.0,
    clearcoatRoughness: 0.06,
    transparent: true,
    opacity: 0.88,
  });
  const cisternSurface = new THREE.Mesh(
    new THREE.PlaneGeometry(cW - wallT * 2 - 0.10, cD - wallT * 2 - 0.10),
    waterSurfaceMat
  );
  cisternSurface.rotation.x = -Math.PI / 2;
  cisternSurface.position.set(0, 0.28 + maxWaterH, 0);
  cisternSurface.renderOrder = 2;
  cisternGroup.add(cisternSurface);

  const frontGlass = new THREE.Mesh(new THREE.PlaneGeometry(cW - 0.56, cH - 0.2), glassDoubleMat);
  frontGlass.position.set(0, cH / 2 + 0.12, cD / 2 - 0.06);
  frontGlass.renderOrder = 3;
  cisternGroup.add(frontGlass);

  const gaugeMat = new THREE.MeshBasicMaterial({
    map: cisternGaugeTex,
    transparent: true,
    opacity: 0.92,
    depthWrite: false,
  });
  const frontGauge = new THREE.Mesh(new THREE.PlaneGeometry(cW - 0.72, cH - 0.35), gaugeMat);
  frontGauge.position.set(0, cH / 2 + 0.12, cD / 2 - 0.02);
  frontGauge.renderOrder = 4;
  cisternGroup.add(frontGauge);

  // -------------------------------------------------------------------------
  // 2C. 8.0 MW_th Backup HTHP Bank (hthp_bank)
  // -------------------------------------------------------------------------
  const hthpGroup = tagInspectable(new THREE.Group(), 'hthp_bank');
  hthpGroup.position.set(-5.6, -3.1, 4.4);
  hubGroup.add(hthpGroup);

  const hthpDischargeMat = new THREE.MeshStandardMaterial({
    color: 0xf59e0b,
    emissive: 0xd97706,
    emissiveIntensity: 0.3,
    roughness: 0.28,
    metalness: 0.72,
  });

  const hthpBatcher = createStaticBatcher();
  const hthpSkidG = new THREE.BoxGeometry(6.8, 0.22, 2.4);
  hthpBatcher.add(hthpSkidG, darkSteelMat, { pos: [0, 0.11, 0] });
  hthpSkidG.dispose();

  const hthpHazG = new THREE.BoxGeometry(6.82, 0.08, 0.08);
  hthpBatcher.add(hthpHazG, hazardMat, { pos: [0, 0.14, 1.21], uvRepeat: [5, 1] });
  hthpHazG.dispose();

  const hthpCabG = new THREE.BoxGeometry(1.36, 1.88, 1.72);
  const hthpCoilFG = new THREE.PlaneGeometry(1.18, 0.82);
  const hthpCoilBG = new THREE.PlaneGeometry(1.18, 1.48);
  const compDrumGeom = new THREE.CylinderGeometry(0.24, 0.24, 1.05, 12);
  const compCapGeom = new THREE.SphereGeometry(0.24, 10, 6);
  const shroudGeom = new THREE.CylinderGeometry(0.56, 0.52, 0.36, 18, 1, true);
  const shroudLipGeom = new THREE.TorusGeometry(0.56, 0.04, 8, 20);
  const hthpRiserGeom = new THREE.CylinderGeometry(0.085, 0.085, 0.75, 10);

  const hthpFanGeom = buildMergedFanGeometry(4, 0.48);
  const hthpFans = [];

  for (let i = 0; i < 4; i++) {
    const ux = -2.45 + i * 1.63;
    hthpBatcher.add(hthpCabG, darkSteelMat, { pos: [ux, 1.16, 0], castShadow: true });
    hthpBatcher.add(hthpCoilFG, finCoilMat, { pos: [ux, 1.56, 0.87] });
    hthpBatcher.add(hthpCoilBG, finCoilMat, { pos: [ux, 1.16, -0.87], rot: [0, Math.PI, 0] });

    for (const dx of [-0.30, 0.30]) {
      hthpBatcher.add(compDrumGeom, brushedSteelMat, { pos: [ux + dx, 0.74, 0.98] });
      hthpBatcher.add(compCapGeom, darkSteelMat, { pos: [ux + dx, 1.26, 0.98], scale: [1, 0.4, 1] });
    }

    hthpBatcher.add(shroudGeom, brushedSteelMat, { pos: [ux, 2.28, 0] });
    hthpBatcher.add(shroudLipGeom, darkSteelMat, { pos: [ux, 2.46, 0], rot: [Math.PI / 2, 0, 0] });
    hthpBatcher.add(hthpRiserGeom, hthpDischargeMat, { pos: [ux, 1.95, -0.98] });

    const fan = new THREE.Mesh(hthpFanGeom, fanBladeMat);
    fan.position.set(ux, 2.28, 0);
    hthpGroup.add(fan);
    hthpFans.push(fan);
  }

  const hthpHeaderG = new THREE.CylinderGeometry(0.13, 0.13, 5.6, 12);
  hthpBatcher.add(hthpHeaderG, hthpDischargeMat, { pos: [0, 2.30, -0.98], rot: [0, 0, Math.PI / 2] });
  hthpHeaderG.dispose();
  hthpCabG.dispose();
  hthpCoilFG.dispose();
  hthpCoilBG.dispose();
  compDrumGeom.dispose();
  compCapGeom.dispose();
  shroudGeom.dispose();
  shroudLipGeom.dispose();
  hthpRiserGeom.dispose();
  hthpBatcher.flushInto(hthpGroup);

  // -------------------------------------------------------------------------
  // 2D. SrBr₂ / K₂CO₃ TCES Reactor Vessels (tces)
  // -------------------------------------------------------------------------
  const tcesGroup = tagInspectable(new THREE.Group(), 'tces');
  tcesGroup.position.set(1.2, -3.1, 4.8);
  hubGroup.add(tcesGroup);

  const tcesMat = new THREE.MeshStandardMaterial({
    color: 0xfbbf24,
    emissive: 0xd97706,
    emissiveIntensity: 0.35,
    roughness: 0.28,
    metalness: 0.55,
  });

  const tcesBatcher = createStaticBatcher();
  const tcesSkidG = new THREE.BoxGeometry(4.6, 0.20, 1.9);
  tcesBatcher.add(tcesSkidG, darkSteelMat, { pos: [0, 0.10, 0] });
  tcesSkidG.dispose();

  const tcesSkirtG = new THREE.CylinderGeometry(0.58, 0.68, 0.35, 16);
  const tcesShellG = new THREE.CylinderGeometry(0.64, 0.64, 2.15, 18);
  const tcesDomeG = new THREE.SphereGeometry(0.64, 16, 8, 0, Math.PI * 2, 0, Math.PI * 0.5);
  const tcesGlowG = new THREE.CylinderGeometry(0.66, 0.66, 0.42, 18);
  const tcesFlangeG = new THREE.TorusGeometry(0.66, 0.045, 8, 20);
  const tcesNeckG = new THREE.CylinderGeometry(0.11, 0.13, 0.52, 10);

  for (let i = 0; i < 3; i++) {
    const vx = -1.48 + i * 1.48;
    tcesBatcher.add(tcesSkirtG, darkSteelMat, { pos: [vx, 0.36, 0] });
    tcesBatcher.add(tcesShellG, brushedSteelMat, { pos: [vx, 1.56, 0], castShadow: true });
    tcesBatcher.add(tcesDomeG, brushedSteelMat, { pos: [vx, 2.63, 0], scale: [1, 0.55, 1] });
    tcesBatcher.add(tcesGlowG, tcesMat, { pos: [vx, 1.56, 0] });
    tcesBatcher.add(tcesFlangeG, darkSteelMat, { pos: [vx, 0.60, 0], rot: [Math.PI / 2, 0, 0] });
    tcesBatcher.add(tcesFlangeG, darkSteelMat, { pos: [vx, 2.52, 0], rot: [Math.PI / 2, 0, 0] });
    tcesBatcher.add(tcesNeckG, darkSteelMat, { pos: [vx, 3.16, 0] });
  }

  const tcesH1G = new THREE.CylinderGeometry(0.10, 0.10, 3.6, 12);
  tcesBatcher.add(tcesH1G, tcesMat, { pos: [0, 3.42, 0], rot: [0, 0, Math.PI / 2] });
  const tcesH2G = new THREE.CylinderGeometry(0.075, 0.075, 3.6, 10);
  tcesBatcher.add(tcesH2G, brushedSteelMat, { pos: [0, 3.18, -0.45], rot: [0, 0, Math.PI / 2] });

  tcesSkirtG.dispose();
  tcesShellG.dispose();
  tcesDomeG.dispose();
  tcesGlowG.dispose();
  tcesFlangeG.dispose();
  tcesNeckG.dispose();
  tcesH1G.dispose();
  tcesH2G.dispose();
  tcesBatcher.flushInto(tcesGroup);

  // -------------------------------------------------------------------------
  // 2E. 2 MW / 16 MWh TI-PTES Carnot Battery (ti_ptes)
  // -------------------------------------------------------------------------
  const ptesGroup = tagInspectable(new THREE.Group(), 'ti_ptes');
  ptesGroup.position.set(6.2, -3.1, 4.4);
  hubGroup.add(ptesGroup);

  const ptesHotMat = new THREE.MeshStandardMaterial({
    color: 0xf97316,
    emissive: 0xea580c,
    emissiveIntensity: 0.45,
    roughness: 0.26,
    metalness: 0.55,
  });
  const ptesColdMat = new THREE.MeshStandardMaterial({
    color: 0x06b6d4,
    emissive: 0x0891b2,
    emissiveIntensity: 0.45,
    roughness: 0.26,
    metalness: 0.55,
  });

  const ptesBatcher = createStaticBatcher();
  const ptesSkidG = new THREE.BoxGeometry(4.2, 0.22, 2.3);
  ptesBatcher.add(ptesSkidG, darkSteelMat, { pos: [0, 0.11, 0] });
  ptesSkidG.dispose();

  const ptesVesselG = new THREE.CylinderGeometry(0.84, 0.84, 2.85, 20);
  const ptesDomeG = new THREE.SphereGeometry(0.84, 18, 10, 0, Math.PI * 2, 0, Math.PI * 0.5);
  const ptesBandG = new THREE.CylinderGeometry(0.87, 0.87, 0.65, 20);
  const ptesFlangeG = new THREE.TorusGeometry(0.86, 0.045, 8, 22);

  for (const [px, bandMat] of [[-1.32, ptesHotMat], [1.32, ptesColdMat]]) {
    ptesBatcher.add(ptesVesselG, brushedSteelMat, { pos: [px, 1.65, 0], castShadow: true });
    ptesBatcher.add(ptesDomeG, brushedSteelMat, { pos: [px, 3.07, 0], scale: [1, 0.48, 1] });
    ptesBatcher.add(ptesBandG, bandMat, { pos: [px, 1.72, 0] });
    for (const fy of [0.45, 1.25, 2.20, 2.95]) {
      ptesBatcher.add(ptesFlangeG, darkSteelMat, { pos: [px, fy, 0], rot: [Math.PI / 2, 0, 0] });
    }
  }

  const turboFrameG = new THREE.BoxGeometry(1.15, 0.92, 1.25);
  ptesBatcher.add(turboFrameG, darkSteelMat, { pos: [0, 0.68, 0] });
  const turboBodyG = new THREE.CylinderGeometry(0.36, 0.44, 0.96, 14);
  ptesBatcher.add(turboBodyG, brushedSteelMat, { pos: [0, 1.48, 0], rot: [0, 0, Math.PI / 2] });
  const bridgePipeG = new THREE.CylinderGeometry(0.10, 0.10, 2.55, 10);
  ptesBatcher.add(bridgePipeG, ptesHotMat, { pos: [0, 2.65, 0.25], rot: [0, 0, Math.PI / 2] });
  ptesBatcher.add(bridgePipeG, ptesColdMat, { pos: [0, 2.25, -0.25], rot: [0, 0, Math.PI / 2] });

  ptesVesselG.dispose();
  ptesDomeG.dispose();
  ptesBandG.dispose();
  ptesFlangeG.dispose();
  turboFrameG.dispose();
  turboBodyG.dispose();
  bridgePipeG.dispose();
  ptesBatcher.flushInto(ptesGroup);

  const ptesFlywheel = new THREE.Mesh(new THREE.TorusGeometry(0.42, 0.065, 8, 18), brassMat);
  ptesFlywheel.rotation.y = Math.PI / 2;
  ptesFlywheel.position.set(0, 1.48, 0);
  ptesGroup.add(ptesFlywheel);

  // -------------------------------------------------------------------------
  // 2F. Micro-ORC Turbine-Expander + Condenser + Generator Skid (micro_orc)
  // -------------------------------------------------------------------------
  const orcGroup = tagInspectable(new THREE.Group(), 'micro_orc');
  orcGroup.position.set(0.2, -3.1, -6.4);
  hubGroup.add(orcGroup);

  const orcTurbineMat = new THREE.MeshStandardMaterial({
    color: 0x10b981,
    emissive: 0x059669,
    emissiveIntensity: 0.45,
    roughness: 0.24,
    metalness: 0.78,
  });

  const orcBusMat = new THREE.MeshStandardMaterial({
    color: 0x34d399,
    emissive: 0x10b981,
    emissiveIntensity: 0.5,
    roughness: 0.2,
    metalness: 0.65,
  });

  const orcBatcher = createStaticBatcher();
  const orcSkidG = new THREE.BoxGeometry(3.7, 0.34, 2.1);
  orcBatcher.add(orcSkidG, darkSteelMat, { pos: [0, 0.17, 0] });
  const orcCondG = new THREE.CylinderGeometry(0.48, 0.48, 2.65, 16);
  orcBatcher.add(orcCondG, brushedSteelMat, { pos: [0, 0.92, -0.52], rot: [0, 0, Math.PI / 2] });
  const orcCapG = new THREE.SphereGeometry(0.48, 12, 8);
  orcBatcher.add(orcCapG, darkSteelMat, { pos: [-1.34, 0.92, -0.52], scale: [0.45, 1, 1] });
  orcBatcher.add(orcCapG, darkSteelMat, { pos: [1.34, 0.92, -0.52], scale: [0.45, 1, 1] });
  const orcVolG = new THREE.CylinderGeometry(0.62, 0.48, 0.82, 16);
  orcBatcher.add(orcVolG, brushedAlumMat, { pos: [-0.98, 1.48, 0.32], rot: [0, 0, Math.PI / 2] });
  const orcGenG = new THREE.CylinderGeometry(0.52, 0.52, 1.15, 16);
  orcBatcher.add(orcGenG, darkSteelMat, { pos: [0.95, 1.48, 0.32], rot: [0, 0, Math.PI / 2] });
  const orcBusG = new THREE.BoxGeometry(3.2, 0.12, 0.14);
  orcBatcher.add(orcBusG, orcBusMat, { pos: [0, 0.52, 0.96] });
  const orcTapG = new THREE.BoxGeometry(0.14, 0.85, 0.14);
  orcBatcher.add(orcTapG, orcBusMat, { pos: [0.95, 0.96, 0.90] });

  orcSkidG.dispose();
  orcCondG.dispose();
  orcCapG.dispose();
  orcVolG.dispose();
  orcGenG.dispose();
  orcBusG.dispose();
  orcTapG.dispose();
  orcBatcher.flushInto(orcGroup);

  // Exposed Spinning Drive Shaft + Merged 3-Fin Rotor Coupling
  const orcRotor = new THREE.Group();
  orcRotor.position.set(0, 1.48, 0.32);
  orcGroup.add(orcRotor);

  const shaftCore = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.16, 1.25, 12), brushedAlumMat);
  shaftCore.rotation.z = Math.PI / 2;
  orcRotor.add(shaftCore);

  const finBatcher = createStaticBatcher();
  const couplingFinGeom = new THREE.BoxGeometry(0.68, 0.52, 0.06);
  for (let f = 0; f < 3; f++) {
    finBatcher.add(couplingFinGeom, orcTurbineMat, { rot: [(f / 3) * Math.PI, 0, 0] });
  }
  couplingFinGeom.dispose();
  finBatcher.flushInto(orcRotor);

  // -------------------------------------------------------------------------
  // 2G. Finned MOF Adsorption Chiller Bank (mof_chiller)
  // -------------------------------------------------------------------------
  const mofGroup = tagInspectable(new THREE.Group(), 'mof_chiller');
  mofGroup.position.set(-8.6, -3.1, -0.4);
  hubGroup.add(mofGroup);

  const mofMat = new THREE.MeshStandardMaterial({
    color: 0x2dd4bf,
    emissive: 0x0d9488,
    emissiveIntensity: 0.35,
    roughness: 0.28,
    metalness: 0.55,
  });

  const mofBatcher = createStaticBatcher();
  const mofBaseG = new THREE.BoxGeometry(2.8, 0.24, 3.0);
  mofBatcher.add(mofBaseG, darkSteelMat, { pos: [0, 0.12, 0] });
  const mofBedG = new THREE.CylinderGeometry(0.56, 0.56, 2.25, 16);
  const mofRingG = new THREE.CylinderGeometry(0.59, 0.59, 0.45, 16);
  for (const mz of [-0.72, 0.72]) {
    mofBatcher.add(mofBedG, brushedSteelMat, { pos: [0, 1.05, mz], rot: [0, 0, Math.PI / 2] });
    mofBatcher.add(mofRingG, mofMat, { pos: [0, 1.05, mz], rot: [0, 0, Math.PI / 2] });
  }
  const mofFinG = new THREE.BoxGeometry(2.35, 0.92, 2.55);
  mofBatcher.add(mofFinG, finCoilMat, { pos: [0, 2.12, 0] });
  const mofCapG = new THREE.BoxGeometry(2.48, 0.14, 2.68);
  mofBatcher.add(mofCapG, darkSteelMat, { pos: [0, 2.62, 0] });

  mofBaseG.dispose();
  mofBedG.dispose();
  mofRingG.dispose();
  mofFinG.dispose();
  mofCapG.dispose();
  mofBatcher.flushInto(mofGroup);

  // -------------------------------------------------------------------------
  // 2H. Louvered N+1 Low-Noise Dry Coolers (dry_coolers)
  // -------------------------------------------------------------------------
  const dryCoolerGroup = tagInspectable(new THREE.Group(), 'dry_coolers');
  dryCoolerGroup.position.set(9.4, -3.1, 0.2);
  hubGroup.add(dryCoolerGroup);

  const dcBatcher = createStaticBatcher();
  const dcLegG = new THREE.BoxGeometry(0.18, 0.52, 0.18);
  for (const lx of [-1.05, 1.05]) {
    for (const lz of [-2.1, 0, 2.1]) {
      dcBatcher.add(dcLegG, darkSteelMat, { pos: [lx, 0.26, lz] });
    }
  }
  // Core steel cabinet + 4 merged louver planes (1 finCoilMat draw call instead of 6-mat array)
  const dcCoreG = new THREE.BoxGeometry(2.46, 1.48, 4.80);
  dcBatcher.add(dcCoreG, darkSteelMat, { pos: [0, 1.24, 0], castShadow: true });
  const dcLouverLR = new THREE.PlaneGeometry(4.76, 1.42);
  dcBatcher.add(dcLouverLR, finCoilMat, { pos: [1.245, 1.24, 0], rot: [0, Math.PI / 2, 0] });
  dcBatcher.add(dcLouverLR, finCoilMat, { pos: [-1.245, 1.24, 0], rot: [0, -Math.PI / 2, 0] });
  const dcLouverFB = new THREE.PlaneGeometry(2.42, 1.42);
  dcBatcher.add(dcLouverFB, finCoilMat, { pos: [0, 1.24, 2.415] });
  dcBatcher.add(dcLouverFB, finCoilMat, { pos: [0, 1.24, -2.415], rot: [0, Math.PI, 0] });

  const dcTopG = new THREE.BoxGeometry(2.58, 0.14, 4.92);
  dcBatcher.add(dcTopG, darkSteelMat, { pos: [0, 2.02, 0] });

  const dcShroudGeom = new THREE.CylinderGeometry(0.72, 0.68, 0.34, 20, 1, true);
  const dcRimGeom = new THREE.TorusGeometry(0.72, 0.045, 8, 24);
  const dcFanGeom = buildMergedFanGeometry(5, 0.64);
  const dryFanBlades = [];

  for (let i = 0; i < 3; i++) {
    const fz = -1.52 + i * 1.52;
    dcBatcher.add(dcShroudGeom, brushedSteelMat, { pos: [0, 2.22, fz] });
    dcBatcher.add(dcRimGeom, darkSteelMat, { pos: [0, 2.39, fz], rot: [Math.PI / 2, 0, 0] });

    const fan = new THREE.Mesh(dcFanGeom, fanBladeMat);
    fan.position.set(0, 2.22, fz);
    dryCoolerGroup.add(fan);
    dryFanBlades.push(fan);
  }

  dcLegG.dispose();
  dcCoreG.dispose();
  dcLouverLR.dispose();
  dcLouverFB.dispose();
  dcTopG.dispose();
  dcShroudGeom.dispose();
  dcRimGeom.dispose();
  dcBatcher.flushInto(dryCoolerGroup);

  // -------------------------------------------------------------------------
  // 2I. Semiconductor Grade C Ultrapure Water Train (ultrapure_train)
  // -------------------------------------------------------------------------
  const ultrapureGroup = tagInspectable(new THREE.Group(), 'ultrapure_train');
  ultrapureGroup.position.set(4.6, -3.1, -7.8);
  hubGroup.add(ultrapureGroup);

  const uvCoreMat = new THREE.MeshStandardMaterial({
    color: 0xa5b4fc,
    emissive: 0x6366f1,
    emissiveIntensity: 1.75,
    roughness: 0.15,
  });

  const upBatcher = createStaticBatcher();
  const upSkidG = new THREE.BoxGeometry(5.1, 0.22, 1.75);
  upBatcher.add(upSkidG, darkSteelMat, { pos: [0, 0.11, 0] });

  const ufVesselG = new THREE.CylinderGeometry(0.28, 0.28, 2.15, 14);
  const ufDomeG = new THREE.SphereGeometry(0.28, 12, 6);
  for (const ux of [-2.05, -1.35]) {
    upBatcher.add(ufVesselG, brushedAlumMat, { pos: [ux, 1.30, 0] });
    upBatcher.add(ufDomeG, darkSteelMat, { pos: [ux, 2.38, 0], scale: [1, 0.45, 1] });
  }

  const mcdiTubeG = new THREE.CylinderGeometry(0.20, 0.20, 1.32, 12);
  for (const my of [0.85, 1.52, 2.18]) {
    upBatcher.add(mcdiTubeG, brushedSteelMat, { pos: [-0.22, my, 0], rot: [0, 0, Math.PI / 2] });
  }

  const diVesselG = new THREE.CylinderGeometry(0.32, 0.32, 2.15, 14);
  upBatcher.add(diVesselG, ptesColdMat, { pos: [0.85, 1.30, 0] });

  const uvCoreG = new THREE.CylinderGeometry(0.16, 0.16, 1.92, 14);
  upBatcher.add(uvCoreG, uvCoreMat, { pos: [1.58, 1.32, 0], renderOrder: 1 });

  const uvQuartzG = new THREE.CylinderGeometry(0.32, 0.32, 2.02, 16);
  upBatcher.add(uvQuartzG, glassTankMat, { pos: [1.58, 1.32, 0], renderOrder: 2 });

  const uvFlangeG = new THREE.CylinderGeometry(0.36, 0.36, 0.12, 14);
  for (const fy of [0.32, 2.32]) {
    upBatcher.add(uvFlangeG, brushedSteelMat, { pos: [1.58, fy, 0] });
  }

  const bddUnitG = new THREE.BoxGeometry(0.68, 1.75, 0.92);
  upBatcher.add(bddUnitG, darkSteelMat, { pos: [2.22, 1.10, 0] });
  const bddStripG = new THREE.BoxGeometry(0.06, 1.15, 0.62);
  upBatcher.add(bddStripG, uvCoreMat, { pos: [2.57, 1.15, 0], renderOrder: 1 });

  upBatcher.addPipe(new THREE.Vector3(-2.05, 2.55, 0), new THREE.Vector3(2.22, 2.55, 0), 0.055, brushedAlumMat);
  const upDropG = new THREE.CylinderGeometry(0.045, 0.045, 0.38, 8);
  for (const px of [-2.05, -1.35, 0.85, 1.58, 2.22]) {
    upBatcher.add(upDropG, brushedAlumMat, { pos: [px, 2.38, 0] });
  }

  upSkidG.dispose();
  ufVesselG.dispose();
  ufDomeG.dispose();
  mcdiTubeG.dispose();
  diVesselG.dispose();
  uvCoreG.dispose();
  uvQuartzG.dispose();
  uvFlangeG.dispose();
  bddUnitG.dispose();
  bddStripG.dispose();
  upDropG.dispose();
  upBatcher.flushInto(ultrapureGroup);

  // =========================================================================
  // 3. REAL-TIME ANIMATION & PHYSICS UPDATE LOOP (zero allocations + dirty tracking)
  // =========================================================================
  let lastTsSoc = -1;
  let lastTsMode = -1;
  let lastCisternSoc = -1;
  let lastCisternLow = null;
  let lastHthpActive = null;
  let lastTcesActive = null;
  let lastPtesActive = null;
  let lastMofActive = null;
  let lastOrcNorm = -1;
  let firstUpdate = true;

  function update(sim, dt, elapsed) {
    if (!group.visible) return;
    const animate = dt > 0 || firstUpdate;

    // 1. Update 5 Subterranean Vaults (plain loop, zero closures/allocations)
    const vaults = sim.vaults;
    for (let idx = 0; idx < vaultNodes.length; idx++) {
      const node = vaultNodes[idx];
      const v = vaults[idx];
      if (!v) continue;

      const faulted = Boolean(v.faulted);
      const loadFactor = Math.max(0, Math.min(1, v.itMw * 0.5));
      const stateChanged = faulted !== node.lastFaulted;

      if (faulted) {
        if (stateChanged) {
          node.rackLedMat.emissive.setHex(0xef4444);
          node.hotHeaderMat.emissiveIntensity = 0.12;
          node.htsMat.emissiveIntensity = 0.18;
          node.bufferFluidMat.emissiveIntensity = 0.20;
          node.faultBeacon.visible = true;
          node.faultHalo.visible = true;
          node.lastFaulted = true;
          node.lastLoadFactor = -1;
        }
        if (animate) {
          const pulse = Math.abs(Math.sin(elapsed * 5.2));
          node.rackLedMat.emissiveIntensity = 0.55 + 1.35 * pulse;
          const sin6 = Math.sin(elapsed * 6.0);
          node.beaconMat.opacity = 0.60 + 0.38 * sin6;
          node.faultBeacon.scale.setScalar(1.0 + 0.05 * sin6);
          const haloPhase = (elapsed * 0.9) % 1;
          node.faultHalo.scale.setScalar(0.95 + haloPhase * 0.38);
          node.faultHaloMat.opacity = (1.0 - haloPhase) * 0.55;
        }
      } else {
        if (stateChanged) {
          node.rackLedMat.emissive.setHex(0x22d3ee);
          node.faultBeacon.visible = false;
          node.faultHalo.visible = false;
          node.beaconMat.opacity = 0;
          node.faultHaloMat.opacity = 0;
          node.lastFaulted = false;
        }
        if (stateChanged || Math.abs(loadFactor - node.lastLoadFactor) > 1e-4) {
          node.rackLedMat.emissiveIntensity = 0.55 + loadFactor * 1.25;
          node.hotHeaderMat.emissiveIntensity = 0.35 + loadFactor * 0.95;
          node.htsMat.emissiveIntensity = 0.65 + loadFactor * 1.05;
          node.bufferFluidMat.emissiveIntensity = 0.45 + loadFactor * 0.95;
          node.lastLoadFactor = loadFactor;
        }
      }
    }

    // 2. 2,500 m³ Stratified Thermal Store (change-detected SOC scale + pulse)
    const thermal = sim.thermal;
    const tsSoc = Math.max(0.06, Math.min(1.0, thermal.thermalStoreSoc));
    if (Math.abs(tsSoc - lastTsSoc) > 1e-4) {
      tsFluid.scale.y = tsSoc;
      tsFluid.position.y = 0.26 + (tsFluidHeight * tsSoc) * 0.5;
      tsMeniscus.position.y = 0.26 + tsFluidHeight * tsSoc;
      lastTsSoc = tsSoc;
    }

    const tsMode = thermal.thermalStoreDischargeMw > 0.1 ? 2 : thermal.thermalStoreChargeMw > 0.1 ? 1 : 0;
    const tsModeChanged = tsMode !== lastTsMode;
    if (tsModeChanged) {
      tsMeniscusMat.emissiveIntensity = tsMode === 2 ? 1.05 : tsMode === 1 ? 0.88 : 0.75;
      if (tsMode === 0) tsFluidMat.emissiveIntensity = 0.34;
      lastTsMode = tsMode;
    }
    if (tsMode === 2 && (animate || tsModeChanged)) {
      tsFluidMat.emissiveIntensity = 0.48 + 0.10 * Math.sin(elapsed * 4.2);
    } else if (tsMode === 1 && (animate || tsModeChanged)) {
      tsFluidMat.emissiveIntensity = 0.42 + 0.06 * Math.sin(elapsed * 2.5);
    }

    // 3. 10,000 m³ Drought Rainwater Cistern (change-detected SOC + caustic ripple drift)
    const cSoc = Math.max(0.05, Math.min(1.0, sim.hydrology.cisternSoc));
    const cisternMoved = Math.abs(cSoc - lastCisternSoc) > 1e-4;
    if (cisternMoved) {
      cisternWater.scale.y = cSoc;
      cisternWater.position.y = 0.25 + (maxWaterH * cSoc) * 0.5;
      lastCisternSoc = cSoc;
    }
    const isLow = cSoc < 0.48;
    if (isLow !== lastCisternLow) {
      cisternWaterMat.color.setHex(isLow ? 0x38bdf8 : 0x0284c7);
      lastCisternLow = isLow;
    }
    if (animate || cisternMoved) {
      cisternSurface.position.y = 0.28 + maxWaterH * cSoc + 0.012 * Math.sin(elapsed * 2.2);
    }
    if (dt > 0) {
      waterCausticTex.map.offset.x = (elapsed * 0.045) % 1;
      waterCausticTex.map.offset.y = (elapsed * 0.032) % 1;
      waterCausticTex.bumpMap.offset.x = (-elapsed * 0.038) % 1;
      waterCausticTex.bumpMap.offset.y = (elapsed * 0.028) % 1;
    }

    // 4. 8.0 MW_th Backup HTHP Bank fans & glowing discharge header
    const hthpActive = thermal.hthpThermalOutputMw > 0.05 || thermal.scenarioBActive;
    if (hthpActive) {
      if (animate || !lastHthpActive) {
        hthpDischargeMat.emissiveIntensity = 1.15 + 0.42 * Math.sin(elapsed * 4.5);
      }
    } else if (lastHthpActive !== false) {
      hthpDischargeMat.emissiveIntensity = 0.20;
    }
    lastHthpActive = hthpActive;
    if (dt > 0) {
      const hthpSpin = dt * (hthpActive ? 5.2 : 0.55);
      for (let i = 0; i < hthpFans.length; i++) hthpFans[i].rotation.y += hthpSpin;
    }

    // 5. Exergy Cascade Bay (TCES, TI-PTES, Micro-ORC, MOF Chillers, N+1 Dry Coolers)
    const tcesActive = thermal.tcesChargeMw > 0.05;
    if (tcesActive) {
      if (animate || !lastTcesActive) {
        tcesMat.emissiveIntensity = 1.25 + 0.25 * Math.sin(elapsed * 3.0);
      }
    } else if (lastTcesActive !== false) {
      tcesMat.emissiveIntensity = 0.22;
    }
    lastTcesActive = tcesActive;

    const ptesActive = thermal.tiPtesChargeMw > 0.05;
    if (ptesActive) {
      if (animate || !lastPtesActive) {
        const ptesIntensity = 1.20 + 0.22 * Math.sin(elapsed * 3.4);
        ptesHotMat.emissiveIntensity = ptesIntensity;
        ptesColdMat.emissiveIntensity = ptesIntensity;
      }
    } else if (lastPtesActive !== false) {
      ptesHotMat.emissiveIntensity = 0.25;
      ptesColdMat.emissiveIntensity = 0.25;
    }
    lastPtesActive = ptesActive;
    if (dt > 0) {
      ptesFlywheel.rotation.x += dt * (ptesActive ? 6.5 : 0.4);
    }

    const orcNorm = Math.max(0, Math.min(1, thermal.orcOutputKw / 440));
    if (Math.abs(orcNorm - lastOrcNorm) > 1e-4) {
      orcTurbineMat.emissiveIntensity = 0.22 + orcNorm * 1.45;
      orcBusMat.emissiveIntensity = 0.20 + orcNorm * 1.75;
      lastOrcNorm = orcNorm;
    }
    if (dt > 0) {
      orcRotor.rotation.x += dt * (0.6 + orcNorm * 11.0);
    }

    const mofActive = thermal.mofChillerMw > 0.05;
    if (mofActive) {
      if (animate || !lastMofActive) {
        mofMat.emissiveIntensity = 1.25 + 0.20 * Math.sin(elapsed * 3.2);
      }
    } else if (lastMofActive !== false) {
      mofMat.emissiveIntensity = 0.20;
    }
    lastMofActive = mofActive;

    if (dt > 0) {
      const dcSpin = dt * (0.8 + thermal.dryCoolerRejectionMw * 3.4);
      for (let i = 0; i < dryFanBlades.length; i++) dryFanBlades[i].rotation.y += dcSpin;
      // 6. Ultrapure UV-C Quartz Chamber subtle high-energy violet shimmer
      uvCoreMat.emissiveIntensity = 1.60 + 0.22 * Math.sin(elapsed * 5.0);
    }

    firstUpdate = false;
  }

  return { group, vaultNodes, update };
}
