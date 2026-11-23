/**
 * RAKDA Ltd. (Group A-10) — 2045 Circular Urban Digital Twin
 * Pure deterministic physics, thermal, hydrological, optical, and financial engine.
 * Grounded strictly in SPEC.md (RAKDA Ltd. Report — Draft 3.2, 21 Nov 2026).
 * ZERO DOM or Three.js dependencies.
 */

export const CONSTANTS = Object.freeze({
  DISTRICT: Object.freeze({
    AREA_HA: 180.0,
    RESIDENTS: 20000,
    HOMES: 7500,
    BUILDINGS: 3095,
    RESIDENTIAL_HA: 90.0,
    DENSITY_DPH: 83.3,
    ROADS_HA: 36.0,
    PARKS_HA: 25.0,
    SCHOOLS_HA: 15.0,
    RETAIL_CIVIC_HA: 14.0,
    HOUSING: Object.freeze({
      detached: Object.freeze({ label: 'Detached (2–3 fl.)', units: 900, buildings: 900, ha: 22.5, residents: 3200 }),
      townhouses: Object.freeze({ label: 'Townhouses (2–3 fl.)', units: 2100, buildings: 2100, ha: 30.0, residents: 6800 }),
      lowRise: Object.freeze({ label: 'Low-rise apts (3–4 fl.)', units: 1200, blocks: 40, ha: 12.0, residents: 3000 }),
      midRise: Object.freeze({ label: 'Mid-rise apts (5–7 fl.)', units: 3300, blocks: 55, ha: 22.0, residents: 7000 }),
    }),
  }),

  DC: Object.freeze({
    VAULT_COUNT: 5,
    VAULT_CAPACITY_MW: 2.0,
    TOTAL_IT_CAPACITY_MW: 10.0,
    VAULT_FOOTPRINT_M2: 350,
    HOURS_PER_YEAR: 8760,
    PUE: 1.015,
    COUNTERFACTUAL_PUE: 1.56,
    PV_PEAK_MW: 12.0,
    PV_ANNUAL_MWH: 10956,
    BESS_POWER_MW: 8.0,
    BESS_ENERGY_MWH: 32.0,
    BUFFER_PER_VAULT_M3: 43.4,
    BUFFER_TOTAL_M3: 217.0,
    BUFFER_DT_K: 10.0,
    WATER_MIST_DB_MAX: 110,
    VAULT_ZONE_WEIGHTS: Object.freeze([0.24, 0.22, 0.20, 0.18, 0.16]),
  }),

  THERMAL: Object.freeze({
    PRIMARY_TEMP_C: 75,
    PRIMARY_DT_K: 25,
    PRIMARY_GROSS_SHARE: 0.80,
    PRIMARY_NOMINAL_GROSS_LMIN: 4635.3,
    PRIMARY_NOMINAL_GROSS_LS: 77.26,
    SECONDARY_TEMP_C: 45,
    SECONDARY_DT_K: 15,
    SECONDARY_GROSS_SHARE: 0.20,
    SECONDARY_NOMINAL_GROSS_LMIN: 1923.1,
    SECONDARY_NOMINAL_GROSS_LS: 32.05,
    ANNUAL_DISTRICT_DEMAND_MWH: 37610,
    ANNUAL_DISTRICT_DEMAND_EXACT_MWH: 37612,
    COLD_SNAP_PEAK_MW: 9.52,
    SUMMER_DHW_BASELINE_MW: 1.45,
    BALANCE_TEMP_C: 18.16,
    SPACE_HEAT_SLOPE_MW_PER_K: (9.52 - 1.45) / (18.16 - (-5.0)),
    THERMAL_STORE_M3: 2500,
    THERMAL_STORE_DT_K: 30,
    THERMAL_STORE_MWH: 87.08,
    HTHP_MAX_MW_TH: 8.0,
    HTHP_COP_MIN: 4.6,
    HTHP_COP_MAX: 6.9,
    RING_LENGTH_KM: 30.0,
    RING_LOSS_W_PER_M: 7.0,
    RING_LOSS_MWH_YR: 1840,
    PRIMARY_PUMPING_KW: 22.1,
    TCES_CAPACITY_MWH_TH: 35.0,
    TI_PTES_POWER_MW_E: 2.0,
    TI_PTES_ENERGY_MWH_E: 16.0,
    ORC_MAX_KW_E: 440,
    ORC_CEILING_MWH_YR: 2412,
    ORC_PRACTICAL_MWH_YR: 2000,
    SUMMER_SURPLUS_MWH_TH: 30148,
  }),

  HYDROLOGY: Object.freeze({
    DUBLIN_RAINFALL_MM_YR: 772.5,
    RUNOFF_COEFF: 0.80,
    FILTRATION_EFF: 0.90,
    CATCHMENT_M2: 120000,
    CISTERN_CAPACITY_M3: 10000,
    DAILY_WATER_DEMAND_M3: 160,
    AVOIDED_POTABLE_WATER_M3_YR: 166500,
    AVOIDED_POTABLE_MIN_M3_YR: 158000,
    AVOIDED_POTABLE_MAX_M3_YR: 175000,
    ULTRAPURE_CONDUCTIVITY_MAX_US_CM: 1.0,
    ULTRAPURE_RESISTIVITY_RANGE_MOHM: '1–18.2 MΩ·cm',
  }),

  OPTICAL: Object.freeze({
    RING_KM: 3.0,
    INTER_VAULT_HOP_KM: 1.0,
    INTER_VAULT_RTT_US: 8.16,
    INTER_VAULT_JITTER_US: 1.2,
    VAULT_TO_POD_KM: 0.25,
    VAULT_TO_POD_NET_RTT_US: 3.17,
    LOCAL_E2E_RTT_US: 143.2,
    LOCAL_E2E_JITTER_US: 2.0,
    CLOUD_DISTANCE_KM: 75,
    CLOUD_TRANSPORT_RTT_MS: 7.64,
    CLOUD_CONTROL_LOOP_MS: 90.0,
    POD_SPEED_KMH: 50.0,
    LOCAL_BREAKOUT_PCT: 87.34,
    WAN_POWER_SAVED_MWH_YR: 455,
    SDN_EFFICIENCY_FACTOR: 74.9,
  }),

  FINANCIAL: Object.freeze({
    CAPEX_TOTAL_M: 137.08,
    CAPEX_DISTRICT_M: 57.73,
    CAPEX_DC_OPERATOR_M: 79.35,
    CAPEX_CRU_PV_BESS_M: 15.30,
    COUNTERFACTUAL_CAPEX_M: 215.59,
    CAPEX_SAVINGS_M: 78.51,
    REVENUE_ANNUAL_M: 34.20,
    OPEX_ANNUAL_M: 12.00,
    EBITDA_ANNUAL_M: 22.20,
    REPEX_30YR_M: 25.70,
    SIMPLE_PAYBACK_YR: 6.2,
    IRR_PCT: 15.7,
    NPV_4PCT_M: 232.98,
    DISCOUNTED_PAYBACK_4PCT_YR: 7.2,
    NPV_8PCT_M: 105.07,
    DISCOUNTED_PAYBACK_8PCT_YR: 8.9,
    AVOIDED_HEATING_CO2_TON_YR: 8020,
    AVOIDED_PUE_CO2_TON_YR: 15822,
    AVOIDED_PUE_ENERGY_MWH_YR: 47742,
    TOTAL_AVOIDED_CO2_TON_YR: 23842,
  }),

  SURVEY: Object.freeze({
    sampleSize: 109,
    dcComfortScore: 3.19,
    dcComfortPosNeutralPct: 64.8,
    recycledWaterScore: 3.86,
    recycledWaterPosPct: 72.0,
    priorityCheapestBillPct: 41.8,
    priorityReliabilityPct: 31.6,
    priorityCombinedPct: 73.5,
    topConcernNoiseVibrationPct: 33.7,
  }),
});

