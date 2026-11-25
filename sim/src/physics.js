/**
 * RAKDA Ltd. (Group A-10) — 2045 Circular Urban Digital Twin
 * Pure deterministic physics, thermal, hydrological, optical, and financial engine.
 * Grounded strictly in SPEC.md (RAKDA-USCI.docx / calc.py, RAKDA USCI, Nov 2026).
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
    VAULT_LIFE_YR: 100,
    HOURS_PER_YEAR: 8760,
    PUE: 1.015,
    COUNTERFACTUAL_PUE: 1.56,
    PV_PEAK_MW: 12.0,
    PV_ANNUAL_MWH: 10956,
    BESS_POWER_MW: 8.0,
    BESS_ENERGY_MWH: 32.0,
    BUFFER_PER_VAULT_M3: 43.0,
    BUFFER_TOTAL_M3: 215.0,
    BUFFER_DT_K: 10.0,
    WATER_MIST_DB_MAX: 110,
    VAULT_ZONE_WEIGHTS: Object.freeze([0.24, 0.22, 0.20, 0.18, 0.16]),
  }),

  THERMAL: Object.freeze({
    PRIMARY_TEMP_C: 75,
    PRIMARY_DT_K: 25,
    PRIMARY_GROSS_SHARE: 0.80,
    PRIMARY_NOMINAL_GROSS_LS: 76.5,
    PRIMARY_NOMINAL_GROSS_LMIN: 76.5 * 60,
    SECONDARY_TEMP_C: 45,
    SECONDARY_DT_K: 15,
    SECONDARY_GROSS_SHARE: 0.20,
    SECONDARY_NOMINAL_GROSS_LS: 32.0,
    SECONDARY_NOMINAL_GROSS_LMIN: 32.0 * 60,
    ANNUAL_DISTRICT_DEMAND_MWH: 37612,
    ANNUAL_DHW_MWH: 26362,
    ANNUAL_SH_MWH: 11250,
    DESIGN_DAY_TEMP_C: -3.0,
    COLD_SNAP_PEAK_MW: 9.52,
    SUMMER_DHW_BASELINE_MW: 3.01,
    MEAN_TEMP_C: 10.0,
    MEAN_DEMAND_MW: 37612 / 8760,
    SPACE_HEAT_SLOPE_MW_PER_K: (9.52 - 37612 / 8760) / (10.0 - (-3.0)),
    BALANCE_TEMP_C: 10.0 + (37612 / 8760 - 3.01) / ((9.52 - 37612 / 8760) / (10.0 - (-3.0))),
    THERMAL_STORE_M3: 2500,
    THERMAL_STORE_DT_K: 35,
    THERMAL_STORE_MWH: 101.6,
    THERMAL_STORE_USABLE_MWH: 91.4,
    HTHP_MAX_MW_TH: 8.0,
    HTHP_COP_MIN: 4.0,
    HTHP_COP_MAX: 6.0,
    SCENARIO_B_EXTRA_HTHP_MW: 8.5,
    SCENARIO_B_LCOH_ADDER_EUR_MWH: 33,
    RING_LENGTH_KM: 30.0,
    RING_SUPPLY_C: 70,
    RING_RETURN_C: 35,
    RING_LOSS_W_PER_M: 13.0,
    RING_LOSS_MWH_YR: 3421,
    RING_LOSS_MW: 0.39,
    RING_LOSS_PCT: 9.1,
    PRIMARY_PUMPING_KW: 21.9,
    PUMPING_ANNUAL_MWH_YR: 259,
    ORC_NOMINAL_MW_E: 0.4,
    ORC_MAX_KW_E: 420,
    ORC_CARNOT_PCT: 17.2,
    ORC_EFF_MIN: 0.072,
    ORC_EFF_MAX: 0.083,
    ORC_ANNUAL_MIN_GWH: 2.60,
    ORC_ANNUAL_MAX_GWH: 2.97,
    SUMMER_SURPLUS_MWH_TH: 44816,
    SUMMER_SURPLUS_75C_MWH_TH: 35852,
  }),

  HYDROLOGY: Object.freeze({
    DUBLIN_RAINFALL_MM_YR: 772.5,
    RUNOFF_COEFF: 0.80,
    FILTRATION_EFF: 0.90,
    CATCHMENT_M2: 100000,
    CISTERN_CAPACITY_M3: 10000,
    CISTERN_MIN_STORAGE_M3: 6700,
    DAILY_WATER_DEMAND_M3: 152.0,
    DROUGHT_RESERVE_DAYS: 44,
    UPW_MAKEUP_MAX_M3_YR: 3265,
    AVOIDED_POTABLE_MIN_M3_YR: 157680,
    AVOIDED_POTABLE_MAX_M3_YR: 175200,
    AVOIDED_POTABLE_WATER_M3_YR: 166440,
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
    LOCAL_E2E_JITTER_US: 2.5,
    FIXED_FIBRE_E2E_RTT_US: 48.2,
    CLOUD_DISTANCE_KM: 75,
    CLOUD_TRANSPORT_RTT_MS: 7.64,
    CLOUD_CONTROL_LOOP_MS: 90.0,
    POD_SPEED_KMH: 50.0,
    LOCAL_BREAKOUT_PCT: 87.34,
    WAN_POWER_SAVED_MWH_YR: 455,
    SDN_OPTICAL_W: 250,
    SDN_HYDRONIC_W: 17530,
    SDN_EFFICIENCY_FACTOR: 70.1,
  }),

  FINANCIAL: Object.freeze({
    CAPEX_TOTAL_M: 136.66,
    CAPEX_DISTRICT_M: 57.31,
    CAPEX_DC_OPERATOR_M: 79.35,
    CAPEX_CRU_PV_BESS_M: 15.30,
    COLOCATION_RENT_EUR_KW_MO: 145,
    COLOCATION_REVENUE_M: 17.40,
    EBITDA_ANNUAL_M: 19.60,
    EBITDA_UTILITY_M: 3.64,
    EBITDA_DC_M: 15.97,
    SIMPLE_PAYBACK_YR: 7.0,
    IRR_PCT: 13.7,
    NPV_4PCT_M: 189.0,
    NPV_8PCT_M: 77.0,
    UTILITY_IRR_PCT: 4.2,
    UTILITY_PAYBACK_YR: 15.8,
    UTILITY_NPV_4PCT_M: 1.1,
    UTILITY_NPV_8PCT_M: -18.7,
    UTILITY_LCOH_NET_EUR_MWH: 93,
    UTILITY_LCOH_GRANT30_EUR_MWH: 68,
    UTILITY_LCOH_SCENARIO_B_EUR_MWH: 126,
    STRESS_IRR_PCT: 4.3,
    STRESS_LCOH_EUR_MWH: 129,
    TCO_USCI_M: 520,
    TCO_GAS_M: 826,
    TCO_ASHP_M: 936,
    BILL_USCI_EUR_YR: 536,
    BILL_GAS_EUR_YR: 1001,
    BILL_ASHP_EUR_YR: 1691,
    EMBODIED_CEMI_TON: 4628,
    EMBODIED_GGBS_TON: 4178,
    EMBODIED_LC3_TON: 3908,
    AVOIDED_HEATING_CO2_MIN_TON_YR: 7230,
    AVOIDED_HEATING_CO2_MAX_TON_YR: 7270,
    AVOIDED_HEATING_CO2_TON_YR: 7250,
    AVOIDED_PUE_CO2_MIN_TON_YR: 7780,
    AVOIDED_PUE_CO2_MAX_TON_YR: 14700,
    AVOIDED_PUE_CO2_TON_YR: 11240,
    AVOIDED_PUE_ENERGY_MWH_YR: 47742,
    RESIDUAL_GRID_CO2_MIN_TON_YR: 12707,
    RESIDUAL_GRID_CO2_MAX_TON_YR: 24011,
    TOTAL_AVOIDED_CO2_TON_YR: 18490,
  }),

  SURVEY: Object.freeze({
    sampleSize: 105,
    rawSampleSize: 109,
    dcComfortScore: 3.20,
    dcComfortPosPct: 47.1,
    dcComfortNeutPct: 17.3,
    dcComfortLowPct: 35.6,
    recycledWaterScore: 3.87,
    recycledWaterPosPct: 72.8,
    priorityCheapestBillPct: 42.6,
    priorityReliabilityPct: 33.0,
    priorityCombinedPct: 75.6,
    topConcernNoiseVibrationPct: 35.1,
    concernFireSafetyPct: 26.6,
    concernWaterContamPct: 26.6,
  }),
});

export const SCENARIO_LIST = Object.freeze([
  Object.freeze({
    id: 'nominal',
    label: 'Nominal 2045 Design',
    shortLabel: 'Nominal 2045',
    badge: '100% IT · 10°C',
    description: 'Full 10.0 MW IT load at 98% capture, Dublin mean 10°C ambient: 228% gross / 209% net coverage across 70/35°C EN 253 network (u_BE = 47.8%).',
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
    label: 'Winter Design Day (−3°C)',
    shortLabel: 'Design Day −3°C',
    badge: '100% IT · −3°C',
    description: 'Peak −3°C design-day demand (9.52 MW_th + 0.39 MW_th EN 253 loss = 9.91 MW_th) vs 9.80 MW_th captured (−0.11 MW_th gap covered by 2,500 m³ store).',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: -3.0,
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
    label: 'Cautious Winter (−3°C)',
    shortLabel: 'Cautious −3°C',
    badge: 'u=0.70 · HTHP On',
    description: 'Cautious winter −3°C (u=0.70, f_cap=0.80 → 5.60 MW_th vs 9.91 MW_th peak): 4.31 MW_th gap met by 2,500 m³ store (91.4 MWh usable) + 8.0 MW_th HTHP (48 h).',
    params: Object.freeze({
      utilization: 0.70,
      heatCaptureEff: 0.80,
      ambientTempC: -3.0,
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
    description: 'Vault 3 offline. Optical NANF HCF ring reroutes V2X traffic (143.2 μs) and SDN shifts 8.0 MW IT load across healthy vaults (70.1× vs EN 253 pumping).',
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
    label: 'Summer Surplus Cascade',
    shortLabel: 'Summer Cascade',
    badge: '24°C · 0.4 MW ORC',
    description: 'Steady DHW baseload (3.01 MW_th + 0.39 MW_th loss) leaves 6.40 MW_th surplus routed to 75°C commercial sales, ~0.4 MW_e Micro-ORC (7.2–8.3% eff.), and dry coolers.',
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
    label: 'Dublin 44-Day Drought',
    shortLabel: 'Dublin Drought',
    badge: '0× Rain · 6,700 m³',
    description: 'Zero rainfall input; 10,000 m³ RC drought cistern (6,700 m³ min storage = 44-day reserve at 152 m³/d) & sealed Grade A UPW loop avoid 157.7–175.2k m³/yr.',
    params: Object.freeze({
      utilization: 1.0,
      heatCaptureEff: 0.98,
      ambientTempC: 21.0,
      rainfallMultiplier: 0.0,
      cisternSoc: 0.67,
      thermalStoreSoc: 0.85,
      sdnEnabled: true,
      vaultFaults: [false, false, false, false, false],
      hthpScenarioB: false,
      timeOfDay: 15.0,
    }),
  }),
  Object.freeze({
    id: 'conservative_stress',
    label: 'Cautious (u=0.70, f=0.80)',
    shortLabel: 'Cautious Base',
    badge: 'u=0.70 · f=0.80',
    description: 'Cautious annual case: 70% IT load and 80% capture yield 49,056 MWh_th/yr (130% gross / 120% net of 37,612 MWh/yr + 3,421 MWh loss; u_BE = 58.6%).',
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
  const ratio = (T + 3) / 28;
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

  const equalIfActive = activeIndices.length > 0 ? totalItMw / activeIndices.length : 0;
  let sdnShiftedMw = 0;
  for (let i = 0; i < CONSTANTS.DC.VAULT_COUNT; i++) {
    if (!faults[i] && sdnEnabled && loads[i] > equalIfActive) {
      sdnShiftedMw += loads[i] - equalIfActive;
    }
  }
  const faultCount = faults.filter(Boolean).length;
  const nominalPerVault = requestedItMw / CONSTANTS.DC.VAULT_COUNT;
  const reroutedFromFaultMw = Math.min(totalItMw, faultCount * nominalPerVault);

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

  // 3. Thermal & EN 253 Network Hydraulic Flows
  const capturedHeatMw = totalItMw * s.heatCaptureEff;
  const annualCapturedHeatMWh = capturedHeatMw * CONSTANTS.DC.HOURS_PER_YEAR;
  const annualGrossCoveragePct = (annualCapturedHeatMWh / CONSTANTS.THERMAL.ANNUAL_DISTRICT_DEMAND_MWH) * 100;
  const annualNetDemandMWh = CONSTANTS.THERMAL.ANNUAL_DISTRICT_DEMAND_MWH + CONSTANTS.THERMAL.RING_LOSS_MWH_YR;
  const annualCoveragePct = (annualCapturedHeatMWh / annualNetDemandMWh) * 100;

  const primaryGrossFlowLs = CONSTANTS.THERMAL.PRIMARY_NOMINAL_GROSS_LS * loadRatio;
  const primaryGrossFlowLmin = primaryGrossFlowLs * 60;
  const primaryNetHeatMw = capturedHeatMw * CONSTANTS.THERMAL.PRIMARY_GROSS_SHARE;
  const primaryNetFlowLmin = primaryGrossFlowLmin * s.heatCaptureEff;

  const secondaryGrossFlowLs = CONSTANTS.THERMAL.SECONDARY_NOMINAL_GROSS_LS * loadRatio;
  const secondaryGrossFlowLmin = secondaryGrossFlowLs * 60;
  const secondaryNetHeatMw = capturedHeatMw * CONSTANTS.THERMAL.SECONDARY_GROSS_SHARE;
  const secondaryNetFlowLmin = secondaryGrossFlowLmin * s.heatCaptureEff;

  const districtDemandMw = computeDistrictDemandMw(s.ambientTempC);
  const pipeLossMw = CONSTANTS.THERMAL.RING_LOSS_MW;
  const totalNetworkDemandMw = districtDemandMw + pipeLossMw;
  const annualisedDemandMWh = districtDemandMw * CONSTANTS.DC.HOURS_PER_YEAR;
  const netThermalBalanceMw = capturedHeatMw - totalNetworkDemandMw;
  const basePumpingKw = CONSTANTS.THERMAL.PUMPING_ANNUAL_MWH_YR * 1000 / CONSTANTS.DC.HOURS_PER_YEAR;
  const ringPumpingKw = basePumpingKw * loadRatio * (s.sdnEnabled ? 1.0 : 1.45);

  // 4. Canonical 3-Tier Surplus Cascade vs Cold-Snap / Scenario B Deficit Coverage
  let surplusMw = 0;
  let deficitMw = 0;
  let thermalStoreChargeMw = 0;
  let thermalStoreDischargeMw = 0;
  let commercialHeatSalesMw = 0;
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
    thermalStoreChargeMw = s.thermalStoreSoc < 0.95 ? Math.min(surplusMw, 0.80) : 0;
    const rem1 = surplusMw - thermalStoreChargeMw;
    // Tier 1: Direct 70-75 C commercial heat sales (pools, greenhouses, laundries)
    commercialHeatSalesMw = Math.min(rem1 * 0.25, 1.40);
    const rem2 = rem1 - commercialHeatSalesMw;
    // Tier 2: ~0.4 MW_e Micro-ORC (7.2-8.3% of 75 C surplus)
    orcThermalInputMw = Math.min(rem2 * 0.80, 5.10);
    orcOutputKw = Math.min(CONSTANTS.THERMAL.ORC_MAX_KW_E, orcThermalInputMw * 1000 * 0.078);
    // Tier 3: N+1 Low-Noise Dry Coolers reject remaining surplus
    dryCoolerRejectionMw = Math.max(0, rem2 - orcThermalInputMw);
  } else {
    deficitMw = -netThermalBalanceMw;
    thermalStoreDischargeMw = s.thermalStoreSoc > 0.05 ? Math.min(deficitMw, 4.0) : 0;
    hthpThermalOutputMw = Math.min(CONSTANTS.THERMAL.HTHP_MAX_MW_TH, Math.max(0, deficitMw - thermalStoreDischargeMw));
    hthpElectricalMw = hthpThermalOutputMw / hthpCop;
    unmetThermalDeficitMw = Math.max(0, deficitMw - thermalStoreDischargeMw - hthpThermalOutputMw);
  }

  // Scenario B fallback (45 C silicon limit -> central HTHPs boost 40 -> 75 C)
  let scenarioBElecMw = 0;
  if (s.hthpScenarioB && capturedHeatMw > 0) {
    const boostHeatMw = Math.min(CONSTANTS.THERMAL.SCENARIO_B_EXTRA_HTHP_MW, totalNetworkDemandMw);
    scenarioBElecMw = boostHeatMw / hthpCop;
  }

  const netGridDrawMw = Math.max(
    0,
    grossFacilityMw + hthpElectricalMw + scenarioBElecMw + ringPumpingKw / 1000 - pvOutputMw - orcOutputKw / 1000 - bessDischargeMw + bessChargeMw
  );

  // 5. Hydrological Loop (10 ha inert roofs -> 55,620 m3/yr @ 1x; 152 m3/d; 10,000 m3 cistern)
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

  // 7. Carbon & Financials (Ali 2026b factors)
  const heatShare = Math.min(1, annualCapturedHeatMWh / annualNetDemandMWh);
  const avoidedHeatingCo2TonYr = CONSTANTS.FINANCIAL.AVOIDED_HEATING_CO2_TON_YR * heatShare;
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
      annualGrossCoveragePct,
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
      pipeLossMw,
      totalNetworkDemandMw,
      annualisedDemandMWh,
      netThermalBalanceMw,
      surplusMw,
      deficitMw,
      thermalStoreSoc: s.thermalStoreSoc,
      thermalStoreEnergyMWh: CONSTANTS.THERMAL.THERMAL_STORE_MWH * s.thermalStoreSoc,
      thermalStoreUsableMWh: CONSTANTS.THERMAL.THERMAL_STORE_USABLE_MWH * s.thermalStoreSoc,
      thermalStoreChargeMw,
      thermalStoreDischargeMw,
      commercialHeatSalesMw,
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
      scenarioBElecMw,
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
      'Reinforced-concrete vault (25 × 16 m, 100-yr EN 1990 life) at Level −4 bedrock beneath public plazas. Isolated by floating acoustic slabs, 1.0 m perimeter vibration trenches, and <110 dB water-mist suppression.',
    specs: [
      '2.0 MW IT capacity · 800 VDC SSTs (−5% loss; HTS busways optional)',
      '43 m³ 15-min thermal buffer tank (ΔT = 10 K · 215 m³ district total)',
      'In-silicon microfluidic cooling: 75°C primary / 45°C secondary',
      '1.00 km NANF Hollow-Core Fiber ring hop (8.16 μs RTT)',
    ],
    getLiveMetrics: (sim) => {
      const v = sim.vaults[idx];
      return [
        ['Status', v.faulted ? 'FAULTED (OFFLINE)' : 'ONLINE (NOMINAL)'],
        ['IT Load', `${v.itMw.toFixed(2)} / 2.00 MW (${v.loadPct.toFixed(0)}%)`],
        ['Captured Heat', `${v.heatMw.toFixed(2)} MW_th`],
        ['75°C Primary Flow', `${(v.flowPrimaryLmin / 60).toFixed(2)} L/s (${v.flowPrimaryLmin.toFixed(0)} L/min)`],
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
    title: '2,500 m³ Stratified Pressurised Thermal Store',
    category: 'Loop 2 · Thermal Buffering',
    description:
      'Insulated stratified water tank (ΔT = 35 K, 101.6 MWh_th gross / 91.4 MWh_th usable) sustaining average winter demand for 15 h with zero DC heat and 48 h alongside the 8.0 MW_th backup HTHPs.',
    specs: [
      'Volume: 2,500 m³ insulated steel/RC tank (100-yr civil life)',
      'Capacity: 101.6 MWh_th gross / 91.4 MWh_th usable (ΔT = 35 K)',
      '15 h winter carry standalone · 48 h peak resilience w/ 8.0 MW HTHP',
    ],
    getLiveMetrics: (sim) => [
      ['State of Charge', `${(sim.thermal.thermalStoreSoc * 100).toFixed(0)}% (${sim.thermal.thermalStoreUsableMWh.toFixed(1)} / 91.4 MWh usable)`],
      ['Charge Rate', `${sim.thermal.thermalStoreChargeMw.toFixed(2)} MW_th`],
      ['Discharge Rate', `${sim.thermal.thermalStoreDischargeMw.toFixed(2)} MW_th`],
    ],
  },

  hthp_bank: {
    title: '8.0 MW_th Natural-Refrigerant Backup HTHP Bank',
    category: 'Loop 2 · Peaking & Scenario B',
    description:
      'High-temperature heat pumps covering cautious −3°C cold-snap deficits (−4.31 MW_th) and Scenario B fallback (40°C → 75°C condensing at COP 4.0–6.0; ~8.5 MW / €5.1M for full boost, +€33/MWh).',
    specs: [
      'Rated thermal output: 8.0 MW_th (natural refrigerant)',
      'Scenario B COP band: 4.0 – 6.0 (5.9–8.8 GWh_e/yr, LCOH → €126/MWh)',
      'Covers −4.31 MW_th cautious deficit at −3°C & 48 h outage resilience',
    ],
    getLiveMetrics: (sim) => [
      ['Thermal Output', `${sim.thermal.hthpThermalOutputMw.toFixed(2)} MW_th`],
      ['Electrical Draw', `${(sim.thermal.hthpElectricalMw + sim.thermal.scenarioBElecMw).toFixed(2)} MW_e`],
      ['Operating COP', `${sim.thermal.hthpCop.toFixed(2)}`],
      ['Scenario B Fallback', sim.thermal.scenarioBActive ? 'Armed (40°C → 75°C, +€33/MWh)' : 'Standby'],
    ],
  },

  tces: {
    title: 'Optional R&D Pad — SrBr₂ / K₂CO₃ TCES (Excluded from Base)',
    category: 'Optional R&D · Not in Base CapEx',
    description:
      'Thermochemical energy storage was evaluated during early concept work and excluded from the canonical RAKDA-USCI base CapEx and 3-tier surplus cascade.',
    specs: [
      'Status: Excluded from €136.66M base CapEx & LCOH model',
      'Replaced by direct 70–75°C commercial sales + 0.4 MW_e Micro-ORC + Dry Coolers',
    ],
    getLiveMetrics: (sim) => [
      ['Base Cascade Allocation', '0.00 MW_th (Excluded from base scope)'],
      ['Total Surplus Available', `${sim.thermal.surplusMw.toFixed(2)} MW_th`],
    ],
  },

  ti_ptes: {
    title: 'Optional R&D Pad — TI-PTES Carnot Battery (Excluded from Base)',
    category: 'Optional R&D · Not in Base CapEx',
    description:
      'Thermally-Integrated Pumped Thermal Energy Storage is excluded from the canonical RAKDA-USCI base CapEx and 3-tier surplus cascade (future R&D option only).',
    specs: [
      'Status: Excluded from €136.66M base CapEx & DCF model',
      'Active grid firming delivered by 8 MW / 32 MWh LFP BESS',
    ],
    getLiveMetrics: (sim) => [
      ['Base Cascade Allocation', '0.00 MW_th (Excluded from base scope)'],
      ['Active BESS Mode', sim.electrical.bessMode],
    ],
  },

  micro_orc: {
    title: 'Summer Residual Micro-ORC Skid (~0.4 MW_e)',
    category: 'Exergy Cascade Tier 2 · Power Recovery',
    description:
      'PFAS-free Organic Rankine Cycle turbine converting 75°C summer surplus (17.2% Carnot ceiling @ 15°C sink → 7.2–8.3% η_ORC) into 2.60–2.97 GWh_e/yr (~0.4 MW_e, €0.35M/yr / +€3.5M utility NPV).',
    specs: [
      'Carnot ceiling: 17.2% (75°C vs 15°C sink) → 7.2–8.3% η_ORC',
      'Output: ~0.4 MW_e (2.60–2.97 GWh_e/yr · €0.35M/yr revenue)',
    ],
    getLiveMetrics: (sim) => [
      ['Thermal Input (75°C)', `${sim.thermal.orcThermalInputMw.toFixed(2)} MW_th`],
      ['Electrical Generation', `${sim.thermal.orcOutputKw.toFixed(1)} kW_e`],
      ['Commercial Heat Sales', `${sim.thermal.commercialHeatSalesMw.toFixed(2)} MW_th`],
    ],
  },

  mof_chiller: {
    title: 'Tier 1 Commercial Heat & Optional Adsorption Cooling',
    category: 'Exergy Cascade Tier 1 · 75°C Direct Sales',
    description:
      'Direct 70–75°C heat sales to local commercial buyers (pools, laundries, greenhouses, thermally driven adsorption chillers) delivering 1.8×–12.5× ORC value.',
    specs: [
      'Priority Tier #1 of 44,816 MWh/yr summer surplus (35,852 MWh @ 75°C)',
      'Direct heat value ~€95/MWh vs ~€10/MWh thermal input for ORC',
    ],
    getLiveMetrics: (sim) => [
      ['Tier 1 Commercial Sales', `${sim.thermal.commercialHeatSalesMw.toFixed(2)} MW_th`],
      ['Total Surplus Stream', `${sim.thermal.surplusMw.toFixed(2)} MW_th`],
    ],
  },

  dry_coolers: {
    title: 'N+1 Low-Noise Dry Coolers (Cascade Tier 3)',
    category: 'Exergy Cascade Tier 3 · Heat Rejection',
    description:
      'Zero-water adiabatic/dry heat rejection bank at the energy centre ensuring server uptime never depends on summer heat demand—saving 157,680–175,200 m³/yr of potable tower water.',
    specs: [
      'Zero evaporative water consumption (saves 157,680–175,200 m³/yr)',
      'N+1 redundancy with remote acoustic shrouding at energy centre',
    ],
    getLiveMetrics: (sim) => [
      ['Heat Rejected', `${sim.thermal.dryCoolerRejectionMw.toFixed(2)} MW_th`],
      ['Avoided Cooling Water', '157,680–175,200 m³/yr'],
    ],
  },

  rainwater_cistern: {
    title: '10,000 m³ Subterranean RC Drought Cistern',
    category: 'Loop 3 · Hydrological Resilience',
    description:
      'Reinforced-concrete cistern capturing runoff from 10 ha (100,000 m²) inert roofs (772.5 mm/yr, C_r=0.80, η_f=0.90 → 55,620 m³/yr, 152 m³/d), keeping ≥6,700 m³ (44-day drought reserve).',
    specs: [
      'Capacity: 10,000 m³ (6,700 m³ min across 3,318 m³ swing = 44 d reserve)',
      'Water grades: Grade A UPW (≤3,265 m³/yr), Grade B toilets/laundry, Grade C parks',
      'Survey (N=105): 3.87/5.00 recycled water score (72.8% comfortable)',
    ],
    getLiveMetrics: (sim) => [
      ['Cistern Volume', `${sim.hydrology.cisternVolumeM3.toFixed(0)} / 10,000 m³ (${(sim.hydrology.cisternSoc * 100).toFixed(0)}%)`],
      ['Drought Reserve', `${sim.hydrology.droughtAutonomyDays.toFixed(1)} days @ 152 m³/d`],
      ['Annual Rain Harvest', `${sim.hydrology.annualRainHarvestM3.toFixed(0)} m³/yr`],
    ],
  },

  ultrapure_train: {
    title: 'Grade A Semiconductor Ultrapure Water (UPW) Train',
    category: 'Loop 3 · Microfluidic Polish',
    description:
      'Closed-loop Grade A UPW makeup train (≤3,265 m³/yr, <5.9% of harvested yield): dead-end UF + MCDI + mixed-bed DI + UV-C LED + BDD/Ti₄O₇ PFAS oxidation, isolated by EN 1717 physical air-gaps.',
    specs: [
      'Grade A = UPW (≤1.0 μS/cm, 1–18.2 MΩ·cm, ≤0.2–1.0 μm, ≤1 ppm SiO₂)',
      'Grade B = toilets & laundry (no dishwashers) · Grade C = park irrigation',
    ],
    getLiveMetrics: (sim) => [
      ['Primary 75°C Flow', `${sim.thermal.primaryGrossFlowLs.toFixed(1)} L/s (${sim.thermal.primaryGrossFlowLmin.toFixed(0)} L/min)`],
      ['Secondary 45°C Flow', `${sim.thermal.secondaryGrossFlowLs.toFixed(1)} L/s (${sim.thermal.secondaryGrossFlowLmin.toFixed(0)} L/min)`],
      ['Annual Grade A Makeup', '≤ 3,265 m³/yr (< 5.9% of yield)'],
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
      'Local HCF+CPO E2E RTT: 143.2 μs wireless / 48.2 μs fibre (1.99 mm @ 50 km/h)',
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
      'Car-free public square sited directly above a Level −4 vault (~350 m² footprint), addressing the #1 resident survey concern (noise/vibration, 35.1%, N=105 cleaned).',
    specs: [
      'Zero vaults under residential foundations (100-yr EN 1990 concrete)',
      'Survey (N=105): 3.20/5.00 DC comfort (47.1% pos / 17.3% neut / 35.6% low)',
    ],
    getLiveMetrics: (sim) => [
      ['Active Vaults Below', `${sim.electrical.activeVaultCount} / 5 online`],
      ['Max Fire-Mist Noise', '< 110 dB (subterranean isolated)'],
      ['Survey Top Concern Addressed', 'Noise/Vibration (35.1%, N=105)'],
    ],
  },

  civic_hub: {
    title: 'Schools, Health Centre & Community Hub (15.0 ha)',
    category: '2045 Circular District Anchor',
    description:
      '4 primary schools, 2 secondary schools, primary health centre, and sports hub served by the 30 km pre-insulated 70/35°C EN 253 network (13.0 W/m loss = 3,421 MWh/yr = 9.1%).',
    specs: [
      '15.0 ha civic/education + 14.0 ha retail/offices + 25.0 ha parks',
      '30 km EN 253 70/35°C steel pipes (13.0 W/m = 9.1% loss; 259 MWh_e/yr pump)',
    ],
    getLiveMetrics: (sim) => [
      ['Network Demand (incl. 0.39 MW loss)', `${sim.thermal.totalNetworkDemandMw.toFixed(2)} MW_th`],
      ['EN 253 Pumping Power', `${sim.thermal.ringPumpingKw.toFixed(1)} kW_e`],
      ['Net Heat Coverage Ratio', `${sim.thermal.annualCoveragePct.toFixed(1)}%`],
    ],
  },

  residential_district: {
    title: '7,500-Home Circular Residential District (20,000 Residents)',
    category: '180.0 ha Masterplan · 90.0 ha Residential',
    description:
      'Mixed-density 2045 urban district (3,095 buildings, 83.3 dph) heated by recovered data-centre waste heat via instantaneous HIUs (≥55°C tap, zero stored DHW).',
    specs: [
      '3,300 mid-rise · 1,200 low-rise · 2,100 townhouses · 900 detached',
      'Annual demand: 37,612 MWh_th/yr · €536/yr bill vs €1,001 Gas / €1,691 ASHP',
      'Avoided heating CO₂: 7,230–7,270 t/yr (6.8–7.5 mo embodied payback)',
    ],
    getLiveMetrics: (sim) => [
      ['Ambient Temp', `${sim.inputs.ambientTempC.toFixed(1)} °C`],
      ['Building Heat Demand', `${sim.thermal.districtDemandMw.toFixed(2)} MW_th`],
      ['Annualised Demand', `${sim.thermal.annualisedDemandMWh.toFixed(0)} MWh_th/yr`],
      ['Avoided Heating CO₂', `${sim.financial.avoidedHeatingCo2TonYr.toFixed(0)} tCO₂/yr (7,230–7,270 nom.)`],
    ],
  },
});
