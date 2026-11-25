import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';

const VAULT_RING_RADIUS = 24;
const VAULT_Y = -16;

const VAULT_POSITIONS = Array.from({ length: 5 }, (_, i) => {
  const angle = -Math.PI / 2 + i * ((Math.PI * 2) / 5);
  return Object.freeze({
    id: i,
    label: `Vault ${i + 1}`,
    angle,
    x: Math.cos(angle) * VAULT_RING_RADIUS,
    y: VAULT_Y,
    z: Math.sin(angle) * VAULT_RING_RADIUS,
  });
});

const v3 = VAULT_POSITIONS[2];

export const WORLD_LAYOUT = Object.freeze({
  DISTRICT_RADIUS: 72,
  VAULT_Y,
  VAULT_RING_RADIUS,
  VAULT_POSITIONS,
  CAMERA_PRESETS: Object.freeze([
    Object.freeze({
      id: 'overview',
      key: '1',
      label: '1 · Full District Overview',
      pos: [88, 62, 92],
      target: [0, -3, 0],
    }),
    Object.freeze({
      id: 'subterranean',
      key: '2',
      label: '2 · Subterranean Vaults',
      pos: [48, -3, 52],
      target: [0, -16, 0],
    }),
    Object.freeze({
      id: 'hub',
      key: '3',
      label: '3 · Central Utility Hub',
      pos: [24, -4, 27],
      target: [0, -15, 0],
    }),
    Object.freeze({
      id: 'vault3',
      key: '4',
      label: '4 · Vault 3 Close-Up',
      pos: [v3.x + 15, VAULT_Y + 12, v3.z + 18],
      target: [v3.x, VAULT_Y + 1, v3.z],
    }),
    Object.freeze({
      id: 'pods',
      key: '5',
      label: '5 · Street-Level Pods',
      pos: [22, 6.2, 14],
      target: [2, 1.8, -6],
    }),
    Object.freeze({
      id: 'topdown',
      key: '6',
      label: '6 · Top-Down Masterplan',
      pos: [0, 146, 20],
      target: [0, 0, 0],
    }),
  ]),
});

