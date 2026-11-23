using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RakdaSim
{
    internal static class PhysicsTests
    {
        private static int _passed;
        private static int _failed;

        private static int Main(string[] args)
        {
            RunTest("1. All 7 scenario presets produce finite outputs & 5 vaults", TestAllPresetsFinite);
            RunTest("2. Nominal 2045 Design exact KPIs from SPEC.md", TestNominalKpis);
            RunTest("3. Conservative Stress (u=0.70, f_cap=0.80) KPIs", TestConservativeStress);
            RunTest("4. Winter Cold Snap (-5C) at 100% IT load (+0.28 MW_th surplus)", TestWinterColdSnap);
            RunTest("5. Cold Snap + 60% Stress (-3.64 MW_th deficit) covered by store + HTHP", TestWinterStress60);
            RunTest("6. Vault 3 Fault + SDN Reroute around optical ring", TestVault3FaultSdn);
            RunTest("7. Summer Exergy Cascade energy conservation & 440 kW_e ORC cap", TestSummerExergyCascade);
            RunTest("8. Dublin hydrological loop & 60-day drought resilience", TestHydrologyDrought);
            RunTest("9. Optical latency, pod blind-travel, Financials & Survey KPIs", TestOpticalFinancialSurvey);
            RunTest("10. Inspector metadata & live metrics for all 16+ subsystem keys", TestSubsystemInspectors);

            Console.WriteLine();
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "PhysicsTests summary: {0} passed, {1} failed (total {2}).",
                _passed, _failed, _passed + _failed));

            return _failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action testAction)
        {
            try
            {
                testAction();
                _passed++;
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                Console.Error.WriteLine("[FAIL] " + name + " -> " + ex.Message);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertEqual<T>(T actual, T expected, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(actual, expected))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "{0}: expected {1}, got {2}", label, expected, actual));
            }
        }

        private static void AssertNear(double actual, double expected, double tol, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > tol)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "{0}: expected {1} (+/- {2}), got {3}", label, expected, tol, actual));
            }
        }

        private static void AssertAllFinite(object obj, string path)
        {
            if (obj == null) return;
            if (obj is double)
            {
                double d = (double)obj;
                AssertTrue(!double.IsNaN(d) && !double.IsInfinity(d), "Expected finite double at " + path);
                return;
            }
            if (obj is float)
            {
                float f = (float)obj;
                AssertTrue(!float.IsNaN(f) && !float.IsInfinity(f), "Expected finite float at " + path);
                return;
            }
            if (obj is string || obj.GetType().IsPrimitive) return;

            Array arr = obj as Array;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    AssertAllFinite(arr.GetValue(i), path + "[" + i + "]");
                }
                return;
            }

            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                AssertAllFinite(fields[i].GetValue(obj), path + "." + fields[i].Name);
            }
        }

        private static void TestAllPresetsFinite()
        {
            AssertEqual(PhysicsEngine.ScenarioList.Length, 7, "ScenarioList.Length");
            for (int i = 0; i < PhysicsEngine.ScenarioList.Length; i++)
            {
                ScenarioPreset preset = PhysicsEngine.ScenarioList[i];
                SimState st = preset.Params.Clone();
                st.ScenarioId = preset.Id;
                SimResult sim = PhysicsEngine.Compute(st);
                AssertAllFinite(sim, preset.Id);
                AssertEqual(sim.Vaults.Length, 5, preset.Id + ".Vaults.Length");
            }
        }

        private static void TestNominalKpis()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("nominal").Params);
            AssertEqual(sim.Electrical.TotalItMw, 10.0, "TotalItMw");
            AssertEqual(sim.Electrical.Pue, 1.015, "Pue");
            AssertEqual((int)Math.Round(sim.Electrical.AnnualGrossMWh), 88914, "AnnualGrossMWh");
            AssertEqual((int)Math.Round(sim.Electrical.AnnualNetGridMWh), 77958, "AnnualNetGridMWh");

            AssertNear(sim.Thermal.CapturedHeatMw, 9.80, 1e-6, "CapturedHeatMw");
            AssertEqual((int)Math.Round(sim.Thermal.AnnualCapturedHeatMWh), 85848, "AnnualCapturedHeatMWh");
            AssertNear(sim.Thermal.AnnualCoveragePct, 228.258, 0.1, "AnnualCoveragePct");

            AssertNear(sim.Thermal.PrimaryGrossFlowLmin, 4635.3, 0.05, "PrimaryGrossFlowLmin");
            AssertNear(sim.Thermal.PrimaryNetHeatMw, 7.84, 1e-6, "PrimaryNetHeatMw");
            AssertNear(sim.Thermal.PrimaryNetFlowLmin, 4543.0, 0.5, "PrimaryNetFlowLmin");
            AssertNear(sim.Thermal.SecondaryGrossFlowLmin, 1923.1, 0.05, "SecondaryGrossFlowLmin");

            AssertNear(sim.Thermal.DistrictDemandMw, 4.293, 0.01, "DistrictDemandMw");
            AssertNear(sim.Thermal.AnnualisedDemandMWh, 37610.0, 5.0, "AnnualisedDemandMWh");
        }

        private static void TestConservativeStress()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("conservative_stress").Params);
            AssertNear(sim.Electrical.TotalItMw, 7.0, 1e-6, "TotalItMw");
            AssertNear(sim.Thermal.CapturedHeatMw, 5.60, 1e-6, "CapturedHeatMw");
            AssertEqual((int)Math.Round(sim.Thermal.AnnualCapturedHeatMWh), 49056, "AnnualCapturedHeatMWh");
            AssertNear(sim.Thermal.AnnualCoveragePct, 130.43, 0.1, "AnnualCoveragePct");
        }

        private static void TestWinterColdSnap()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("winter_cold_snap").Params);
            AssertNear(PhysicsEngine.ComputeDistrictDemandMw(-5.0), 9.52, 1e-6, "ComputeDistrictDemandMw(-5)");
            AssertNear(sim.Thermal.DistrictDemandMw, 9.52, 1e-6, "DistrictDemandMw");
            AssertNear(sim.Thermal.NetThermalBalanceMw, 0.28, 1e-6, "NetThermalBalanceMw");
            AssertEqual(sim.Thermal.DeficitMw, 0.0, "DeficitMw");
        }

        private static void TestWinterStress60()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("winter_stress_60").Params);
            AssertNear(sim.Thermal.CapturedHeatMw, 5.88, 1e-6, "CapturedHeatMw");
            AssertNear(sim.Thermal.DeficitMw, 3.64, 1e-6, "DeficitMw");
            AssertTrue(sim.Thermal.ThermalStoreDischargeMw > 0.0, "ThermalStoreDischargeMw > 0");
            AssertTrue(sim.Thermal.HthpThermalOutputMw > 0.0 && sim.Thermal.HthpThermalOutputMw <= 8.0, "HthpThermalOutputMw in (0, 8]");
            AssertTrue(sim.Thermal.HthpCop >= 4.6 && sim.Thermal.HthpCop <= 6.9, "HthpCop in [4.6, 6.9]");
            AssertEqual(sim.Thermal.UnmetThermalDeficitMw, 0.0, "UnmetThermalDeficitMw");
            AssertTrue(PhysicsEngine.ComputeHthpCop(25.0) <= 6.9 && PhysicsEngine.ComputeHthpCop(-5.0) >= 4.6, "ComputeHthpCop bounds");
        }

        private static void TestVault3FaultSdn()
        {
            SimState stOn = PhysicsEngine.GetPreset("vault_fault_sdn").Params.Clone();
            SimState stOff = stOn.Clone();
            stOff.SdnEnabled = false;

            SimResult simSdnOn = PhysicsEngine.Compute(stOn);
            SimResult simSdnOff = PhysicsEngine.Compute(stOff);

            AssertEqual(simSdnOn.Vaults[2].Faulted, true, "Vault 3 Faulted");
            AssertEqual(simSdnOn.Vaults[2].ItMw, 0.0, "Vault 3 ItMw");
            AssertEqual(simSdnOn.Electrical.ActiveVaultCount, 4, "ActiveVaultCount");
            AssertNear(simSdnOn.Electrical.TotalItMw, 8.0, 1e-6, "TotalItMw");
            for (int i = 0; i < simSdnOn.Vaults.Length; i++)
            {
                AssertTrue(simSdnOn.Vaults[i].ItMw <= 2.0 + 1e-9, "Vault capacity cap");
            }
            AssertEqual(simSdnOn.Optical.SdnEfficiencyFactor, 74.9, "SdnEfficiencyFactor");
            AssertTrue(simSdnOn.Thermal.RingPumpingKw < simSdnOff.Thermal.RingPumpingKw, "SDN reduces pumping power");
            AssertTrue(simSdnOn.Optical.ActiveLocalE2eRttUs < 160.0, "ActiveLocalE2eRttUs < 160");
        }

        private static void TestSummerExergyCascade()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("summer_exergy").Params);
            AssertNear(sim.Thermal.DistrictDemandMw, 1.45, 1e-6, "DistrictDemandMw");
            AssertNear(sim.Thermal.SurplusMw, 8.35, 1e-6, "SurplusMw");

            double cascadeSum =
                sim.Thermal.ThermalStoreChargeMw +
                sim.Thermal.TcesChargeMw +
                sim.Thermal.TiPtesChargeMw +
                sim.Thermal.OrcThermalInputMw +
                sim.Thermal.MofChillerMw +
                sim.Thermal.DryCoolerRejectionMw;
            AssertNear(cascadeSum, sim.Thermal.SurplusMw, 1e-6, "Cascade energy conservation");
            AssertTrue(sim.Thermal.OrcOutputKw >= 418.0 && sim.Thermal.OrcOutputKw <= 440.0, "OrcOutputKw in [418, 440]");

            SimState extreme = PhysicsEngine.GetPreset("summer_exergy").Params.Clone();
            extreme.AmbientTempC = 30.0;
            extreme.ThermalStoreSoc = 1.0;
            SimResult extremeSim = PhysicsEngine.Compute(extreme);
            AssertTrue(extremeSim.Thermal.OrcOutputKw <= 440.0, "Extreme summer OrcOutputKw <= 440");
        }

        private static void TestHydrologyDrought()
        {
            SimResult nominal = PhysicsEngine.Compute(PhysicsEngine.GetPreset("nominal").Params);
            AssertNear(nominal.Hydrology.AnnualHarvestPer100kM3, 55620.0, 1e-3, "AnnualHarvestPer100kM3");
            AssertNear(nominal.Hydrology.AnnualRainHarvestM3, 66744.0, 1e-3, "AnnualRainHarvestM3");
            AssertTrue(nominal.Hydrology.FullTankAutonomyDays >= 45.0 && nominal.Hydrology.FullTankAutonomyDays <= 66.0, "FullTankAutonomyDays");
            AssertEqual(nominal.Hydrology.AvoidedPotableWaterM3Yr, 166500.0, "AvoidedPotableWaterM3Yr");

            SimResult drought = PhysicsEngine.Compute(PhysicsEngine.GetPreset("dublin_drought").Params);
            AssertEqual(drought.Hydrology.AnnualRainHarvestM3, 0.0, "Drought AnnualRainHarvestM3");
            AssertNear(drought.Hydrology.CisternVolumeM3, 4200.0, 1e-6, "Drought CisternVolumeM3");
        }

        private static void TestOpticalFinancialSurvey()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("nominal").Params);
            AssertEqual(sim.Optical.ActiveLocalE2eRttUs, 143.2, "ActiveLocalE2eRttUs");
            AssertNear(sim.Optical.LocalBlindTravelMm, 1.99, 0.02, "LocalBlindTravelMm");
            AssertEqual(sim.Optical.CloudTransportRttMs, 7.64, "CloudTransportRttMs");
            AssertEqual(sim.Optical.CloudControlLoopMs, 90.0, "CloudControlLoopMs");
            AssertNear(sim.Optical.CloudBlindTravelM, 1.25, 0.01, "CloudBlindTravelM");
            AssertEqual(sim.Optical.LocalBreakoutPct, 87.34, "LocalBreakoutPct");
            AssertEqual(sim.Optical.WanPowerSavedMWhYr, 455.0, "WanPowerSavedMWhYr");

            AssertEqual(sim.Financial.CapexTotalM, 137.08, "CapexTotalM");
            AssertEqual(sim.Financial.CounterfactualCapexM, 215.59, "CounterfactualCapexM");
            AssertEqual(sim.Financial.EbitdaAnnualM, 22.20, "EbitdaAnnualM");
            AssertEqual(sim.Financial.SimplePaybackYr, 6.2, "SimplePaybackYr");
            AssertEqual(sim.Financial.IrrPct, 15.7, "IrrPct");
            AssertEqual(sim.Financial.Npv4PctM, 232.98, "Npv4PctM");
            AssertEqual(sim.Financial.Npv8PctM, 105.07, "Npv8PctM");
            AssertEqual((int)Math.Round(sim.Financial.TotalAvoidedCo2TonYr), 23842, "TotalAvoidedCo2TonYr");

            AssertEqual(sim.Survey.SampleSize, 109, "Survey.SampleSize");
            AssertEqual(sim.Survey.DcComfortScore, 3.19, "Survey.DcComfortScore");
            AssertEqual(sim.Survey.RecycledWaterScore, 3.86, "Survey.RecycledWaterScore");
            AssertEqual(sim.Survey.TopConcernNoiseVibrationPct, 33.7, "Survey.TopConcernNoiseVibrationPct");
        }

        private static void TestSubsystemInspectors()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("nominal").Params);
            string[] keys = PhysicsEngine.GetAllSubsystemKeys();
            AssertTrue(keys.Length >= 16, "Expected >= 16 subsystem keys, got " + keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                SubsystemInfo info = PhysicsEngine.GetSubsystemInfo(key);
                AssertTrue(info != null, "Missing SubsystemInfo for " + key);
                AssertTrue(!string.IsNullOrEmpty(info.Title) && !string.IsNullOrEmpty(info.Category) && !string.IsNullOrEmpty(info.Description),
                    "Incomplete metadata for " + key);
                AssertTrue(info.Specs != null && info.Specs.Length > 0, "Missing specs for " + key);

                List<KeyValuePair<string, string>> metrics = info.GetLiveMetrics(sim);
                AssertTrue(metrics != null && metrics.Count > 0, "Missing metrics for " + key);
                for (int j = 0; j < metrics.Count; j++)
                {
                    AssertTrue(!string.IsNullOrEmpty(metrics[j].Key) && !string.IsNullOrEmpty(metrics[j].Value),
                        "Empty metric KV for " + key);
                }
            }
        }
    }
}
