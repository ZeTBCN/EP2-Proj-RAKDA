import {
  SCENARIO_PRESETS,
  createDefaultState,
  computeSimulation,
} from './physics.js';
import { createUI } from './ui.js';

const state = createDefaultState();
let sim = computeSimulation(state);

const layers = {
  surface: true,
  heatmap: false,
  loop1: true,
  loop2: true,
  loop3: true,
  optical: true,
  vaults: true,
};

const runtime = {
  playing: true,
  speed: 1.0,
  autoDay: false,
  groundOpacity: 0.78,
};

let sceneApi = null;
let vaultsApi = null;
let districtApi = null;
let flowsApi = null;

function refreshPhysics() {
  sim = computeSimulation(state);
  ui.syncControlsFromState(state, runtime.groundOpacity);
  ui.renderTelemetry(sim, runtime);
  if (sceneApi) {
    sceneApi.setSunFromState(state);
  }
}

const ui = createUI({
  onPresetSelect(presetId) {
    const preset = SCENARIO_PRESETS[presetId];
    if (!preset) return;
    Object.assign(state, {
      ...preset.params,
      vaultFaults: [...preset.params.vaultFaults],
      scenarioId: preset.id,
    });
    refreshPhysics();
  },

  onStatePatch(patch) {
    Object.assign(state, patch);
    refreshPhysics();
  },

  onToggleVaultFault(idx) {
    state.vaultFaults[idx] = !state.vaultFaults[idx];
    state.scenarioId = 'custom';
    refreshPhysics();
  },

  onGroundOpacity(opacity01) {
    runtime.groundOpacity = opacity01;
    if (sceneApi) sceneApi.setGroundOpacity(opacity01);
    ui.syncControlsFromState(state, runtime.groundOpacity);
  },

  onLayerToggle(layerKey, visible) {
    layers[layerKey] = visible;
    if (layerKey === 'surface' && districtApi) {
      districtApi.group.visible = visible;
      sceneApi?.markShadowsDirty?.();
    }
    if (layerKey === 'heatmap' && districtApi) districtApi.setHeatmapVisible(visible);
    if (layerKey === 'vaults' && vaultsApi) {
      vaultsApi.group.visible = visible;
      sceneApi?.markShadowsDirty?.();
    }
    if (flowsApi && flowsApi.groups[layerKey]) flowsApi.groups[layerKey].visible = visible;
  },

  onCameraPreset(idx) {
    if (sceneApi) sceneApi.flyToPreset(idx);
  },

  onAction(action) {
    if (action === 'togglePlay') {
      runtime.playing = !runtime.playing;
    } else if (action === 'cycleSpeed') {
      runtime.speed = runtime.speed === 1.0 ? 2.0 : runtime.speed === 2.0 ? 0.5 : 1.0;
    } else if (action === 'toggleDayCycle') {
      runtime.autoDay = !runtime.autoDay;
    } else if (action === 'toggleXray') {
      runtime.groundOpacity = runtime.groundOpacity > 0.4 ? 0.18 : 0.92;
      if (sceneApi) sceneApi.setGroundOpacity(runtime.groundOpacity);
      ui.syncControlsFromState(state, runtime.groundOpacity);
    } else if (action === 'togglePresent') {
      document.body.classList.toggle('presentation-mode');
    }
    ui.renderTelemetry(sim, runtime);
  },
});

// Populate HUD immediately before/regardless of WebGL availability
refreshPhysics();

/**
 * Creates a non-intrusive holographic selection reticle & elevation ring
 * that highlights whichever 3D subsystem the user clicks to inspect.
 */