// Deterministic PRNG for reproducible geological & architectural textures
function createSeededRng(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Generates procedural Dublin geological cross-section textures:
 *  - 0.0m to -1.8m : Topsoil, urban fill & peat
 *  - -1.8m to -6.5m: Dublin glacial boulder clay (till) with cobbles
 *  - -6.5m to -14.5m: Bedded Calp limestone (Lucan Formation) & shale seams
 *  - -14.5m to -20.0m: Level -4 competent granite / deep bedrock
 */
function createGeologicalStrataTextures(maxAnisotropy) {
  if (typeof document === 'undefined') return null;

  const w = 1024;
  const h = 512;
  const albedoCanvas = document.createElement('canvas');
  albedoCanvas.width = w;
  albedoCanvas.height = h;
  const ctx = albedoCanvas.getContext('2d');

  const bumpCanvas = document.createElement('canvas');
  bumpCanvas.width = w;
  bumpCanvas.height = h;
  const bCtx = bumpCanvas.getContext('2d');

  const rng = createSeededRng(20451010);

  // Seamless horizontal wave offset for natural sedimentary bedding
  const strataWave = (x, amp, freq1, phase1, freq2, phase2) => {
    const u = (x / w) * Math.PI * 2;
    return Math.sin(u * freq1 + phase1) * amp + Math.cos(u * freq2 + phase2) * (amp * 0.45);
  };

  // Base vertical gradient
  const baseGrad = ctx.createLinearGradient(0, 0, 0, h);
  baseGrad.addColorStop(0.0, '#362d24');   // Topsoil / peat
  baseGrad.addColorStop(0.08, '#2b231c');
  baseGrad.addColorStop(0.11, '#4c4136');  // Glacial boulder clay
  baseGrad.addColorStop(0.31, '#3f3831');
  baseGrad.addColorStop(0.35, '#2b3646');  // Bedded Calp limestone
  baseGrad.addColorStop(0.56, '#232e3d');
  baseGrad.addColorStop(0.73, '#1c2533');
  baseGrad.addColorStop(0.77, '#1a2230');  // Level -4 deep granite bedrock
  baseGrad.addColorStop(1.0, '#101622');
  ctx.fillStyle = baseGrad;
  ctx.fillRect(0, 0, w, h);

  bCtx.fillStyle = '#808080';
  bCtx.fillRect(0, 0, w, h);

  // Helper to paint wavy sedimentary bands cleanly wrapping horizontally
  function drawStrataBand(yStart, yEnd, fillColor, bumpVal, amp, f1, p1, f2, p2, alpha = 1) {
    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.fillStyle = fillColor;
    ctx.beginPath();
    for (let x = 0; x <= w; x += 8) {
      const y = yStart + strataWave(x, amp, f1, p1, f2, p2);
      if (x === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    for (let x = w; x >= 0; x -= 8) {
      const y = yEnd + strataWave(x, amp * 0.9, f1, p1 + 0.35, f2, p2 - 0.25);
      ctx.lineTo(x, y);
    }
    ctx.closePath();
    ctx.fill();
    ctx.restore();

    bCtx.save();
    bCtx.globalAlpha = alpha * 0.85;
    bCtx.fillStyle = `rgb(${bumpVal},${bumpVal},${bumpVal})`;
    bCtx.beginPath();
    for (let x = 0; x <= w; x += 8) {
      const y = yStart + strataWave(x, amp, f1, p1, f2, p2);
      if (x === 0) bCtx.moveTo(x, y);
      else bCtx.lineTo(x, y);
    }
    for (let x = w; x >= 0; x -= 8) {
      const y = yEnd + strataWave(x, amp * 0.9, f1, p1 + 0.35, f2, p2 - 0.25);
      bCtx.lineTo(x, y);
    }
    bCtx.closePath();
    bCtx.fill();
    bCtx.restore();
  }

  // 1. Topsoil & urban sub-base (0% - 10%)
  drawStrataBand(0, 26, '#3e3429', 155, 2.2, 2, 0.3, 5, 1.1, 0.85);
  drawStrataBand(22, 46, '#2d241c', 110, 3.0, 3, 1.4, 6, 0.2, 0.9);

  // Fine topsoil gravel & root speckles
  for (let i = 0; i < 950; i++) {
    const x = rng() * w;
    const y = rng() * 48;
    const r = 0.7 + rng() * 1.9;
    const light = rng() > 0.55;
    ctx.fillStyle = light ? 'rgba(130,115,95,0.35)' : 'rgba(22,17,13,0.45)';
    ctx.fillRect(x, y, r, r * 0.7);
    bCtx.fillStyle = light ? 'rgba(210,210,210,0.4)' : 'rgba(55,55,55,0.4)';
    bCtx.fillRect(x, y, r, r * 0.7);
  }

  // 2. Glacial Boulder Clay / Dublin Till (10% - 34%)
  const tillBands = [
    [44, 72, '#54483c', 140],
    [68, 102, '#4a4036', 125],
    [98, 138, '#5a4e40', 150],
    [132, 174, '#413933', 115],
  ];
  tillBands.forEach(([y0, y1, col, bv], i) => {
    drawStrataBand(y0, y1, col, bv, 4.2, 2, i * 1.3, 4, i * 0.7, 0.75);
  });

  // Embedded rounded limestone cobbles & glacial pebbles in boulder clay
  for (let i = 0; i < 340; i++) {
    const x = rng() * w;
    const y = 52 + rng() * 116;
    const rx = 2.0 + rng() * 7.5;
    const ry = 1.4 + rng() * 4.2;
    const rot = (rng() - 0.5) * 0.5;
    const shade = Math.floor(82 + rng() * 58);

    // Draw wrapped across seam if near horizontal edge
    for (const dx of [0, x < 16 ? w : x > w - 16 ? -w : 9999]) {
      if (dx === 9999) continue;
      ctx.save();
      ctx.translate(x + dx, y);
      ctx.rotate(rot);
      ctx.fillStyle = `rgba(${shade},${shade - 4},${shade - 10},0.65)`;
      ctx.beginPath();
      ctx.ellipse(0, 0, rx, ry, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = 'rgba(24,20,16,0.45)';
      ctx.lineWidth = 0.8;
      ctx.stroke();
      ctx.restore();

      bCtx.save();
      bCtx.translate(x + dx, y);
      bCtx.rotate(rot);
      bCtx.fillStyle = 'rgba(225,225,225,0.72)';
      bCtx.beginPath();
      bCtx.ellipse(0, 0, rx, ry, 0, 0, Math.PI * 2);
      bCtx.fill();
      bCtx.restore();
    }
  }

  // 3. Bedded Calp Limestone & Shale Partings (34% - 75%, y = 174..382)
  let curY = 174;
  let bedIdx = 0;
  while (curY < 380) {
    const thick = 8 + rng() * 14;
    const isShale = bedIdx % 3 === 1;
    const col = isShale
      ? '#1d2633'
      : bedIdx % 2 === 0
        ? '#313e50'
        : '#3a495d';
    const bv = isShale ? 88 : 162 + Math.floor(rng() * 35);
    drawStrataBand(
      curY,
      curY + thick,
      col,
      bv,
      2.6,
      1,
      bedIdx * 0.4,
      3,
      bedIdx * 0.9,
      0.88
    );

    // Dark sedimentary bedding seam line
    ctx.strokeStyle = 'rgba(10,15,24,0.68)';
    ctx.lineWidth = isShale ? 1.4 : 1.0;
    ctx.beginPath();
    for (let x = 0; x <= w; x += 8) {
      const y = curY + strataWave(x, 2.6, 1, bedIdx * 0.4, 3, bedIdx * 0.9);
      if (x === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();

    bCtx.strokeStyle = 'rgba(25,25,25,0.85)';
    bCtx.lineWidth = 1.6;
    bCtx.beginPath();
    for (let x = 0; x <= w; x += 8) {
      const y = curY + strataWave(x, 2.6, 1, bedIdx * 0.4, 3, bedIdx * 0.9);
      if (x === 0) bCtx.moveTo(x, y);
      else bCtx.lineTo(x, y);
    }
    bCtx.stroke();

    // Vertical joint fractures inside limestone beds
    if (!isShale) {
      const joints = 6 + Math.floor(rng() * 7);
      for (let j = 0; j < joints; j++) {
        const jx = rng() * w;
        const jTop = curY + strataWave(jx, 2.6, 1, bedIdx * 0.4, 3, bedIdx * 0.9);
        const tilt = (rng() - 0.5) * 4;
        ctx.strokeStyle = rng() > 0.72 ? 'rgba(148,175,205,0.28)' : 'rgba(10,16,26,0.55)';
        ctx.lineWidth = 0.9;
        ctx.beginPath();
        ctx.moveTo(jx, jTop);
        ctx.lineTo(jx + tilt, jTop + thick);
        ctx.stroke();

        bCtx.strokeStyle = 'rgba(38,38,38,0.7)';
        bCtx.lineWidth = 1.1;
        bCtx.beginPath();
        bCtx.moveTo(jx, jTop);
        bCtx.lineTo(jx + tilt, jTop + thick);
        bCtx.stroke();
      }
    }

    curY += thick;
    bedIdx++;
  }

  // 4. Level -4 Competent Granite / Deep Bedrock (75% - 100%, y = 382..512)
  drawStrataBand(378, 512, '#1a2332', 138, 3.0, 2, 0.8, 5, 2.1, 0.92);

  // Crystalline granite mineral speckling (feldspar, quartz, biotite)
  for (let i = 0; i < 4200; i++) {
    const x = rng() * w;
    const y = 380 + rng() * (h - 380);
    const sz = 0.8 + rng() * 2.2;
    const roll = rng();
    if (roll > 0.76) {
      ctx.fillStyle = 'rgba(118,140,168,0.34)';
      bCtx.fillStyle = 'rgba(220,220,220,0.45)';
    } else if (roll > 0.42) {
      ctx.fillStyle = 'rgba(56,72,94,0.42)';
      bCtx.fillStyle = 'rgba(155,155,155,0.35)';
    } else {
      ctx.fillStyle = 'rgba(11,16,25,0.55)';
      bCtx.fillStyle = 'rgba(55,55,55,0.5)';
    }
    ctx.fillRect(x, y, sz, sz);
    bCtx.fillRect(x, y, sz, sz);
  }

  // Subtle architectural Level -4 datum horizon line at ~80% depth (y ≈ -16m)
  const datumY = Math.round(h * 0.80);
  ctx.strokeStyle = 'rgba(34, 211, 238, 0.34)';
  ctx.lineWidth = 1.5;
  ctx.setLineDash([12, 10]);
  ctx.beginPath();
  ctx.moveTo(0, datumY);
  ctx.lineTo(w, datumY);
  ctx.stroke();
  ctx.setLineDash([]);

  // Fine surface micro-grain across entire cutaway
  for (let i = 0; i < 2400; i++) {
    const x = rng() * w;
    const y = rng() * h;
    const alpha = 0.05 + rng() * 0.08;
    const dark = rng() > 0.5;
    ctx.fillStyle = dark ? `rgba(0,0,0,${alpha})` : `rgba(200,220,240,${alpha * 0.6})`;
    ctx.fillRect(x, y, 2, 1.5);
  }

  const map = new THREE.CanvasTexture(albedoCanvas);
  map.colorSpace = THREE.SRGBColorSpace;
  map.wrapS = THREE.RepeatWrapping;
  map.wrapT = THREE.ClampToEdgeWrapping;
  map.repeat.set(4, 1);
  map.anisotropy = maxAnisotropy;

  const bumpMap = new THREE.CanvasTexture(bumpCanvas);
  bumpMap.wrapS = THREE.RepeatWrapping;
  bumpMap.wrapT = THREE.ClampToEdgeWrapping;
  bumpMap.repeat.set(4, 1);
  bumpMap.anisotropy = maxAnisotropy;

  return { map, bumpMap };
}

/**
 * Generates procedural Level -4 bedrock/engineered concrete floor texture.
 */
function createBedrockFloorTexture(maxAnisotropy) {
  if (typeof document === 'undefined') return null;

  const size = 512;
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d');
  const rng = createSeededRng(44109);

  ctx.fillStyle = '#0e1624';
  ctx.fillRect(0, 0, size, size);

  // Subtle rock/concrete aggregate speckles
  for (let i = 0; i < 5200; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const s = 1 + rng() * 2.5;
    const bright = rng() > 0.58;
    ctx.fillStyle = bright ? 'rgba(56, 78, 106, 0.16)' : 'rgba(5, 9, 16, 0.28)';
    ctx.fillRect(x, y, s, s);
  }

  // Engineered concrete joint grid
  ctx.strokeStyle = 'rgba(34, 211, 238, 0.08)';
  ctx.lineWidth = 1;
  const step = size / 8;
  for (let i = 0; i <= 8; i++) {
    const p = i * step + 0.5;
    ctx.beginPath();
    ctx.moveTo(p, 0);
    ctx.lineTo(p, size);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(0, p);
    ctx.lineTo(size, p);
    ctx.stroke();
  }

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  tex.repeat.set(5, 5);
  tex.anisotropy = maxAnisotropy;
  return tex;
}

/**
 * Generates subtle architectural site-model ground texture for the X-Ray ground disk.
 */
function createGroundSurfaceTexture(maxAnisotropy) {
  if (typeof document === 'undefined') return null;

  const size = 512;
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d');
  const rng = createSeededRng(88213);

  const cx = size / 2;
  const cy = size / 2;

  const radGrad = ctx.createRadialGradient(cx, cy, 12, cx, cy, size * 0.5);
  radGrad.addColorStop(0.0, '#1d2d42');
  radGrad.addColorStop(0.55, '#172537');
  radGrad.addColorStop(1.0, '#131e2e');
  ctx.fillStyle = radGrad;
  ctx.fillRect(0, 0, size, size);

  // Subtle organic earth/urban micro-grain
  for (let i = 0; i < 4200; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const s = 1 + rng() * 2;
    ctx.fillStyle = rng() > 0.5 ? 'rgba(90, 122, 156, 0.07)' : 'rgba(8, 14, 22, 0.14)';
    ctx.fillRect(x, y, s, s);
  }

  // Delicate architectural masterplan concentric survey circles
  ctx.strokeStyle = 'rgba(56, 189, 248, 0.09)';
  ctx.lineWidth = 1;
  for (const frac of [0.18, 0.34, 0.53, 0.72, 0.90]) {
    ctx.beginPath();
    ctx.arc(cx, cy, size * 0.5 * frac, 0, Math.PI * 2);
    ctx.stroke();
  }

  // Subtle radial masterplan axes
  ctx.strokeStyle = 'rgba(148, 163, 184, 0.06)';
  for (let i = 0; i < 10; i++) {
    const a = (i / 10) * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx + Math.cos(a) * size * 0.5, cy + Math.sin(a) * size * 0.5);
    ctx.stroke();
  }

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = maxAnisotropy;
  return tex;
}

/**
 * Generates architectural elevation ruler texture for cardinal cutaway depth markers.
 */
function createDepthScaleTexture(maxAnisotropy) {
  if (typeof document === 'undefined') return null;

  const w = 160;
  const h = 512;
  const canvas = document.createElement('canvas');
  canvas.width = w;
  canvas.height = h;
  const ctx = canvas.getContext('2d');

  ctx.clearRect(0, 0, w, h);

  // Vertical ruler spine
  ctx.strokeStyle = 'rgba(56, 189, 248, 0.55)';
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(22, 8);
  ctx.lineTo(22, h - 8);
  ctx.stroke();

  // Minor ticks every 1m (20m total depth)
  for (let m = 0; m <= 20; m++) {
    const y = 12 + (m / 20) * (h - 24);
    const major = m % 5 === 0 || m === 16;
    ctx.strokeStyle = major ? 'rgba(56, 189, 248, 0.85)' : 'rgba(148, 163, 184, 0.42)';
    ctx.lineWidth = major ? 2 : 1;
    ctx.beginPath();
    ctx.moveTo(22, y);
    ctx.lineTo(major ? 46 : 34, y);
    ctx.stroke();
  }

  // Architectural depth labels
  ctx.font = '600 18px ui-monospace, SFMono-Regular, Menlo, Consolas, monospace';
  ctx.textBaseline = 'middle';
  const labels = [
    { m: 0, text: '0m GL', col: '#bae6fd' },
    { m: 5, text: '−5m TILL', col: '#cbd5e1' },
    { m: 10, text: '−10m CALP', col: '#94a3b8' },
    { m: 16, text: '−16m L−4', col: '#38bdf8' },
    { m: 20, text: '−20m ROCK', col: '#94a3b8' },
  ];
  for (const item of labels) {
    const y = 12 + (item.m / 20) * (h - 24);
    ctx.fillStyle = item.col;
    ctx.fillText(item.text, 52, y);
  }

  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = maxAnisotropy;
  return tex;
}

/**
 * Creates a custom GLSL procedural atmospheric sky dome synchronized to
 * timeOfDay, ambientTempC, and rainfallMultiplier.
 */
function createSkyDome() {
  const uniforms = {
    uSunDir: { value: new THREE.Vector3(0.4, 0.8, 0.4).normalize() },
    uZenithColor: { value: new THREE.Color(0x0d2140) },
    uMidSkyColor: { value: new THREE.Color(0x1d406b) },
    uHorizonColor: { value: new THREE.Color(0x496f96) },
    uNadirColor: { value: new THREE.Color(0x1c3552) },
    uSunGlowColor: { value: new THREE.Color(0xffdfa8) },
    uSunIntensity: { value: 0.75 },
    uHorizonGlow: { value: 0.25 },
    uStarVisibility: { value: 0.0 },
    uCloudCover: { value: 0.35 },
    uTime: { value: 0.0 },
  };

  const vertexShader = /* glsl */ `
    varying vec3 vDir;
    void main() {
      vDir = normalize(position);
      gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
    }
  `;

  const fragmentShader = /* glsl */ `
    uniform vec3 uSunDir;
    uniform vec3 uZenithColor;
    uniform vec3 uMidSkyColor;
    uniform vec3 uHorizonColor;
    uniform vec3 uNadirColor;
    uniform vec3 uSunGlowColor;
    uniform float uSunIntensity;
    uniform float uHorizonGlow;
    uniform float uStarVisibility;
    uniform float uCloudCover;
    uniform float uTime;

    varying vec3 vDir;

    float hash21(vec2 p) {
      p = fract(p * vec2(123.34, 456.21));
      p += dot(p, p + 45.32);
      return fract(p.x * p.y);
    }

    float hash31(vec3 p) {
      p = fract(p * vec3(443.897, 441.423, 437.195));
      p += dot(p, p.yzx + 19.19);
      return fract((p.x + p.y) * p.z);
    }

    float valueNoise(vec2 p) {
      vec2 i = floor(p);
      vec2 f = fract(p);
      f = f * f * (3.0 - 2.0 * f);
      float a = hash21(i);
      float b = hash21(i + vec2(1.0, 0.0));
      float c = hash21(i + vec2(0.0, 1.0));
      float d = hash21(i + vec2(1.0, 1.0));
      return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
    }

    float fbm(vec2 p) {
      float sum = 0.0;
      float amp = 0.5;
      mat2 rot = mat2(0.80, -0.60, 0.60, 0.80);
      for (int i = 0; i < 4; i++) {
        sum += amp * valueNoise(p);
        p = rot * p * 2.04 + vec2(13.1, 7.7);
        amp *= 0.5;
      }
      return sum;
    }

    float starLayer(vec3 dir, float scale, float threshold) {
      vec3 cell = floor(dir * scale);
      vec3 local = fract(dir * scale) - 0.5;
      float rnd = hash31(cell);
      if (rnd < threshold) return 0.0;
      vec3 jitter = vec3(
        hash31(cell + 11.7) - 0.5,
        hash31(cell + 27.3) - 0.5,
        hash31(cell + 43.1) - 0.5
      ) * 0.58;
      float d = length(local - jitter);
      float star = smoothstep(0.16, 0.015, d);
      float twinkle = 0.72 + 0.28 * sin(uTime * (1.2 + rnd * 2.4) + rnd * 62.83);
      return star * twinkle * smoothstep(threshold, 1.0, rnd);
    }

    void main() {
      vec3 dir = normalize(vDir);
      float y = dir.y;

      // Base upper & lower atmospheric dome gradient
      vec3 col;
      if (y >= 0.0) {
        float tMid = smoothstep(0.0, 0.26, y);
        float tZenith = smoothstep(0.16, 0.86, y);
        col = mix(uHorizonColor, uMidSkyColor, tMid);
        col = mix(col, uZenithColor, tZenith);
      } else {
        float tDown = smoothstep(0.0, -0.55, y);
        col = mix(uHorizonColor * 0.78, uNadirColor, tDown);
      }

      // Directional Mie horizon & solar corona scattering
      float sunDot = max(dot(dir, normalize(uSunDir)), 0.0);
      float horizonBand = exp(-abs(y) * 7.5);
      float clearSky = 1.0 - uCloudCover * 0.72;

      // Warm sunrise / sunset atmospheric belt
      float sunAzimuthGlow = pow(sunDot, 3.0) * horizonBand * uHorizonGlow * (0.45 + 0.55 * clearSky);
      col += uSunGlowColor * sunAzimuthGlow * 0.62;

      // Soft solar halo + subtle disc (bounded so bloom stays refined)
      if (y > -0.05 && uSunIntensity > 0.01) {
        float halo = pow(sunDot, 22.0) * 0.30 * uSunIntensity * clearSky;
        float disc = smoothstep(0.9985, 0.9996, sunDot) * 0.65 * uSunIntensity * clearSky;
        col += uSunGlowColor * (halo + disc);
      }

      // High-altitude wispy & overcast cloud layer
      if (y > 0.01 && uCloudCover > 0.02) {
        vec2 skyUv = dir.xz / (y + 0.22);
        vec2 drift = vec2(uTime * 0.0085, uTime * 0.0032);
        float n = fbm(skyUv * 1.15 + drift);
        float density = smoothstep(0.66 - uCloudCover * 0.42, 0.92 - uCloudCover * 0.22, n);
        float horizonFade = smoothstep(0.01, 0.22, y);
        vec3 cloudLit = mix(uMidSkyColor * 1.18, uSunGlowColor * 0.72 + uHorizonColor * 0.45, pow(sunDot, 2.5) * 0.45 + uHorizonGlow * 0.25);
        vec3 cloudShade = mix(uZenithColor * 0.95, uHorizonColor * 0.62, 0.45);
        vec3 cloudCol = mix(cloudShade, cloudLit, smoothstep(0.25, 0.85, n) * (0.35 + 0.65 * uSunIntensity));
        col = mix(col, cloudCol, density * horizonFade * clamp(uCloudCover * 0.82, 0.0, 0.78));
      }

      // Crisp procedural starfield during twilight & night
      if (y > 0.02 && uStarVisibility > 0.01) {
        float starMask = smoothstep(0.03, 0.28, y) * uStarVisibility * (1.0 - uCloudCover * 0.85);
        float s1 = starLayer(dir, 145.0, 0.968);
        float s2 = starLayer(dir + vec3(0.17, 0.31, 0.09), 235.0, 0.982) * 0.72;
        vec3 starTint = mix(vec3(0.75, 0.86, 1.0), vec3(1.0, 0.92, 0.80), hash31(floor(dir * 145.0) + 5.1));
        col += starTint * (s1 + s2) * starMask * 0.85;
      }

      gl_FragColor = vec4(col, 1.0);
      #include <tonemapping_fragment>
      #include <colorspace_fragment>
    }
  `;

  const geom = new THREE.SphereGeometry(480, 48, 32);
  const mat = new THREE.ShaderMaterial({
    uniforms,
    vertexShader,
    fragmentShader,
    side: THREE.BackSide,
    depthWrite: false,
    fog: false,
  });

  const mesh = new THREE.Mesh(geom, mat);
  mesh.name = 'atmospheric_sky_dome';
  mesh.frustumCulled = false;
  mesh.renderOrder = -100;

  return { mesh, uniforms, geometry: geom, material: mat };
}

export function createScene(container) {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0b1220);
  scene.fog = new THREE.FogExp2(0x16263c, 0.0023);

  const width = Math.max(container.clientWidth || window.innerWidth, 1);
  const height = Math.max(container.clientHeight || window.innerHeight, 1);

  const camera = new THREE.PerspectiveCamera(46, width / height, 0.5, 1100);
  const initPreset = WORLD_LAYOUT.CAMERA_PRESETS[0];
  camera.position.set(...initPreset.pos);

  const renderer = new THREE.WebGLRenderer({
    antialias: true,
    powerPreference: 'high-performance',
  });
  const pixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);
  renderer.setPixelRatio(pixelRatio);
  renderer.setSize(width, height);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.08;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.shadowMap.autoUpdate = false;
  renderer.shadowMap.needsUpdate = true;
  renderer.info.autoReset = false;
  container.appendChild(renderer.domElement);

  const maxAnisotropy = Math.min(4, renderer.capabilities.getMaxAnisotropy?.() || 1);

  function markShadowsDirty() {
    renderer.shadowMap.needsUpdate = true;
  }

  // 1. Image-Based Lighting (IBL) via PMREMGenerator + RoomEnvironment
  let envRT = null;
  try {
    const pmremGenerator = new THREE.PMREMGenerator(renderer);
    const roomEnv = new RoomEnvironment();
    envRT = pmremGenerator.fromScene(roomEnv, 0.04);
    scene.environment = envRT.texture;
    if ('environmentIntensity' in scene) {
      scene.environmentIntensity = 0.72;
    }
    roomEnv.dispose();
    pmremGenerator.dispose();
  } catch (_) {
    envRT = null;
  }

  // 2. HDR Post-Processing Pipeline (EffectComposer + RenderPass + half-res UnrealBloomPass + OutputPass)
  let composer = null;
  let bloomPass = null;
  let outputPass = null;
  let composerRT = null;
  try {
    const gl = renderer.getContext();
    const supportsHalfFloat = Boolean(
      gl.getExtension('EXT_color_buffer_float') || gl.getExtension('EXT_color_buffer_half_float')
    );
    const maxSamples = renderer.capabilities.maxSamples || 0;
    const targetSamples = pixelRatio > 1.25 ? 2 : 4;
    const rtOptions = {
      samples: Math.min(targetSamples, maxSamples),
    };
    if (supportsHalfFloat) {
      rtOptions.type = THREE.HalfFloatType;
    }

    composerRT = new THREE.WebGLRenderTarget(width, height, rtOptions);
    composer = new EffectComposer(renderer, composerRT);
    composer.setPixelRatio(pixelRatio);
    composer.setSize(width, height);

    const renderPass = new RenderPass(scene, camera);
    composer.addPass(renderPass);

    const bloomW = Math.max(1, Math.floor(width * 0.5));
    const bloomH = Math.max(1, Math.floor(height * 0.5));
    bloomPass = new UnrealBloomPass(
      new THREE.Vector2(bloomW, bloomH),
      0.30, // gentle cinematic strength
      0.44, // refined halo radius
      0.96  // high threshold so architecture & cavern hub stay crisp without glare
    );
    composer.addPass(bloomPass);

    outputPass = new OutputPass();
    composer.addPass(outputPass);

    // Ensure bloom stays at half-resolution after composer.setSize
    bloomPass.setSize(bloomW, bloomH);
  } catch (_) {
    if (composerRT) composerRT.dispose();
    composerRT = null;
    composer = null;
    bloomPass = null;
    outputPass = null;
  }

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.065;
  controls.minDistance = 10;
  controls.maxDistance = 260;
  controls.maxPolarAngle = Math.PI * 0.58;
  controls.target.set(...initPreset.target);
  controls.update();

  // 3. Procedural Atmospheric Sky Dome
  const skyDome = createSkyDome();
  skyDome.mesh.position.copy(camera.position);
  scene.add(skyDome.mesh);

  // Dynamic Surface & Atmospheric Lighting Rig
  const hemiLight = new THREE.HemisphereLight(0x9ec5ff, 0x162238, 0.72);
  scene.add(hemiLight);

  const sunLight = new THREE.DirectionalLight(0xfff4e0, 1.9);
  sunLight.position.set(60, 95, 45);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.set(2048, 2048);
  sunLight.shadow.camera.near = 10;
  sunLight.shadow.camera.far = 250;
  sunLight.shadow.camera.left = -80;
  sunLight.shadow.camera.right = 80;
  sunLight.shadow.camera.top = 80;
  sunLight.shadow.camera.bottom = -80;
  sunLight.shadow.bias = -0.00035;
  sunLight.shadow.normalBias = 0.045;
  sunLight.shadow.radius = 2.4;
  scene.add(sunLight);

  // Cool opposing atmospheric rim/fill directional light
  const fillLight = new THREE.DirectionalLight(0x60a5fa, 0.44);
  fillLight.position.set(-70, 38, -65);
  scene.add(fillLight);

  // Subterranean Level -4 Cavern Accent Lighting (balanced to avoid center hub hotspot)
  const cavernLight = new THREE.PointLight(0x22d3ee, 7.0, 118, 1.35);
  cavernLight.position.set(0, -10.5, 0);
  scene.add(cavernLight);

  const cavernWarmLight = new THREE.PointLight(0xff8a4c, 3.8, 88, 1.45);
  cavernWarmLight.position.set(0, -14.4, 0);
  scene.add(cavernWarmLight);

  const cavernRimA = new THREE.PointLight(0x38bdf8, 5.0, 76, 1.5);
  cavernRimA.position.set(30, -11.8, 24);
  scene.add(cavernRimA);

  const cavernRimB = new THREE.PointLight(0x2dd4bf, 4.5, 76, 1.5);
  cavernRimB.position.set(-30, -11.8, -24);
  scene.add(cavernRimB);

  // 4. High-Fidelity Geological Cutaway Plinth
  const disposableTextures = [];
  const disposableGeometries = [skyDome.geometry];
  const disposableMaterials = [skyDome.material];

  const plinthGroup = new THREE.Group();
  plinthGroup.name = 'geological_plinth';
  scene.add(plinthGroup);

  const bedrockTex = createBedrockFloorTexture(maxAnisotropy);
  if (bedrockTex) disposableTextures.push(bedrockTex);

  const bedrockGeom = new THREE.CylinderGeometry(
    WORLD_LAYOUT.DISTRICT_RADIUS + 1.8,
    WORLD_LAYOUT.DISTRICT_RADIUS + 2.8,
    2.0,
    96
  );
  const bedrockMat = new THREE.MeshStandardMaterial({
    color: 0x182334,
    map: bedrockTex || null,
    roughness: 0.88,
    metalness: 0.16,
  });
  disposableGeometries.push(bedrockGeom);
  disposableMaterials.push(bedrockMat);

  const bedrockFloor = new THREE.Mesh(bedrockGeom, bedrockMat);
  bedrockFloor.position.y = -21;
  bedrockFloor.receiveShadow = true;
  plinthGroup.add(bedrockFloor);

  // Lower machined steel base flange framing the bedrock plinth
  const baseFlangeGeom = new THREE.TorusGeometry(WORLD_LAYOUT.DISTRICT_RADIUS + 2.2, 0.34, 12, 96);
  const metalTrimMat = new THREE.MeshStandardMaterial({
    color: 0x475569,
    roughness: 0.28,
    metalness: 0.84,
  });
  disposableGeometries.push(baseFlangeGeom);
  disposableMaterials.push(metalTrimMat);

  const baseFlange = new THREE.Mesh(baseFlangeGeom, metalTrimMat);
  baseFlange.rotation.x = Math.PI / 2;
  baseFlange.position.y = -20.12;
  plinthGroup.add(baseFlange);

  // Level -4 engineering grid & concentric bedrock vault datum rings
  const cavernGrid = new THREE.GridHelper(134, 28, 0x1e3a5f, 0x122238);
  cavernGrid.position.y = -19.95;
  if (Array.isArray(cavernGrid.material)) {
    cavernGrid.material.forEach((m) => {
      m.transparent = true;
      m.opacity = 0.55;
    });
  } else {
    cavernGrid.material.transparent = true;
    cavernGrid.material.opacity = 0.55;
  }
  plinthGroup.add(cavernGrid);

  const datumRingMat = new THREE.MeshBasicMaterial({
    color: 0x22d3ee,
    transparent: true,
    opacity: 0.24,
    side: THREE.DoubleSide,
    depthWrite: false,
  });
  disposableMaterials.push(datumRingMat);

  for (const r of [WORLD_LAYOUT.VAULT_RING_RADIUS, 48, WORLD_LAYOUT.DISTRICT_RADIUS - 0.5]) {
    const rGeom = new THREE.RingGeometry(r - 0.14, r + 0.14, 96);
    disposableGeometries.push(rGeom);
    const ring = new THREE.Mesh(rGeom, datumRingMat);
    ring.rotation.x = -Math.PI / 2;
    ring.position.y = -19.93;
    plinthGroup.add(ring);
  }

  // Procedural Dublin Geological Strata Cutaway Wall (Topsoil -> Boulder Clay -> Calp Limestone -> Granite Bedrock)
  const strataTex = createGeologicalStrataTextures(maxAnisotropy);
  if (strataTex) {
    disposableTextures.push(strataTex.map, strataTex.bumpMap);
  }

  const strataHeight = 19.65;
  const strataGeom = new THREE.CylinderGeometry(
    WORLD_LAYOUT.DISTRICT_RADIUS + 1.12,
    WORLD_LAYOUT.DISTRICT_RADIUS + 1.58,
    strataHeight,
    96,
    1,
    true
  );
  const strataMat = new THREE.MeshStandardMaterial({
    color: 0xdbe6f2,
    map: strataTex?.map || null,
    bumpMap: strataTex?.bumpMap || null,
    bumpScale: 0.58,
    roughness: 0.88,
    metalness: 0.08,
    transparent: true,
    opacity: 0.64,
    side: THREE.DoubleSide,
    depthWrite: false,
  });
  disposableGeometries.push(strataGeom);
  disposableMaterials.push(strataMat);

  const strataSkirt = new THREE.Mesh(strataGeom, strataMat);
  strataSkirt.position.y = -0.38 - strataHeight / 2;
  plinthGroup.add(strataSkirt);

  // Subtle exterior Level -4 engineering datum band around geological cutaway
  const l4BandGeom = new THREE.TorusGeometry(WORLD_LAYOUT.DISTRICT_RADIUS + 1.56, 0.09, 8, 96);
  const l4BandMat = new THREE.MeshStandardMaterial({
    color: 0x38bdf8,
    emissive: 0x0ea5e9,
    emissiveIntensity: 0.72,
    roughness: 0.3,
    metalness: 0.5,
    transparent: true,
    opacity: 0.65,
  });
  disposableGeometries.push(l4BandGeom);
  disposableMaterials.push(l4BandMat);

  const l4Band = new THREE.Mesh(l4BandGeom, l4BandMat);
  l4Band.rotation.x = Math.PI / 2;
  l4Band.position.y = VAULT_Y;
  plinthGroup.add(l4Band);

  // Crisp Metallic Bevelled Rim Collar around the District Plinth
  const R = WORLD_LAYOUT.DISTRICT_RADIUS;
  const bevelProfile = [
    new THREE.Vector2(R + 0.55, -0.45),
    new THREE.Vector2(R + 0.75, 0.02),
    new THREE.Vector2(R + 1.15, 0.14),
    new THREE.Vector2(R + 1.95, 0.14),
    new THREE.Vector2(R + 2.35, -0.06),
    new THREE.Vector2(R + 2.35, -0.52),
    new THREE.Vector2(R + 1.35, -0.62),
  ];
  const bevelGeom = new THREE.LatheGeometry(bevelProfile, 96);
  disposableGeometries.push(bevelGeom);

  const bevelRim = new THREE.Mesh(bevelGeom, metalTrimMat);
  bevelRim.receiveShadow = true;
  plinthGroup.add(bevelRim);

  // Flush Glowing Cyan Telemetry Inlay Ring embedded on the Bevelled Rim
  const inlayGeom = new THREE.TorusGeometry(R + 1.48, 0.085, 10, 96);
  const inlayMat = new THREE.MeshStandardMaterial({
    color: 0x67e8f9,
    emissive: 0x06b6d4,
    emissiveIntensity: 1.05,
    roughness: 0.2,
    metalness: 0.3,
  });
  disposableGeometries.push(inlayGeom);
  disposableMaterials.push(inlayMat);

  const rimInlay = new THREE.Mesh(inlayGeom, inlayMat);
  rimInlay.rotation.x = Math.PI / 2;
  rimInlay.position.y = 0.145;
  plinthGroup.add(rimInlay);

  // Architectural Azimuth Tick Marks around the Metallic Rim (every 10 deg)
  const tickGeom = new THREE.BoxGeometry(0.14, 0.04, 0.78);
  const tickMat = new THREE.MeshStandardMaterial({
    color: 0x94a3b8,
    emissive: 0x0ea5e9,
    emissiveIntensity: 0.32,
    roughness: 0.32,
    metalness: 0.75,
  });
  disposableGeometries.push(tickGeom);
  disposableMaterials.push(tickMat);

  const tickCount = 36;
  const rimTicks = new THREE.InstancedMesh(tickGeom, tickMat, tickCount);
  const dummy = new THREE.Object3D();
  for (let i = 0; i < tickCount; i++) {
    const angle = (i / tickCount) * Math.PI * 2;
    const isCardinal = i % 9 === 0;
    const rad = R + 1.85;
    dummy.position.set(Math.cos(angle) * rad, 0.155, Math.sin(angle) * rad);
    dummy.rotation.set(0, -angle + Math.PI / 2, 0);
    dummy.scale.set(isCardinal ? 1.8 : 1.0, 1.0, isCardinal ? 1.35 : 0.78);
    dummy.updateMatrix();
    rimTicks.setMatrixAt(i, dummy.matrix);
  }
  rimTicks.instanceMatrix.needsUpdate = true;
  plinthGroup.add(rimTicks);

  // Cardinal Geological Depth Elevation Scales along the Cutaway Exterior
  const depthScaleTex = createDepthScaleTexture(maxAnisotropy);
  if (depthScaleTex) {
    disposableTextures.push(depthScaleTex);
    const scaleGeom = new THREE.PlaneGeometry(6.2, strataHeight);
    const scaleMat = new THREE.MeshBasicMaterial({
      map: depthScaleTex,
      transparent: true,
      opacity: 0.78,
      side: THREE.FrontSide,
      depthWrite: false,
    });
    disposableGeometries.push(scaleGeom);
    disposableMaterials.push(scaleMat);

    const scaleInst = new THREE.InstancedMesh(scaleGeom, scaleMat, 4);
    for (let i = 0; i < 4; i++) {
      const angle = i * (Math.PI / 2) + Math.PI / 4;
      const dist = R + 2.05;
      dummy.position.set(Math.cos(angle) * dist, -0.38 - strataHeight / 2, Math.sin(angle) * dist);
      dummy.rotation.set(0, -angle + Math.PI / 2, 0);
      dummy.scale.set(1, 1, 1);
      dummy.updateMatrix();
      scaleInst.setMatrixAt(i, dummy.matrix);
    }
    scaleInst.instanceMatrix.needsUpdate = true;
    plinthGroup.add(scaleInst);
  }

  // Main Ground Surface Disk (controllable smooth X-Ray cutaway opacity)
  const groundTex = createGroundSurfaceTexture(maxAnisotropy);
  if (groundTex) disposableTextures.push(groundTex);

  const groundGeom = new THREE.CylinderGeometry(R + 0.75, R + 0.75, 0.34, 96);
  const groundMat = new THREE.MeshStandardMaterial({
    color: 0xdbe6f4,
    map: groundTex || null,
    roughness: 0.86,
    metalness: 0.1,
    transparent: true,
    opacity: 0.78,
    depthWrite: false,
  });
  disposableGeometries.push(groundGeom);
  disposableMaterials.push(groundMat);

  const groundDisk = new THREE.Mesh(groundGeom, groundMat);
  groundDisk.position.y = -0.22;
  groundDisk.receiveShadow = true;
  plinthGroup.add(groundDisk);

  // Smooth X-Ray Ground & Geological Strata Transparency Controller
  let hasInitGroundOpacity = false;
  let currentGroundOpacity = 0.78;
  let targetGroundOpacity = 0.78;

  function applyGroundOpacityImmediate(opacity01) {
    const clamped = Math.min(1, Math.max(0.02, opacity01));
    groundMat.opacity = clamped;
    const nextDepthWrite = clamped > 0.92;
    if (groundMat.depthWrite !== nextDepthWrite) {
      groundMat.depthWrite = nextDepthWrite;
      groundMat.needsUpdate = true;
      markShadowsDirty();
    }

    // Geological cutaway strata glides cleanly with X-Ray transparency
    strataMat.opacity = 0.16 + clamped * 0.64;
    l4BandMat.opacity = 0.32 + (1 - clamped) * 0.55;
  }

  function setGroundOpacity(opacity01) {
    const clamped = Math.min(1, Math.max(0.02, opacity01));
    if (Math.abs(targetGroundOpacity - clamped) > 0.005) {
      markShadowsDirty();
    }
    targetGroundOpacity = clamped;
    if (!hasInitGroundOpacity) {
      hasInitGroundOpacity = true;
      currentGroundOpacity = clamped;
      applyGroundOpacityImmediate(clamped);
    }
  }

  // Atmospheric Palette Colors (linear-interpolated by timeOfDay, ambientTempC, rainfallMultiplier)
  const zenithNight = new THREE.Color(0x040914);
  const zenithTwilight = new THREE.Color(0x0d1b38);
  const zenithDay = new THREE.Color(0x0e2545);
  const zenithCold = new THREE.Color(0x0b1f3d);
  const zenithWarm = new THREE.Color(0x123059);

  const midNight = new THREE.Color(0x081326);
  const midTwilight = new THREE.Color(0x233052);
  const midDay = new THREE.Color(0x1f4875);

  const horizNight = new THREE.Color(0x0f1e36);
  const horizGolden = new THREE.Color(0xd97b43);
  const horizDay = new THREE.Color(0x527da6);

  const nadirNight = new THREE.Color(0x050a14);
  const nadirTwilight = new THREE.Color(0x101f36);
  const nadirDay = new THREE.Color(0x1c3552);

  const overcastZenith = new THREE.Color(0x182332);
  const overcastMid = new THREE.Color(0x29374a);
  const overcastHoriz = new THREE.Color(0x3e4f63);
  const overcastNadir = new THREE.Color(0x1c2838);

  const sunWarm = new THREE.Color(0xff9f5a);
  const sunMid = new THREE.Color(0xfff5e6);
  const moonCool = new THREE.Color(0x88b4f0);

  const hemiSkyDay = new THREE.Color(0x9ec5ff);
  const hemiSkyGolden = new THREE.Color(0xf4b686);
  const hemiSkyNight = new THREE.Color(0x3a588a);
  const hemiGroundDay = new THREE.Color(0x1b2940);
  const hemiGroundNight = new THREE.Color(0x0c1526);

  const tmpSunDir = new THREE.Vector3();
  const lastShadowSunPos = new THREE.Vector3(Infinity, Infinity, Infinity);
  const tmpZenith = new THREE.Color();
  const tmpMid = new THREE.Color();
  const tmpHoriz = new THREE.Color();
  const tmpFog = new THREE.Color();
  const tmpNadir = new THREE.Color();
  const skyZenithTarget = new THREE.Color();
  const skyMidTarget = new THREE.Color();
  const skyHorizTarget = new THREE.Color();
  const skyNadirTarget = new THREE.Color();
  const horizDayMuted = new THREE.Color();

  function setSunFromState(simInputs = {}) {
    const tod = Number.isFinite(simInputs.timeOfDay) ? simInputs.timeOfDay : 13.5;
    const rain = Math.max(0, Math.min(2.5, Number.isFinite(simInputs.rainfallMultiplier) ? simInputs.rainfallMultiplier : 1.0));
    const tempC = Number.isFinite(simInputs.ambientTempC) ? simInputs.ambientTempC : 10.0;

    // Smooth solar elevation across 0..24h (sunrise ~06:00, solar noon ~12:00, sunset ~18:00)
    const dayPhase = Math.max(0, Math.min(1, (tod - 5.5) / 13.0));
    const rawElev = tod >= 5.5 && tod <= 18.5 ? Math.sin(dayPhase * Math.PI) : 0;
    const daylight = Math.pow(rawElev, 0.85);

    // Golden-hour factor peaks smoothly around sunrise (~6.5) & sunset (~17.5)
    const sunrisePeak = Math.exp(-Math.pow((tod - 6.6) / 1.35, 2));
    const sunsetPeak = Math.exp(-Math.pow((tod - 17.4) / 1.45, 2));
    const goldenHour = Math.min(1, Math.max(sunrisePeak, sunsetPeak));

    // Sun azimuth sweeps east -> south -> west; moonlight takes elevated opposite angle at night
    const sunAngle = ((tod - 6.0) / 12.0) * Math.PI;
    const sx = Math.cos(sunAngle) * 98;
    const sy = Math.sin(sunAngle) * 112;
    const sz = 48;

    if (rawElev > 0.02) {
      sunLight.position.set(sx, Math.max(11, sy), sz);
      tmpSunDir.set(sx, Math.max(3, sy), sz).normalize();
    } else {
      // Gentle silvery moonlight arc so shadows & rim highlights remain crisp at night
      sunLight.position.set(-54, 78, -42);
      tmpSunDir.set(sx, Math.max(-14, sy), sz).normalize();
    }

    // Only rebake the 2048x2048 shadow map when sun position moves meaningfully (>0.5 units)
    if (sunLight.position.distanceToSquared(lastShadowSunPos) > 0.25) {
      lastShadowSunPos.copy(sunLight.position);
      markShadowsDirty();
    }

    // Cloud cover & rain dimming
    const cloudCover = Math.min(1.0, Math.max(0.05, rain * 0.46 + (tempC < 2 ? 0.14 : 0)));
    const rainDim = Math.max(0.52, 1.0 - rain * 0.22);

    // Warmth shift from Dublin winter (-5°C) to summer (+24°C)
    const tempWarmth = Math.min(1, Math.max(0, (tempC + 5) / 30));

    // Direct sun / moon light intensity & color
    if (daylight > 0.06) {
      sunLight.intensity = (0.48 + daylight * 1.72) * rainDim;
      if (daylight > 0.55) {
        sunLight.color.copy(sunMid);
      } else {
        sunLight.color.lerpColors(sunWarm, sunMid, Math.min(1, daylight / 0.55));
      }
    } else {
      sunLight.intensity = 0.42 * (0.75 + 0.25 * rainDim);
      sunLight.color.copy(moonCool);
    }

    // Hemisphere & opposing fill light sync
    hemiLight.intensity = (0.38 + daylight * 0.50) * (0.72 + 0.28 * rainDim);
    hemiLight.color.copy(hemiSkyNight).lerp(hemiSkyDay, daylight);
    if (goldenHour > 0.05) {
      hemiLight.color.lerp(hemiSkyGolden, goldenHour * 0.42 * (1 - cloudCover * 0.5));
    }
    hemiLight.groundColor.copy(hemiGroundNight).lerp(hemiGroundDay, daylight);
    fillLight.intensity = (0.28 + daylight * 0.26) * (0.8 + 0.2 * rainDim);

    // Subtle IBL scaling so both sunlit panels and nighttime subterranean racks reflect crisply
    if ('environmentIntensity' in scene) {
      scene.environmentIntensity = (0.50 + daylight * 0.36) * (0.82 + 0.18 * rainDim);
    }

    // Atmospheric Sky Dome palette interpolation (zero per-frame allocations)
    tmpZenith.copy(zenithCold).lerp(zenithWarm, tempWarmth);
    tmpZenith.lerp(zenithDay, 0.35);

    skyZenithTarget.copy(zenithNight);
    skyMidTarget.copy(midNight);
    skyHorizTarget.copy(horizNight);
    skyNadirTarget.copy(nadirNight);

    if (daylight > 0.02) {
      if (daylight < 0.35) {
        const k = daylight / 0.35;
        skyZenithTarget.lerpColors(zenithNight, zenithTwilight, k);
        skyMidTarget.lerpColors(midNight, midTwilight, k);
        skyHorizTarget.lerpColors(horizNight, horizDay, k * 0.6);
        skyNadirTarget.lerpColors(nadirNight, nadirTwilight, k);
      } else {
        const k = (daylight - 0.35) / 0.65;
        skyZenithTarget.lerpColors(zenithTwilight, tmpZenith, k);
        skyMidTarget.lerpColors(midTwilight, midDay, k);
        horizDayMuted.copy(horizDay).multiplyScalar(0.75);
        skyHorizTarget.lerpColors(horizDayMuted, horizDay, k);
        skyNadirTarget.lerpColors(nadirTwilight, nadirDay, k);
      }
    }

    // Golden sunrise / sunset horizon warmth
    if (goldenHour > 0.02) {
      const gWeight = goldenHour * (1 - cloudCover * 0.55);
      skyHorizTarget.lerp(horizGolden, Math.min(0.82, gWeight * 0.85));
      skyMidTarget.lerp(sunWarm, Math.min(0.28, gWeight * 0.30));
      skyNadirTarget.lerp(nadirTwilight, Math.min(0.35, gWeight * 0.35));
    }

    // Overcast rain atmospheric damping
    const overcastBlend = Math.min(0.78, Math.max(0, (rain - 0.45) * 0.48));
    if (overcastBlend > 0.01) {
      const darkScale = 0.38 + daylight * 0.62;
      skyZenithTarget.lerp(tmpMid.copy(overcastZenith).multiplyScalar(darkScale), overcastBlend);
      skyMidTarget.lerp(tmpHoriz.copy(overcastMid).multiplyScalar(darkScale), overcastBlend);
      skyHorizTarget.lerp(tmpFog.copy(overcastHoriz).multiplyScalar(darkScale), overcastBlend);
      skyNadirTarget.lerp(tmpNadir.copy(overcastNadir).multiplyScalar(darkScale), overcastBlend * 0.7);
    }

    skyDome.uniforms.uSunDir.value.copy(tmpSunDir);
    skyDome.uniforms.uZenithColor.value.copy(skyZenithTarget);
    skyDome.uniforms.uMidSkyColor.value.copy(skyMidTarget);
    skyDome.uniforms.uHorizonColor.value.copy(skyHorizTarget);
    skyDome.uniforms.uNadirColor.value.copy(skyNadirTarget);
    skyDome.uniforms.uSunGlowColor.value.copy(sunMid).lerp(sunWarm, Math.min(1, goldenHour * 0.9 + (1 - daylight) * 0.4));
    skyDome.uniforms.uSunIntensity.value = daylight;
    skyDome.uniforms.uHorizonGlow.value = Math.min(1, goldenHour * 0.95 + daylight * 0.25);
    skyDome.uniforms.uStarVisibility.value = Math.max(0, 1.0 - daylight * 3.2);
    skyDome.uniforms.uCloudCover.value = cloudCover;

    // Sync scene.background & exponential atmospheric fog cleanly with sky mid-horizon
    scene.background.copy(skyMidTarget).lerp(skyZenithTarget, 0.35);
    scene.fog.color.copy(skyHorizTarget).lerp(skyMidTarget, 0.55);
    scene.fog.density = 0.0019 + cloudCover * 0.0011;
  }

  // Smooth fly-to animation (reusable scratch state, zero per-fly allocations)
  const flyAnim = {
    active: false,
    startPos: new THREE.Vector3(),
    endPos: new THREE.Vector3(),
    startTarget: new THREE.Vector3(),
    endTarget: new THREE.Vector3(),
    elapsed: 0,
    duration: 0.85,
  };
  controls.addEventListener('start', () => {
    flyAnim.active = false;
  });

  function flyToPoint(posArr, targetArr, duration = 0.85) {
    flyAnim.startPos.copy(camera.position);
    flyAnim.endPos.set(posArr[0], posArr[1], posArr[2]);
    flyAnim.startTarget.copy(controls.target);
    flyAnim.endTarget.set(targetArr[0], targetArr[1], targetArr[2]);
    flyAnim.elapsed = 0;
    flyAnim.duration = duration;
    flyAnim.active = true;
  }

  function flyToPreset(index) {
    const preset = WORLD_LAYOUT.CAMERA_PRESETS[index];
    if (!preset) return;
    flyToPoint(preset.pos, preset.target);
  }

  function updateCamera(dt) {
    const safeDt = Math.min(0.1, Math.max(0, dt || 0));

    if (flyAnim.active) {
      flyAnim.elapsed += safeDt;
      const t = Math.min(1, flyAnim.elapsed / flyAnim.duration);
      const ease = 1 - Math.pow(1 - t, 3);
      camera.position.lerpVectors(flyAnim.startPos, flyAnim.endPos, ease);
      controls.target.lerpVectors(flyAnim.startTarget, flyAnim.endTarget, ease);
      if (t >= 1) flyAnim.active = false;
    }

    // Smooth X-Ray ground & geological cutaway opacity transition
    if (Math.abs(targetGroundOpacity - currentGroundOpacity) > 0.0008) {
      const alpha = 1 - Math.exp(-10.5 * safeDt);
      currentGroundOpacity += (targetGroundOpacity - currentGroundOpacity) * alpha;
      applyGroundOpacityImmediate(currentGroundOpacity);
    } else if (currentGroundOpacity !== targetGroundOpacity) {
      currentGroundOpacity = targetGroundOpacity;
      applyGroundOpacityImmediate(currentGroundOpacity);
    }

    // Subtle atmospheric drift & sky dome anchoring
    skyDome.uniforms.uTime.value += safeDt;
    controls.update();
    skyDome.mesh.position.copy(camera.position);
  }

  function onResize() {
    const w = Math.max(container.clientWidth || window.innerWidth, 1);
    const h = Math.max(container.clientHeight || window.innerHeight, 1);
    const pr = Math.min(window.devicePixelRatio || 1, 1.5);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setPixelRatio(pr);
    renderer.setSize(w, h);
    if (composer) {
      composer.setPixelRatio(pr);
      composer.setSize(w, h);
      if (bloomPass) {
        const bw = Math.max(1, Math.floor(w * 0.5));
        const bh = Math.max(1, Math.floor(h * 0.5));
        bloomPass.setSize(bw, bh);
      }
    }
  }

  window.addEventListener('resize', onResize);
  const resizeObserver = new ResizeObserver(onResize);
  resizeObserver.observe(container);

  function render() {
    renderer.info.reset();
    if (composer) {
      try {
        composer.render();
        return;
      } catch (_) {
        composer = null;
      }
    }
    renderer.render(scene, camera);
  }

  function dispose() {
    window.removeEventListener('resize', onResize);
    resizeObserver.disconnect();
    controls.dispose();
    bloomPass?.dispose?.();
    outputPass?.dispose?.();
    composer?.dispose?.();
    if (composerRT) composerRT.dispose();
    if (envRT) envRT.dispose();
    for (const tex of disposableTextures) tex.dispose();
    for (const geom of disposableGeometries) geom.dispose();
    for (const mat of disposableMaterials) mat.dispose();
    renderer.dispose();
  }

  return {
    scene,
    camera,
    renderer,
    controls,
    setGroundOpacity,
    setSunFromState,
    markShadowsDirty,
    flyToPreset,
    flyToPoint,
    updateCamera,
    render,
    dispose,
  };
}