export const SCENARIO_LIST = Object.freeze([
  Object.freeze({
    id: 'nominal',
    label: 'Nominal 2045 Design',
    shortLabel: 'Nominal 2045',
    badge: '100% IT · 10°C',
    description: 'Full 10.0 MW IT load at 98% capture, Dublin mean 10°C ambient, 228.3% annual heat coverage.',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: 10.0,
      rainfallMultiplier: 1.0,
      cisternSoc: 0.85,
      thermalStoreSoc: 0.70,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: false,
      timeOfDay: 13.5,
    }),
  }),
  Object.freeze({
    id: 'winter_cold_snap',
    label: 'Winter Cold Snap (−5°C)',
    shortLabel: 'Cold Snap −5°C',
    badge: '100% IT · −5°C',
    description: 'Peak winter heating demand (9.52 MW_th) met directly by 9.80 MW_th captured waste heat (+0.28 MW_th surplus).',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: -5.0,
      rainfallMultiplier: 1.2,
      cisternSoc: 0.90,
      thermalStoreSoc: 0.85,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: false,
      timeOfDay: 17.5,
    }),
  }),
  Object.freeze({
    id: 'winter_stress_60',
    label: 'Cold Snap + 60% Stress',
    shortLabel: '60% Stress −5°C',
    badge: '60% IT · HTHP On',
    description: 'Severe winter −5°C with DC throttled to 60% (5.88 MW_th). 2,500 m³ thermal store + 8.0 MW_th HTHP cover −3.64 MW_th deficit.',
    params: Object.freeze({
      utilization: 0.60,
      heatCaptureEff: 0.98,
      ambientTempC: -5.0,
      rainfallMultiplier: 1.1,
      cisternSoc: 0.88,
      thermalStoreSoc: 0.80,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: true,
      timeOfDay: 19.0,
    }),
  }),
  Object.freeze({
    id: 'vault_fault_sdn',
    label: 'Vault 3 Fault + SDN Reroute',
    shortLabel: 'Vault 3 Fault',
    badge: 'V3 Fault · SDN',
    description: 'Vault 3 offline. Optical NANF HCF ring reroutes V2X traffic and SDN shifts 8.0 MW IT load across healthy vaults (74.9× vs pumping).',
    params: Object.freeze({
      utilization: 0.80,
      heatCaptureEff: 0.98,
      ambientTempC: 4.0,
      rainfallMultiplier: 1.0,
      cisternSoc: 0.82,
      thermalStoreSoc: 0.72,
      sdnEnabled: true,
      vaultFaults: [false, false, true, false, false],
      hthpScenarioB: false,
      timeOfDay: 14.0,
    }),
  }),
  Object.freeze({
    id: 'summer_exergy',
    label: 'Summer Exergy Cascade',
    shortLabel: 'Summer Exergy',
    badge: '24°C · ORC + TCES',
    description: 'Low DHW baseline (1.45 MW_th) leaves 8.35 MW_th surplus routed to SrBr₂ TCES, TI-PTES Carnot battery, ~422 kW_e micro-ORC, and MOF chillers.',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: 24.0,
      rainfallMultiplier: 0.6,
      cisternSoc: 0.75,
      thermalStoreSoc: 0.95,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: false,
      timeOfDay: 13.0,
    }),
  }),
  Object.freeze({
    id: 'dublin_drought',
    label: 'Dublin 60-Day Drought',
    shortLabel: 'Dublin Drought',
    badge: '0× Rain · Cistern',
    description: 'Zero rainfall input; closed-loop microfluidic cooling & 10,000 m³ RC drought cistern sustain operations for 45–66 dry days.',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: 21.0,
      rainfallMultiplier: 0.0,
      cisternSoc: 0.42,
      thermalStoreSoc: 0.85,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: false,
      timeOfDay: 15.0,
    }),
  }),
  Object.freeze({
    id: 'conservative_stress',
    label: 'Conservative (u=0.70, f=0.80)',
    shortLabel: 'Conservative',
    badge: 'u=0.70 · f=0.80',
    description: 'Conservative stress margin: 70% IT load and 80% heat capture yield 49,056 MWh_th/yr (130.4% of 37,610 MWh_th/yr district demand).',
    params: Object.freeze({
      utilization: 0.70,
      heatCaptureEff: 0.80,
      ambientTempC: 10.0,
      rainfallMultiplier: 0.85,
      cisternSoc: 0.78,
      thermalStoreSoc: 0.65,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: true,
      timeOfDay: 11.0,
    }),
  }),
]);

export const SCENARIO_PRESETS = Object.freeze(
  Object.fromEntries(SCENARIO_LIST.map((preset) => [preset.id, preset]))
);

