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
const THEME_STORAGE_KEY = 'rakda_theme';

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
    metaThemeColor: document.getElementById('meta-theme-color'),

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

    btnTheme: document.getElementById('btn-theme'),
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

  function setPressed(el, key, pressed) {
    const str = pressed ? 'true' : 'false';
    if (boolCache[key] !== str) {
      el.setAttribute('aria-pressed', str);
      boolCache[key] = str;
    }
  }

  // Theme initialization & toggle (Dark Slate <-> rakda.ltd Light Paper)
  function applyTheme(isLight) {
    document.body.classList.toggle('theme-light', isLight);
    if (els.btnTheme) {
      els.btnTheme.textContent = isLight ? '☾ Slate' : '☀ Paper';
      els.btnTheme.setAttribute('aria-pressed', isLight ? 'true' : 'false');
    }
    if (els.metaThemeColor) {
      els.metaThemeColor.setAttribute('content', isLight ? '#FAF7F2' : '#0b1220');
    }
  }

  let lightTheme = false;
  try {
    lightTheme = window.localStorage?.getItem(THEME_STORAGE_KEY) === 'light';
  } catch {
    lightTheme = false;
  }
  applyTheme(lightTheme);

  if (els.btnTheme) {
    els.btnTheme.addEventListener('click', () => {
      lightTheme = !lightTheme;
      applyTheme(lightTheme);
      try {
        window.localStorage?.setItem(THEME_STORAGE_KEY, lightTheme ? 'light' : 'dark');
      } catch {
        // Ignore storage restrictions
      }
    });
  }

  // Auto-collapse side drawers on narrow mobile viewports (<= 900px)
  function setPanelCollapsed(panel, toggleBtn, collapsed) {
    panel.classList.toggle('collapsed', collapsed);
    toggleBtn.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
  }

  if (typeof window !== 'undefined' && window.innerWidth <= 900) {
    setPanelCollapsed(els.leftPanel, els.btnToggleLeft, true);
    setPanelCollapsed(els.rightPanel, els.btnToggleRight, true);
  }

  let inspectedKey = null;
  let renderedInspectedKey = null;
  let inspectorMetricNodes = [];

  // Build persistent DOM structures for telemetry cards once (zero innerHTML thrashing)
  const thermalCapturedBar = createBarBlock('Captured Heat (98% nom.)', '#ff6b2c');
  const thermalDemandBar = createBarBlock('Network Demand (Incl. 0.39 MW loss)', '#22d3ee');
  const thermalCoreKv = createKvGrid(
    [
      ['balance', 'Net Thermal Balance', ''],
      ['annual', 'Annual Heat / Net Cov.', ''],
      ['primary', 'Primary 75°C (Gross/Net)', ''],
      ['secondary', 'Secondary 45°C Flow', ''],
      ['store', '2,500 m³ Store (101.6 MWh)', ''],
    ],
    '4px'
  );
  const thermalSurplusKv = createKvGrid(
    [
      ['storeCharge', '2,500 m³ Store Charge', ''],
      ['commSales', 'Tier 1: 75°C Heat Sales', 'accent-orange'],
      ['orc', 'Tier 2: Micro-ORC (~0.4 MW_e)', 'accent-emerald'],
      ['dryCool', 'Tier 3: N+1 Dry Coolers', ''],
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

  const cisternBar = createBarBlock('10,000 m³ Cistern (44 d Reserve)', '#0ea5e9');
  const gridWaterKv = createKvGrid(
    [
      ['gridAnnual', 'Gross / Net Grid Annual', ''],
      ['pvImport', '12 MW_p PV / Net Import', ''],
      ['bess', '8 MW / 32 MWh BESS', ''],
      ['rain', 'Rain Harvest (10 ha inert)', ''],
      ['waterSaved', 'Avoided Tower Water', 'accent-cyan'],
      ['v2x', 'Local V2X vs Cloud (50 km/h)', 'accent-violet'],
      ['breakout', 'Local Breakout / WAN Saved', ''],
    ],
    '4px'
  );
  els.telGridWater.replaceChildren(cisternBar.el, gridWaterKv.el);

  const financeKv = createKvGrid(
    [
      ['capex', 'CapEx (Utility + DC)', ''],
      ['ebitda', 'Case 2 EBITDA / Payback', 'accent-emerald'],
      ['irr', 'Case 2 IRR / NPV @ 8%', ''],
      ['utility', 'Case 1 Utility IRR / LCOH', ''],
      ['tco', '30-Yr TCO (USCI/Gas/ASHP)', ''],
      ['co2', 'Avoided CO₂ (Heat + PUE)', 'accent-emerald'],
      ['survey', 'Survey Comfort / H₂O (N=105)', ''],
    ],
    ''
  );
  els.telFinance.replaceChildren(financeKv.el);

  // Build 7 scenario buttons
  const presetBtnMap = new Map();
  let activeScenarioId = null;
  for (const preset of SCENARIO_LIST) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'preset-btn';
    btn.setAttribute('aria-pressed', 'false');
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
    btn.type = 'button';
    btn.className = 'vault-btn';
    btn.setAttribute('aria-pressed', 'false');
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
    if (camBtns[activeCamIdx]) {
      camBtns[activeCamIdx].classList.remove('active');
      camBtns[activeCamIdx].setAttribute('aria-pressed', 'false');
    }
    if (camBtns[idx]) {
      camBtns[idx].classList.add('active');
      camBtns[idx].setAttribute('aria-pressed', 'true');
    }
    activeCamIdx = idx;
  }

  CAMERA_PRESET_LABELS.forEach((cp, idx) => {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'cam-btn' + (idx === 0 ? ' active' : '');
    btn.setAttribute('aria-pressed', idx === 0 ? 'true' : 'false');
    btn.textContent = cp.label;
    btn.addEventListener('click', () => {
      setActiveCamBtn(idx);
      callbacks.onCameraPreset(idx);
    });
    els.cameraPresets.appendChild(btn);
    camBtns.push(btn);
  });

  // Leading-edge + rAF-coalesced slider dispatch
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
  els.btnToggleLeft.addEventListener('click', () => {
    const nextCollapsed = !els.leftPanel.classList.contains('collapsed');
    setPanelCollapsed(els.leftPanel, els.btnToggleLeft, nextCollapsed);
  });
  els.btnToggleRight.addEventListener('click', () => {
    const nextCollapsed = !els.rightPanel.classList.contains('collapsed');
    setPanelCollapsed(els.rightPanel, els.btnToggleRight, nextCollapsed);
  });

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
        const prevBtn = presetBtnMap.get(activeScenarioId);
        prevBtn.classList.remove('active');
        prevBtn.setAttribute('aria-pressed', 'false');
      }
      if (presetBtnMap.has(state.scenarioId)) {
        const nextBtn = presetBtnMap.get(state.scenarioId);
        nextBtn.classList.add('active');
        nextBtn.setAttribute('aria-pressed', 'true');
      }
      activeScenarioId = state.scenarioId;

      const preset = SCENARIO_PRESETS[state.scenarioId];
      setText(
        els.scenarioDesc,
        'scenarioDesc',
        preset ? preset.description : 'Custom interactive engineering configuration (live RAKDA USCI equations).'
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

    setPressed(els.btnXray, 'btnXray', groundOpacity <= 0.4);

    for (let i = 0; i < faultBtnList.length; i++) {
      const faulted = Boolean(state.vaultFaults[i]);
      if (lastVaultFaults[i] !== faulted) {
        lastVaultFaults[i] = faulted;
        const btn = faultBtnList[i];
        btn.classList.toggle('faulted', faulted);
        btn.setAttribute('aria-pressed', faulted ? 'true' : 'false');
        btn.textContent = faulted ? `V${i + 1} FLT` : `V${i + 1} OK`;
      }
    }
  }

  function updateInspector(sim) {
    if (!inspectedKey || !SUBSYSTEM_INFO[inspectedKey]) return;
    const info = SUBSYSTEM_INFO[inspectedKey];

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
    const isPresent = document.body.classList.contains('presentation-mode');

    // Top KPI Pills
    setText(els.kpiIt, 'kpiIt', `${e.totalItMw.toFixed(2)} MW`);
    setText(els.kpiPue, 'kpiPue', `${e.pue.toFixed(3)}`);
    setText(els.kpiCov, 'kpiCov', `${t.annualCoveragePct.toFixed(1)}%`);
    setText(els.kpiRtt, 'kpiRtt', `${o.activeLocalE2eRttUs.toFixed(1)} μs`);
    setText(els.kpiCo2, 'kpiCo2', `${co2Rounded} t/yr`);
    setText(els.kpiNpv, 'kpiNpv', `+€${f.NPV_4PCT_M.toFixed(1)}M`);

    // Dock button states
    setText(els.btnPlay, 'btnPlay', runtimeFlags.playing ? '⏸ Pause' : '▶ Play');
    setPressed(els.btnPlay, 'btnPlay', Boolean(runtimeFlags.playing));
    setText(els.btnSpeed, 'btnSpeed', `${runtimeFlags.speed.toFixed(1)}× Speed`);
    setText(els.btnDaycycle, 'btnDaycycle', runtimeFlags.autoDay ? '☀ Day Cycle: On' : '☀ Day Cycle: Off');
    setPressed(els.btnDaycycle, 'btnDaycycle', Boolean(runtimeFlags.autoDay));
    setPressed(els.btnPresent, 'btnPresent', isPresent);
    setPressed(els.btnPresentTop, 'btnPresentTop', isPresent);

    // Thermal & Surplus Cascade card
    thermalCapturedBar.update(`${t.capturedHeatMw.toFixed(2)} MW_th`, (t.capturedHeatMw / 10) * 100);
    thermalDemandBar.update(`${t.totalNetworkDemandMw.toFixed(2)} MW_th`, (t.totalNetworkDemandMw / 10) * 100);

    const isSurplus = t.netThermalBalanceMw >= 0;
    const balSign = isSurplus ? '+' : '−';
    const balClass = isSurplus ? 'accent-emerald' : 'accent-amber';

    thermalCoreKv.set('balance', `${balSign}${Math.abs(t.netThermalBalanceMw).toFixed(2)} MW_th`, balClass);
    thermalCoreKv.set(
      'annual',
      `${INT_FORMATTER.format(Math.round(t.annualCapturedHeatMWh))} MWh (${t.annualGrossCoveragePct.toFixed(0)}% / ${t.annualCoveragePct.toFixed(0)}%)`
    );
    thermalCoreKv.set(
      'primary',
      `${t.primaryGrossFlowLs.toFixed(1)} L/s (${t.primaryGrossFlowLmin.toFixed(0)} L/m)`
    );
    thermalCoreKv.set(
      'secondary',
      `${t.secondaryGrossFlowLs.toFixed(1)} L/s (${t.secondaryGrossFlowLmin.toFixed(0)} L/m)`
    );
    thermalCoreKv.set(
      'store',
      `${(t.thermalStoreSoc * 100).toFixed(0)}% (${t.thermalStoreUsableMWh.toFixed(1)} MWh usable)`
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
      thermalSurplusKv.set('commSales', `${t.commercialHeatSalesMw.toFixed(2)} MW_th`);
      thermalSurplusKv.set('orc', `${t.orcOutputKw.toFixed(1)} kW_e`);
      thermalSurplusKv.set('dryCool', `${t.dryCoolerRejectionMw.toFixed(2)} MW_th`);
    } else {
      thermalDeficitKv.set('storeDischarge', `${t.thermalStoreDischargeMw.toFixed(2)} MW_th`);
      thermalDeficitKv.set('hthpOut', `${t.hthpThermalOutputMw.toFixed(2)} MW_th (COP ${t.hthpCop.toFixed(2)})`);
      thermalDeficitKv.set('hthpElec', `${(t.hthpElectricalMw + t.scenarioBElecMw).toFixed(2)} MW_e`);
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
    gridWaterKv.set('waterSaved', '157.7–175.2k m³/yr');
    gridWaterKv.set(
      'v2x',
      `${o.localBlindTravelMm.toFixed(2)} mm vs ${o.cloudBlindTravelM.toFixed(2)} m`
    );
    gridWaterKv.set('breakout', `${o.localBreakoutPct}% / ${o.wanPowerSavedMWhYr.toFixed(0)} MWh_e/yr`);

    // Financials & Resident Survey
    financeKv.set('capex', `€${f.CAPEX_TOTAL_M.toFixed(2)}M (€${f.CAPEX_DISTRICT_M}M + €${f.CAPEX_DC_OPERATOR_M}M)`);
    financeKv.set('ebitda', `+€${f.EBITDA_ANNUAL_M.toFixed(1)}M/yr · ${f.SIMPLE_PAYBACK_YR.toFixed(1)} yr`);
    financeKv.set('irr', `${f.IRR_PCT}% · +€${f.NPV_8PCT_M}M (8%)`);
    financeKv.set('utility', `${f.UTILITY_IRR_PCT}% · €${f.UTILITY_LCOH_NET_EUR_MWH}/MWh`);
    financeKv.set('tco', `€${f.TCO_USCI_M}M / €${f.TCO_GAS_M}M / €${f.TCO_ASHP_M}M`);
    financeKv.set('co2', `${co2Rounded} t/yr (Heat+PUE)`);
    financeKv.set('survey', `${sv.dcComfortScore.toFixed(2)}/5 · ${sv.recycledWaterScore.toFixed(2)}/5`);

    updateInspector(sim);
  }

  return {
    syncControlsFromState,
    renderTelemetry,
    setInspectedKey,
  };
}
