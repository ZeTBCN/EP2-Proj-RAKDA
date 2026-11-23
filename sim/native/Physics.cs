using System;
using System.Collections.Generic;
using System.Globalization;

namespace RakdaSim
{
    public sealed class SimState
    {
        public string ScenarioId;
        public double Utilization;
        public double HeatCaptureEff;
        public double AmbientTempC;
        public double RainfallMultiplier;
        public double CisternSoc;
        public double ThermalStoreSoc;
        public bool SdnEnabled;
        public bool HthpScenarioB;
        public double TimeOfDay;
        public bool[] VaultFaults;

        public SimState()
        {
            ScenarioId = "nominal";
            Utilization = 1.0;
            HeatCaptureEff = 0.98;
            AmbientTempC = 10.0;
            RainfallMultiplier = 1.0;
            CisternSoc = 0.85;
            ThermalStoreSoc = 0.70;
            SdnEnabled = true;
            HthpScenarioB = false;
            TimeOfDay = 13.5;
            VaultFaults = new bool[5];
        }

        public SimState Clone()
        {
            SimState copy = new SimState();
            copy.ScenarioId = ScenarioId;
            copy.Utilization = Utilization;
            copy.HeatCaptureEff = HeatCaptureEff;
            copy.AmbientTempC = AmbientTempC;
            copy.RainfallMultiplier = RainfallMultiplier;
            copy.CisternSoc = CisternSoc;
            copy.ThermalStoreSoc = ThermalStoreSoc;
            copy.SdnEnabled = SdnEnabled;
            copy.HthpScenarioB = HthpScenarioB;
            copy.TimeOfDay = TimeOfDay;
            copy.VaultFaults = new bool[5];
            if (VaultFaults != null)
            {
                int len = Math.Min(5, VaultFaults.Length);
                for (int i = 0; i < len; i++)
                {
                    copy.VaultFaults[i] = VaultFaults[i];
                }
            }
            return copy;
        }
    }

    public sealed class ScenarioPreset
    {
        public string Id;
        public string Label;
        public string ShortLabel;
        public string Badge;
        public string Description;
        public SimState Params;
    }

    public sealed class VaultTelemetry
    {
        public int Id;
        public string Name;
        public string InspectKey;
        public bool Faulted;
        public double ItMw;
        public double HeatMw;
        public double LoadPct;
        public double ZoneWeight;
        public double BufferM3;
        public double FlowPrimaryLmin;
        public double FlowSecondaryLmin;
    }

    public sealed class ElectricalTelemetry
    {
        public double TotalItMw;
        public double RequestedItMw;
        public int ActiveVaultCount;
        public int FaultedVaultCount;
        public double Pue;
        public double GrossFacilityMw;
        public double AnnualGrossMWh;
        public double PvOutputMw;
        public double AnnualPvMWh;
        public double AnnualNetGridMWh;
        public double BessChargeMw;
        public double BessDischargeMw;
        public string BessMode;
        public double NetGridDrawMw;
    }

    public sealed class ThermalTelemetry
    {
        public double HeatCaptureEff;
        public double CapturedHeatMw;
        public double AnnualCapturedHeatMWh;
        public double AnnualCoveragePct;
        public double PrimaryGrossFlowLmin;
        public double PrimaryGrossFlowLs;
        public double PrimaryNetHeatMw;
        public double PrimaryNetFlowLmin;
        public double SecondaryGrossFlowLmin;
        public double SecondaryGrossFlowLs;
        public double SecondaryNetHeatMw;
        public double SecondaryNetFlowLmin;
        public double DistrictDemandMw;
        public double AnnualisedDemandMWh;
        public double NetThermalBalanceMw;
        public double SurplusMw;
        public double DeficitMw;
        public double ThermalStoreSoc;
        public double ThermalStoreEnergyMWh;
        public double ThermalStoreChargeMw;
        public double ThermalStoreDischargeMw;
        public double TcesChargeMw;
        public double TiPtesChargeMw;
        public double TiPtesElectricEqMw;
        public double OrcThermalInputMw;
        public double OrcOutputKw;
        public double MofChillerMw;
        public double MofCoolingMw;
        public double DryCoolerRejectionMw;
        public double HthpThermalOutputMw;
        public double HthpElectricalMw;
        public double HthpCop;
        public bool ScenarioBActive;
        public double UnmetThermalDeficitMw;
        public double RingPumpingKw;
    }

    public sealed class HydrologyTelemetry
    {
        public double RainfallMultiplier;
        public double EffectiveRainfallMmYr;
        public double AnnualHarvestPer100kM3;
        public double AnnualRainHarvestM3;
        public double DailyHarvestM3;
        public double CisternSoc;
        public double CisternVolumeM3;
        public double CisternCapacityM3;
        public double DroughtAutonomyDays;
        public double FullTankAutonomyDays;
        public double AvoidedPotableWaterM3Yr;
    }

    public sealed class OpticalTelemetry
    {
        public bool SdnEnabled;
        public double SdnEfficiencyFactor;
        public double SdnShiftedMw;
        public double ReroutedFromFaultMw;
        public double InterVaultRttUs;
        public double ActiveLocalE2eRttUs;
        public double LocalBlindTravelMm;
        public double CloudTransportRttMs;
        public double CloudControlLoopMs;
        public double CloudBlindTravelM;
        public double LocalBreakoutPct;
        public double WanPowerSavedMWhYr;
    }

    public sealed class FinancialTelemetry
    {
        public double CapexTotalM;
        public double CapexDistrictM;
        public double CapexDcOperatorM;
        public double CapexCruPvBessM;
        public double CounterfactualCapexM;
        public double CapexSavingsM;
        public double RevenueAnnualM;
        public double OpexAnnualM;
        public double EbitdaAnnualM;
        public double Repex30YrM;
        public double SimplePaybackYr;
        public double IrrPct;
        public double Npv4PctM;
        public double DiscountedPayback4PctYr;
        public double Npv8PctM;
        public double DiscountedPayback8PctYr;
        public double AvoidedHeatingCo2TonYr;
        public double AvoidedPueCo2TonYr;
        public double AvoidedPueEnergyMWhYr;
        public double TotalAvoidedCo2TonYr;
    }

    public sealed class SurveyStats
    {
        public int SampleSize;
        public double DcComfortScore;
        public double DcComfortPosNeutralPct;
        public double RecycledWaterScore;
        public double RecycledWaterPosPct;
        public double PriorityCheapestBillPct;
        public double PriorityReliabilityPct;
        public double PriorityCombinedPct;
        public double TopConcernNoiseVibrationPct;
    }

    public sealed class SimResult
    {
        public SimState Inputs;
        public VaultTelemetry[] Vaults;
        public ElectricalTelemetry Electrical;
        public ThermalTelemetry Thermal;
        public HydrologyTelemetry Hydrology;
        public OpticalTelemetry Optical;
        public FinancialTelemetry Financial;
        public SurveyStats Survey;
    }

    public sealed class SubsystemInfo
    {
        public string Key;
        public string Title;
        public string Category;
        public string Description;
        public string[] Specs;
        private readonly Func<SimResult, List<KeyValuePair<string, string>>> _metricsFactory;

        public SubsystemInfo(
            string key,
            string title,
            string category,
            string description,
            string[] specs,
            Func<SimResult, List<KeyValuePair<string, string>>> metricsFactory)
        {
            Key = key;
            Title = title;
            Category = category;
            Description = description;
            Specs = specs;
            _metricsFactory = metricsFactory;
        }