export function createDefaultState() {
  const base = SCENARIO_PRESETS.nominal.params;
  return {
    scenarioId: 'nominal',
    utilization: base.utilization,
    heatCaptureEff: base.heatCaptureEff,
    ambientTempC: base.ambientTempC,
    rainfallMultiplier: base.rainfallMultiplier,
    cisternSoc: base.cisternSoc,
    thermalStoreSoc: base.thermalStoreSoc,
    sdnEnabled: base.sdnEnabled,
    vaultFaults: [...base.vaultFaults],
    hthpScenarioB: base.hthpScenarioB,
    timeOfDay: base.timeOfDay,
  };
}

export const DEFAULT_STATE = Object.freeze(createDefaultState());

export function clamp(val, min, max) {
  return Math.min(max, Math.max(min, val));
}

export function computeDistrictDemandMw(ambientTempC) {
  const T = Number.isFinite(ambientTempC) ? ambientTempC : 10.0;
  const heatingDeltaK = Math.max(0, CONSTANTS.THERMAL.BALANCE_TEMP_C - T);
  return CONSTANTS.THERMAL.SUMMER_DHW_BASELINE_MW + heatingDeltaK * CONSTANTS.THERMAL.SPACE_HEAT_SLOPE_MW_PER_K;
}

export function computeHthpCop(ambientTempC) {
  const T = clamp(Number.isFinite(ambientTempC) ? ambientTempC : 10.0, -5, 25);
  const ratio = (T + 5) / 30;
  return clamp(
    CONSTANTS.THERMAL.HTHP_COP_MIN + ratio * (CONSTANTS.THERMAL.HTHP_COP_MAX - CONSTANTS.THERMAL.HTHP_COP_MIN),
    CONSTANTS.THERMAL.HTHP_COP_MIN,
    CONSTANTS.THERMAL.HTHP_COP_MAX
  );
}

/**
 * Allocates requested IT load across the 5 subterranean 2.0 MW vaults.
 * Uses iterative water-filling weighted by spatial thermal zone demand when SDN is enabled.
 */
export function allocateVaultLoads(requestedItMw, vaultFaults, sdnEnabled, heatCaptureEff = 0.98) {
  const faults = Array.from({ length: CONSTANTS.DC.VAULT_COUNT }, (_, i) => Boolean(vaultFaults?.[i]));
  const activeIndices = [];
  for (let i = 0; i < CONSTANTS.DC.VAULT_COUNT; i++) {
    if (!faults[i]) activeIndices.push(i);
  }

  const maxAvailableMw = activeIndices.length * CONSTANTS.DC.VAULT_CAPACITY_MW;
  const totalItMw = clamp(requestedItMw, 0, maxAvailableMw);
  const loads = new Array(CONSTANTS.DC.VAULT_COUNT).fill(0);

  if (activeIndices.length > 0 && totalItMw > 0) {
    if (!sdnEnabled) {
      const equalShare = totalItMw / activeIndices.length;
      for (const idx of activeIndices) {
        loads[idx] = Math.min(CONSTANTS.DC.VAULT_CAPACITY_MW, equalShare);
      }
    } else {
      let remaining = totalItMw;
      const open = new Set(activeIndices);
      while (open.size > 0 && remaining > 1e-9) {
        let weightSum = 0;
        for (const idx of open) weightSum += CONSTANTS.DC.VAULT_ZONE_WEIGHTS[idx];
        let cappedAny = false;
        const nextRemaining = remaining;
        for (const idx of [...open]) {
          const share = nextRemaining * (CONSTANTS.DC.VAULT_ZONE_WEIGHTS[idx] / weightSum);
          const headroom = CONSTANTS.DC.VAULT_CAPACITY_MW - loads[idx];
          if (share >= headroom - 1e-9) {
            loads[idx] += headroom;
            remaining -= headroom;
            open.delete(idx);
            cappedAny = true;
          }
        }
        if (!cappedAny) {
          for (const idx of open) {
            loads[idx] += remaining * (CONSTANTS.DC.VAULT_ZONE_WEIGHTS[idx] / weightSum);
          }
          remaining = 0;
        }
      }
    }
  }

  const equalIfAllHealthy = requestedItMw / CONSTANTS.DC.VAULT_COUNT;
  const equalIfActive = activeIndices.length > 0 ? totalItMw / activeIndices.length : 0;
  let reroutedFromFaultMw = 0;
  let sdnShiftedMw = 0;
  for (let i = 0; i < CONSTANTS.DC.VAULT_COUNT; i++) {
    if (faults[i]) {
      reroutedFromFaultMw += Math.min(equalIfAllHealthy, Math.max(0, maxAvailableMw - totalItMw + equalIfAllHealthy));
    } else if (sdnEnabled && loads[i] > equalIfActive) {
      sdnShiftedMw += loads[i] - equalIfActive;
    }
  }
  const faultCount = faults.filter(Boolean).length;
  const nominalPerVault = requestedItMw / CONSTANTS.DC.VAULT_COUNT;
  reroutedFromFaultMw = Math.min(totalItMw, faultCount * nominalPerVault);

  const vaults = loads.map((itMw, idx) => {
    const heatMw = itMw * heatCaptureEff;
    const shareOfCap = itMw / CONSTANTS.DC.VAULT_CAPACITY_MW;
    return {
      id: idx,
      name: `Vault ${idx + 1}`,
      inspectKey: `vault_${idx}`,
      faulted: faults[idx],
      itMw,
      heatMw,
      loadPct: shareOfCap * 100,
      zoneWeight: CONSTANTS.DC.VAULT_ZONE_WEIGHTS[idx],
      bufferM3: CONSTANTS.DC.BUFFER_PER_VAULT_M3,
      flowPrimaryLmin: (CONSTANTS.THERMAL.PRIMARY_NOMINAL_GROSS_LMIN / CONSTANTS.DC.VAULT_COUNT) * shareOfCap,
      flowSecondaryLmin: (CONSTANTS.THERMAL.SECONDARY_NOMINAL_GROSS_LMIN / CONSTANTS.DC.VAULT_COUNT) * shareOfCap,
    };
  });

  return {
    vaults,
    totalItMw,
    activeVaultCount: activeIndices.length,
    faultedVaultCount: faultCount,
    reroutedFromFaultMw,
    sdnShiftedMw,
  };
}

