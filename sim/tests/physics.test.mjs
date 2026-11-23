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

describe('RAKDA EP2 Physics & Engineering Model', () => {
  it('runs all 7 scenario presets cleanly with finite outputs', () => {
    assert.equal(SCENARIO_LIST.length, 7);
    for (const preset of SCENARIO_LIST) {
      const sim = computeSimulation({ ...preset.params, scenarioId: preset.id });
      assertAllFinite(sim, preset.id);
      assert.equal(sim.vaults.length, 5);
    }
  });

  it('matches Nominal 2045 Design exact KPIs from SPEC.md', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.equal(sim.electrical.totalItMw, 10.0);
    assert.equal(sim.electrical.pue, 1.015);
    assert.equal(Math.round(sim.electrical.annualGrossMWh), 88914);
    assert.equal(Math.round(sim.electrical.annualNetGridMWh), 77958);

    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 9.80) < 1e-6);
    assert.equal(Math.round(sim.thermal.annualCapturedHeatMWh), 85848);
    assert.ok(Math.abs(sim.thermal.annualCoveragePct - 228.258) < 0.1);

    assert.ok(Math.abs(sim.thermal.primaryGrossFlowLmin - 4635.3) < 0.05);
    assert.ok(Math.abs(sim.thermal.primaryNetHeatMw - 7.84) < 1e-6);
    assert.ok(Math.abs(sim.thermal.primaryNetFlowLmin - 4543) < 0.5);
    assert.ok(Math.abs(sim.thermal.secondaryGrossFlowLmin - 1923.1) < 0.05);

    assert.ok(Math.abs(sim.thermal.districtDemandMw - 4.293) < 0.01);
    assert.ok(Math.abs(sim.thermal.annualisedDemandMWh - 37610) < 5);
  });

  it('matches Conservative Stress (u=0.70, f_cap=0.80) KPIs', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.conservative_stress.params);
    assert.ok(Math.abs(sim.electrical.totalItMw - 7.0) < 1e-6);
    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 5.60) < 1e-6);
    assert.equal(Math.round(sim.thermal.annualCapturedHeatMWh), 49056);
    assert.ok(Math.abs(sim.thermal.annualCoveragePct - 130.43) < 0.1);
  });

  it('handles Winter Cold Snap (-5°C) at 100% IT load (+0.28 MW_th surplus)', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.winter_cold_snap.params);
    assert.ok(Math.abs(computeDistrictDemandMw(-5.0) - 9.52) < 1e-6);
    assert.ok(Math.abs(sim.thermal.districtDemandMw - 9.52) < 1e-6);
    assert.ok(Math.abs(sim.thermal.netThermalBalanceMw - 0.28) < 1e-6);
    assert.equal(sim.thermal.deficitMw, 0);
  });

  it('covers Winter Cold Snap + 60% Stress (-3.64 MW_th deficit) with thermal store + HTHP', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.winter_stress_60.params);
    assert.ok(Math.abs(sim.thermal.capturedHeatMw - 5.88) < 1e-6);
    assert.ok(Math.abs(sim.thermal.deficitMw - 3.64) < 1e-6);
    assert.ok(sim.thermal.thermalStoreDischargeMw > 0);
    assert.ok(sim.thermal.hthpThermalOutputMw > 0 && sim.thermal.hthpThermalOutputMw <= 8.0);
    assert.ok(sim.thermal.hthpCop >= 4.6 && sim.thermal.hthpCop <= 6.9);
    assert.equal(sim.thermal.unmetThermalDeficitMw, 0);
    assert.ok(computeHthpCop(25) <= 6.9 && computeHthpCop(-5) >= 4.6);
  });

  it('reroutes IT load and optical ring around Vault 3 Fault with SDN', () => {
    const simSdnOn = computeSimulation(SCENARIO_PRESETS.vault_fault_sdn.params);
    const simSdnOff = computeSimulation({ ...SCENARIO_PRESETS.vault_fault_sdn.params, sdnEnabled: false });

    assert.equal(simSdnOn.vaults[2].faulted, true);
    assert.equal(simSdnOn.vaults[2].itMw, 0);
    assert.equal(simSdnOn.electrical.activeVaultCount, 4);
    assert.ok(Math.abs(simSdnOn.electrical.totalItMw - 8.0) < 1e-6);
    for (const v of simSdnOn.vaults) {
      assert.ok(v.itMw <= 2.0 + 1e-9);
    }
    assert.equal(simSdnOn.optical.sdnEfficiencyFactor, 74.9);
    assert.ok(simSdnOn.thermal.ringPumpingKw < simSdnOff.thermal.ringPumpingKw);
    assert.ok(simSdnOn.optical.activeLocalE2eRttUs < 160);
  });

  it('conserves energy across the Summer Exergy Cascade and caps Micro-ORC at 440 kW_e', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.summer_exergy.params);
    assert.ok(Math.abs(sim.thermal.districtDemandMw - 1.45) < 1e-6);
    assert.ok(Math.abs(sim.thermal.surplusMw - 8.35) < 1e-6);

    const cascadeSum =
      sim.thermal.thermalStoreChargeMw +
      sim.thermal.tcesChargeMw +
      sim.thermal.tiPtesChargeMw +
      sim.thermal.orcThermalInputMw +
      sim.thermal.mofChillerMw +
      sim.thermal.dryCoolerRejectionMw;
    assert.ok(Math.abs(cascadeSum - sim.thermal.surplusMw) < 1e-6);
    assert.ok(sim.thermal.orcOutputKw >= 418 && sim.thermal.orcOutputKw <= 440);

    const extremeSummer = computeSimulation({
      ...SCENARIO_PRESETS.summer_exergy.params,
      ambientTempC: 30,
      thermalStoreSoc: 1.0,
    });
    assert.ok(extremeSummer.thermal.orcOutputKw <= 440);
  });

  it('models Dublin hydrological loop and 60-day drought resilience', () => {
    const nominal = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.ok(Math.abs(nominal.hydrology.annualHarvestPer100kM3 - 55620) < 1e-3);
    assert.ok(Math.abs(nominal.hydrology.annualRainHarvestM3 - 66744) < 1e-3);
    assert.ok(nominal.hydrology.fullTankAutonomyDays >= 45 && nominal.hydrology.fullTankAutonomyDays <= 66);
    assert.equal(nominal.hydrology.avoidedPotableWaterM3Yr, 166500);

    const drought = computeSimulation(SCENARIO_PRESETS.dublin_drought.params);
    assert.equal(drought.hydrology.annualRainHarvestM3, 0);
    assert.ok(Math.abs(drought.hydrology.cisternVolumeM3 - 4200) < 1e-6);
  });

  it('matches Optical Latency, Autonomous Pod blind-travel, Financials & Survey KPIs', () => {
    const sim = computeSimulation(SCENARIO_PRESETS.nominal.params);
    assert.equal(sim.optical.activeLocalE2eRttUs, 143.2);
    assert.ok(Math.abs(sim.optical.localBlindTravelMm - 1.99) < 0.02);
    assert.equal(sim.optical.cloudTransportRttMs, 7.64);
    assert.equal(sim.optical.cloudControlLoopMs, 90.0);
    assert.ok(Math.abs(sim.optical.cloudBlindTravelM - 1.25) < 0.01);
    assert.equal(sim.optical.localBreakoutPct, 87.34);
    assert.equal(sim.optical.wanPowerSavedMWhYr, 455);

    assert.equal(sim.financial.CAPEX_TOTAL_M, 137.08);
    assert.equal(sim.financial.COUNTERFACTUAL_CAPEX_M, 215.59);
    assert.equal(sim.financial.EBITDA_ANNUAL_M, 22.20);
    assert.equal(sim.financial.SIMPLE_PAYBACK_YR, 6.2);
    assert.equal(sim.financial.IRR_PCT, 15.7);
    assert.equal(sim.financial.NPV_4PCT_M, 232.98);
    assert.equal(sim.financial.NPV_8PCT_M, 105.07);
    assert.equal(Math.round(sim.financial.totalAvoidedCo2TonYr), 23842);

    assert.equal(CONSTANTS.SURVEY.sampleSize, 109);
    assert.equal(CONSTANTS.SURVEY.dcComfortScore, 3.19);
    assert.equal(CONSTANTS.SURVEY.recycledWaterScore, 3.86);
    assert.equal(CONSTANTS.SURVEY.topConcernNoiseVibrationPct, 33.7);
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
