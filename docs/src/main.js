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
    if (layerKey === 'surface' && districtApi) districtApi.group.visible = visible;
    if (layerKey === 'heatmap' && districtApi) districtApi.setHeatmapVisible(visible);
    if (layerKey === 'vaults' && vaultsApi) vaultsApi.group.visible = visible;
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

    // Click-to-inspect 3D raycasting (ignoring orbit drags > 5px)
    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    let downPos = null;
    const dom = sceneApi.renderer.domElement;

    dom.addEventListener('pointerdown', (e) => {
      downPos = { x: e.clientX, y: e.clientY };
    });

    dom.addEventListener('pointerup', (e) => {
      if (!downPos) return;
      const dist = Math.hypot(e.clientX - downPos.x, e.clientY - downPos.y);
      downPos = null;
      if (dist > 5) return;

      const rect = dom.getBoundingClientRect();
      pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.setFromCamera(pointer, sceneApi.camera);

      const candidates = [vaultsApi.group, districtApi.group];
      const hits = raycaster.intersectObjects(candidates, true);
      for (const hit of hits) {
        let obj = hit.object;
        while (obj) {
          if (obj.userData?.inspectKey) {
            ui.setInspectedKey(obj.userData.inspectKey, sim);
            return;
          }
          obj = obj.parent;
        }
      }
    });

    // Animation loop
    let lastTime = performance.now();
    let elapsed = 0;
    let uiTimer = 0;

    function tick(now) {
      requestAnimationFrame(tick);
      const rawDt = Math.min(0.05, (now - lastTime) / 1000);
      lastTime = now;

      const simDt = runtime.playing ? rawDt * runtime.speed : 0;
      elapsed += simDt;

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

      vaultsApi.update(sim, simDt, elapsed);
      districtApi.update(sim, simDt, elapsed);
      flowsApi.update(sim, simDt, elapsed);
      sceneApi.updateCamera(rawDt);
      sceneApi.render();
    }

    requestAnimationFrame(tick);
  } catch (err) {
    if (offlineBanner && offlineMsg) {
      offlineMsg.textContent = `Three.js 0.170.0 / WebGL failed to initialize (${err?.message || 'network or WebGL unavailable'}). All physics controls and live telemetry remain active.`;
      offlineBanner.classList.remove('hidden');
    }
  }
}

init3D();