/**
 * Pure full-system simulation step from input state.
 */
export function computeSimulation(inputState = {}) {
  const s = {
    ...DEFAULT_STATE,
    ...inputState,
    utilization: clamp(Number(inputState.utilization ?? DEFAULT_STATE.utilization), 0.1, 1.0),
    heatCaptureEff: clamp(Number(inputState.heatCaptureEff ?? DEFAULT_STATE.heatCaptureEff), 0.70, 0.99),
    ambientTempC: clamp(Number(inputState.ambientTempC ?? DEFAULT_STATE.ambientTempC), -10, 35),
    rainfallMultiplier: clamp(Number(inputState.rainfallMultiplier ?? DEFAULT_STATE.rainfallMultiplier), 0.0, 2.5),
    cisternSoc: clamp(Number(inputState.cisternSoc ?? DEFAULT_STATE.cisternSoc), 0.05, 1.0),
    thermalStoreSoc: clamp(Number(inputState.thermalStoreSoc ?? DEFAULT_STATE.thermalStoreSoc), 0.0, 1.0),
    sdnEnabled: inputState.sdnEnabled ?? DEFAULT_STATE.sdnEnabled,
    hthpScenarioB: inputState.hthpScenarioB ?? DEFAULT_STATE.hthpScenarioB,
    timeOfDay: ((Number(inputState.timeOfDay ?? DEFAULT_STATE.timeOfDay) % 24) + 24) % 24,
    vaultFaults: Array.from({ length: 5 }, (_, i) => Boolean(inputState.vaultFaults?.[i])),
  };

  // 1. Vault allocation & SDN
  const requestedItMw = s.utilization * CONSTANTS.DC.TOTAL_IT_CAPACITY_MW;
  const alloc = allocateVaultLoads(requestedItMw, s.vaultFaults, s.sdnEnabled, s.heatCaptureEff);
  const totalItMw = alloc.totalItMw;
  const loadRatio = totalItMw / CONSTANTS.DC.TOTAL_IT_CAPACITY_MW;

  // 2. Electrical & BESS
  const grossFacilityMw = totalItMw * CONSTANTS.DC.PUE;
  const annualGrossMWh = grossFacilityMw * CONSTANTS.DC.HOURS_PER_YEAR;
  const daylightPhase = clamp((s.timeOfDay - 6) / 12, 0, 1);
  const solarElev = s.timeOfDay >= 6 && s.timeOfDay <= 18 ? Math.sin(daylightPhase * Math.PI) : 0;
  const seasonSolarFactor = clamp(0.45 + (s.ambientTempC + 5) / 45, 0.38, 1.0);
  const rainCloudFactor = clamp(1 - s.rainfallMultiplier * 0.18, 0.55, 1.0);
  const pvOutputMw = CONSTANTS.DC.PV_PEAK_MW * solarElev * seasonSolarFactor * rainCloudFactor;
  const annualPvMWh = CONSTANTS.DC.PV_ANNUAL_MWH;
  const annualNetGridMWh = Math.max(0, annualGrossMWh - annualPvMWh);

  let bessChargeMw = 0;
  let bessDischargeMw = 0;
  let bessMode = 'Standby';
  if (pvOutputMw > 6.5) {
    bessChargeMw = Math.min(4.0, (pvOutputMw - 6.0) * 0.8);
    bessMode = 'Charging (Solar)';
  } else if ((s.timeOfDay >= 17 && s.timeOfDay <= 21) || s.ambientTempC <= -2 || alloc.faultedVaultCount > 0) {
    bessDischargeMw = Math.min(CONSTANTS.DC.BESS_POWER_MW, grossFacilityMw * 0.35 + (s.ambientTempC <= -2 ? 1.5 : 0));
    bessMode = 'Discharging (Peak Shave)';
  }

  // 3. Thermal & Hydraulic Flows
  const capturedHeatMw = totalItMw * s.heatCaptureEff;
  const annualCapturedHeatMWh = capturedHeatMw * CONSTANTS.DC.HOURS_PER_YEAR;
  const annualCoveragePct = (annualCapturedHeatMWh / CONSTANTS.THERMAL.ANNUAL_DISTRICT_DEMAND_MWH) * 100;

  const primaryGrossFlowLmin = CONSTANTS.THERMAL.PRIMARY_NOMINAL_GROSS_LMIN * loadRatio;
  const primaryGrossFlowLs = primaryGrossFlowLmin / 60;
  const primaryNetHeatMw = capturedHeatMw * CONSTANTS.THERMAL.PRIMARY_GROSS_SHARE;
  const primaryNetFlowLmin = primaryGrossFlowLmin * s.heatCaptureEff;

  const secondaryGrossFlowLmin = CONSTANTS.THERMAL.SECONDARY_NOMINAL_GROSS_LMIN * loadRatio;
  const secondaryGrossFlowLs = secondaryGrossFlowLmin / 60;
  const secondaryNetHeatMw = capturedHeatMw * CONSTANTS.THERMAL.SECONDARY_GROSS_SHARE;
  const secondaryNetFlowLmin = secondaryGrossFlowLmin * s.heatCaptureEff;

  const districtDemandMw = computeDistrictDemandMw(s.ambientTempC);
  const annualisedDemandMWh = districtDemandMw * CONSTANTS.DC.HOURS_PER_YEAR;
  const netThermalBalanceMw = capturedHeatMw - districtDemandMw;
  const ringPumpingKw = CONSTANTS.THERMAL.PRIMARY_PUMPING_KW * loadRatio * (s.sdnEnabled ? 1.0 : 1.45);

  // 4. Surplus Exergy Cascade vs Cold-Snap Deficit Coverage
  let surplusMw = 0;
  let deficitMw = 0;
  let thermalStoreChargeMw = 0;
  let thermalStoreDischargeMw = 0;
  let tcesChargeMw = 0;
  let tiPtesChargeMw = 0;
  let tiPtesElectricEqMw = 0;
  let orcThermalInputMw = 0;
  let orcOutputKw = 0;
  let mofChillerMw = 0;
  let mofCoolingMw = 0;
  let dryCoolerRejectionMw = 0;
  let hthpThermalOutputMw = 0;
  let hthpElectricalMw = 0;
  let unmetThermalDeficitMw = 0;
  const hthpCop = computeHthpCop(s.ambientTempC);

  if (netThermalBalanceMw >= 0) {
    surplusMw = netThermalBalanceMw;
    thermalStoreChargeMw = s.thermalStoreSoc < 0.95 ? Math.min(surplusMw, 1.0) : 0;
    const rem1 = surplusMw - thermalStoreChargeMw;
    tcesChargeMw = Math.min(rem1, 1.20);
    const rem2 = rem1 - tcesChargeMw;
    tiPtesChargeMw = Math.min(rem2, 2.00);
    tiPtesElectricEqMw = Math.min(CONSTANTS.THERMAL.TI_PTES_POWER_MW_E, tiPtesChargeMw * 0.70);
    const rem3 = rem2 - tiPtesChargeMw;
    orcThermalInputMw = Math.min(rem3 * 0.78, 4.19);
    orcOutputKw = Math.min(CONSTANTS.THERMAL.ORC_MAX_KW_E, orcThermalInputMw * 1000 * 0.105);
    const rem4 = rem3 - orcThermalInputMw;
    mofChillerMw = s.ambientTempC > 18 ? Math.min(rem4, 0.85) : 0;
    mofCoolingMw = mofChillerMw * 0.72;
    dryCoolerRejectionMw = Math.max(0, rem4 - mofChillerMw);
  } else {
    deficitMw = -netThermalBalanceMw;
    thermalStoreDischargeMw = s.thermalStoreSoc > 0.05 ? Math.min(deficitMw * 0.45, 4.0) : 0;
    hthpThermalOutputMw = Math.min(CONSTANTS.THERMAL.HTHP_MAX_MW_TH, Math.max(0, deficitMw - thermalStoreDischargeMw));
    hthpElectricalMw = hthpThermalOutputMw / hthpCop;
    unmetThermalDeficitMw = Math.max(0, deficitMw - thermalStoreDischargeMw - hthpThermalOutputMw);
  }

  const netGridDrawMw = Math.max(
    0,
    grossFacilityMw + hthpElectricalMw + ringPumpingKw / 1000 - pvOutputMw - orcOutputKw / 1000 - bessDischargeMw + bessChargeMw
  );

  // 5. Hydrological Loop
  const annualHarvestPer100kM3 =
    CONSTANTS.HYDROLOGY.DUBLIN_RAINFALL_MM_YR *
    s.rainfallMultiplier *
    0.001 *
    100000 *
    CONSTANTS.HYDROLOGY.RUNOFF_COEFF *
    CONSTANTS.HYDROLOGY.FILTRATION_EFF;
  const annualRainHarvestM3 =
    CONSTANTS.HYDROLOGY.DUBLIN_RAINFALL_MM_YR *
    s.rainfallMultiplier *
    0.001 *
    CONSTANTS.HYDROLOGY.CATCHMENT_M2 *
    CONSTANTS.HYDROLOGY.RUNOFF_COEFF *
    CONSTANTS.HYDROLOGY.FILTRATION_EFF;
  const dailyHarvestM3 = annualRainHarvestM3 / 365;
  const cisternVolumeM3 = CONSTANTS.HYDROLOGY.CISTERN_CAPACITY_M3 * s.cisternSoc;
  const droughtAutonomyDays = cisternVolumeM3 / CONSTANTS.HYDROLOGY.DAILY_WATER_DEMAND_M3;
  const fullTankAutonomyDays = CONSTANTS.HYDROLOGY.CISTERN_CAPACITY_M3 / CONSTANTS.HYDROLOGY.DAILY_WATER_DEMAND_M3;

  // 6. Optical Ring & Autonomous Pod Safety
  const activeLocalE2eRttUs =
    alloc.faultedVaultCount > 0
      ? CONSTANTS.OPTICAL.LOCAL_E2E_RTT_US + CONSTANTS.OPTICAL.INTER_VAULT_RTT_US * alloc.faultedVaultCount
      : CONSTANTS.OPTICAL.LOCAL_E2E_RTT_US;
  const podSpeedMs = CONSTANTS.OPTICAL.POD_SPEED_KMH / 3.6;
  const localBlindTravelMm = podSpeedMs * (activeLocalE2eRttUs * 1e-6) * 1000;
  const cloudBlindTravelM = podSpeedMs * (CONSTANTS.OPTICAL.CLOUD_CONTROL_LOOP_MS / 1000);
  const wanPowerSavedMWhYr = CONSTANTS.OPTICAL.WAN_POWER_SAVED_MWH_YR * loadRatio;

  // 7. Carbon & Financials
  const avoidedHeatingCo2TonYr =
    CONSTANTS.FINANCIAL.AVOIDED_HEATING_CO2_TON_YR * Math.min(1, annualCapturedHeatMWh / CONSTANTS.THERMAL.ANNUAL_DISTRICT_DEMAND_MWH);
  const avoidedPueCo2TonYr = CONSTANTS.FINANCIAL.AVOIDED_PUE_CO2_TON_YR * loadRatio;
  const totalAvoidedCo2TonYr = avoidedHeatingCo2TonYr + avoidedPueCo2TonYr;

  return {
    inputs: s,
    vaults: alloc.vaults,
    electrical: {
      totalItMw,
      requestedItMw,
      activeVaultCount: alloc.activeVaultCount,
      faultedVaultCount: alloc.faultedVaultCount,
      pue: CONSTANTS.DC.PUE,
      grossFacilityMw,
      annualGrossMWh,
      pvOutputMw,
      annualPvMWh,
      annualNetGridMWh,
      bessChargeMw,
      bessDischargeMw,
      bessMode,
      netGridDrawMw,
    },
    thermal: {
      heatCaptureEff: s.heatCaptureEff,
      capturedHeatMw,
      annualCapturedHeatMWh,
      annualCoveragePct,
      primaryGrossFlowLmin,
      primaryGrossFlowLs,
      primaryNetHeatMw,
      primaryNetFlowLmin,
      secondaryGrossFlowLmin,
      secondaryGrossFlowLs,
      secondaryNetHeatMw,
      secondaryNetFlowLmin,
      districtDemandMw,
      annualisedDemandMWh,
      netThermalBalanceMw,
      surplusMw,
      deficitMw,
      thermalStoreSoc: s.thermalStoreSoc,
      thermalStoreEnergyMWh: CONSTANTS.THERMAL.THERMAL_STORE_MWH * s.thermalStoreSoc,
      thermalStoreChargeMw,
      thermalStoreDischargeMw,
      tcesChargeMw,
      tiPtesChargeMw,
      tiPtesElectricEqMw,
      orcThermalInputMw,
      orcOutputKw,
      mofChillerMw,
      mofCoolingMw,
      dryCoolerRejectionMw,
      hthpThermalOutputMw,
      hthpElectricalMw,
      hthpCop,
      scenarioBActive: Boolean(s.hthpScenarioB),
      unmetThermalDeficitMw,
      ringPumpingKw,
    },
    hydrology: {
      rainfallMultiplier: s.rainfallMultiplier,
      effectiveRainfallMmYr: CONSTANTS.HYDROLOGY.DUBLIN_RAINFALL_MM_YR * s.rainfallMultiplier,
      annualHarvestPer100kM3,
      annualRainHarvestM3,
      dailyHarvestM3,
      cisternSoc: s.cisternSoc,
      cisternVolumeM3,
      cisternCapacityM3: CONSTANTS.HYDROLOGY.CISTERN_CAPACITY_M3,
      droughtAutonomyDays,
      fullTankAutonomyDays,
      avoidedPotableWaterM3Yr: CONSTANTS.HYDROLOGY.AVOIDED_POTABLE_WATER_M3_YR,
    },
    optical: {
      sdnEnabled: s.sdnEnabled,
      sdnEfficiencyFactor: CONSTANTS.OPTICAL.SDN_EFFICIENCY_FACTOR,
      sdnShiftedMw: alloc.sdnShiftedMw,
      reroutedFromFaultMw: alloc.reroutedFromFaultMw,
      interVaultRttUs: CONSTANTS.OPTICAL.INTER_VAULT_RTT_US,
      activeLocalE2eRttUs,
      localBlindTravelMm,
      cloudTransportRttMs: CONSTANTS.OPTICAL.CLOUD_TRANSPORT_RTT_MS,
      cloudControlLoopMs: CONSTANTS.OPTICAL.CLOUD_CONTROL_LOOP_MS,
      cloudBlindTravelM,
      localBreakoutPct: CONSTANTS.OPTICAL.LOCAL_BREAKOUT_PCT,
      wanPowerSavedMWhYr,
    },
    financial: {
      ...CONSTANTS.FINANCIAL,
      avoidedHeatingCo2TonYr,
      avoidedPueCo2TonYr,
      totalAvoidedCo2TonYr,
    },
    survey: CONSTANTS.SURVEY,
  };
}

