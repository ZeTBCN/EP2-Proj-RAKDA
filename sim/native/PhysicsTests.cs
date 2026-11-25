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
            RunTest("2. Nominal 2045 Design exact KPIs from RAKDA-USCI.docx", TestNominalKpis);
            RunTest("3. Cautious Base (u=0.70, f_cap=0.80) KPIs (130% gross / 120% net)", TestConservativeStress);
            RunTest("4. Winter Design Day (-3C) at 100% IT load (9.91 vs 9.80 MW = -0.11 MW)", TestWinterColdSnap);
            RunTest("5. Cautious Winter (-3C, u=0.70, f=0.80 -> 4.31 MW deficit) covered by store + HTHP", TestWinterStress60);
            RunTest("6. Vault 3 Fault + SDN Reroute around optical ring (70.1x vs EN 253)", TestVault3FaultSdn);
            RunTest("7. Summer Surplus 3-Tier Cascade energy conservation (no TI-PTES/TCES in base)", TestSummerExergyCascade);
            RunTest("8. Dublin hydrological loop (55,620 m3/yr, 44-day drought reserve at 6,700 m3)", TestHydrologyDrought);
            RunTest("9. Optical latency, Financials (Case 1 & 2, TCO) & Survey (N=105) KPIs", TestOpticalFinancialSurvey);
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
            AssertNear(sim.Thermal.AnnualGrossCoveragePct, 228.25, 0.3, "AnnualGrossCoveragePct");
            AssertNear(sim.Thermal.AnnualCoveragePct, 209.22, 0.3, "AnnualCoveragePct");

            AssertNear(sim.Thermal.PrimaryGrossFlowLs, 76.5, 0.05, "PrimaryGrossFlowLs");
            AssertNear(sim.Thermal.PrimaryNetHeatMw, 7.84, 1e-6, "PrimaryNetHeatMw");
            AssertNear(sim.Thermal.SecondaryGrossFlowLs, 32.0, 0.05, "SecondaryGrossFlowLs");

            AssertNear(sim.Thermal.DistrictDemandMw, 4.2936, 0.01, "DistrictDemandMw");
            AssertNear(sim.Thermal.AnnualisedDemandMWh, 37612.0, 5.0, "AnnualisedDemandMWh");
            AssertEqual(PhysicsEngine.RingLossWPerM, 13.0, "RingLossWPerM");
            AssertEqual(PhysicsEngine.RingLossMWhYr, 3421.0, "RingLossMWhYr");
            AssertEqual(PhysicsEngine.BufferTotalM3, 215.0, "BufferTotalM3");
            AssertEqual(PhysicsEngine.VaultLifeYr, 100, "VaultLifeYr");
        }

        private static void TestConservativeStress()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("conservative_stress").Params);
            AssertNear(sim.Electrical.TotalItMw, 7.0, 1e-6, "TotalItMw");
            AssertNear(sim.Thermal.CapturedHeatMw, 5.60, 1e-6, "CapturedHeatMw");
            AssertEqual((int)Math.Round(sim.Thermal.AnnualCapturedHeatMWh), 49056, "AnnualCapturedHeatMWh");
            AssertNear(sim.Thermal.AnnualGrossCoveragePct, 130.43, 0.2, "AnnualGrossCoveragePct");
            AssertNear(sim.Thermal.AnnualCoveragePct, 119.55, 0.5, "AnnualCoveragePct");
        }

        private static void TestWinterColdSnap()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("winter_cold_snap").Params);
            AssertNear(PhysicsEngine.ComputeDistrictDemandMw(-3.0), 9.52, 1e-6, "ComputeDistrictDemandMw(-3)");
            AssertNear(sim.Thermal.DistrictDemandMw, 9.52, 1e-6, "DistrictDemandMw");
            AssertNear(sim.Thermal.TotalNetworkDemandMw, 9.91, 1e-6, "TotalNetworkDemandMw");
            AssertNear(sim.Thermal.NetThermalBalanceMw, -0.11, 1e-6, "NetThermalBalanceMw");
            AssertNear(sim.Thermal.DeficitMw, 0.11, 1e-6, "DeficitMw");
            AssertEqual(sim.Thermal.UnmetThermalDeficitMw, 0.0, "UnmetThermalDeficitMw");
        }

        private static void TestWinterStress60()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("winter_stress_60").Params);
            AssertNear(sim.Thermal.CapturedHeatMw, 5.60, 1e-6, "CapturedHeatMw");
            AssertNear(sim.Thermal.DeficitMw, 4.31, 1e-6, "DeficitMw");
            AssertTrue(sim.Thermal.ThermalStoreDischargeMw > 0.0, "ThermalStoreDischargeMw > 0");
            AssertTrue(sim.Thermal.HthpThermalOutputMw > 0.0 && sim.Thermal.HthpThermalOutputMw <= 8.0, "HthpThermalOutputMw in (0, 8]");
            AssertTrue(sim.Thermal.HthpCop >= 4.0 && sim.Thermal.HthpCop <= 6.0, "HthpCop in [4.0, 6.0]");
            AssertEqual(sim.Thermal.UnmetThermalDeficitMw, 0.0, "UnmetThermalDeficitMw");
            AssertTrue(PhysicsEngine.ComputeHthpCop(25.0) <= 6.0 && PhysicsEngine.ComputeHthpCop(-3.0) >= 4.0, "ComputeHthpCop bounds");
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
            AssertEqual(simSdnOn.Optical.SdnEfficiencyFactor, 70.1, "SdnEfficiencyFactor");
            AssertTrue(simSdnOn.Thermal.RingPumpingKw < simSdnOff.Thermal.RingPumpingKw, "SDN reduces pumping power");
            AssertTrue(simSdnOn.Optical.ActiveLocalE2eRttUs < 160.0, "ActiveLocalE2eRttUs < 160");
        }

        private static void TestSummerExergyCascade()
        {
            SimResult sim = PhysicsEngine.Compute(PhysicsEngine.GetPreset("summer_exergy").Params);
            AssertNear(sim.Thermal.DistrictDemandMw, 3.01, 1e-6, "DistrictDemandMw");
            AssertNear(sim.Thermal.TotalNetworkDemandMw, 3.40, 1e-6, "TotalNetworkDemandMw");
            AssertNear(sim.Thermal.SurplusMw, 6.40, 1e-6, "SurplusMw");
            AssertEqual(sim.Thermal.TcesChargeMw, 0.0, "TcesChargeMw");
            AssertEqual(sim.Thermal.TiPtesChargeMw, 0.0, "TiPtesChargeMw");

            double cascadeSum =
                sim.Thermal.ThermalStoreChargeMw +
                sim.Thermal.CommercialHeatSalesMw +
                sim.Thermal.OrcThermalInputMw +
                sim.Thermal.DryCoolerRejectionMw;
            AssertNear(cascadeSum, sim.Thermal.SurplusMw, 1e-6, "Cascade energy conservation");
            AssertTrue(sim.Thermal.OrcOutputKw >= 300.0 && sim.Thermal.OrcOutputKw <= 420.0, "OrcOutputKw in [300, 420]");

            SimState extreme = PhysicsEngine.GetPreset("summer_exergy").Params.Clone();
            extreme.AmbientTempC = 30.0;
            extreme.ThermalStoreSoc = 1.0;
            SimResult extremeSim = PhysicsEngine.Compute(extreme);
            AssertTrue(extremeSim.Thermal.OrcOutputKw <= 420.0, "Extreme summer OrcOutputKw <= 420");
        }

        private static void TestHydrologyDrought()
        {
            SimResult nominal = PhysicsEngine.Compute(PhysicsEngine.GetPreset("nominal").Params);
            AssertNear(nominal.Hydrology.AnnualHarvestPer100kM3, 55620.0, 1e-3, "AnnualHarvestPer100kM3");
            AssertNear(nominal.Hydrology.AnnualRainHarvestM3, 55620.0, 1e-3, "AnnualRainHarvestM3");
            AssertEqual(PhysicsEngine.DroughtReserveDays, 44, "DroughtReserveDays");
            AssertEqual(PhysicsEngine.UpwMakeupMaxM3Yr, 3265.0, "UpwMakeupMaxM3Yr");
            AssertEqual(PhysicsEngine.AvoidedPotableMinM3Yr, 157680.0, "AvoidedPotableMinM3Yr");
            AssertEqual(PhysicsEngine.AvoidedPotableMaxM3Yr, 175200.0, "AvoidedPotableMaxM3Yr");

            SimResult drought = PhysicsEngine.Compute(PhysicsEngine.GetPreset("dublin_drought").Params);
            AssertEqual(drought.Hydrology.AnnualRainHarvestM3, 0.0, "Drought AnnualRainHarvestM3");
            AssertNear(drought.Hydrology.CisternVolumeM3, 6700.0, 1e-6, "Drought CisternVolumeM3");
            AssertTrue(drought.Hydrology.DroughtAutonomyDays >= 44.0 && drought.Hydrology.DroughtAutonomyDays <= 45.0, "Drought reserve ~44 d");
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

            AssertEqual(sim.Financial.CapexTotalM, 136.66, "CapexTotalM");
            AssertEqual(sim.Financial.CapexDistrictM, 57.31, "CapexDistrictM");
            AssertEqual(sim.Financial.CapexDcOperatorM, 79.35, "CapexDcOperatorM");
            AssertEqual(sim.Financial.ColocationRentEurKwMo, 145.0, "ColocationRentEurKwMo");
            AssertEqual(sim.Financial.EbitdaAnnualM, 19.60, "EbitdaAnnualM");
            AssertEqual(sim.Financial.SimplePaybackYr, 7.0, "SimplePaybackYr");
            AssertEqual(sim.Financial.IrrPct, 13.7, "IrrPct");
            AssertEqual(sim.Financial.Npv4PctM, 189.0, "Npv4PctM");
            AssertEqual(sim.Financial.Npv8PctM, 77.0, "Npv8PctM");
            AssertEqual(sim.Financial.UtilityIrrPct, 4.2, "UtilityIrrPct");
            AssertEqual(sim.Financial.UtilityLcohNetEurMwh, 93.0, "UtilityLcohNetEurMwh");
            AssertEqual(sim.Financial.TcoUsciM, 520.0, "TcoUsciM");
            AssertEqual(sim.Financial.BillUsciEurYr, 536.0, "BillUsciEurYr");

            AssertEqual(sim.Survey.SampleSize, 105, "Survey.SampleSize");
            AssertEqual(sim.Survey.DcComfortScore, 3.20, "Survey.DcComfortScore");
            AssertEqual(sim.Survey.RecycledWaterScore, 3.87, "Survey.RecycledWaterScore");
            AssertEqual(sim.Survey.TopConcernNoiseVibrationPct, 35.1, "Survey.TopConcernNoiseVibrationPct");
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