function createSelectionHighlight(THREE, scene) {
  const root = new THREE.Group();
  root.name = 'selection_highlight';
  root.visible = false;
  scene.add(root);

  const primaryRingMat = new THREE.MeshStandardMaterial({
    color: 0x67e8f9,
    emissive: 0x22d3ee,
    emissiveIntensity: 1.55,
    roughness: 0.2,
    metalness: 0.2,
    transparent: true,
    opacity: 0.92,
    depthWrite: false,
  });

  const haloMat = new THREE.MeshBasicMaterial({
    color: 0x38bdf8,
    transparent: true,
    opacity: 0.22,
    side: THREE.DoubleSide,
    blending: THREE.AdditiveBlending,
    depthWrite: false,
  });

  const scanRingMat = new THREE.MeshStandardMaterial({
    color: 0xa5f3fc,
    emissive: 0x38bdf8,
    emissiveIntensity: 1.35,
    roughness: 0.25,
    metalness: 0.2,
    transparent: true,
    opacity: 0.55,
    depthWrite: false,
  });

  // Base glowing telemetry ring
  const baseRing = new THREE.Mesh(new THREE.TorusGeometry(1.0, 0.028, 10, 64), primaryRingMat);
  baseRing.rotation.x = Math.PI / 2;
  root.add(baseRing);

  // Soft additive floor glow disc
  const floorHalo = new THREE.Mesh(new THREE.RingGeometry(0.80, 1.04, 64), haloMat);
  floorHalo.rotation.x = -Math.PI / 2;
  floorHalo.position.y = 0.02;
  root.add(floorHalo);

  // Four rotating architectural corner bracket ticks
  const bracketGroup = new THREE.Group();
  root.add(bracketGroup);
  const tickGeom = new THREE.BoxGeometry(0.24, 0.045, 0.055);
  for (let i = 0; i < 4; i++) {
    const a = (i / 4) * Math.PI * 2;
    const tick = new THREE.Mesh(tickGeom, primaryRingMat);
    tick.position.set(Math.cos(a) * 1.12, 0.03, Math.sin(a) * 1.12);
    tick.rotation.y = -a;
    bracketGroup.add(tick);
  }

  // Subtle vertical scanning ring
  const scanRing = new THREE.Mesh(new THREE.TorusGeometry(1.01, 0.015, 8, 64), scanRingMat);
  scanRing.rotation.x = Math.PI / 2;
  root.add(scanRing);

  let selectedObj = null;
  let localAnchor = new THREE.Vector3();
  let baseRadius = 2.0;
  let verticalSpan = 2.5;
  let pulseTime = 0;

  const tmpBox = new THREE.Box3();
  const tmpSize = new THREE.Vector3();
  const tmpCenter = new THREE.Vector3();
  const tmpWorldPos = new THREE.Vector3();
  const tmpBottomWorld = new THREE.Vector3();
  const tmpInstMat = new THREE.Matrix4();

  function isEffectivelyVisible(obj) {
    let cur = obj;
    while (cur) {
      if (!cur.visible) return false;
      cur = cur.parent;
    }
    return true;
  }

  function select(obj, hit = null) {
    if (!obj) {
      clear();
      return;
    }
    selectedObj = obj;
    obj.updateWorldMatrix(true, true);

    if (hit?.object?.isInstancedMesh && hit.instanceId !== undefined && obj === hit.object) {
      hit.object.getMatrixAt(hit.instanceId, tmpInstMat);
      tmpInstMat.premultiply(hit.object.matrixWorld);
      if (!hit.object.geometry.boundingBox) {
        hit.object.geometry.computeBoundingBox();
      }
      tmpBox.copy(hit.object.geometry.boundingBox).applyMatrix4(tmpInstMat);
    } else {
      tmpBox.setFromObject(obj);
    }

    if (tmpBox.isEmpty()) {
      obj.getWorldPosition(tmpCenter);
      tmpSize.set(2.4, 2.0, 2.4);
      tmpBox.min.set(tmpCenter.x - 1.2, tmpCenter.y - 1.0, tmpCenter.z - 1.2);
    } else {
      tmpBox.getSize(tmpSize);
      tmpBox.getCenter(tmpCenter);
    }

    // Anchor at bottom-center in the selected object's local frame (tracks moving pods too)
    tmpBottomWorld.set(tmpCenter.x, tmpBox.min.y + 0.06, tmpCenter.z);
    localAnchor.copy(tmpBottomWorld);
    obj.worldToLocal(localAnchor);

    const horizRadius = Math.hypot(tmpSize.x, tmpSize.z) * 0.5;
    baseRadius = Math.min(22, Math.max(1.2, horizRadius * 1.04 + 0.35));
    verticalSpan = Math.min(12, Math.max(1.0, tmpSize.y));
    pulseTime = 0;

    root.position.copy(tmpBottomWorld);
    root.visible = true;
  }

  function clear() {
    selectedObj = null;
    root.visible = false;
  }

  function update(dt, elapsed) {
    if (!selectedObj) return;
    if (!isEffectivelyVisible(selectedObj)) {
      root.visible = false;
      return;
    }
    root.visible = true;
    pulseTime += dt;

    // Follow selected object in world space (O(1) transform)
    tmpWorldPos.copy(localAnchor).applyMatrix4(selectedObj.matrixWorld);
    root.position.copy(tmpWorldPos);

    // Crisp pop-in ease on click + gentle ongoing breathing
    const popScale = 1.0 + 0.20 * Math.exp(-9.5 * pulseTime);
    const breath = 1.0 + 0.025 * Math.sin(elapsed * 3.1);
    const r = baseRadius * popScale * breath;

    baseRing.scale.set(r, r, 1);
    floorHalo.scale.set(r, r, 1);
    bracketGroup.scale.set(r, 1, r);
    bracketGroup.rotation.y = elapsed * 0.55;

    const scanPhase = 0.5 - 0.5 * Math.cos(elapsed * 2.1);
    scanRing.position.y = scanPhase * verticalSpan;
    scanRing.scale.set(r * 0.99, r * 0.99, 1);

    primaryRingMat.emissiveIntensity = 1.35 + 0.35 * Math.sin(elapsed * 3.6);
    haloMat.opacity = 0.16 + 0.08 * Math.sin(elapsed * 3.1);
    scanRingMat.opacity = 0.22 + 0.38 * Math.sin(scanPhase * Math.PI);
  }

  return { select, clear, update };
}