function makeVaultInspector(idx) {
  return {
    title: `Subterranean AI Vault ${idx + 1} (2.0 MW)`,
    category: 'Level −4 Bedrock · Data Centre',
    description:
      'Reinforced-concrete vault at Level −4 bedrock beneath public plazas. Isolated by floating acoustic slabs, 1.0 m perimeter vibration trenches, and <110 dB low-noise water-mist suppression.',
    specs: [
      '2.0 MW IT capacity · 800 VDC SSTs + HTS busways (−98% Cu)',
      '43.4 m³ 15-min thermal buffer tank (ΔT = 10 K)',
      'In-silicon microfluidic cooling: 75°C primary / 45°C secondary',
      '1.00 km NANF Hollow-Core Fiber ring hop (8.16 μs RTT)',
    ],
    getLiveMetrics: (sim) => {
      const v = sim.vaults[idx];
      return [
        ['Status', v.faulted ? 'FAULTED (OFFLINE)' : 'ONLINE (NOMINAL)'],
        ['IT Load', `${v.itMw.toFixed(2)} / 2.00 MW (${v.loadPct.toFixed(0)}%)`],
        ['Captured Heat', `${v.heatMw.toFixed(2)} MW_th`],
        ['75°C Primary Flow', `${v.flowPrimaryLmin.toFixed(1)} L/min`],
        ['Zone Thermal Weight', `${(v.zoneWeight * 100).toFixed(0)}%`],
      ];
    },
  };
}

