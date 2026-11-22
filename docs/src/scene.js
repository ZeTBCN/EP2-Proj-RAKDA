import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

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
      pos: [34, 8.5, 34],
      target: [8, 1.5, 6],
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

export function createScene(container) {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0b1220);
  scene.fog = new THREE.FogExp2(0x0b1220, 0.0028);

  const width = Math.max(container.clientWidth || window.innerWidth, 1);
  const height = Math.max(container.clientHeight || window.innerHeight, 1);

  const camera = new THREE.PerspectiveCamera(46, width / height, 0.5, 600);
  const initPreset = WORLD_LAYOUT.CAMERA_PRESETS[0];
  camera.position.set(...initPreset.pos);

  const renderer = new THREE.WebGLRenderer({
    antialias: true,
    powerPreference: 'high-performance',
  });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.setSize(width, height);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.15;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  container.appendChild(renderer.domElement);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.065;
  controls.minDistance = 10;
  controls.maxDistance = 260;
  controls.maxPolarAngle = Math.PI * 0.58;
  controls.target.set(...initPreset.target);
  controls.update();

  // Dynamic lighting rig
  const hemiLight = new THREE.HemisphereLight(0x9ec5ff, 0x162238, 0.72);
  scene.add(hemiLight);

  const sunLight = new THREE.DirectionalLight(0xfff4e0, 1.85);
  sunLight.position.set(60, 95, 45);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.set(2048, 2048);
  sunLight.shadow.camera.near = 10;
  sunLight.shadow.camera.far = 260;
  sunLight.shadow.camera.left = -85;
  sunLight.shadow.camera.right = 85;
  sunLight.shadow.camera.top = 85;
  sunLight.shadow.camera.bottom = -85;
  sunLight.shadow.bias = -0.0005;
  scene.add(sunLight);

  // Studio fill & subterranean cavern illumination
  const fillLight = new THREE.DirectionalLight(0x60a5fa, 0.45);
  fillLight.position.set(-70, 35, -65);
  scene.add(fillLight);

  const cavernLight = new THREE.PointLight(0x22d3ee, 18, 120, 1.2);
  cavernLight.position.set(0, -10, 0);
  scene.add(cavernLight);

  const cavernWarmLight = new THREE.PointLight(0xff8a4c, 8, 95, 1.3);
  cavernWarmLight.position.set(0, -14, 0);
  scene.add(cavernWarmLight);

  // Geological Cutaway Plinth
  const plinthGroup = new THREE.Group();
  plinthGroup.name = 'geological_plinth';
  scene.add(plinthGroup);

  const bedrockFloor = new THREE.Mesh(
    new THREE.CylinderGeometry(WORLD_LAYOUT.DISTRICT_RADIUS + 2, WORLD_LAYOUT.DISTRICT_RADIUS + 3, 2, 72),
    new THREE.MeshStandardMaterial({
      color: 0x0f172a,
      roughness: 0.92,
      metalness: 0.12,
    })
  );
  bedrockFloor.position.y = -21;
  bedrockFloor.receiveShadow = true;
  plinthGroup.add(bedrockFloor);

  // Subtle Level -4 engineering grid on bedrock floor
  const cavernGrid = new THREE.GridHelper(130, 26, 0x1e3a5f, 0x132238);
  cavernGrid.position.y = -19.95;
  plinthGroup.add(cavernGrid);

  // Geological cutaway side skirt strata (Calp limestone / clay / topsoil)
  const strataSpecs = [
    { topY: -1.5, h: 4.5, color: 0x263244, opacity: 0.36 },
    { topY: -6.0, h: 6.5, color: 0x1c2739, opacity: 0.34 },
    { topY: -12.5, h: 7.5, color: 0x141d2d, opacity: 0.38 },
  ];
  for (const s of strataSpecs) {
    const skirt = new THREE.Mesh(
      new THREE.CylinderGeometry(
        WORLD_LAYOUT.DISTRICT_RADIUS + 1.2,
        WORLD_LAYOUT.DISTRICT_RADIUS + 1.4,
        s.h,
        72,
        1,
        true
      ),
      new THREE.MeshStandardMaterial({
        color: s.color,
        roughness: 0.9,
        metalness: 0.08,
        transparent: true,
        opacity: s.opacity,
        side: THREE.DoubleSide,
        depthWrite: false,
      })
    );
    skirt.position.y = s.topY - s.h / 2;
    plinthGroup.add(skirt);
  }

  // Rim ring around district plinth
  const rimRing = new THREE.Mesh(
    new THREE.RingGeometry(WORLD_LAYOUT.DISTRICT_RADIUS + 0.5, WORLD_LAYOUT.DISTRICT_RADIUS + 1.8, 96),
    new THREE.MeshBasicMaterial({
      color: 0x22d3ee,
      transparent: true,
      opacity: 0.32,
      side: THREE.DoubleSide,
    })
  );
  rimRing.rotation.x = -Math.PI / 2;
  rimRing.position.y = -0.05;
  plinthGroup.add(rimRing);

  // Main ground surface disk (controllable X-Ray cutaway opacity)
  const groundMat = new THREE.MeshStandardMaterial({
    color: 0x182538,
    roughness: 0.88,
    metalness: 0.08,
    transparent: true,
    opacity: 0.78,
    depthWrite: false,
  });
  const groundDisk = new THREE.Mesh(
    new THREE.CylinderGeometry(WORLD_LAYOUT.DISTRICT_RADIUS + 0.8, WORLD_LAYOUT.DISTRICT_RADIUS + 0.8, 0.35, 72),
    groundMat
  );
  groundDisk.position.y = -0.22;
  groundDisk.receiveShadow = true;
  plinthGroup.add(groundDisk);

  function setGroundOpacity(opacity01) {
    const clamped = Math.min(1, Math.max(0.02, opacity01));
    groundMat.opacity = clamped;
    groundMat.depthWrite = clamped > 0.92;
    groundMat.needsUpdate = true;
  }

  const skyNight = new THREE.Color(0x070d18);
  const skyTwilight = new THREE.Color(0x16243d);
  const skyDay = new THREE.Color(0x122238);
  const skyRain = new THREE.Color(0x101826);
  const sunWarm = new THREE.Color(0xffb677);
  const sunMid = new THREE.Color(0xfff5e4);
  const sunCool = new THREE.Color(0x93c5fd);

  function setSunFromState(simInputs) {
    const tod = simInputs.timeOfDay ?? 13.5;
    const rain = simInputs.rainfallMultiplier ?? 1.0;
    const daylight = Math.max(0, Math.sin(Math.max(0, Math.min(1, (tod - 6) / 12)) * Math.PI));
    const sunAngle = ((tod - 6) / 12) * Math.PI;
    const sx = Math.cos(sunAngle) * 95;
    const sy = Math.max(12, Math.sin(sunAngle) * 110);
    const sz = 46;
    sunLight.position.set(sx, sy, sz);

    const rainDim = Math.max(0.55, 1 - rain * 0.2);
    sunLight.intensity = (0.35 + daylight * 1.75) * rainDim;
    hemiLight.intensity = (0.45 + daylight * 0.45) * rainDim;

    if (daylight > 0.55) {
      sunLight.color.copy(sunMid);
    } else if (daylight > 0.08) {
      sunLight.color.lerpColors(sunWarm, sunMid, daylight / 0.55);
    } else {
      sunLight.color.copy(sunCool);
    }

    const bg = scene.background;
    if (daylight < 0.05) {
      bg.copy(skyNight);
    } else if (daylight < 0.35) {
      bg.lerpColors(skyNight, skyTwilight, (daylight - 0.05) / 0.3);
    } else {
      bg.lerpColors(skyTwilight, skyDay, (daylight - 0.35) / 0.65);
    }
    if (rain > 0.9) {
      bg.lerp(skyRain, Math.min(0.45, (rain - 0.8) * 0.35));
    }
    scene.fog.color.copy(bg);
  }

  // Smooth fly-to animation
  let flyAnim = null;
  controls.addEventListener('start', () => {
    flyAnim = null;
  });

  function flyToPoint(posArr, targetArr, duration = 0.85) {
    flyAnim = {
      startPos: camera.position.clone(),
      endPos: new THREE.Vector3(...posArr),
      startTarget: controls.target.clone(),
      endTarget: new THREE.Vector3(...targetArr),
      elapsed: 0,
      duration,
    };
  }

  function flyToPreset(index) {
    const preset = WORLD_LAYOUT.CAMERA_PRESETS[index];
    if (!preset) return;
    flyToPoint(preset.pos, preset.target);
  }

  function updateCamera(dt) {
    if (flyAnim) {
      flyAnim.elapsed += dt;
      const t = Math.min(1, flyAnim.elapsed / flyAnim.duration);
      const ease = 1 - Math.pow(1 - t, 3);
      camera.position.lerpVectors(flyAnim.startPos, flyAnim.endPos, ease);
      controls.target.lerpVectors(flyAnim.startTarget, flyAnim.endTarget, ease);
      if (t >= 1) flyAnim = null;
    }
    controls.update();
  }

  function onResize() {
    const w = Math.max(container.clientWidth || window.innerWidth, 1);
    const h = Math.max(container.clientHeight || window.innerHeight, 1);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h);
  }

  window.addEventListener('resize', onResize);
  const resizeObserver = new ResizeObserver(onResize);
  resizeObserver.observe(container);

  function render() {
    renderer.render(scene, camera);
  }

  function dispose() {
    window.removeEventListener('resize', onResize);
    resizeObserver.disconnect();
    controls.dispose();
    renderer.dispose();
  }

  return {
    scene,
    camera,
    renderer,
    controls,
    setGroundOpacity,
    setSunFromState,
    flyToPreset,
    flyToPoint,
    updateCamera,
    render,
    dispose,
  };
}