        public List<KeyValuePair<string, string>> GetLiveMetrics(SimResult sim)
        {
            if (_metricsFactory == null)
            {
                return new List<KeyValuePair<string, string>>();
            }
            return _metricsFactory(sim);
        }
    }

    public static class PhysicsEngine
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // District constants (Draft 3.2)
        public const double DistrictAreaHa = 180.0;
        public const int DistrictResidents = 20000;
        public const int DistrictHomes = 7500;
        public const int DistrictBuildings = 3095;
        public const double ResidentialHa = 90.0;
        public const double DensityDph = 83.3;
        public const double RoadsHa = 36.0;
        public const double ParksHa = 25.0;
        public const double SchoolsHa = 15.0;
        public const double RetailCivicHa = 14.0;

        // Data Centre constants
        public const int VaultCount = 5;
        public const double VaultCapacityMw = 2.0;
        public const double TotalItCapacityMw = 10.0;
        public const double VaultFootprintM2 = 350.0;
        public const double HoursPerYear = 8760.0;
        public const double Pue = 1.015;
        public const double CounterfactualPue = 1.56;
        public const double PvPeakMw = 12.0;
        public const double PvAnnualMWh = 10956.0;
        public const double BessPowerMw = 8.0;
        public const double BessEnergyMWh = 32.0;
        public const double BufferPerVaultM3 = 43.4;
        public const double BufferTotalM3 = 217.0;
        public const double BufferDtK = 10.0;
        public const double WaterMistDbMax = 110.0;
        public static readonly double[] VaultZoneWeights = new double[] { 0.24, 0.22, 0.20, 0.18, 0.16 };

        // Thermal constants
        public const double PrimaryTempC = 75.0;
        public const double PrimaryDtK = 25.0;
        public const double PrimaryGrossShare = 0.80;
        public const double PrimaryNominalGrossLmin = 4635.3;
        public const double PrimaryNominalGrossLs = 77.26;
        public const double SecondaryTempC = 45.0;
        public const double SecondaryDtK = 15.0;
        public const double SecondaryGrossShare = 0.20;
        public const double SecondaryNominalGrossLmin = 1923.1;
        public const double SecondaryNominalGrossLs = 32.05;
        public const double AnnualDistrictDemandMWh = 37610.0;
        public const double AnnualDistrictDemandExactMWh = 37612.0;
        public const double ColdSnapPeakMw = 9.52;
        public const double SummerDhwBaselineMw = 1.45;
        public const double BalanceTempC = 18.16;
        public static readonly double SpaceHeatSlopeMwPerK = (9.52 - 1.45) / (18.16 - (-5.0));
        public const double ThermalStoreM3 = 2500.0;
        public const double ThermalStoreDtK = 30.0;
        public const double ThermalStoreMWh = 87.08;
        public const double HthpMaxMwTh = 8.0;
        public const double HthpCopMin = 4.6;
        public const double HthpCopMax = 6.9;
        public const double RingLengthKm = 30.0;
        public const double RingLossWPerM = 7.0;
        public const double RingLossMWhYr = 1840.0;
        public const double PrimaryPumpingKw = 22.1;
        public const double TcesCapacityMWhTh = 35.0;
        public const double TiPtesPowerMwE = 2.0;
        public const double TiPtesEnergyMWhE = 16.0;
        public const double OrcMaxKwE = 440.0;
        public const double OrcCeilingMWhYr = 2412.0;
        public const double OrcPracticalMWhYr = 2000.0;
        public const double SummerSurplusMWhTh = 30148.0;

        // Hydrology constants
        public const double DublinRainfallMmYr = 772.5;
        public const double RunoffCoeff = 0.80;
        public const double FiltrationEff = 0.90;
        public const double CatchmentM2 = 120000.0;
        public const double CisternCapacityM3 = 10000.0;
        public const double DailyWaterDemandM3 = 160.0;
        public const double AvoidedPotableWaterM3Yr = 166500.0;
        public const double AvoidedPotableMinM3Yr = 158000.0;
        public const double AvoidedPotableMaxM3Yr = 175000.0;
        public const double UltrapureConductivityMaxUsCm = 1.0;

        // Optical constants
        public const double OpticalRingKm = 3.0;
        public const double InterVaultHopKm = 1.0;
        public const double InterVaultRttUs = 8.16;
        public const double InterVaultJitterUs = 1.2;
        public const double VaultToPodKm = 0.25;
        public const double VaultToPodNetRttUs = 3.17;
        public const double LocalE2eRttUs = 143.2;
        public const double LocalE2eJitterUs = 2.0;
        public const double CloudDistanceKm = 75.0;
        public const double CloudTransportRttMs = 7.64;
        public const double CloudControlLoopMs = 90.0;
        public const double PodSpeedKmh = 50.0;
        public const double LocalBreakoutPct = 87.34;
        public const double WanPowerSavedMWhYr = 455.0;
        public const double SdnEfficiencyFactor = 74.9;

        // Financial constants
        public const double CapexTotalM = 137.08;
        public const double CapexDistrictM = 57.73;
        public const double CapexDcOperatorM = 79.35;
        public const double CapexCruPvBessM = 15.30;
        public const double CounterfactualCapexM = 215.59;
        public const double CapexSavingsM = 78.51;
        public const double RevenueAnnualM = 34.20;
        public const double OpexAnnualM = 12.00;
        public const double EbitdaAnnualM = 22.20;
        public const double Repex30YrM = 25.70;
        public const double SimplePaybackYr = 6.2;
        public const double IrrPct = 15.7;
        public const double Npv4PctM = 232.98;
        public const double DiscountedPayback4PctYr = 7.2;
        public const double Npv8PctM = 105.07;
        public const double DiscountedPayback8PctYr = 8.9;
        public const double AvoidedHeatingCo2TonYr = 8020.0;
        public const double AvoidedPueCo2TonYr = 15822.0;
        public const double AvoidedPueEnergyMWhYr = 47742.0;
        public const double TotalAvoidedCo2TonYr = 23842.0;

        // Survey constants
        public static readonly SurveyStats SurveyData = new SurveyStats
        {
            SampleSize = 109,
            DcComfortScore = 3.19,
            DcComfortPosNeutralPct = 64.8,
            RecycledWaterScore = 3.86,
            RecycledWaterPosPct = 72.0,
            PriorityCheapestBillPct = 41.8,
            PriorityReliabilityPct = 31.6,
            PriorityCombinedPct = 73.5,
            TopConcernNoiseVibrationPct = 33.7
        };

        public static readonly ScenarioPreset[] ScenarioList;
        private static readonly Dictionary<string, ScenarioPreset> ScenarioMap;
        private static readonly Dictionary<string, SubsystemInfo> SubsystemMap;
        private static readonly string[] SubsystemKeyList;

        static PhysicsEngine()
        {
            ScenarioList = BuildScenarios();
            ScenarioMap = new Dictionary<string, ScenarioPreset>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ScenarioList.Length; i++)
            {
                ScenarioMap[ScenarioList[i].Id] = ScenarioList[i];
            }