async function init3D() {
  const viewport = document.getElementById('viewport');
  const offlineBanner = document.getElementById('offline-banner');
  const offlineMsg = document.getElementById('offline-msg');

  try {
    const [THREE, sceneMod, vaultsMod, districtMod, flowsMod] = await Promise.all([
      import('three'),
      import('./scene.js'),
      import('./vaults.js'),
      import('./district.js'),
      import('./flows.js'),
    ]);

    sceneApi = sceneMod.createScene(viewport);
    vaultsApi = vaultsMod.createVaults(sceneApi.scene);
    districtApi = districtMod.createDistrict(sceneApi.scene);
    flowsApi = flowsMod.createFlows(sceneApi.scene);

    sceneApi.setGroundOpacity(runtime.groundOpacity);
    sceneApi.setSunFromState(state);
    sceneApi.markShadowsDirty?.();

    const selectionHighlight = createSelectionHighlight(THREE, sceneApi.scene);
    const flowGroupList = Object.values(flowsApi.groups);

    // Expose read-only performance metrics getter for benchmarking
    Object.defineProperty(window, '__RAKDA_PERF__', {
      configurable: true,
      enumerable: true,
      get() {
        const info = sceneApi?.renderer?.info;
        return {
          calls: info?.render?.calls ?? 0,
          triangles: info?.render?.triangles ?? 0,
          geometries: info?.memory?.geometries ?? 0,
          textures: info?.memory?.textures ?? 0,
        };
      },
    });

    // Graceful WebGL context loss / recovery handling
    const dom = sceneApi.renderer.domElement;
    dom.addEventListener('webglcontextlost', (e) => {
      e.preventDefault();
      if (offlineBanner && offlineMsg) {
        offlineMsg.textContent =
          'WebGL rendering context was lost. All physics controls and live telemetry remain active.';
        offlineBanner.classList.remove('hidden');
      }
    });
    dom.addEventListener('webglcontextrestored', () => {
      if (offlineBanner) offlineBanner.classList.add('hidden');
      sceneApi.markShadowsDirty?.();
    });

    // Click-to-inspect 3D raycasting (ignoring orbit drags > 5px)
    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    const raycastCandidates = [];
    const downPos = { active: false, x: 0, y: 0 };

    dom.addEventListener('pointerdown', (e) => {
      downPos.active = true;
      downPos.x = e.clientX;
      downPos.y = e.clientY;
    });

    dom.addEventListener('pointerup', (e) => {
      if (!downPos.active) return;
      const dist = Math.hypot(e.clientX - downPos.x, e.clientY - downPos.y);
      downPos.active = false;
      if (dist > 5) return;

      const rect = dom.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) return;
      pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.setFromCamera(pointer, sceneApi.camera);

      raycastCandidates.length = 0;
      if (vaultsApi.group.visible) raycastCandidates.push(vaultsApi.group);
      if (districtApi.group.visible) raycastCandidates.push(districtApi.group);

      const hits = raycaster.intersectObjects(raycastCandidates, true);
      for (const hit of hits) {
        let obj = hit.object;
        while (obj) {
          if (obj.userData?.inspectKey) {
            ui.setInspectedKey(obj.userData.inspectKey, sim);
            selectionHighlight.select(obj, hit);
            return;
          }
          obj = obj.parent;
        }
      }

      // Clicking empty space dismisses the 3D highlight ring cleanly
      selectionHighlight.clear();
    });

    // Animation loop (pauses cleanly when tab is hidden)
    let rafId = 0;
    let lastTime = performance.now();
    let elapsed = 0;
    let visualElapsed = 0;
    let uiTimer = 0;

    function anyFlowGroupVisible() {
      for (let i = 0; i < flowGroupList.length; i++) {
        if (flowGroupList[i].visible) return true;
      }
      return false;
    }

    function tick(now) {
      if (document.hidden) {
        rafId = 0;
        return;
      }
      rafId = requestAnimationFrame(tick);
      const rawDt = Math.min(0.05, Math.max(0, (now - lastTime) / 1000));
      lastTime = now;

      const simDt = runtime.playing ? rawDt * runtime.speed : 0;
      elapsed += simDt;
      visualElapsed += rawDt;

      if (runtime.playing && runtime.autoDay) {
        state.timeOfDay = (state.timeOfDay + simDt * 0.45) % 24;
        sim = computeSimulation(state);
        sceneApi.setSunFromState(state);
        uiTimer += rawDt;
        if (uiTimer >= 0.15) {
          uiTimer = 0;
          ui.syncControlsFromState(state, runtime.groundOpacity);
          ui.renderTelemetry(sim, runtime);
        }
      }

      if (vaultsApi.group.visible) {
        vaultsApi.update(sim, simDt, elapsed);
      }
      if (districtApi.group.visible) {
        districtApi.update(sim, simDt, elapsed);
      }
      if (anyFlowGroupVisible()) {
        flowsApi.update(sim, simDt, elapsed);
      }
      selectionHighlight.update(rawDt, visualElapsed);
      sceneApi.updateCamera(rawDt);
      sceneApi.render();
    }

    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        if (rafId) {
          cancelAnimationFrame(rafId);
          rafId = 0;
        }
      } else if (!rafId) {
        lastTime = performance.now();
        rafId = requestAnimationFrame(tick);
      }
    });

    // Prime initial frame so shadows & __RAKDA_PERF__ are populated immediately
    if (vaultsApi.group.visible) vaultsApi.update(sim, 0, 0);
    if (districtApi.group.visible) districtApi.update(sim, 0, 0);
    if (anyFlowGroupVisible()) flowsApi.update(sim, 0, 0);
    sceneApi.updateCamera(0);
    sceneApi.render();

    if (!document.hidden) {
      rafId = requestAnimationFrame(tick);
    }
  } catch (err) {
    if (offlineBanner && offlineMsg) {
      offlineMsg.textContent = `Three.js 0.170.0 / WebGL failed to initialize (${err?.message || 'network or WebGL unavailable'}). All physics controls and live telemetry remain active.`;
      offlineBanner.classList.remove('hidden');
    }
  }
}

init3D();
