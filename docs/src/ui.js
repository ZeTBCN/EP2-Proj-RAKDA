import { SCENARIO_LIST, SCENARIO_PRESETS, SUBSYSTEM_INFO } from './physics.js';

const CAMERA_PRESET_LABELS = [
  { key: '1', label: '1 · District Overview' },
  { key: '2', label: '2 · Level −4 Vaults' },
  { key: '3', label: '3 · Central Hub' },
  { key: '4', label: '4 · Vault 3 Close-Up' },
  { key: '5', label: '5 · Street Pods' },
  { key: '6', label: '6 · Top Masterplan' },
];

function formatClock(hoursFloat) {
  const wrapped = ((hoursFloat % 24) + 24) % 24;
  const hh = Math.floor(wrapped);
  const mm = Math.round((wrapped - hh) * 60) % 60;
  return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`;
}

function barRow(label, valueText, pct, color) {
  const clamped = Math.min(100, Math.max(0, pct));
  return `
    <div class="bar-block">
      <div class="bar-head">
        <span>${label}</span>
        <span class="mono">${valueText}</span>
      </div>
      <div class="bar-track">
        <div class="bar-fill" style="width:${clamped.toFixed(1)}%; background:${color};"></div>
      </div>
    </div>
  `;
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

  let inspectedKey = null;

  // Build 7 scenario buttons
  const presetBtnMap = new Map();
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
  CAMERA_PRESET_LABELS.forEach((cp, idx) => {
    const btn = document.createElement('button');
    btn.className = 'cam-btn' + (idx === 0 ? ' active' : '');
    btn.textContent = cp.label;
    btn.addEventListener('click', () => {
      camBtns.forEach((b, j) => b.classList.toggle('active', j === idx));
      callbacks.onCameraPreset(idx);
    });
    els.cameraPresets.appendChild(btn);
    camBtns.push(btn);
  });

  // Slider bindings
  els.sliderUtil.addEventListener('input', () =>
    callbacks.onStatePatch({ utilization: Number(els.sliderUtil.value) / 100, scenarioId: 'custom' })
  );
  els.sliderFcap.addEventListener('input', () =>
    callbacks.onStatePatch({ heatCaptureEff: Number(els.sliderFcap.value) / 100, scenarioId: 'custom' })
  );
  els.sliderTemp.addEventListener('input', () =>
    callbacks.onStatePatch({ ambientTempC: Number(els.sliderTemp.value), scenarioId: 'custom' })
  );
  els.sliderRain.addEventListener('input', () =>
    callbacks.onStatePatch({ rainfallMultiplier: Number(els.sliderRain.value), scenarioId: 'custom' })
  );
  els.sliderTime.addEventListener('input', () =>
    callbacks.onStatePatch({ timeOfDay: Number(els.sliderTime.value) })
  );
  els.sliderXray.addEventListener('input', () =>
    callbacks.onGroundOpacity(Number(els.sliderXray.value) / 100)
  );

  els.toggleSdn.addEventListener('change', () =>
    callbacks.onStatePatch({ sdnEnabled: els.toggleSdn.checked, scenarioId: 'custom' })
  );
  els.toggleScenarioB.addEventListener('change', () =>
    callbacks.onStatePatch({ hthpScenarioB: els.toggleScenarioB.checked, scenarioId: 'custom' })
  );

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
  els.btnReset.addEventListener('click', () => callbacks.onCameraPreset(0));
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
      camBtns.forEach((b, j) => b.classList.toggle('active', j === idx));
      callbacks.onCameraPreset(idx);
    } else if (e.key === 'x' || e.key === 'X') {
      callbacks.onAction('toggleXray');
    } else if (e.key === ' ') {
      e.preventDefault();
      callbacks.onAction('togglePlay');
    } else if (e.key === 'h' || e.key === 'H') {
      callbacks.onAction('togglePresent');
    } else if (e.key === 'r' || e.key === 'R') {
      camBtns.forEach((b, j) => b.classList.toggle('active', j === 0));
      callbacks.onCameraPreset(0);
    }
  });

  function syncControlsFromState(state, groundOpacity) {
    for (const [id, btn] of presetBtnMap.entries()) {
      btn.classList.toggle('active', id === state.scenarioId);
    }
    const preset = SCENARIO_PRESETS[state.scenarioId];
    els.scenarioDesc.textContent = preset
      ? preset.description
      : 'Custom interactive engineering configuration (live Draft 3.2 equations).';

    els.sliderUtil.value = Math.round(state.utilization * 100);
    els.valUtil.textContent = `${Math.round(state.utilization * 100)}% (${(state.utilization * 10).toFixed(1)} MW)`;

    els.sliderFcap.value = Math.round(state.heatCaptureEff * 100);
    els.valFcap.textContent = `${Math.round(state.heatCaptureEff * 100)}%`;

    els.sliderTemp.value = state.ambientTempC;
    els.valTemp.textContent = `${state.ambientTempC.toFixed(1)} °C`;

    els.sliderRain.value = state.rainfallMultiplier;
    els.valRain.textContent = `${state.rainfallMultiplier.toFixed(2)}× (${(772.5 * state.rainfallMultiplier).toFixed(0)} mm/yr)`;

    els.sliderTime.value = state.timeOfDay;
    els.valTime.textContent = formatClock(state.timeOfDay);

    els.sliderXray.value = Math.round(groundOpacity * 100);
    els.valXray.textContent = `${Math.round(groundOpacity * 100)}%`;

    els.toggleSdn.checked = Boolean(state.sdnEnabled);
    els.toggleScenarioB.checked = Boolean(state.hthpScenarioB);

    faultBtnList.forEach((btn, i) => {
      const faulted = Boolean(state.vaultFaults[i]);
      btn.classList.toggle('faulted', faulted);
      btn.textContent = faulted ? `V${i + 1} FLT` : `V${i + 1} OK`;
    });
  }

  function updateInspector(sim) {
    if (!inspectedKey || !SUBSYSTEM_INFO[inspectedKey]) return;
    const info = SUBSYSTEM_INFO[inspectedKey];
    els.inspBadge.textContent = info.category;
    els.inspTitle.textContent = info.title;
    els.inspDesc.textContent = info.description;
    els.inspSpecs.innerHTML = info.specs.map((s) => `<li>${s}</li>`).join('');
    els.inspMetrics.innerHTML = info
      .getLiveMetrics(sim)
      .map(([k, v]) => `<span class="kv-k">${k}</span><span class="kv-v">${v}</span>`)
      .join('');
  }

  function setInspectedKey(key, sim) {
    if (!SUBSYSTEM_INFO[key]) return;
    inspectedKey = key;
    updateInspector(sim);
  }

  function renderTelemetry(sim, runtimeFlags) {
    // Top KPI Pills
    els.kpiIt.textContent = `${sim.electrical.totalItMw.toFixed(2)} MW`;
    els.kpiPue.textContent = `${sim.electrical.pue.toFixed(3)}`;
    els.kpiCov.textContent = `${sim.thermal.annualCoveragePct.toFixed(1)}%`;
    els.kpiRtt.textContent = `${sim.optical.activeLocalE2eRttUs.toFixed(1)} μs`;
    els.kpiCo2.textContent = `${Math.round(sim.financial.totalAvoidedCo2TonYr).toLocaleString()} t/yr`;
    els.kpiNpv.textContent = `+€${sim.financial.NPV_4PCT_M.toFixed(2)}M`;

    // Dock button states
    els.btnPlay.textContent = runtimeFlags.playing ? '⏸ Pause' : '▶ Play';
    els.btnSpeed.textContent = `${runtimeFlags.speed.toFixed(1)}× Speed`;
    els.btnDaycycle.textContent = runtimeFlags.autoDay ? '☀ Day Cycle: On' : '☀ Day Cycle: Off';

    // Thermal & Exergy Cascade card
    const t = sim.thermal;
    const isSurplus = t.netThermalBalanceMw >= 0;
    const balSign = isSurplus ? '+' : '−';
    const balClass = isSurplus ? 'accent-emerald' : 'accent-amber';

    let modeDetails = '';
    if (isSurplus) {
      modeDetails = `
        <div class="kv-grid" style="margin-top:6px;">
          <span class="kv-k">2,500 m³ Store Charge</span><span class="kv-v">${t.thermalStoreChargeMw.toFixed(2)} MW_th</span>
          <span class="kv-k">SrBr₂ TCES (35 MWh_th)</span><span class="kv-v">${t.tcesChargeMw.toFixed(2)} MW_th</span>
          <span class="kv-k">TI-PTES (2 MW / 16 MWh)</span><span class="kv-v">${t.tiPtesChargeMw.toFixed(2)} MW_th</span>
          <span class="kv-k">Micro-ORC (≤440 kW_e)</span><span class="kv-v accent-emerald">${t.orcOutputKw.toFixed(1)} kW_e</span>
          <span class="kv-k">MOF Chillers / Dry Cool</span><span class="kv-v">${t.mofChillerMw.toFixed(2)} / ${t.dryCoolerRejectionMw.toFixed(2)} MW</span>
        </div>
      `;
    } else {
      modeDetails = `
        <div class="kv-grid" style="margin-top:6px;">
          <span class="kv-k">Thermal Store Discharge</span><span class="kv-v accent-orange">${t.thermalStoreDischargeMw.toFixed(2)} MW_th</span>
          <span class="kv-k">8.0 MW_th Backup HTHP</span><span class="kv-v accent-amber">${t.hthpThermalOutputMw.toFixed(2)} MW_th (COP ${t.hthpCop.toFixed(2)})</span>
          <span class="kv-k">HTHP Electric Draw</span><span class="kv-v">${t.hthpElectricalMw.toFixed(2)} MW_e</span>
          <span class="kv-k">Unmet Deficit</span><span class="kv-v accent-emerald">${t.unmetThermalDeficitMw.toFixed(2)} MW_th</span>
        </div>
      `;
    }

    els.telThermal.innerHTML = `
      ${barRow('Captured Heat (98% nom.)', `${t.capturedHeatMw.toFixed(2)} MW_th`, (t.capturedHeatMw / 10) * 100, '#ff6b2c')}
      ${barRow('District Demand (7,500 homes)', `${t.districtDemandMw.toFixed(2)} MW_th`, (t.districtDemandMw / 10) * 100, '#22d3ee')}
      <div class="kv-grid" style="margin-top:4px;">
        <span class="kv-k">Net Thermal Balance</span><span class="kv-v ${balClass}">${balSign}${Math.abs(t.netThermalBalanceMw).toFixed(2)} MW_th</span>
        <span class="kv-k">Annual Heat / Coverage</span><span class="kv-v">${Math.round(t.annualCapturedHeatMWh).toLocaleString()} MWh (${t.annualCoveragePct.toFixed(1)}%)</span>
        <span class="kv-k">Primary 75°C (Gross/Net)</span><span class="kv-v">${t.primaryGrossFlowLmin.toFixed(1)} / ${t.primaryNetFlowLmin.toFixed(0)} L/min</span>
        <span class="kv-k">Secondary 45°C Flow</span><span class="kv-v">${t.secondaryGrossFlowLmin.toFixed(1)} L/min (${t.secondaryGrossFlowLs.toFixed(2)} L/s)</span>
        <span class="kv-k">2,500 m³ Thermal Store</span><span class="kv-v">${(t.thermalStoreSoc * 100).toFixed(0)}% (${t.thermalStoreEnergyMWh.toFixed(1)} MWh_th)</span>
      </div>
      ${modeDetails}
    `;

    // Electrical, Hydrology & Optical Card
    const e = sim.electrical;
    const h = sim.hydrology;
    const o = sim.optical;
    els.telGridWater.innerHTML = `
      ${barRow('10,000 m³ Drought Cistern', `${h.cisternVolumeM3.toFixed(0)} m³ (${h.droughtAutonomyDays.toFixed(1)} d)`, h.cisternSoc * 100, '#0ea5e9')}
      <div class="kv-grid" style="margin-top:4px;">
        <span class="kv-k">Gross / Net Grid Annual</span><span class="kv-v">${Math.round(e.annualGrossMWh).toLocaleString()} / ${Math.round(e.annualNetGridMWh).toLocaleString()} MWh_e</span>
        <span class="kv-k">12 MW_p PV / Net Import</span><span class="kv-v">${e.pvOutputMw.toFixed(2)} MW / ${e.netGridDrawMw.toFixed(2)} MW</span>
        <span class="kv-k">8 MW / 32 MWh BESS</span><span class="kv-v">${e.bessMode}</span>
        <span class="kv-k">Rain Harvest (120k m²)</span><span class="kv-v">${Math.round(h.annualRainHarvestM3).toLocaleString()} m³/yr</span>
        <span class="kv-k">Avoided Cooling Water</span><span class="kv-v accent-cyan">${h.avoidedPotableWaterM3Yr.toLocaleString()} m³/yr</span>
        <span class="kv-k">Local V2X vs Cloud (50 km/h)</span><span class="kv-v accent-violet">${o.localBlindTravelMm.toFixed(2)} mm vs ${o.cloudBlindTravelM.toFixed(2)} m</span>
        <span class="kv-k">Local Breakout / WAN Saved</span><span class="kv-v">${o.localBreakoutPct}% / ${o.wanPowerSavedMWhYr.toFixed(0)} MWh_e/yr</span>
      </div>
    `;

    // Financials & Resident Survey
    const f = sim.financial;
    const sv = sim.survey;
    els.telFinance.innerHTML = `
      <div class="kv-grid">
        <span class="kv-k">Case 2 CapEx vs Counterfactual</span><span class="kv-v">€${f.CAPEX_TOTAL_M}M vs €${f.COUNTERFACTUAL_CAPEX_M}M</span>
        <span class="kv-k">EBITDA / Simple Payback</span><span class="kv-v accent-emerald">+€${f.EBITDA_ANNUAL_M.toFixed(2)}M/yr · ${f.SIMPLE_PAYBACK_YR} yr</span>
        <span class="kv-k">30-Yr IRR / NPV @ 8%</span><span class="kv-v">${f.IRR_PCT}% · +€${f.NPV_8PCT_M}M</span>
        <span class="kv-k">Avoided Carbon (Heat + PUE)</span><span class="kv-v accent-emerald">${Math.round(f.totalAvoidedCo2TonYr).toLocaleString()} tCO₂/yr</span>
        <span class="kv-k">Survey Comfort / Recycled H₂O</span><span class="kv-v">${sv.dcComfortScore}/5 · ${sv.recycledWaterScore}/5 (N=${sv.sampleSize})</span>
      </div>
    `;

    updateInspector(sim);
  }

  return {
    syncControlsFromState,
    renderTelemetry,
    setInspectedKey,
  };
}