            SubsystemMap = BuildSubsystems();
            SubsystemKeyList = new string[SubsystemMap.Count];
            SubsystemMap.Keys.CopyTo(SubsystemKeyList, 0);
        }

        private static ScenarioPreset[] BuildScenarios()
        {
            return new ScenarioPreset[]
            {
                new ScenarioPreset
                {
                    Id = "nominal",
                    Label = "Nominal 2045 Design",
                    ShortLabel = "Nominal 2045",
                    Badge = "100% IT · 10°C",
                    Description = "Full 10.0 MW IT load at 98% capture, Dublin mean 10°C ambient, 228.3% annual heat coverage.",
                    Params = new SimState
                    {
                        ScenarioId = "nominal",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = 10.0,
                        RainfallMultiplier = 1.0,
                        CisternSoc = 0.85,
                        ThermalStoreSoc = 0.70,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = false,
                        TimeOfDay = 13.5
                    }
                },
                new ScenarioPreset
                {
                    Id = "winter_cold_snap",
                    Label = "Winter Cold Snap (−5°C)",
                    ShortLabel = "Cold Snap −5°C",
                    Badge = "100% IT · −5°C",
                    Description = "Peak winter heating demand (9.52 MW_th) met directly by 9.80 MW_th captured waste heat (+0.28 MW_th surplus).",
                    Params = new SimState
                    {
                        ScenarioId = "winter_cold_snap",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = -5.0,
                        RainfallMultiplier = 1.2,
                        CisternSoc = 0.90,
                        ThermalStoreSoc = 0.85,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = false,
                        TimeOfDay = 17.5
                    }
                },
                new ScenarioPreset
                {
                    Id = "winter_stress_60",
                    Label = "Cold Snap + 60% Stress",
                    ShortLabel = "60% Stress −5°C",
                    Badge = "60% IT · HTHP On",
                    Description = "Severe winter −5°C with DC throttled to 60% (5.88 MW_th). 2,500 m³ thermal store + 8.0 MW_th HTHP cover −3.64 MW_th deficit.",
                    Params = new SimState
                    {
                        ScenarioId = "winter_stress_60",
                        Utilization = 0.60,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = -5.0,
                        RainfallMultiplier = 1.1,
                        CisternSoc = 0.88,
                        ThermalStoreSoc = 0.80,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = true,
                        TimeOfDay = 19.0
                    }
                },
                new ScenarioPreset
                {
                    Id = "vault_fault_sdn",
                    Label = "Vault 3 Fault + SDN Reroute",
                    ShortLabel = "Vault 3 Fault",
                    Badge = "V3 Fault · SDN",
                    Description = "Vault 3 offline. Optical NANF HCF ring reroutes V2X traffic and SDN shifts 8.0 MW IT load across healthy vaults (74.9× vs pumping).",
                    Params = new SimState
                    {
                        ScenarioId = "vault_fault_sdn",
                        Utilization = 0.80,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = 4.0,
                        RainfallMultiplier = 1.0,
                        CisternSoc = 0.82,
                        ThermalStoreSoc = 0.72,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, true, false, false },
                        HthpScenarioB = false,
                        TimeOfDay = 14.0
                    }
                },
                new ScenarioPreset
                {
                    Id = "summer_exergy",
                    Label = "Summer Exergy Cascade",
                    ShortLabel = "Summer Exergy",
                    Badge = "24°C · ORC + TCES",
                    Description = "Low DHW baseline (1.45 MW_th) leaves 8.35 MW_th surplus routed to SrBr₂ TCES, TI-PTES Carnot battery, ~422 kW_e micro-ORC, and MOF chillers.",
                    Params = new SimState
                    {
                        ScenarioId = "summer_exergy",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = 24.0,
                        RainfallMultiplier = 0.6,
                        CisternSoc = 0.75,
                        ThermalStoreSoc = 0.95,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = false,
                        TimeOfDay = 13.0
                    }
                },
                new ScenarioPreset
                {
                    Id = "dublin_drought",
                    Label = "Dublin 60-Day Drought",
                    ShortLabel = "Dublin Drought",
                    Badge = "0× Rain · Cistern",
                    Description = "Zero rainfall input; closed-loop microfluidic cooling & 10,000 m³ RC drought cistern sustain operations for 45–66 dry days.",
                    Params = new SimState
                    {
                        ScenarioId = "dublin_drought",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = 21.0,
                        RainfallMultiplier = 0.0,
                        CisternSoc = 0.42,
                        ThermalStoreSoc = 0.85,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = false,
                        TimeOfDay = 15.0
                    }
                },
                new ScenarioPreset
                {
                    Id = "conservative_stress",
                    Label = "Conservative (u=0.70, f=0.80)",
                    ShortLabel = "Conservative",
                    Badge = "u=0.70 · f=0.80",
                    Description = "Conservative stress margin: 70% IT load and 80% heat capture yield 49,056 MWh_th/yr (130.4% of 37,610 MWh_th/yr district demand).",
                    Params = new SimState
                    {
                        ScenarioId = "conservative_stress",
                        Utilization = 0.70,
                        HeatCaptureEff = 0.80,
                        AmbientTempC = 10.0,
                        RainfallMultiplier = 0.85,
                        CisternSoc = 0.78,
                        ThermalStoreSoc = 0.65,
                        SdnEnabled = true,
                        VaultFaults = new bool[] { false, false, false, false, false },
                        HthpScenarioB = true,
                        TimeOfDay = 11.0
                    }
                }
            };
        }

        public static ScenarioPreset GetPreset(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ScenarioPreset preset;
            if (ScenarioMap.TryGetValue(id, out preset))
            {
                return preset;
            }
            return null;
        }

        public static SimState CreateDefaultState()
        {
            return ScenarioMap["nominal"].Params.Clone();
        }

        public static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static bool IsFinite(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        public static double ComputeDistrictDemandMw(double ambientTempC)
        {
            double t = IsFinite(ambientTempC) ? ambientTempC : 10.0;
            double heatingDeltaK = Math.Max(0.0, BalanceTempC - t);
            return SummerDhwBaselineMw + heatingDeltaK * SpaceHeatSlopeMwPerK;
        }

        public static double ComputeHthpCop(double ambientTempC)
        {
            double t = Clamp(IsFinite(ambientTempC) ? ambientTempC : 10.0, -5.0, 25.0);
            double ratio = (t + 5.0) / 30.0;
            return Clamp(HthpCopMin + ratio * (HthpCopMax - HthpCopMin), HthpCopMin, HthpCopMax);
        }

        private sealed class VaultAllocationResult
        {
            public VaultTelemetry[] Vaults;
            public double TotalItMw;
            public int ActiveVaultCount;
            public int FaultedVaultCount;
            public double ReroutedFromFaultMw;
            public double SdnShiftedMw;
        }

        private static VaultAllocationResult AllocateVaultLoads(double requestedItMw, bool[] vaultFaults, bool sdnEnabled, double heatCaptureEff)
        {
            bool[] faults = new bool[VaultCount];
            List<int> activeIndices = new List<int>(VaultCount);
            int faultCount = 0;

            for (int i = 0; i < VaultCount; i++)
            {
                bool f = (vaultFaults != null && i < vaultFaults.Length) ? vaultFaults[i] : false;
                faults[i] = f;
                if (f) faultCount++;
                else activeIndices.Add(i);
            }

            double maxAvailableMw = activeIndices.Count * VaultCapacityMw;
            double totalItMw = Clamp(requestedItMw, 0.0, maxAvailableMw);
            double[] loads = new double[VaultCount];

            if (activeIndices.Count > 0 && totalItMw > 0.0)
            {
                if (!sdnEnabled)
                {
                    double equalShare = totalItMw / activeIndices.Count;
                    for (int k = 0; k < activeIndices.Count; k++)
                    {
                        int idx = activeIndices[k];
                        loads[idx] = Math.Min(VaultCapacityMw, equalShare);
                    }
                }
                else
                {
                    double remaining = totalItMw;
                    List<int> open = new List<int>(activeIndices);
                    while (open.Count > 0 && remaining > 1e-9)
                    {
                        double weightSum = 0.0;
                        for (int k = 0; k < open.Count; k++)
                        {
                            weightSum += VaultZoneWeights[open[k]];
                        }

                        bool cappedAny = false;
                        double nextRemaining = remaining;
                        List<int> toRemove = new List<int>();

                        for (int k = 0; k < open.Count; k++)
                        {
                            int idx = open[k];
                            double share = nextRemaining * (VaultZoneWeights[idx] / weightSum);
                            double headroom = VaultCapacityMw - loads[idx];
                            if (share >= headroom - 1e-9)
                            {
                                loads[idx] += headroom;
                                remaining -= headroom;
                                toRemove.Add(idx);
                                cappedAny = true;
                            }
                        }

                        for (int k = 0; k < toRemove.Count; k++)
                        {
                            open.Remove(toRemove[k]);
                        }

                        if (!cappedAny)
                        {
                            for (int k = 0; k < open.Count; k++)
                            {
                                int idx = open[k];
                                loads[idx] += remaining * (VaultZoneWeights[idx] / weightSum);
                            }
                            remaining = 0.0;
                        }
                    }
                }
            }

            double equalIfActive = activeIndices.Count > 0 ? totalItMw / activeIndices.Count : 0.0;
            double sdnShiftedMw = 0.0;
            for (int i = 0; i < VaultCount; i++)
            {
                if (!faults[i] && sdnEnabled && loads[i] > equalIfActive)
                {
                    sdnShiftedMw += loads[i] - equalIfActive;
                }
            }

            double nominalPerVault = requestedItMw / VaultCount;
            double reroutedFromFaultMw = Math.Min(totalItMw, faultCount * nominalPerVault);

            VaultTelemetry[] vaults = new VaultTelemetry[VaultCount];
            for (int idx = 0; idx < VaultCount; idx++)
            {
                double itMw = loads[idx];
                double heatMw = itMw * heatCaptureEff;
                double shareOfCap = itMw / VaultCapacityMw;
                vaults[idx] = new VaultTelemetry
                {
                    Id = idx,
                    Name = "Vault " + (idx + 1).ToString(Inv),
                    InspectKey = "vault_" + idx.ToString(Inv),
                    Faulted = faults[idx],
                    ItMw = itMw,
                    HeatMw = heatMw,
                    LoadPct = shareOfCap * 100.0,
                    ZoneWeight = VaultZoneWeights[idx],
                    BufferM3 = BufferPerVaultM3,
                    FlowPrimaryLmin = (PrimaryNominalGrossLmin / VaultCount) * shareOfCap,
                    FlowSecondaryLmin = (SecondaryNominalGrossLmin / VaultCount) * shareOfCap
                };
            }

            return new VaultAllocationResult
            {
                Vaults = vaults,
                TotalItMw = totalItMw,
                ActiveVaultCount = activeIndices.Count,
                FaultedVaultCount = faultCount,
                ReroutedFromFaultMw = reroutedFromFaultMw,
                SdnShiftedMw = sdnShiftedMw
            };
        }

        public static SimResult Compute(SimState inputState)
        {
            SimState src = inputState != null ? inputState : CreateDefaultState();
            SimState s = new SimState();
            s.ScenarioId = string.IsNullOrEmpty(src.ScenarioId) ? "nominal" : src.ScenarioId;
            s.Utilization = Clamp(IsFinite(src.Utilization) ? src.Utilization : 1.0, 0.1, 1.0);
            s.HeatCaptureEff = Clamp(IsFinite(src.HeatCaptureEff) ? src.HeatCaptureEff : 0.98, 0.70, 0.99);
            s.AmbientTempC = Clamp(IsFinite(src.AmbientTempC) ? src.AmbientTempC : 10.0, -10.0, 35.0);
            s.RainfallMultiplier = Clamp(IsFinite(src.RainfallMultiplier) ? src.RainfallMultiplier : 1.0, 0.0, 2.5);
            s.CisternSoc = Clamp(IsFinite(src.CisternSoc) ? src.CisternSoc : 0.85, 0.05, 1.0);
            s.ThermalStoreSoc = Clamp(IsFinite(src.ThermalStoreSoc) ? src.ThermalStoreSoc : 0.70, 0.0, 1.0);
            s.SdnEnabled = src.SdnEnabled;
            s.HthpScenarioB = src.HthpScenarioB;
            double rawTod = IsFinite(src.TimeOfDay) ? src.TimeOfDay : 13.5;
            s.TimeOfDay = ((rawTod % 24.0) + 24.0) % 24.0;
            s.VaultFaults = new bool[VaultCount];
            if (src.VaultFaults != null)
            {
                int len = Math.Min(VaultCount, src.VaultFaults.Length);
                for (int i = 0; i < len; i++) s.VaultFaults[i] = src.VaultFaults[i];
            }

            // 1. Vault allocation & SDN
            double requestedItMw = s.Utilization * TotalItCapacityMw;
            VaultAllocationResult alloc = AllocateVaultLoads(requestedItMw, s.VaultFaults, s.SdnEnabled, s.HeatCaptureEff);
            double totalItMw = alloc.TotalItMw;
            double loadRatio = totalItMw / TotalItCapacityMw;

            // 2. Electrical & BESS
            double grossFacilityMw = totalItMw * Pue;
            double annualGrossMWh = grossFacilityMw * HoursPerYear;
            double daylightPhase = Clamp((s.TimeOfDay - 6.0) / 12.0, 0.0, 1.0);
            double solarElev = (s.TimeOfDay >= 6.0 && s.TimeOfDay <= 18.0) ? Math.Sin(daylightPhase * Math.PI) : 0.0;
            double seasonSolarFactor = Clamp(0.45 + (s.AmbientTempC + 5.0) / 45.0, 0.38, 1.0);
            double rainCloudFactor = Clamp(1.0 - s.RainfallMultiplier * 0.18, 0.55, 1.0);
            double pvOutputMw = PvPeakMw * solarElev * seasonSolarFactor * rainCloudFactor;
            double annualPvMWh = PvAnnualMWh;
            double annualNetGridMWh = Math.Max(0.0, annualGrossMWh - annualPvMWh);

            double bessChargeMw = 0.0;
            double bessDischargeMw = 0.0;
            string bessMode = "Standby";
            if (pvOutputMw > 6.5)
            {
                bessChargeMw = Math.Min(4.0, (pvOutputMw - 6.0) * 0.8);
                bessMode = "Charging (Solar)";
            }
            else if ((s.TimeOfDay >= 17.0 && s.TimeOfDay <= 21.0) || s.AmbientTempC <= -2.0 || alloc.FaultedVaultCount > 0)
            {
                bessDischargeMw = Math.Min(BessPowerMw, grossFacilityMw * 0.35 + (s.AmbientTempC <= -2.0 ? 1.5 : 0.0));
                bessMode = "Discharging (Peak Shave)";
            }

            // 3. Thermal & Hydraulic Flows
            double capturedHeatMw = totalItMw * s.HeatCaptureEff;
            double annualCapturedHeatMWh = capturedHeatMw * HoursPerYear;
            double annualCoveragePct = (annualCapturedHeatMWh / AnnualDistrictDemandMWh) * 100.0;

            double primaryGrossFlowLmin = PrimaryNominalGrossLmin * loadRatio;
            double primaryGrossFlowLs = primaryGrossFlowLmin / 60.0;
            double primaryNetHeatMw = capturedHeatMw * PrimaryGrossShare;
            double primaryNetFlowLmin = primaryGrossFlowLmin * s.HeatCaptureEff;

            double secondaryGrossFlowLmin = SecondaryNominalGrossLmin * loadRatio;
            double secondaryGrossFlowLs = secondaryGrossFlowLmin / 60.0;
            double secondaryNetHeatMw = capturedHeatMw * SecondaryGrossShare;
            double secondaryNetFlowLmin = secondaryGrossFlowLmin * s.HeatCaptureEff;

            double districtDemandMw = ComputeDistrictDemandMw(s.AmbientTempC);
            double annualisedDemandMWh = districtDemandMw * HoursPerYear;
            double netThermalBalanceMw = capturedHeatMw - districtDemandMw;
            double ringPumpingKw = PrimaryPumpingKw * loadRatio * (s.SdnEnabled ? 1.0 : 1.45);

            // 4. Surplus Exergy Cascade vs Cold-Snap Deficit Coverage
            double surplusMw = 0.0;
            double deficitMw = 0.0;
            double thermalStoreChargeMw = 0.0;
            double thermalStoreDischargeMw = 0.0;
            double tcesChargeMw = 0.0;
            double tiPtesChargeMw = 0.0;
            double tiPtesElectricEqMw = 0.0;
            double orcThermalInputMw = 0.0;
            double orcOutputKw = 0.0;
            double mofChillerMw = 0.0;
            double mofCoolingMw = 0.0;
            double dryCoolerRejectionMw = 0.0;
            double hthpThermalOutputMw = 0.0;
            double hthpElectricalMw = 0.0;
            double unmetThermalDeficitMw = 0.0;
            double hthpCop = ComputeHthpCop(s.AmbientTempC);

            if (netThermalBalanceMw >= 0.0)
            {
                surplusMw = netThermalBalanceMw;
                thermalStoreChargeMw = s.ThermalStoreSoc < 0.95 ? Math.Min(surplusMw, 1.0) : 0.0;
                double rem1 = surplusMw - thermalStoreChargeMw;
                tcesChargeMw = Math.Min(rem1, 1.20);
                double rem2 = rem1 - tcesChargeMw;
                tiPtesChargeMw = Math.Min(rem2, 2.00);
                tiPtesElectricEqMw = Math.Min(TiPtesPowerMwE, tiPtesChargeMw * 0.70);
                double rem3 = rem2 - tiPtesChargeMw;
                orcThermalInputMw = Math.Min(rem3 * 0.78, 4.19);
                orcOutputKw = Math.Min(OrcMaxKwE, orcThermalInputMw * 1000.0 * 0.105);
                double rem4 = rem3 - orcThermalInputMw;
                mofChillerMw = s.AmbientTempC > 18.0 ? Math.Min(rem4, 0.85) : 0.0;
                mofCoolingMw = mofChillerMw * 0.72;
                dryCoolerRejectionMw = Math.Max(0.0, rem4 - mofChillerMw);
            }
            else
            {
                deficitMw = -netThermalBalanceMw;
                thermalStoreDischargeMw = s.ThermalStoreSoc > 0.05 ? Math.Min(deficitMw * 0.45, 4.0) : 0.0;
                hthpThermalOutputMw = Math.Min(HthpMaxMwTh, Math.Max(0.0, deficitMw - thermalStoreDischargeMw));
                hthpElectricalMw = hthpThermalOutputMw / hthpCop;
                unmetThermalDeficitMw = Math.Max(0.0, deficitMw - thermalStoreDischargeMw - hthpThermalOutputMw);
            }

            double netGridDrawMw = Math.Max(
                0.0,
                grossFacilityMw + hthpElectricalMw + ringPumpingKw / 1000.0 - pvOutputMw - orcOutputKw / 1000.0 - bessDischargeMw + bessChargeMw
            );

            // 5. Hydrological Loop
            double annualHarvestPer100kM3 =
                DublinRainfallMmYr * s.RainfallMultiplier * 0.001 * 100000.0 * RunoffCoeff * FiltrationEff;
            double annualRainHarvestM3 =
                DublinRainfallMmYr * s.RainfallMultiplier * 0.001 * CatchmentM2 * RunoffCoeff * FiltrationEff;
            double dailyHarvestM3 = annualRainHarvestM3 / 365.0;
            double cisternVolumeM3 = CisternCapacityM3 * s.CisternSoc;
            double droughtAutonomyDays = cisternVolumeM3 / DailyWaterDemandM3;
            double fullTankAutonomyDays = CisternCapacityM3 / DailyWaterDemandM3;

            // 6. Optical Ring & Autonomous Pod Safety
            double activeLocalE2eRttUs =
                alloc.FaultedVaultCount > 0
                    ? LocalE2eRttUs + InterVaultRttUs * alloc.FaultedVaultCount
                    : LocalE2eRttUs;
            double podSpeedMs = PodSpeedKmh / 3.6;
            double localBlindTravelMm = podSpeedMs * (activeLocalE2eRttUs * 1e-6) * 1000.0;
            double cloudBlindTravelM = podSpeedMs * (CloudControlLoopMs / 1000.0);
            double wanPowerSavedMWhYr = WanPowerSavedMWhYr * loadRatio;

            // 7. Carbon & Financials
            double avoidedHeatingCo2TonYr =
                AvoidedHeatingCo2TonYr * Math.Min(1.0, annualCapturedHeatMWh / AnnualDistrictDemandMWh);
            double avoidedPueCo2TonYr = AvoidedPueCo2TonYr * loadRatio;
            double totalAvoidedCo2TonYr = avoidedHeatingCo2TonYr + avoidedPueCo2TonYr;

            return new SimResult
            {
                Inputs = s,
                Vaults = alloc.Vaults,
                Electrical = new ElectricalTelemetry
                {
                    TotalItMw = totalItMw,
                    RequestedItMw = requestedItMw,
                    ActiveVaultCount = alloc.ActiveVaultCount,
                    FaultedVaultCount = alloc.FaultedVaultCount,
                    Pue = Pue,
                    GrossFacilityMw = grossFacilityMw,
                    AnnualGrossMWh = annualGrossMWh,
                    PvOutputMw = pvOutputMw,
                    AnnualPvMWh = annualPvMWh,
                    AnnualNetGridMWh = annualNetGridMWh,
                    BessChargeMw = bessChargeMw,
                    BessDischargeMw = bessDischargeMw,
                    BessMode = bessMode,
                    NetGridDrawMw = netGridDrawMw
                },
                Thermal = new ThermalTelemetry
                {
                    HeatCaptureEff = s.HeatCaptureEff,
                    CapturedHeatMw = capturedHeatMw,
                    AnnualCapturedHeatMWh = annualCapturedHeatMWh,
                    AnnualCoveragePct = annualCoveragePct,
                    PrimaryGrossFlowLmin = primaryGrossFlowLmin,
                    PrimaryGrossFlowLs = primaryGrossFlowLs,
                    PrimaryNetHeatMw = primaryNetHeatMw,
                    PrimaryNetFlowLmin = primaryNetFlowLmin,
                    SecondaryGrossFlowLmin = secondaryGrossFlowLmin,
                    SecondaryGrossFlowLs = secondaryGrossFlowLs,
                    SecondaryNetHeatMw = secondaryNetHeatMw,
                    SecondaryNetFlowLmin = secondaryNetFlowLmin,
                    DistrictDemandMw = districtDemandMw,
                    AnnualisedDemandMWh = annualisedDemandMWh,
                    NetThermalBalanceMw = netThermalBalanceMw,
                    SurplusMw = surplusMw,
                    DeficitMw = deficitMw,
                    ThermalStoreSoc = s.ThermalStoreSoc,
                    ThermalStoreEnergyMWh = ThermalStoreMWh * s.ThermalStoreSoc,
                    ThermalStoreChargeMw = thermalStoreChargeMw,
                    ThermalStoreDischargeMw = thermalStoreDischargeMw,
                    TcesChargeMw = tcesChargeMw,
                    TiPtesChargeMw = tiPtesChargeMw,
                    TiPtesElectricEqMw = tiPtesElectricEqMw,
                    OrcThermalInputMw = orcThermalInputMw,
                    OrcOutputKw = orcOutputKw,
                    MofChillerMw = mofChillerMw,
                    MofCoolingMw = mofCoolingMw,
                    DryCoolerRejectionMw = dryCoolerRejectionMw,
                    HthpThermalOutputMw = hthpThermalOutputMw,
                    HthpElectricalMw = hthpElectricalMw,
                    HthpCop = hthpCop,
                    ScenarioBActive = s.HthpScenarioB,
                    UnmetThermalDeficitMw = unmetThermalDeficitMw,
                    RingPumpingKw = ringPumpingKw
                },
                Hydrology = new HydrologyTelemetry
                {
                    RainfallMultiplier = s.RainfallMultiplier,
                    EffectiveRainfallMmYr = DublinRainfallMmYr * s.RainfallMultiplier,
                    AnnualHarvestPer100kM3 = annualHarvestPer100kM3,
                    AnnualRainHarvestM3 = annualRainHarvestM3,
                    DailyHarvestM3 = dailyHarvestM3,
                    CisternSoc = s.CisternSoc,
                    CisternVolumeM3 = cisternVolumeM3,
                    CisternCapacityM3 = CisternCapacityM3,
                    DroughtAutonomyDays = droughtAutonomyDays,
                    FullTankAutonomyDays = fullTankAutonomyDays,
                    AvoidedPotableWaterM3Yr = AvoidedPotableWaterM3Yr
                },
                Optical = new OpticalTelemetry
                {
                    SdnEnabled = s.SdnEnabled,
                    SdnEfficiencyFactor = SdnEfficiencyFactor,
                    SdnShiftedMw = alloc.SdnShiftedMw,
                    ReroutedFromFaultMw = alloc.ReroutedFromFaultMw,
                    InterVaultRttUs = InterVaultRttUs,
                    ActiveLocalE2eRttUs = activeLocalE2eRttUs,
                    LocalBlindTravelMm = localBlindTravelMm,
                    CloudTransportRttMs = CloudTransportRttMs,
                    CloudControlLoopMs = CloudControlLoopMs,
                    CloudBlindTravelM = cloudBlindTravelM,
                    LocalBreakoutPct = LocalBreakoutPct,
                    WanPowerSavedMWhYr = wanPowerSavedMWhYr
                },
                Financial = new FinancialTelemetry
                {
                    CapexTotalM = CapexTotalM,
                    CapexDistrictM = CapexDistrictM,
                    CapexDcOperatorM = CapexDcOperatorM,
                    CapexCruPvBessM = CapexCruPvBessM,
                    CounterfactualCapexM = CounterfactualCapexM,
                    CapexSavingsM = CapexSavingsM,
                    RevenueAnnualM = RevenueAnnualM,
                    OpexAnnualM = OpexAnnualM,
                    EbitdaAnnualM = EbitdaAnnualM,
                    Repex30YrM = Repex30YrM,
                    SimplePaybackYr = SimplePaybackYr,
                    IrrPct = IrrPct,
                    Npv4PctM = Npv4PctM,
                    DiscountedPayback4PctYr = DiscountedPayback4PctYr,
                    Npv8PctM = Npv8PctM,
                    DiscountedPayback8PctYr = DiscountedPayback8PctYr,
                    AvoidedHeatingCo2TonYr = avoidedHeatingCo2TonYr,
                    AvoidedPueCo2TonYr = avoidedPueCo2TonYr,
                    AvoidedPueEnergyMWhYr = AvoidedPueEnergyMWhYr,
                    TotalAvoidedCo2TonYr = totalAvoidedCo2TonYr
                },
                Survey = SurveyData
            };
        }

        public static SubsystemInfo GetSubsystemInfo(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            SubsystemInfo info;
            if (SubsystemMap.TryGetValue(key, out info))
            {
                return info;
            }
            return null;
        }

        public static string[] GetAllSubsystemKeys()
        {
            string[] copy = new string[SubsystemKeyList.Length];
            Array.Copy(SubsystemKeyList, copy, SubsystemKeyList.Length);
            return copy;
        }

        private static KeyValuePair<string, string> Kv(string k, string v)
        {
            return new KeyValuePair<string, string>(k, v);
        }

        private static string Fmt(string format, params object[] args)
        {
            return string.Format(Inv, format, args);
        }

        private static SubsystemInfo MakeVaultInspector(int idx)
        {
            string key = "vault_" + idx.ToString(Inv);
            return new SubsystemInfo(
                key,
                Fmt("Subterranean AI Vault {0} (2.0 MW)", idx + 1),
                "Level −4 Bedrock · Data Centre",
                "Reinforced-concrete vault at Level −4 bedrock beneath public plazas. Isolated by floating acoustic slabs, 1.0 m perimeter vibration trenches, and <110 dB low-noise water-mist suppression.",
                new string[]
                {
                    "2.0 MW IT capacity · 800 VDC SSTs + HTS busways (−98% Cu)",
                    "43.4 m³ 15-min thermal buffer tank (ΔT = 10 K)",
                    "In-silicon microfluidic cooling: 75°C primary / 45°C secondary",
                    "1.00 km NANF Hollow-Core Fiber ring hop (8.16 μs RTT)"
                },
                delegate(SimResult sim)
                {
                    VaultTelemetry v = sim.Vaults[idx];
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Status", v.Faulted ? "FAULTED (OFFLINE)" : "ONLINE (NOMINAL)"),
                        Kv("IT Load", Fmt("{0:F2} / 2.00 MW ({1:F0}%)", v.ItMw, v.LoadPct)),
                        Kv("Captured Heat", Fmt("{0:F2} MW_th", v.HeatMw)),
                        Kv("75°C Primary Flow", Fmt("{0:F1} L/min", v.FlowPrimaryLmin)),
                        Kv("Zone Thermal Weight", Fmt("{0:F0}%", v.ZoneWeight * 100.0))
                    };
                }
            );
        }

        private static Dictionary<string, SubsystemInfo> BuildSubsystems()
        {
            Dictionary<string, SubsystemInfo> map = new Dictionary<string, SubsystemInfo>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < VaultCount; i++)
            {
                SubsystemInfo vi = MakeVaultInspector(i);
                map[vi.Key] = vi;
            }

            map["thermal_store"] = new SubsystemInfo(
                "thermal_store",
                "2,500 m³ Diurnal / Cold-Snap Thermal Store",
                "Loop 2 · Thermal Buffering",
                "Insulated stratified water tank (ΔT = 30 K, ~87.1 MWh_th) buffering diurnal demand swings and winter cold-snap deficits alongside the 8.0 MW_th HTHP bank.",
                new string[]
                {
                    "Volume: 2,500 m³ insulated steel/RC tank",
                    "Storage capacity: ~87.08 MWh_th across ΔT = 30 K",
                    "Discharges up to 4.0 MW_th during cold-snap deficits"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("State of Charge", Fmt("{0:F0}% ({1:F1} MWh_th)", sim.Thermal.ThermalStoreSoc * 100.0, sim.Thermal.ThermalStoreEnergyMWh)),
                        Kv("Charge Rate", Fmt("{0:F2} MW_th", sim.Thermal.ThermalStoreChargeMw)),
                        Kv("Discharge Rate", Fmt("{0:F2} MW_th", sim.Thermal.ThermalStoreDischargeMw))
                    };
                }
            );

            map["hthp_bank"] = new SubsystemInfo(
                "hthp_bank",
                "8.0 MW_th Natural-Refrigerant Backup HTHP Bank",
                "Loop 2 · Peaking & Scenario B",
                "High-temperature heat pumps (Scenario B fallback: upgrades 45°C secondary water to 70°C supply at COP 4.6–6.9) covering cold-snap stress deficits.",
                new string[]
                {
                    "Rated thermal output: 8.0 MW_th (natural refrigerant)",
                    "Operating COP band: 4.6 – 6.9",
                    "Covers −3.64 MW_th deficit at −5°C & 60% DC load"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Thermal Output", Fmt("{0:F2} MW_th", sim.Thermal.HthpThermalOutputMw)),
                        Kv("Electrical Draw", Fmt("{0:F2} MW_e", sim.Thermal.HthpElectricalMw)),
                        Kv("Operating COP", Fmt("{0:F2}", sim.Thermal.HthpCop)),
                        Kv("Scenario B Fallback", sim.Thermal.ScenarioBActive ? "Armed (45°C → 70°C)" : "Standby")
                    };
                }
            );

            map["tces"] = new SubsystemInfo(
                "tces",
                "SrBr₂ / K₂CO₃ Thermochemical Energy Storage (TCES)",
                "Exergy-Priority Cascade",
                "Lossless salt-hydrate thermochemical bed storing summer surplus heat (~35 MWh_th) at high energy density without seasonal standby losses.",
                new string[]
                {
                    "Chemistry: SrBr₂ / K₂CO₃ reversible hydration",
                    "Capacity: ~35 MWh_th",
                    "Priority tier #2 in summer surplus exergy cascade"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Active Charging", Fmt("{0:F2} MW_th", sim.Thermal.TcesChargeMw)),
                        Kv("Nominal Capacity", "35.0 MWh_th"),
                        Kv("Total Surplus Available", Fmt("{0:F2} MW_th", sim.Thermal.SurplusMw))
                    };
                }
            );

            map["ti_ptes"] = new SubsystemInfo(
                "ti_ptes",
                "2 MW_e / 16 MWh_e TI-PTES Carnot Battery",
                "Exergy-Priority Cascade · Grid Arbitrage",
                "Thermally-Integrated Pumped Thermal Energy Storage coupling waste heat with wind/grid electricity arbitrage for 8-hour dispatch.",
                new string[]
                {
                    "Power / Energy: 2.0 MW_e / 16.0 MWh_e",
                    "Couples 75°C DC waste heat into Carnot compression cycle"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Thermal Input", Fmt("{0:F2} MW_th", sim.Thermal.TiPtesChargeMw)),
                        Kv("Electric Storage Eq.", Fmt("{0:F2} MW_e", sim.Thermal.TiPtesElectricEqMw)),
                        Kv("Rated Capacity", "2.0 MW_e / 16.0 MWh_e")
                    };
                }
            );

            map["micro_orc"] = new SubsystemInfo(
                "micro_orc",
                "Summer Residual Micro-ORC Skid (≤ 440 kW_e)",
                "Exergy-Priority Cascade · Power Recovery",
                "Organic Rankine Cycle turbine converting summer residual 75°C thermal surplus into clean on-site electricity (~420 kW_e peak, ≤ 2,412 MWh_e/yr ceiling).",
                new string[]
                {
                    "Nominal summer output: ~418–440 kW_e (hard cap 440 kW_e)",
                    "Annual ceiling: 2,412 MWh_e/yr (~2,000 MWh_e/yr practical)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Thermal Input", Fmt("{0:F2} MW_th", sim.Thermal.OrcThermalInputMw)),
                        Kv("Electrical Generation", Fmt("{0:F1} kW_e", sim.Thermal.OrcOutputKw)),
                        Kv("Hard Ceiling", "440.0 kW_e")
                    };
                }
            );

            map["mof_chiller"] = new SubsystemInfo(
                "mof_chiller",
                "MOF Adsorption Chillers",
                "Exergy-Priority Cascade · Cooling",
                "Metal-Organic Framework adsorption chillers thermally driven by residual 75°C waste heat during warm summer afternoons (>18°C).",
                new string[]
                {
                    "Thermally driven cooling from 75°C surplus stream",
                    "Zero vapour-compression HFC refrigerants"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Heat Driven Input", Fmt("{0:F2} MW_th", sim.Thermal.MofChillerMw)),
                        Kv("Chilled Output", Fmt("{0:F2} MW_th", sim.Thermal.MofCoolingMw))
                    };
                }
            );

            map["dry_coolers"] = new SubsystemInfo(
                "dry_coolers",
                "N+1 Low-Noise Dry Coolers",
                "Heat Rejection Backup",
                "Zero-water adiabatic/dry heat rejection bank handling residual surplus after all exergy cascade stages are saturated—eliminating cooling-tower water loss.",
                new string[]
                {
                    "Zero evaporative water consumption (saves 166,500 m³/yr)",
                    "N+1 redundancy with acoustic shrouding"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Heat Rejected", Fmt("{0:F2} MW_th", sim.Thermal.DryCoolerRejectionMw)),
                        Kv("Avoided Potable Water", Fmt("{0:N0} m³/yr", sim.Hydrology.AvoidedPotableWaterM3Yr))
                    };
                }
            );

            map["rainwater_cistern"] = new SubsystemInfo(
                "rainwater_cistern",
                "10,000 m³ Subterranean RC Drought Cistern",
                "Loop 3 · Hydrological Resilience",
                "Reinforced-concrete cistern capturing runoff from 120,000 m² of inert district roofs (Dublin 772.5 mm/yr, C_r=0.80, η_f=0.90), sized for 45–66 day dry spells.",
                new string[]
                {
                    "Capacity: 10,000 m³ (62.5 days autonomy at 160 m³/day)",
                    "Catchment: 120,000 m² inert roofs (55,620 m³/yr per 100k m²)",
                    "Resident survey recycled-water support: 3.86/5.00 (72.0% pos)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Cistern Volume", Fmt("{0:F0} / 10,000 m³ ({1:F0}%)", sim.Hydrology.CisternVolumeM3, sim.Hydrology.CisternSoc * 100.0)),
                        Kv("Drought Autonomy", Fmt("{0:F1} days (Full: {1:F1} d)", sim.Hydrology.DroughtAutonomyDays, sim.Hydrology.FullTankAutonomyDays)),
                        Kv("Annual Rain Harvest", Fmt("{0:F0} m³/yr", sim.Hydrology.AnnualRainHarvestM3))
                    };
                }
            );

            map["ultrapure_train"] = new SubsystemInfo(
                "ultrapure_train",
                "Semiconductor Grade C Ultrapure Water Train",
                "Loop 3 · Microfluidic Polish",
                "Closed-loop UF + MCDI/2D + DI + UV-C LED + BDD/Ti₄O₇ PFAS mineralisation train supplying ultra-pure coolant to in-silicon microchannels.",
                new string[]
                {
                    "Conductivity ≤ 1.0 μS/cm · Resistivity 1–18.2 MΩ·cm",
                    "BDD/Ti₄O₇ electrochemical PFAS destruction + UV-C LED"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Primary 75°C Loop Flow", Fmt("{0:F1} L/min", sim.Thermal.PrimaryGrossFlowLmin)),
                        Kv("Secondary 45°C Flow", Fmt("{0:F1} L/min", sim.Thermal.SecondaryGrossFlowLmin)),
                        Kv("Target Quality", "≤ 1.0 μS/cm · 1–18.2 MΩ·cm")
                    };
                }
            );

            map["rooftop_pv"] = new SubsystemInfo(
                "rooftop_pv",
                "12 MW_p Rooftop Solar PV Array (CRU Asset)",
                "Behind-the-Meter Power",
                "District-wide rooftop PV fleet generating 10,956 MWh_e/yr behind the meter to reduce grid draw from 88,914 to 77,958 MWh_e/yr.",
                new string[]
                {
                    "Installed capacity: 12.0 MW_p · 10,956 MWh_e/yr",
                    "Part of €15.30M CRU/EirGrid LEU compliance package"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Live PV Generation", Fmt("{0:F2} MW_e", sim.Electrical.PvOutputMw)),
                        Kv("Annual PV Yield", Fmt("{0:N0} MWh_e/yr", sim.Electrical.AnnualPvMWh)),
                        Kv("Net Grid Import", Fmt("{0:F2} MW_e", sim.Electrical.NetGridDrawMw))
                    };
                }
            );

            map["bess_yard"] = new SubsystemInfo(
                "bess_yard",
                "8 MW / 32 MWh LFP Battery Energy Storage (BESS)",
                "CRU / EirGrid Grid Support",
                "4-hour lithium iron phosphate battery yard delivering peak shaving, solar time-shifting, and CRU Large Energy User grid firming.",
                new string[]
                {
                    "Power / Energy: 8.0 MW / 32.0 MWh (4-hour duration)",
                    "Instantaneous vault backup + EirGrid DS3 frequency response"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Dispatch Mode", sim.Electrical.BessMode),
                        Kv("Charge / Discharge", Fmt("+{0:F2} / −{1:F2} MW_e", sim.Electrical.BessChargeMw, sim.Electrical.BessDischargeMw)),
                        Kv("Facility Gross Load", Fmt("{0:F2} MW_e", sim.Electrical.GrossFacilityMw))
                    };
                }
            );

            map["autonomous_pods"] = new SubsystemInfo(
                "autonomous_pods",
                "Autonomous 6G V2X Transit Pods & HCF Optical Link",
                "Optical Ring · Mobility Safety",
                "Zero-emission autonomous pods steered via 0.25 km NANF Hollow-Core Fiber + Co-Packaged Optics to the nearest healthy subterranean vault.",
                new string[]
                {
                    "Local HCF+CPO E2E RTT: 143.2 μs (±2.0 μs) = 1.99 mm @ 50 km/h",
                    "vs Regional Cloud (75 km SMF): 7.64 ms RTT / 90 ms loop = 1.25 m blind",
                    "87.34% local traffic breakout (saves ~455 MWh_e/yr WAN power)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Active V2X E2E RTT", Fmt("{0:F1} μs", sim.Optical.ActiveLocalE2eRttUs)),
                        Kv("Blind Travel @ 50 km/h", Fmt("{0:F2} mm (vs 1.25 m cloud)", sim.Optical.LocalBlindTravelMm)),
                        Kv("WAN Energy Saved", Fmt("{0:F0} MWh_e/yr", sim.Optical.WanPowerSavedMWhYr))
                    };
                }
            );

            map["civic_plaza"] = new SubsystemInfo(
                "civic_plaza",
                "Public Civic Plaza (Over Vault Footprint)",
                "Acoustic & Social Mitigation",
                "Car-free public square sited directly above a Level −4 vault (~350 m² footprint), addressing the #1 resident survey concern (noise/vibration, 33.7%).",
                new string[]
                {
                    "Zero vaults under residential foundations",
                    "Survey (N=109): 3.19/5.00 DC comfort (64.8% pos/neutral)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Active Vaults Below", Fmt("{0} / 5 online", sim.Electrical.ActiveVaultCount)),
                        Kv("Max Fire-Mist Noise", "< 110 dB (subterranean isolated)"),
                        Kv("Survey Top Concern Addressed", "Noise/Vibration (33.7%)")
                    };
                }
            );

            map["civic_hub"] = new SubsystemInfo(
                "civic_hub",
                "Schools, Health Centre & Community Hub (15.0 ha)",
                "2045 Circular District Anchor",
                "4 primary schools, 2 secondary schools, primary health centre, and sports hub heated at near-ambient 5GDHC efficiency.",
                new string[]
                {
                    "15.0 ha civic/education + 14.0 ha retail/offices + 25.0 ha parks",
                    "30 km twin-pipe HDPE 5GDHC ring (7.0 W/m loss = 4.9%)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("District Thermal Demand", Fmt("{0:F2} MW_th", sim.Thermal.DistrictDemandMw)),
                        Kv("5GDHC Pumping Power", Fmt("{0:F1} kW_e", sim.Thermal.RingPumpingKw)),
                        Kv("Heat Coverage Ratio", Fmt("{0:F1}%", sim.Thermal.AnnualCoveragePct))
                    };
                }
            );

            map["residential_district"] = new SubsystemInfo(
                "residential_district",
                "7,500-Home Circular Residential District (20,000 Residents)",
                "180.0 ha Masterplan · 90.0 ha Residential",
                "Mixed-density 2045 urban district (3,095 buildings, 83.3 dph) heated by recovered data-centre waste heat via the 5GDHC network.",
                new string[]
                {
                    "3,300 mid-rise (55 blks) · 1,200 low-rise (40 blks)",
                    "2,100 townhouses · 900 detached houses",
                    "Annual demand: 37,610 MWh_th/yr · Displaced CO₂: ~8,020 tCO₂/yr"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Ambient Temp", Fmt("{0:F1} °C", sim.Inputs.AmbientTempC)),
                        Kv("Total Heat Demand", Fmt("{0:F2} MW_th", sim.Thermal.DistrictDemandMw)),
                        Kv("Annualised Demand", Fmt("{0:F0} MWh_th/yr", sim.Thermal.AnnualisedDemandMWh)),
                        Kv("Avoided Heating CO₂", Fmt("{0:F0} tCO₂/yr", sim.Financial.AvoidedHeatingCo2TonYr))
                    };
                }
            );

            return map;
        }
    }
}
