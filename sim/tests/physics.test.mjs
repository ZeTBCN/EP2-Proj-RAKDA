import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  CONSTANTS,
  SCENARIO_LIST,
  SCENARIO_PRESETS,
  SUBSYSTEM_INFO,
  computeSimulation,
  computeDistrictDemandMw,
  computeHthpCop,
} from '../src/physics.js';

function assertAllFinite(obj, path = 'root') {
  if (typeof obj === 'number') {
    assert.ok(Number.isFinite(obj), `Expected finite number at ${path}, got ${obj}`);
  } else if (Array.isArray(obj)) {
    obj.forEach((v, i) => assertAllFinite(v, `${path}[${i}]`));
  } else if (obj && typeof obj === 'object') {
    for (const [k, v] of Object.entries(obj)) {
      assertAllFinite(v, `${path}.${k}`);
    }
  }
}

describe('RAKDA EP2 Physics & Engineering Model (RAKDA USCI Nov 2026)', () => {
  it('runs all 7 scenario presets cleanly with finite outputs', () => {
    assert.equal(SCENARIO_LIST.length, 7);
    for (const preset of SCENARIO_LIST) {
      const sim = computeSimulation({ ...preset.params, scenarioId: preset.id });
      assertAllFinite(sim, preset.id);
      assert.equal(sim.vaults.length, 5);
    }
  });

  it('matches Nominal 2045 Design exact KPIs from RAKDA-USCI.docx', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.equal(sim.electrical.totalItMw, 10.0);
    assert.equal(sim.electrical.pue, 1.015);
    assert.equal(Math.round(sim.electrical.annualGrossMWh), 88914);
    assert.equal(Math.round(sim.electrical.annualNetGridMWh), 77958);

    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 9.80) < 1e-6);
    assert.equal(Math.round(sim.thermal.annualCapturedHeatMWh), 85848);
    assert.ok(Math.abs(sim.thermal.annualGrossCoveragePct - 228.25) < 0.3);
    assert.ok(Math.abs(sim.thermal.annualCoveragePct - 209.22) < 0.3);

    assert.ok(Math.abs(sim.thermal.primaryGrossFlowLs - 76.5) < 0.05);
    assert.ok(Math.abs(sim.thermal.primaryNetHeatMw - 7.84) < 1e-6);
    assert.ok(Math.abs(sim.thermal.secondaryGrossFlowLs - 32.0) < 0.05);

    assert.ok(Math.abs(sim.thermal.districtDemandMw - 4.2936) < 0.01);
    assert.ok(Math.abs(sim.thermal.annualisedDemandMWh - 37612) < 5);
    assert.equal(CONSTANTS.THERMAL.RING_LOSS_W_PER_M, 13.0);
    assert.equal(CONSTANTS.THERMAL.RING_LOSS_MWH_YR, 3421);
    assert.equal(CONSTANTS.DC.BUFFER_TOTAL_M3, 215.0);
    assert.equal(CONSTANTS.DC.VAULT_LIFE_YR, 100);
  });

  it('matches Cautious Base (u=0.70, f_cap=0.80) KPIs (130% gross / 120% net)', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.conservative_stress.params);
    assert.ok(Math.abs(sim.electrical.totalItMw - 7.0) < 1e-6);
    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 5.60) < 1e-6);
    assert.equal(Math.round(sim.thermal.annualCapturedHeatMWh), 49056);
    assert.ok(Math.abs(sim.thermal.annualGrossCoveragePct - 130.43) < 0.2);
    assert.ok(Math.abs(sim.thermal.annualCoveragePct - 119.55) < 0.5);
  });

  it('handles Winter Design Day (-3°C) at 100% IT load (9.91 MW vs 9.80 MW = -0.11 MW gap)', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.winter_cold_snap.params);
    assert.ok(Math.abs(computeDistrictDemandMw(-3.0) - 9.52) < 1e-6);
    assert.ok(Math.abs(sim.thermal.districtDemandMw - 9.52) < 1e-6);
    assert.ok(Math.abs(sim.thermal.totalNetworkDemandMw - 9.91) < 1e-6);
    assert.ok(Math.abs(sim.thermal.netThermalBalanceMw - (-0.11)) < 1e-6);
    assert.ok(Math.abs(sim.thermal.deficitMw - 0.11) < 1e-6);
    assert.equal(sim.thermal.unmetThermalDeficitMw, 0);
  });

  it('covers Cautious Winter (-3°C, u=0.70, f=0.80 -> 4.31 MW deficit) with store + HTHP', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.winter_stress_60.params);
    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 5.60) < 1e-6);
    assert.ok(Math.abs(sim.thermal.deficitMw - 4.31) < 1e-6);
    assert.ok(sim.thermal.thermalStoreDischargeMw > 0);
    assert.ok(sim.thermal.hthpThermalOutputMw > 0 && sim.thermal.hthpThermalOutputMw <= 8.0);
    assert.ok(sim.thermal.hthpCop >= 4.0 && sim.thermal.hthpCop <= 6.0);
    assert.equal(sim.thermal.unmetThermalDeficitMw, 0);
    assert.ok(computeHthpCop(25) <= 6.0 && computeHthpCop(-3) >= 4.0);
  });

  it('reroutes IT load and optical ring around Vault 3 Fault with SDN (70.1x vs EN 253)', () => {
    const simSdnOn = computeSimulation(SCENARIO_PRESETS.vault_fault_sdn.params);
    const simSdnOff = computeSimulation({ ...SCENARIO_PRESETS.vault_fault_sdn.params, sdnEnabled: false });

    assert.equal(simSdnOn.vaults[2].faulted, true);
    assert.equal(simSdnOn.vaults[2].itMw, 0);
    assert.equal(simSdnOn.electrical.activeVaultCount, 4);
    assert.ok(Math.abs(simSdnOn.electrical.totalItMw - 8.0) < 1e-6);
    for (const v of simSdnOn.vaults) {
      assert.ok(v.itMw <= 2.0 + 1e-9);
    }
    assert.equal(simSdnOn.optical.sdnEfficiencyFactor, 70.1);
    assert.ok(simSdnOn.thermal.ringPumpingKw < simSdnOff.thermal.ringPumpingKw);
    assert.ok(simSdnOn.optical.activeLocalE2eRttUs < 160);
  });

  it('conserves energy across the 3-tier Summer Surplus Cascade without TI-PTES/TCES in base', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.summer_exergy.params);
    assert.ok(Math.abs(sim.thermal.districtDemandMw - 3.01) < 1e-6);
    assert.ok(Math.abs(sim.thermal.totalNetworkDemandMw - 3.40) < 1e-6);
    assert.ok(Math.abs(sim.thermal.surplusMw - 6.40) < 1e-6);
    assert.equal(sim.thermal.tcesChargeMw, 0);
    assert.equal(sim.thermal.tiPtesChargeMw, 0);

    const cascadeSum =
      sim.thermal.thermalStoreChargeMw +
      sim.thermal.commercialHeatSalesMw +
      sim.thermal.orcThermalInputMw +
      sim.thermal.dryCoolerRejectionMw;
    assert.ok(Math.abs(cascadeSum - sim.thermal.surplusMw) < 1e-6);
    assert.ok(sim.thermal.orcOutputKw >= 300 && sim.thermal.orcOutputKw <= 420);

    const extremeSummer = computeSimulation({
      ...SCENARIO_PRESETS.summer_exergy.params,
      ambientTempC: 30,
      thermalStoreSoc: 1.0,
    });
    assert.ok(extremeSummer.thermal.orcOutputKw <= 420);
  });

  it('models Dublin hydrological loop (55,620 m3/yr, 44-day drought reserve at 6,700 m3)', () => {
    const nominal = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.ok(Math.abs(nominal.hydrology.annualHarvestPer100kM3 - 55620) < 1e-3);
    assert.ok(Math.abs(nominal.hydrology.annualRainHarvestM3 - 55620) < 1e-3);
    assert.equal(CONSTANTS.HYDROLOGY.DROUGHT_RESERVE_DAYS, 44);
    assert.equal(CONSTANTS.HYDROLOGY.UPW_MAKEUP_MAX_M3_YR, 3265);
    assert.equal(CONSTANTS.HYDROLOGY.AVOIDED_POTABLE_MIN_M3_YR, 157680);
    assert.equal(CONSTANTS.HYDROLOGY.AVOIDED_POTABLE_MAX_M3_YR, 175200);

    const drought = computeSimulation(SCENARIO_PRESETS.dublin_drought.params);
    assert.equal(drought.hydrology.annualRainHarvestM3, 0);
    assert.ok(Math.abs(drought.hydrology.cisternVolumeM3 - 6700) < 1e-6);
    assert.ok(drought.hydrology.droughtAutonomyDays >= 44 && drought.hydrology.droughtAutonomyDays <= 45);
  });

  it('matches Optical Latency, Financials (Case 1 & 2, TCO) & Survey (N=105) KPIs', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.equal(sim.optical.activeLocalE2eRttUs, 143.2);
    assert.ok(Math.abs(sim.optical.localBlindTravelMm - 1.99) < 0.02);
    assert.equal(sim.optical.cloudTransportRttMs, 7.64);
    assert.equal(sim.optical.cloudControlLoopMs, 90.0);
    assert.ok(Math.abs(sim.optical.cloudBlindTravelM - 1.25) < 0.01);
    assert.equal(sim.optical.localBreakoutPct, 87.34);
    assert.equal(sim.optical.wanPowerSavedMWhYr, 455);

    assert.equal(sim.financial.CAPEX_TOTAL_M, 136.66);
    assert.equal(sim.financial.CAPEX_DISTRICT_M, 57.31);
    assert.equal(sim.financial.CAPEX_DC_OPERATOR_M, 79.35);
    assert.equal(sim.financial.COLOCATION_RENT_EUR_KW_MO, 145);
    assert.equal(sim.financial.EBITDA_ANNUAL_M, 19.60);
    assert.equal(sim.financial.SIMPLE_PAYBACK_YR, 7.0);
    assert.equal(sim.financial.IRR_PCT, 13.7);
    assert.equal(sim.financial.NPV_4PCT_M, 189.0);
    assert.equal(sim.financial.NPV_8PCT_M, 77.0);
    assert.equal(sim.financial.UTILITY_IRR_PCT, 4.2);
    assert.equal(sim.financial.UTILITY_LCOH_NET_EUR_MWH, 93);
    assert.equal(sim.financial.TCO_USCI_M, 520);
    assert.equal(sim.financial.TCO_GAS_M, 826);
    assert.equal(sim.financial.TCO_ASHP_M, 936);
    assert.equal(sim.financial.BILL_USCI_EUR_YR, 536);

    assert.equal(CONSTANTS.SURVEY.sampleSize, 105);
    assert.equal(CONSTANTS.SURVEY.dcComfortScore, 3.20);
    assert.equal(CONSTANTS.SURVEY.recycledWaterScore, 3.87);
    assert.equal(CONSTANTS.SURVEY.topConcernNoiseVibrationPct, 35.1);
  });

  it('provides valid Inspector metadata and live metrics for every subsystem key', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.nominal.params);
    const keys = Object.keys(SUBSYSTEM_INFO);
    assert.ok(keys.length >= 16);
    for (const key of keys) {
      const info = SUBSYSTEM_INFO[key];
      assert.ok(info.title && info.category && info.description);
      assert.ok(Array.isArray(info.specs) && info.specs.length > 0);
      const metrics = info.getLiveMetrics(sim);
      assert.ok(Array.isArray(metrics) && metrics.length > 0);
      for (const [label, value] of metrics) {
        assert.equal(typeof label, 'string');
        assert.equal(typeof value, 'string');
      }
    }
  });
});