export const SUBSYSTEM_INFO = Object.freeze({
  vault_0: makeVaultInspector(0),
  vault_1: makeVaultInspector(1),
  vault_2: makeVaultInspector(2),
  vault_3: makeVaultInspector(3),
  vault_4: makeVaultInspector(4),

  thermal_store: {
    title: '2,500 m³ Diurnal / Cold-Snap Thermal Store',
    category: 'Loop 2 · Thermal Buffering',
    description:
      'Insulated stratified water tank (ΔT = 30 K, ~87.1 MWh_th) buffering diurnal demand swings and winter cold-snap deficits alongside the 8.0 MW_th HTHP bank.',
    specs: [
      'Volume: 2,500 m³ insulated steel/RC tank',
      'Storage capacity: ~87.08 MWh_th across ΔT = 30 K',
      'Discharges up to 4.0 MW_th during cold-snap deficits',
    ],
    getLiveMetrics: (sim) => [
      ['State of Charge', `${(sim.thermal.thermalStoreSoc * 100).toFixed(0)}% (${sim.thermal.thermalStoreEnergyMWh.toFixed(1)} MWh_th)`],
      ['Charge Rate', `${sim.thermal.thermalStoreChargeMw.toFixed(2)} MW_th`],
      ['Discharge Rate', `${sim.thermal.thermalStoreDischargeMw.toFixed(2)} MW_th`],
    ],
  },

  hthp_bank: {
    title: '8.0 MW_th Natural-Refrigerant Backup HTHP Bank',
    category: 'Loop 2 · Peaking & Scenario B',
    description:
      'High-temperature heat pumps (Scenario B fallback: upgrades 45°C secondary water to 70°C supply at COP 4.6–6.9) covering cold-snap stress deficits.',
    specs: [
      'Rated thermal output: 8.0 MW_th (natural refrigerant)',
      'Operating COP band: 4.6 – 6.9',
      'Covers −3.64 MW_th deficit at −5°C & 60% DC load',
    ],
    getLiveMetrics: (sim) => [
      ['Thermal Output', `${sim.thermal.hthpThermalOutputMw.toFixed(2)} MW_th`],
      ['Electrical Draw', `${sim.thermal.hthpElectricalMw.toFixed(2)} MW_e`],
      ['Operating COP', `${sim.thermal.hthpCop.toFixed(2)}`],
      ['Scenario B Fallback', sim.thermal.scenarioBActive ? 'Armed (45°C → 70°C)' : 'Standby'],
    ],
  },

  tces: {
    title: 'SrBr₂ / K₂CO₃ Thermochemical Energy Storage (TCES)',
    category: 'Exergy-Priority Cascade',
    description:
      'Lossless salt-hydrate thermochemical bed storing summer surplus heat (~35 MWh_th) at high energy density without seasonal standby losses.',
    specs: [
      'Chemistry: SrBr₂ / K₂CO₃ reversible hydration',
      'Capacity: ~35 MWh_th',
      'Priority tier #2 in summer surplus exergy cascade',
    ],
    getLiveMetrics: (sim) => [
      ['Active Charging', `${sim.thermal.tcesChargeMw.toFixed(2)} MW_th`],
      ['Nominal Capacity', '35.0 MWh_th'],
      ['Total Surplus Available', `${sim.thermal.surplusMw.toFixed(2)} MW_th`],
    ],
  },

  ti_ptes: {
    title: '2 MW_e / 16 MWh_e TI-PTES Carnot Battery',
    category: 'Exergy-Priority Cascade · Grid Arbitrage',
    description:
      'Thermally-Integrated Pumped Thermal Energy Storage coupling waste heat with wind/grid electricity arbitrage for 8-hour dispatch.',
    specs: [
      'Power / Energy: 2.0 MW_e / 16.0 MWh_e',
      'Couples 75°C DC waste heat into Carnot compression cycle',
    ],
    getLiveMetrics: (sim) => [
      ['Thermal Input', `${sim.thermal.tiPtesChargeMw.toFixed(2)} MW_th`],
      ['Electric Storage Eq.', `${sim.thermal.tiPtesElectricEqMw.toFixed(2)} MW_e`],
      ['Rated Capacity', '2.0 MW_e / 16.0 MWh_e'],
    ],
  },

  micro_orc: {
    title: 'Summer Residual Micro-ORC Skid (≤ 440 kW_e)',
    category: 'Exergy-Priority Cascade · Power Recovery',
    description:
      'Organic Rankine Cycle turbine converting summer residual 75°C thermal surplus into clean on-site electricity (~420 kW_e peak, ≤ 2,412 MWh_e/yr ceiling).',
    specs: [
      'Nominal summer output: ~418–440 kW_e (hard cap 440 kW_e)',
      'Annual ceiling: 2,412 MWh_e/yr (~2,000 MWh_e/yr practical)',
    ],
    getLiveMetrics: (sim) => [
      ['Thermal Input', `${sim.thermal.orcThermalInputMw.toFixed(2)} MW_th`],
      ['Electrical Generation', `${sim.thermal.orcOutputKw.toFixed(1)} kW_e`],
      ['Hard Ceiling', '440.0 kW_e'],
    ],
  },

  mof_chiller: {
    title: 'MOF Adsorption Chillers',
    category: 'Exergy-Priority Cascade · Cooling',
    description:
      'Metal-Organic Framework adsorption chillers thermally driven by residual 75°C waste heat during warm summer afternoons (>18°C).',
    specs: [
      'Thermally driven cooling from 75°C surplus stream',
      'Zero vapour-compression HFC refrigerants',
    ],
    getLiveMetrics: (sim) => [
      ['Heat Driven Input', `${sim.thermal.mofChillerMw.toFixed(2)} MW_th`],
      ['Chilled Output', `${sim.thermal.mofCoolingMw.toFixed(2)} MW_th`],
    ],
  },

  dry_coolers: {
    title: 'N+1 Low-Noise Dry Coolers',
    category: 'Heat Rejection Backup',
    description:
      'Zero-water adiabatic/dry heat rejection bank handling residual surplus after all exergy cascade stages are saturated—eliminating cooling-tower water loss.',
    specs: [
      'Zero evaporative water consumption (saves 166,500 m³/yr)',
      'N+1 redundancy with acoustic shrouding',
    ],
    getLiveMetrics: (sim) => [
      ['Heat Rejected', `${sim.thermal.dryCoolerRejectionMw.toFixed(2)} MW_th`],
      ['Avoided Potable Water', `${sim.hydrology.avoidedPotableWaterM3Yr.toLocaleString()} m³/yr`],
    ],
  },

  rainwater_cistern: {
    title: '10,000 m³ Subterranean RC Drought Cistern',
    category: 'Loop 3 · Hydrological Resilience',
    description:
      'Reinforced-concrete cistern capturing runoff from 120,000 m² of inert district roofs (Dublin 772.5 mm/yr, C_r=0.80, η_f=0.90), sized for 45–66 day dry spells.',
    specs: [
      'Capacity: 10,000 m³ (62.5 days autonomy at 160 m³/day)',
      'Catchment: 120,000 m² inert roofs (55,620 m³/yr per 100k m²)',
      'Resident survey recycled-water support: 3.86/5.00 (72.0% pos)',
    ],
    getLiveMetrics: (sim) => [
      ['Cistern Volume', `${sim.hydrology.cisternVolumeM3.toFixed(0)} / 10,000 m³ (${(sim.hydrology.cisternSoc * 100).toFixed(0)}%)`],
      ['Drought Autonomy', `${sim.hydrology.droughtAutonomyDays.toFixed(1)} days (Full: ${sim.hydrology.fullTankAutonomyDays.toFixed(1)} d)`],
      ['Annual Rain Harvest', `${sim.hydrology.annualRainHarvestM3.toFixed(0)} m³/yr`],
    ],
  },

  ultrapure_train: {
    title: 'Semiconductor Grade C Ultrapure Water Train',
    category: 'Loop 3 · Microfluidic Polish',
    description:
      'Closed-loop UF + MCDI/2D + DI + UV-C LED + BDD/Ti₄O₇ PFAS mineralisation train supplying ultra-pure coolant to in-silicon microchannels.',
    specs: [
      'Conductivity ≤ 1.0 μS/cm · Resistivity 1–18.2 MΩ·cm',
      'BDD/Ti₄O₇ electrochemical PFAS destruction + UV-C LED',
    ],
    getLiveMetrics: (sim) => [
      ['Primary 75°C Loop Flow', `${sim.thermal.primaryGrossFlowLmin.toFixed(1)} L/min`],
      ['Secondary 45°C Flow', `${sim.thermal.secondaryGrossFlowLmin.toFixed(1)} L/min`],
      ['Target Quality', '≤ 1.0 μS/cm · 1–18.2 MΩ·cm'],
    ],
  },

  rooftop_pv: {
    title: '12 MW_p Rooftop Solar PV Array (CRU Asset)',
    category: 'Behind-the-Meter Power',
    description:
      'District-wide rooftop PV fleet generating 10,956 MWh_e/yr behind the meter to reduce grid draw from 88,914 to 77,958 MWh_e/yr.',
    specs: [
      'Installed capacity: 12.0 MW_p · 10,956 MWh_e/yr',
      'Part of €15.30M CRU/EirGrid LEU compliance package',
    ],
    getLiveMetrics: (sim) => [
      ['Live PV Generation', `${sim.electrical.pvOutputMw.toFixed(2)} MW_e`],
      ['Annual PV Yield', `${sim.electrical.annualPvMWh.toLocaleString()} MWh_e/yr`],
      ['Net Grid Import', `${sim.electrical.netGridDrawMw.toFixed(2)} MW_e`],
    ],
  },

  bess_yard: {
    title: '8 MW / 32 MWh LFP Battery Energy Storage (BESS)',
    category: 'CRU / EirGrid Grid Support',
    description:
      '4-hour lithium iron phosphate battery yard delivering peak shaving, solar time-shifting, and CRU Large Energy User grid firming.',
    specs: [
      'Power / Energy: 8.0 MW / 32.0 MWh (4-hour duration)',
      'Instantaneous vault backup + EirGrid DS3 frequency response',
    ],
    getLiveMetrics: (sim) => [
      ['Dispatch Mode', sim.electrical.bessMode],
      ['Charge / Discharge', `+${sim.electrical.bessChargeMw.toFixed(2)} / −${sim.electrical.bessDischargeMw.toFixed(2)} MW_e`],
      ['Facility Gross Load', `${sim.electrical.grossFacilityMw.toFixed(2)} MW_e`],
    ],
  },

  autonomous_pods: {
    title: 'Autonomous 6G V2X Transit Pods & HCF Optical Link',
    category: 'Optical Ring · Mobility Safety',
    description:
      'Zero-emission autonomous pods steered via 0.25 km NANF Hollow-Core Fiber + Co-Packaged Optics to the nearest healthy subterranean vault.',
    specs: [
      'Local HCF+CPO E2E RTT: 143.2 μs (±2.0 μs) = 1.99 mm @ 50 km/h',
      'vs Regional Cloud (75 km SMF): 7.64 ms RTT / 90 ms loop = 1.25 m blind',
      '87.34% local traffic breakout (saves ~455 MWh_e/yr WAN power)',
    ],
    getLiveMetrics: (sim) => [
      ['Active V2X E2E RTT', `${sim.optical.activeLocalE2eRttUs.toFixed(1)} μs`],
      ['Blind Travel @ 50 km/h', `${sim.optical.localBlindTravelMm.toFixed(2)} mm (vs 1.25 m cloud)`],
      ['WAN Energy Saved', `${sim.optical.wanPowerSavedMWhYr.toFixed(0)} MWh_e/yr`],
    ],
  },

  civic_plaza: {
    title: 'Public Civic Plaza (Over Vault Footprint)',
    category: 'Acoustic & Social Mitigation',
    description:
      'Car-free public square sited directly above a Level −4 vault (~350 m² footprint), addressing the #1 resident survey concern (noise/vibration, 33.7%).',
    specs: [
      'Zero vaults under residential foundations',
      'Survey (N=109): 3.19/5.00 DC comfort (64.8% pos/neutral)',
    ],
    getLiveMetrics: (sim) => [
      ['Active Vaults Below', `${sim.electrical.activeVaultCount} / 5 online`],
      ['Max Fire-Mist Noise', '< 110 dB (subterranean isolated)'],
      ['Survey Top Concern Addressed', 'Noise/Vibration (33.7%)'],
    ],
  },

  civic_hub: {
    title: 'Schools, Health Centre & Community Hub (15.0 ha)',
    category: '2045 Circular District Anchor',
    description:
      '4 primary schools, 2 secondary schools, primary health centre, and sports hub heated at near-ambient 5GDHC efficiency.',
    specs: [
      '15.0 ha civic/education + 14.0 ha retail/offices + 25.0 ha parks',
      '30 km twin-pipe HDPE 5GDHC ring (7.0 W/m loss = 4.9%)',
    ],
    getLiveMetrics: (sim) => [
      ['District Thermal Demand', `${sim.thermal.districtDemandMw.toFixed(2)} MW_th`],
      ['5GDHC Pumping Power', `${sim.thermal.ringPumpingKw.toFixed(1)} kW_e`],
      ['Heat Coverage Ratio', `${sim.thermal.annualCoveragePct.toFixed(1)}%`],
    ],
  },

  residential_district: {
    title: '7,500-Home Circular Residential District (20,000 Residents)',
    category: '180.0 ha Masterplan · 90.0 ha Residential',
    description:
      'Mixed-density 2045 urban district (3,095 buildings, 83.3 dph) heated by recovered data-centre waste heat via the 5GDHC network.',
    specs: [
      '3,300 mid-rise (55 blks) · 1,200 low-rise (40 blks)',
      '2,100 townhouses · 900 detached houses',
      'Annual demand: 37,610 MWh_th/yr · Displaced CO₂: ~8,020 tCO₂/yr',
    ],
    getLiveMetrics: (sim) => [
      ['Ambient Temp', `${sim.inputs.ambientTempC.toFixed(1)} °C`],
      ['Total Heat Demand', `${sim.thermal.districtDemandMw.toFixed(2)} MW_th`],
      ['Annualised Demand', `${sim.thermal.annualisedDemandMWh.toFixed(0)} MWh_th/yr`],
      ['Avoided Heating CO₂', `${sim.financial.avoidedHeatingCo2TonYr.toFixed(0)} tCO₂/yr`],
    ],
  },
});
