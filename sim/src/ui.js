import { SCENARIO_LIST, SCENARIO_PRESETS, SUBSYSTEM_INFO } from './physics.js';

const CAMERA_PRESET_LABELS = [
  { key: '1', label: '1 · District Overview' },
  { key: '2', label: '2 · Level −4 Vaults' },
  { key: '3', label: '3 · Central Hub' },
  { key: '4', label: '4 · Vault 3 Close-Up' },
  { key: '5', label: '5 · Street Pods' },
  { key: '6', label: '6 · Top Masterplan' },
];

const INT_FORMATTER = new Intl.NumberFormat();

function formatClock(hoursFloat) {
  const wrapped = ((hoursFloat % 24) + 24) % 24;
  const hh = Math.floor(wrapped);
  const mm = Math.round((wrapped - hh) * 60) % 60;
  return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`;
}

function createBarBlock(label, color) {
  const block = document.createElement('div');
  block.className = 'bar-block';

  const head = document.createElement('div');
  head.className = 'bar-head';

  const labelEl = document.createElement('span');
  labelEl.textContent = label;

  const valEl = document.createElement('span');
  valEl.className = 'mono';

  head.appendChild(labelEl);
  head.appendChild(valEl);

  const track = document.createElement('div');
  track.className = 'bar-track';

  const fill = document.createElement('div');
  fill.className = 'bar-fill';
  fill.style.background = color;
  fill.style.width = '0.0%';

  track.appendChild(fill);
  block.appendChild(head);
  block.appendChild(track);

  let lastText = '';
  let lastWidth = '';

  return {
    el: block,
    update(valueText, pct) {
      if (lastText !== valueText) {
        valEl.textContent = valueText;
        lastText = valueText;
      }
      const clamped = Math.min(100, Math.max(0, pct));
      const widthStr = `${clamped.toFixed(1)}%`;
      if (lastWidth !== widthStr) {
        fill.style.width = widthStr;
        lastWidth = widthStr;
      }
    },
  };
}

function createKvGrid(rows, marginTop) {
  const grid = document.createElement('div');
  grid.className = 'kv-grid';
  if (marginTop) grid.style.marginTop = marginTop;

  const valueMap = Object.create(null);
  const cacheMap = Object.create(null);
  const classMap = Object.create(null);

  for (const [id, label, baseClass] of rows) {
    const kEl = document.createElement('span');
    kEl.className = 'kv-k';
    kEl.textContent = label;

    const vEl = document.createElement('span');
    const cls = baseClass ? `kv-v ${baseClass}` : 'kv-v';
    vEl.className = cls;

    grid.appendChild(kEl);
    grid.appendChild(vEl);

    valueMap[id] = vEl;
    cacheMap[id] = '';
    classMap[id] = cls;
  }

  return {
    el: grid,
    set(id, text, extraClass) {
      const vEl = valueMap[id];
      if (!vEl) return;
      if (cacheMap[id] !== text) {
        vEl.textContent = text;
        cacheMap[id] = text;
      }
      if (extraClass !== undefined) {
        const nextCls = extraClass ? `kv-v ${extraClass}` : 'kv-v';
        if (classMap[id] !== nextCls) {
          vEl.className = nextCls;
          classMap[id] = nextCls;
        }
      }
    },
  };
}

export function createUI(callbacks) {
  const els = {
    leftPanel: document.getElementById('left-panel'),
    rightPanel: document.getElementById('right-panel'),
    scenarioBtns: document.getElementById('scenario-buttons'),
    scenarioDesc: document.getElementById('scenario-desc'),
    vaultFaultBtns: document.getElementById('vault-fault-btns'),
    cameraPresets: document.getElementById('camera-presets'),

    sliderUtil: document.getElementById('slider-util'),
    sliderFcap: document.getElementById('slider-fcap'),
    sliderTemp: document.getElementById('slider-temp'),
    sliderRain: document.getElementById('slider-rain'),
    sliderTime: document.getElementById('slider-time'),
    sliderXray: document.getElementById('slider-xray'),

    valUtil: document.getElementById('val-util'),
    valFcap: document.getElementById('val-fcap'),
    valTemp: document.getElementById('val-temp'),
    valRain: document.getElementById('val-rain'),
    valTime: document.getElementById('val-time'),
    valXray: document.getElementById('val-xray'),

    toggleSdn: document.getElementById('toggle-sdn'),
    toggleScenarioB: document.getElementById('toggle-scenariob'),

    layerSurface: document.getElementById('layer-surface'),
    layerHeatmap: document.getElementById('layer-heatmap'),
    layerLoop1: document.getElementById('layer-loop1'),
    layerLoop2: document.getElementById('layer-loop2'),
    layerLoop3: document.getElementById('layer-loop3'),
    layerOptical: document.getElementById('layer-optical'),
    layerVaults: document.getElementById('layer-vaults'),

    kpiIt: document.getElementById('kpi-it'),
    kpiPue: document.getElementById('kpi-pue'),
    kpiCov: document.getElementById('kpi-cov'),
    kpiRtt: document.getElementById('kpi-rtt'),
    kpiCo2: document.getElementById('kpi-co2'),
    kpiNpv: document.getElementById('kpi-npv'),

    telThermal: document.getElementById('telemetry-thermal'),
    telGridWater: document.getElementById('telemetry-grid-water'),
    telFinance: document.getElementById('telemetry-finance'),

    inspBadge: document.getElementById('inspector-badge'),
    inspTitle: document.getElementById('inspector-title'),
    inspDesc: document.getElementById('inspector-desc'),
    inspSpecs: document.getElementById('inspector-specs'),
    inspMetrics: document.getElementById('inspector-metrics'),

    btnPlay: document.getElementById('btn-play'),
    btnSpeed: document.getElementById('btn-speed'),
    btnDaycycle: document.getElementById('btn-daycycle'),
    btnXray: document.getElementById('btn-xray'),
    btnReset: document.getElementById('btn-reset'),
    btnPresent: document.getElementById('btn-present'),
    btnPresentTop: document.getElementById('btn-present-top'),
    btnToggleLeft: document.getElementById('btn-toggle-left'),
    btnToggleRight: document.getElementById('btn-toggle-right'),
  };

  // Cached text / input values to avoid redundant DOM writes
  const textCache = Object.create(null);
  const valCache = Object.create(null);
  const boolCache = Object.create(null);

  function setText(el, key, value) {
    if (textCache[key] !== value) {
      el.textContent = value;
      textCache[key] = value;
    }
  }

  function setInputVal(el, key, value) {
    const str = String(value);
    if (valCache[key] !== str) {
      el.value = str;
      valCache[key] = str;
    }
  }

  function setChecked(el, key, checked) {
    if (boolCache[key] !== checked) {
      el.checked = checked;
      boolCache[key] = checked;
    }
  }

  let inspectedKey = null;
  let renderedInspectedKey = null;
  let inspectorMetricNodes = [];

  // Build persistent DOM structures for telemetry cards once (zero innerHTML thrashing)
  const thermalCapturedBar = createBarBlock('Captured Heat (98% nom.)', '#ff6b2c');
  const thermalDemandBar = createBarBlock('District Demand (7,500 homes)', '#22d3ee');
  const thermalCoreKv = createKvGrid(
    [
      ['balance', 'Net Thermal Balance', ''],
      ['annual', 'Annual Heat / Coverage', ''],
      ['primary', 'Primary 75°C (Gross/Net)', ''],
      ['secondary', 'Secondary 45°C Flow', ''],
      ['store', '2,500 m³ Thermal Store', ''],
    ],
    '4px'
  );
  const thermalSurplusKv = createKvGrid(
    [
      ['storeCharge', '2,500 m³ Store Charge', ''],
      ['tces', 'SrBr₂ TCES (35 MWh_th)', ''],
      ['tiPtes', 'TI-PTES (2 MW / 16 MWh)', ''],
      ['orc', 'Micro-ORC (≤440 kW_e)', 'accent-emerald'],
      ['mofDry', 'MOF Chillers / Dry Cool', ''],
    ],
    '6px'
  );
  const thermalDeficitKv = createKvGrid(
    [
      ['storeDischarge', 'Thermal Store Discharge', 'accent-orange'],
      ['hthpOut', '8.0 MW_th Backup HTHP', 'accent-amber'],
      ['hthpElec', 'HTHP Electric Draw', ''],
      ['unmet', 'Unmet Deficit', 'accent-emerald'],
    ],
    '6px'
  );
  thermalDeficitKv.el.hidden = true;
  thermalDeficitKv.el.classList.add('hidden');
  let lastSurplusMode = true;

  els.telThermal.replaceChildren(
    thermalCapturedBar.el,
    thermalDemandBar.el,
    thermalCoreKv.el,
    thermalSurplusKv.el,
    thermalDeficitKv.el
  );

  const cisternBar = createBarBlock('10,000 m³ Drought Cistern', '#0ea5e9');
  const gridWaterKv = createKvGrid(
    [
      ['gridAnnual', 'Gross / Net Grid Annual', ''],
      ['pvImport', '12 MW_p PV / Net Import', ''],
      ['bess', '8 MW / 32 MWh BESS', ''],
      ['rain', 'Rain Harvest (120k m²)', ''],
      ['waterSaved', 'Avoided Cooling Water', 'accent-cyan'],
      ['v2x', 'Local V2X vs Cloud (50 km/h)', 'accent-violet'],
      ['breakout', 'Local Breakout / WAN Saved', ''],
    ],
    '4px'
  );
  els.telGridWater.replaceChildren(cisternBar.el, gridWaterKv.el);

  const financeKv = createKvGrid(
    [
      ['capex', 'Case 2 CapEx vs Counterfactual', ''],
      ['ebitda', 'EBITDA / Simple Payback', 'accent-emerald'],
      ['irr', '30-Yr IRR / NPV @ 8%', ''],
      ['co2', 'Avoided Carbon (Heat + PUE)', 'accent-emerald'],
      ['survey', 'Survey Comfort / Recycled H₂O', ''],
    ],
    ''
  );
  els.telFinance.replaceChildren(financeKv.el);

  // Build 7 scenario buttons
  const presetBtnMap = new Map();
  let activeScenarioId = null;
  for (const preset of SCENARIO_LIST) {
    const btn = document.createElement('button');
    btn.className = 'preset-btn';
    btn.innerHTML = `<span class="p-title">${preset.shortLabel}</span><span class="p-badge">${preset.badge}</span>`;
    btn.addEventListener('click', () => callbacks.onPresetSelect(preset.id));
    els.scenarioBtns.appendChild(btn);
    presetBtnMap.set(preset.id, btn);
  }

  // Build 5 Vault fault buttons
  const faultBtnList = [];
  const lastVaultFaults = [false, false, false, false, false];
  for (let i = 0; i < 5; i++) {
    const btn = document.createElement('button');
    btn.className = 'vault-btn';
    btn.textContent = `V${i + 1} OK`;
    btn.addEventListener('click', () => callbacks.onToggleVaultFault(i));
    els.vaultFaultBtns.appendChild(btn);
    faultBtnList.push(btn);
  }

  // Build 6 camera preset buttons
  const camBtns = [];
  let activeCamIdx = 0;
  function setActiveCamBtn(idx) {
    if (activeCamIdx === idx) return;
    if (camBtns[activeCamIdx]) camBtns[activeCamIdx].classList.remove('active');
    if (camBtns[idx]) camBtns[idx].classList.add('active');
    activeCamIdx = idx;
  }

  CAMERA_PRESET_LABELS.forEach((cp, idx) => {
    const btn = document.createElement('button');
    btn.className = 'cam-btn' + (idx === 0 ? ' active' : '');
    btn.textContent = cp.label;
    btn.addEventListener('click', () => {
      setActiveCamBtn(idx);
      callbacks.onCameraPreset(idx);
    });
    els.cameraPresets.appendChild(btn);
    camBtns.push(btn);
  });

  // Leading-edge + rAF-coalesced slider dispatch:
  // Immediate on first event (0 latency for clicks/tests), coalesced to 1/frame during rapid drags.
  let pendingStatePatch = null;
  let pendingGroundOpacity = null;
  let sliderRafId = 0;

  function flushPendingSliders() {
    sliderRafId = 0;
    const patch = pendingStatePatch;
    const opacity = pendingGroundOpacity;
    pendingStatePatch = null;
    pendingGroundOpacity = null;
    if (patch) callbacks.onStatePatch(patch);
    if (opacity !== null) callbacks.onGroundOpacity(opacity);
  }

  function scheduleSliderFrame() {
    if (sliderRafId || typeof requestAnimationFrame !== 'function') return;
    sliderRafId = requestAnimationFrame(flushPendingSliders);
  }

  function emitSliderState(patch) {
    if (!sliderRafId) {
      callbacks.onStatePatch(patch);
      scheduleSliderFrame();
    } else {
      pendingStatePatch = pendingStatePatch ? Object.assign(pendingStatePatch, patch) : Object.assign({}, patch);
    }
  }

  function emitGroundOpacity(opacity01) {
    if (!sliderRafId) {
      callbacks.onGroundOpacity(opacity01);
      scheduleSliderFrame();
    } else {
      pendingGroundOpacity = opacity01;
    }
  }

  // Slider bindings
  els.sliderUtil.addEventListener('input', () => {
    const util = Number(els.sliderUtil.value) / 100;
    valCache.sliderUtil = els.sliderUtil.value;
    setText(els.valUtil, 'valUtil', `${Math.round(util * 100)}% (${(util * 10).toFixed(1)} MW)`);
    emitSliderState({ utilization: util, scenarioId: 'custom' });
  });
  els.sliderFcap.addEventListener('input', () => {
    const fcap = Number(els.sliderFcap.value) / 100;
    valCache.sliderFcap = els.sliderFcap.value;
    setText(els.valFcap, 'valFcap', `${Math.round(fcap * 100)}%`);
    emitSliderState({ heatCaptureEff: fcap, scenarioId: 'custom' });
  });
  els.sliderTemp.addEventListener('input', () => {
    const temp = Number(els.sliderTemp.value);
    valCache.sliderTemp = els.sliderTemp.value;
    setText(els.valTemp, 'valTemp', `${temp.toFixed(1)} °C`);
    emitSliderState({ ambientTempC: temp, scenarioId: 'custom' });
  });
  els.sliderRain.addEventListener('input', () => {
    const rain = Number(els.sliderRain.value);
    valCache.sliderRain = els.sliderRain.value;
    setText(els.valRain, 'valRain', `${rain.toFixed(2)}× (${(772.5 * rain).toFixed(0)} mm/yr)`);
    emitSliderState({ rainfallMultiplier: rain, scenarioId: 'custom' });
  });
  els.sliderTime.addEventListener('input', () => {
    const tod = Number(els.sliderTime.value);
    valCache.sliderTime = els.sliderTime.value;
    setText(els.valTime, 'valTime', formatClock(tod));
    emitSliderState({ timeOfDay: tod });
  });
  els.sliderXray.addEventListener('input', () => {
    const op = Number(els.sliderXray.value) / 100;
    valCache.sliderXray = els.sliderXray.value;
    setText(els.valXray, 'valXray', `${Math.round(op * 100)}%`);
    emitGroundOpacity(op);
  });

  els.toggleSdn.addEventListener('change', () => {
    boolCache.sdn = els.toggleSdn.checked;
    callbacks.onStatePatch({ sdnEnabled: els.toggleSdn.checked, scenarioId: 'custom' });
  });
  els.toggleScenarioB.addEventListener('change', () => {
    boolCache.scenarioB = els.toggleScenarioB.checked;
    callbacks.onStatePatch({ hthpScenarioB: els.toggleScenarioB.checked, scenarioId: 'custom' });
  });

  // Layer toggles
  const layerBindings = [
    ['surface', els.layerSurface],
    ['heatmap', els.layerHeatmap],
    ['loop1', els.layerLoop1],
    ['loop2', els.layerLoop2],
    ['loop3', els.layerLoop3],
    ['optical', els.layerOptical],
    ['vaults', els.layerVaults],
  ];
  for (const [key, input] of layerBindings) {
    input.addEventListener('change', () => callbacks.onLayerToggle(key, input.checked));
  }

  // Dock & header buttons
  els.btnPlay.addEventListener('click', () => callbacks.onAction('togglePlay'));
  els.btnSpeed.addEventListener('click', () => callbacks.onAction('cycleSpeed'));
  els.btnDaycycle.addEventListener('click', () => callbacks.onAction('toggleDayCycle'));
  els.btnXray.addEventListener('click', () => callbacks.onAction('toggleXray'));
  els.btnReset.addEventListener('click', () => {
    setActiveCamBtn(0);
    callbacks.onCameraPreset(0);
  });
  els.btnPresent.addEventListener('click', () => callbacks.onAction('togglePresent'));
  els.btnPresentTop.addEventListener('click', () => callbacks.onAction('togglePresent'));
  els.btnToggleLeft.addEventListener('click', () => els.leftPanel.classList.toggle('collapsed'));
  els.btnToggleRight.addEventListener('click', () => els.rightPanel.classList.toggle('collapsed'));

  // Keyboard shortcuts
  window.addEventListener('keydown', (e) => {
    const tag = e.target?.tagName?.toLowerCase();
    if (tag === 'input' || tag === 'textarea' || tag === 'select') return;
    if (e.key >= '1' && e.key <= '6') {
      const idx = Number(e.key) - 1;
      setActiveCamBtn(idx);
      callbacks.onCameraPreset(idx);
    } else if (e.key === 'x' || e.key === 'X') {
      callbacks.onAction('toggleXray');
    } else if (e.key === ' ') {
      e.preventDefault();
      callbacks.onAction('togglePlay');
    } else if (e.key === 'h' || e.key === 'H') {
      callbacks.onAction('togglePresent');
    } else if (e.key === 'r' || e.key === 'R') {
      setActiveCamBtn(0);
      callbacks.onCameraPreset(0);
    }
  });

  function syncControlsFromState(state, groundOpacity) {
    if (activeScenarioId !== state.scenarioId) {
      if (activeScenarioId && presetBtnMap.has(activeScenarioId)) {
        presetBtnMap.get(activeScenarioId).classList.remove('active');
      }
      if (presetBtnMap.has(state.scenarioId)) {
        presetBtnMap.get(state.scenarioId).classList.add('active');
      }
      activeScenarioId = state.scenarioId;

      const preset = SCENARIO_PRESETS[state.scenarioId];
      setText(
        els.scenarioDesc,
        'scenarioDesc',
        preset ? preset.description : 'Custom interactive engineering configuration (live Draft 3.2 equations).'
      );
    }

    const utilPct = Math.round(state.utilization * 100);
    setInputVal(els.sliderUtil, 'sliderUtil', utilPct);
    setText(els.valUtil, 'valUtil', `${utilPct}% (${(state.utilization * 10).toFixed(1)} MW)`);

    const fcapPct = Math.round(state.heatCaptureEff * 100);
    setInputVal(els.sliderFcap, 'sliderFcap', fcapPct);
    setText(els.valFcap, 'valFcap', `${fcapPct}%`);

    setInputVal(els.sliderTemp, 'sliderTemp', state.ambientTempC);
    setText(els.valTemp, 'valTemp', `${state.ambientTempC.toFixed(1)} °C`);

    setInputVal(els.sliderRain, 'sliderRain', state.rainfallMultiplier);
    setText(
      els.valRain,
      'valRain',
      `${state.rainfallMultiplier.toFixed(2)}× (${(772.5 * state.rainfallMultiplier).toFixed(0)} mm/yr)`
    );

    setInputVal(els.sliderTime, 'sliderTime', state.timeOfDay);
    setText(els.valTime, 'valTime', formatClock(state.timeOfDay));

    const xrayPct = Math.round(groundOpacity * 100);
    setInputVal(els.sliderXray, 'sliderXray', xrayPct);
    setText(els.valXray, 'valXray', `${xrayPct}%`);

    setChecked(els.toggleSdn, 'sdn', Boolean(state.sdnEnabled));
    setChecked(els.toggleScenarioB, 'scenarioB', Boolean(state.hthpScenarioB));

    for (let i = 0; i < faultBtnList.length; i++) {
      const faulted = Boolean(state.vaultFaults[i]);
      if (lastVaultFaults[i] !== faulted) {
        lastVaultFaults[i] = faulted;
        const btn = faultBtnList[i];
        btn.classList.toggle('faulted', faulted);
        btn.textContent = faulted ? `V${i + 1} FLT` : `V${i + 1} OK`;
      }
    }
  }

  function updateInspector(sim) {
    if (!inspectedKey || !SUBSYSTEM_INFO[inspectedKey]) return;
    const info = SUBSYSTEM_INFO[inspectedKey];

    // Static metadata and specs are only rebuilt when the selected subsystem changes
    if (renderedInspectedKey !== inspectedKey) {
      renderedInspectedKey = inspectedKey;
      setText(els.inspBadge, 'inspBadge', info.category);
      setText(els.inspTitle, 'inspTitle', info.title);
      setText(els.inspDesc, 'inspDesc', info.description);

      const specFrag = document.createDocumentFragment();
      for (const spec of info.specs) {
        const li = document.createElement('li');
        li.textContent = spec;
        specFrag.appendChild(li);
      }
      els.inspSpecs.replaceChildren(specFrag);
      inspectorMetricNodes = [];
    }

    const metrics = info.getLiveMetrics(sim);
    if (inspectorMetricNodes.length !== metrics.length) {
      const metricFrag = document.createDocumentFragment();
      inspectorMetricNodes = [];
      for (const [k, v] of metrics) {
        const kEl = document.createElement('span');
        kEl.className = 'kv-k';
        kEl.textContent = k;

        const vEl = document.createElement('span');
        vEl.className = 'kv-v';
        const vStr = String(v);
        vEl.textContent = vStr;

        metricFrag.appendChild(kEl);
        metricFrag.appendChild(vEl);
        inspectorMetricNodes.push({ kEl, vEl, lastK: k, lastV: vStr });
      }
      els.inspMetrics.replaceChildren(metricFrag);
    } else {
      for (let i = 0; i < metrics.length; i++) {
        const [k, v] = metrics[i];
        const node = inspectorMetricNodes[i];
        if (node.lastK !== k) {
          node.kEl.textContent = k;
          node.lastK = k;
        }
        const vStr = String(v);
        if (node.lastV !== vStr) {
          node.vEl.textContent = vStr;
          node.lastV = vStr;
        }
      }
    }
  }

  function setInspectedKey(key, sim) {
    if (!SUBSYSTEM_INFO[key]) return;
    inspectedKey = key;
    updateInspector(sim);
  }

  function renderTelemetry(sim, runtimeFlags) {
    const e = sim.electrical;
    const t = sim.thermal;
    const h = sim.hydrology;
    const o = sim.optical;
    const f = sim.financial;
    const sv = sim.survey;

    const co2Rounded = INT_FORMATTER.format(Math.round(f.totalAvoidedCo2TonYr));

    // Top KPI Pills (diffed via textCache)
    setText(els.kpiIt, 'kpiIt', `${e.totalItMw.toFixed(2)} MW`);
    setText(els.kpiPue, 'kpiPue', `${e.pue.toFixed(3)}`);
    setText(els.kpiCov, 'kpiCov', `${t.annualCoveragePct.toFixed(1)}%`);
    setText(els.kpiRtt, 'kpiRtt', `${o.activeLocalE2eRttUs.toFixed(1)} μs`);
    setText(els.kpiCo2, 'kpiCo2', `${co2Rounded} t/yr`);
    setText(els.kpiNpv, 'kpiNpv', `+€${f.NPV_4PCT_M.toFixed(2)}M`);

    // Dock button states
    setText(els.btnPlay, 'btnPlay', runtimeFlags.playing ? '⏸ Pause' : '▶ Play');
    setText(els.btnSpeed, 'btnSpeed', `${runtimeFlags.speed.toFixed(1)}× Speed`);
    setText(els.btnDaycycle, 'btnDaycycle', runtimeFlags.autoDay ? '☀ Day Cycle: On' : '☀ Day Cycle: Off');

    // Thermal & Exergy Cascade card (targeted DOM node updates)
    thermalCapturedBar.update(`${t.capturedHeatMw.toFixed(2)} MW_th`, (t.capturedHeatMw / 10) * 100);
    thermalDemandBar.update(`${t.districtDemandMw.toFixed(2)} MW_th`, (t.districtDemandMw / 10) * 100);

    const isSurplus = t.netThermalBalanceMw >= 0;
    const balSign = isSurplus ? '+' : '−';
    const balClass = isSurplus ? 'accent-emerald' : 'accent-amber';

    thermalCoreKv.set('balance', `${balSign}${Math.abs(t.netThermalBalanceMw).toFixed(2)} MW_th`, balClass);
    thermalCoreKv.set(
      'annual',
      `${INT_FORMATTER.format(Math.round(t.annualCapturedHeatMWh))} MWh (${t.annualCoveragePct.toFixed(1)}%)`
    );
    thermalCoreKv.set(
      'primary',
      `${t.primaryGrossFlowLmin.toFixed(1)} / ${t.primaryNetFlowLmin.toFixed(0)} L/min`
    );
    thermalCoreKv.set(
      'secondary',
      `${t.secondaryGrossFlowLmin.toFixed(1)} L/min (${t.secondaryGrossFlowLs.toFixed(2)} L/s)`
    );
    thermalCoreKv.set(
      'store',
      `${(t.thermalStoreSoc * 100).toFixed(0)}% (${t.thermalStoreEnergyMWh.toFixed(1)} MWh_th)`
    );

    if (lastSurplusMode !== isSurplus) {
      lastSurplusMode = isSurplus;
      thermalSurplusKv.el.hidden = !isSurplus;
      thermalSurplusKv.el.classList.toggle('hidden', !isSurplus);
      thermalDeficitKv.el.hidden = isSurplus;
      thermalDeficitKv.el.classList.toggle('hidden', isSurplus);
    }

    if (isSurplus) {
      thermalSurplusKv.set('storeCharge', `${t.thermalStoreChargeMw.toFixed(2)} MW_th`);
      thermalSurplusKv.set('tces', `${t.tcesChargeMw.toFixed(2)} MW_th`);
      thermalSurplusKv.set('tiPtes', `${t.tiPtesChargeMw.toFixed(2)} MW_th`);
      thermalSurplusKv.set('orc', `${t.orcOutputKw.toFixed(1)} kW_e`);
      thermalSurplusKv.set('mofDry', `${t.mofChillerMw.toFixed(2)} / ${t.dryCoolerRejectionMw.toFixed(2)} MW`);
    } else {
      thermalDeficitKv.set('storeDischarge', `${t.thermalStoreDischargeMw.toFixed(2)} MW_th`);
      thermalDeficitKv.set('hthpOut', `${t.hthpThermalOutputMw.toFixed(2)} MW_th (COP ${t.hthpCop.toFixed(2)})`);
      thermalDeficitKv.set('hthpElec', `${t.hthpElectricalMw.toFixed(2)} MW_e`);
      thermalDeficitKv.set('unmet', `${t.unmetThermalDeficitMw.toFixed(2)} MW_th`);
    }

    // Electrical, Hydrology & Optical Card
    cisternBar.update(`${h.cisternVolumeM3.toFixed(0)} m³ (${h.droughtAutonomyDays.toFixed(1)} d)`, h.cisternSoc * 100);
    gridWaterKv.set(
      'gridAnnual',
      `${INT_FORMATTER.format(Math.round(e.annualGrossMWh))} / ${INT_FORMATTER.format(Math.round(e.annualNetGridMWh))} MWh_e`
    );
    gridWaterKv.set('pvImport', `${e.pvOutputMw.toFixed(2)} MW / ${e.netGridDrawMw.toFixed(2)} MW`);
    gridWaterKv.set('bess', e.bessMode);
    gridWaterKv.set('rain', `${INT_FORMATTER.format(Math.round(h.annualRainHarvestM3))} m³/yr`);
    gridWaterKv.set('waterSaved', `${INT_FORMATTER.format(h.avoidedPotableWaterM3Yr)} m³/yr`);
    gridWaterKv.set(
      'v2x',
      `${o.localBlindTravelMm.toFixed(2)} mm vs ${o.cloudBlindTravelM.toFixed(2)} m`
    );
    gridWaterKv.set('breakout', `${o.localBreakoutPct}% / ${o.wanPowerSavedMWhYr.toFixed(0)} MWh_e/yr`);

    // Financials & Resident Survey (mostly static; zero DOM writes unless values change)
    financeKv.set('capex', `€${f.CAPEX_TOTAL_M}M vs €${f.COUNTERFACTUAL_CAPEX_M}M`);
    financeKv.set('ebitda', `+€${f.EBITDA_ANNUAL_M.toFixed(2)}M/yr · ${f.SIMPLE_PAYBACK_YR} yr`);
    financeKv.set('irr', `${f.IRR_PCT}% · +€${f.NPV_8PCT_M}M`);
    financeKv.set('co2', `${co2Rounded} tCO₂/yr`);
    financeKv.set('survey', `${sv.dcComfortScore}/5 · ${sv.recycledWaterScore}/5 (N=${sv.sampleSize})`);

    updateInspector(sim);
  }

  return {
    syncControlsFromState,
    renderTelemetry,
    setInspectedKey,
  };
}
