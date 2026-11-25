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
        public double AnnualGrossCoveragePct;
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
        public double PipeLossMw;
        public double TotalNetworkDemandMw;
        public double AnnualisedDemandMWh;
        public double NetThermalBalanceMw;
        public double SurplusMw;
        public double DeficitMw;
        public double ThermalStoreSoc;
        public double ThermalStoreEnergyMWh;
        public double ThermalStoreUsableMWh;
        public double ThermalStoreChargeMw;
        public double ThermalStoreDischargeMw;
        public double CommercialHeatSalesMw;
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
        public double ScenarioBElecMw;
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
        public double ColocationRentEurKwMo;
        public double ColocationRevenueM;
        public double EbitdaAnnualM;
        public double EbitdaUtilityM;
        public double EbitdaDcM;
        public double SimplePaybackYr;
        public double IrrPct;
        public double Npv4PctM;
        public double Npv8PctM;
        public double UtilityIrrPct;
        public double UtilityPaybackYr;
        public double UtilityLcohNetEurMwh;
        public double TcoUsciM;
        public double TcoGasM;
        public double TcoAshpM;
        public double BillUsciEurYr;
        public double AvoidedHeatingCo2TonYr;
        public double AvoidedPueCo2TonYr;
        public double AvoidedPueEnergyMWhYr;
        public double TotalAvoidedCo2TonYr;
    }

    public sealed class SurveyStats
    {
        public int SampleSize;
        public int RawSampleSize;
        public double DcComfortScore;
        public double DcComfortPosPct;
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

        // District constants (RAKDA USCI Nov 2026)
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
        public const int VaultLifeYr = 100;
        public const double HoursPerYear = 8760.0;
        public const double Pue = 1.015;
        public const double CounterfactualPue = 1.56;
        public const double PvPeakMw = 12.0;
        public const double PvAnnualMWh = 10956.0;
        public const double BessPowerMw = 8.0;
        public const double BessEnergyMWh = 32.0;
        public const double BufferPerVaultM3 = 43.0;
        public const double BufferTotalM3 = 215.0;
        public const double BufferDtK = 10.0;
        public const double WaterMistDbMax = 110.0;
        public static readonly double[] VaultZoneWeights = new double[] { 0.24, 0.22, 0.20, 0.18, 0.16 };

        // Thermal & EN 253 Network constants
        public const double PrimaryTempC = 75.0;
        public const double PrimaryDtK = 25.0;
        public const double PrimaryGrossShare = 0.80;
        public const double PrimaryNominalGrossLs = 76.5;
        public const double PrimaryNominalGrossLmin = 76.5 * 60.0;
        public const double SecondaryTempC = 45.0;
        public const double SecondaryDtK = 15.0;
        public const double SecondaryGrossShare = 0.20;
        public const double SecondaryNominalGrossLs = 32.0;
        public const double SecondaryNominalGrossLmin = 32.0 * 60.0;
        public const double AnnualDistrictDemandMWh = 37612.0;
        public const double AnnualDhwMWh = 26362.0;
        public const double AnnualShMWh = 11250.0;
        public const double DesignDayTempC = -3.0;
        public const double ColdSnapPeakMw = 9.52;
        public const double SummerDhwBaselineMw = 3.01;
        public const double MeanTempC = 10.0;
        public const double MeanDemandMw = AnnualDistrictDemandMWh / HoursPerYear;
        public static readonly double SpaceHeatSlopeMwPerK = (ColdSnapPeakMw - MeanDemandMw) / (MeanTempC - DesignDayTempC);
        public static readonly double BalanceTempC = MeanTempC + (MeanDemandMw - SummerDhwBaselineMw) / SpaceHeatSlopeMwPerK;
        public const double ThermalStoreM3 = 2500.0;
        public const double ThermalStoreDtK = 35.0;
        public const double ThermalStoreMWh = 101.6;
        public const double ThermalStoreUsableMWh = 91.4;
        public const double HthpMaxMwTh = 8.0;
        public const double HthpCopMin = 4.0;
        public const double HthpCopMax = 6.0;
        public const double ScenarioBExtraHthpMw = 8.5;
        public const double RingLengthKm = 30.0;
        public const double RingLossWPerM = 13.0;
        public const double RingLossMWhYr = 3421.0;
        public const double RingLossMw = 0.39;
        public const double PrimaryPumpingKw = 21.9;
        public const double PumpingAnnualMWhYr = 259.0;
        public const double OrcMaxKwE = 420.0;
        public const double SummerSurplusMWhTh = 44816.0;

        // Hydrology constants
        public const double DublinRainfallMmYr = 772.5;
        public const double RunoffCoeff = 0.80;
        public const double FiltrationEff = 0.90;
        public const double CatchmentM2 = 100000.0;
        public const double CisternCapacityM3 = 10000.0;
        public const double CisternMinStorageM3 = 6700.0;
        public const double DailyWaterDemandM3 = 152.0;
        public const int DroughtReserveDays = 44;
        public const double UpwMakeupMaxM3Yr = 3265.0;
        public const double AvoidedPotableMinM3Yr = 157680.0;
        public const double AvoidedPotableMaxM3Yr = 175200.0;
        public const double AvoidedPotableWaterM3Yr = 166440.0;
        public const double UltrapureConductivityMaxUsCm = 1.0;

        // Optical constants
        public const double OpticalRingKm = 3.0;
        public const double InterVaultHopKm = 1.0;
        public const double InterVaultRttUs = 8.16;
        public const double InterVaultJitterUs = 1.2;
        public const double VaultToPodKm = 0.25;
        public const double VaultToPodNetRttUs = 3.17;
        public const double LocalE2eRttUs = 143.2;
        public const double LocalE2eJitterUs = 2.5;
        public const double CloudDistanceKm = 75.0;
        public const double CloudTransportRttMs = 7.64;
        public const double CloudControlLoopMs = 90.0;
        public const double PodSpeedKmh = 50.0;
        public const double LocalBreakoutPct = 87.34;
        public const double WanPowerSavedMWhYr = 455.0;
        public const double SdnEfficiencyFactor = 70.1;

        // Financial & Carbon constants (RAKDA USCI Nov 2026)
        public const double CapexTotalM = 136.66;
        public const double CapexDistrictM = 57.31;
        public const double CapexDcOperatorM = 79.35;
        public const double CapexCruPvBessM = 15.30;
        public const double ColocationRentEurKwMo = 145.0;
        public const double ColocationRevenueM = 17.40;
        public const double EbitdaAnnualM = 19.60;
        public const double EbitdaUtilityM = 3.64;
        public const double EbitdaDcM = 15.97;
        public const double SimplePaybackYr = 7.0;
        public const double IrrPct = 13.7;
        public const double Npv4PctM = 189.0;
        public const double Npv8PctM = 77.0;
        public const double UtilityIrrPct = 4.2;
        public const double UtilityPaybackYr = 15.8;
        public const double UtilityLcohNetEurMwh = 93.0;
        public const double TcoUsciM = 520.0;
        public const double TcoGasM = 826.0;
        public const double TcoAshpM = 936.0;
        public const double BillUsciEurYr = 536.0;
        public const double AvoidedHeatingCo2TonYr = 7250.0;
        public const double AvoidedPueCo2TonYr = 11240.0;
        public const double AvoidedPueEnergyMWhYr = 47742.0;
        public const double TotalAvoidedCo2TonYr = 18490.0;

        // Survey constants (N = 105 cleaned / 109 raw)
        public static readonly SurveyStats SurveyData = new SurveyStats
        {
            SampleSize = 105,
            RawSampleSize = 109,
            DcComfortScore = 3.20,
            DcComfortPosPct = 47.1,
            RecycledWaterScore = 3.87,
            RecycledWaterPosPct = 72.8,
            PriorityCheapestBillPct = 42.6,
            PriorityReliabilityPct = 33.0,
            PriorityCombinedPct = 75.6,
            TopConcernNoiseVibrationPct = 35.1
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
                    Description = "Full 10.0 MW IT load at 98% capture, Dublin mean 10°C ambient: 228% gross / 209% net coverage across 70/35°C EN 253 network (u_BE = 47.8%).",
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
                    Label = "Winter Design Day (−3°C)",
                    ShortLabel = "Design Day −3°C",
                    Badge = "100% IT · −3°C",
                    Description = "Peak −3°C design-day demand (9.52 MW_th + 0.39 MW_th EN 253 loss = 9.91 MW_th) vs 9.80 MW_th captured (−0.11 MW_th gap covered by 2,500 m³ store).",
                    Params = new SimState
                    {
                        ScenarioId = "winter_cold_snap",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = -3.0,
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
                    Label = "Cautious Winter (−3°C)",
                    ShortLabel = "Cautious −3°C",
                    Badge = "u=0.70 · HTHP On",
                    Description = "Cautious winter −3°C (u=0.70, f_cap=0.80 → 5.60 MW_th vs 9.91 MW_th peak): 4.31 MW_th gap met by 2,500 m³ store (91.4 MWh usable) + 8.0 MW_th HTHP (48 h).",
                    Params = new SimState
                    {
                        ScenarioId = "winter_stress_60",
                        Utilization = 0.70,
                        HeatCaptureEff = 0.80,
                        AmbientTempC = -3.0,
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
                    Description = "Vault 3 offline. Optical NANF HCF ring reroutes V2X traffic (143.2 μs) and SDN shifts 8.0 MW IT load across healthy vaults (70.1× vs EN 253 pumping).",
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
                    Label = "Summer Surplus Cascade",
                    ShortLabel = "Summer Cascade",
                    Badge = "24°C · 0.4 MW ORC",
                    Description = "Steady DHW baseload (3.01 MW_th + 0.39 MW_th loss) leaves 6.40 MW_th surplus routed to 75°C commercial sales, ~0.4 MW_e Micro-ORC (7.2–8.3% eff.), and dry coolers.",
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
                    Label = "Dublin 44-Day Drought",
                    ShortLabel = "Dublin Drought",
                    Badge = "0× Rain · 6,700 m³",
                    Description = "Zero rainfall input; 10,000 m³ RC drought cistern (6,700 m³ min storage = 44-day reserve at 152 m³/d) & sealed Grade A UPW loop avoid 157.7–175.2k m³/yr.",
                    Params = new SimState
                    {
                        ScenarioId = "dublin_drought",
                        Utilization = 1.0,
                        HeatCaptureEff = 0.98,
                        AmbientTempC = 21.0,
                        RainfallMultiplier = 0.0,
                        CisternSoc = 0.67,
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
                    Label = "Cautious (u=0.70, f=0.80)",
                    ShortLabel = "Cautious Base",
                    Badge = "u=0.70 · f=0.80",
                    Description = "Cautious annual case: 70% IT load and 80% capture yield 49,056 MWh_th/yr (130% gross / 120% net of 37,612 MWh/yr + 3,421 MWh loss; u_BE = 58.6%).",
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
            double ratio = (t + 3.0) / 28.0;
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

            // 3. Thermal & EN 253 Network Hydraulic Flows
            double capturedHeatMw = totalItMw * s.HeatCaptureEff;
            double annualCapturedHeatMWh = capturedHeatMw * HoursPerYear;
            double annualGrossCoveragePct = (annualCapturedHeatMWh / AnnualDistrictDemandMWh) * 100.0;
            double annualNetDemandMWh = AnnualDistrictDemandMWh + RingLossMWhYr;
            double annualCoveragePct = (annualCapturedHeatMWh / annualNetDemandMWh) * 100.0;

            double primaryGrossFlowLs = PrimaryNominalGrossLs * loadRatio;
            double primaryGrossFlowLmin = primaryGrossFlowLs * 60.0;
            double primaryNetHeatMw = capturedHeatMw * PrimaryGrossShare;
            double primaryNetFlowLmin = primaryGrossFlowLmin * s.HeatCaptureEff;

            double secondaryGrossFlowLs = SecondaryNominalGrossLs * loadRatio;
            double secondaryGrossFlowLmin = secondaryGrossFlowLs * 60.0;
            double secondaryNetHeatMw = capturedHeatMw * SecondaryGrossShare;
            double secondaryNetFlowLmin = secondaryGrossFlowLmin * s.HeatCaptureEff;

            double districtDemandMw = ComputeDistrictDemandMw(s.AmbientTempC);
            double pipeLossMw = RingLossMw;
            double totalNetworkDemandMw = districtDemandMw + pipeLossMw;
            double annualisedDemandMWh = districtDemandMw * HoursPerYear;
            double netThermalBalanceMw = capturedHeatMw - totalNetworkDemandMw;
            double basePumpingKw = PumpingAnnualMWhYr * 1000.0 / HoursPerYear;
            double ringPumpingKw = basePumpingKw * loadRatio * (s.SdnEnabled ? 1.0 : 1.45);

            // 4. Canonical 3-Tier Surplus Cascade vs Cold-Snap / Scenario B Deficit Coverage
            double surplusMw = 0.0;
            double deficitMw = 0.0;
            double thermalStoreChargeMw = 0.0;
            double thermalStoreDischargeMw = 0.0;
            double commercialHeatSalesMw = 0.0;
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
                thermalStoreChargeMw = s.ThermalStoreSoc < 0.95 ? Math.Min(surplusMw, 0.80) : 0.0;
                double rem1 = surplusMw - thermalStoreChargeMw;
                commercialHeatSalesMw = Math.Min(rem1 * 0.25, 1.40);
                double rem2 = rem1 - commercialHeatSalesMw;
                orcThermalInputMw = Math.Min(rem2 * 0.80, 5.10);
                orcOutputKw = Math.Min(OrcMaxKwE, orcThermalInputMw * 1000.0 * 0.078);
                dryCoolerRejectionMw = Math.Max(0.0, rem2 - orcThermalInputMw);
            }
            else
            {
                deficitMw = -netThermalBalanceMw;
                thermalStoreDischargeMw = s.ThermalStoreSoc > 0.05 ? Math.Min(deficitMw, 4.0) : 0.0;
                hthpThermalOutputMw = Math.Min(HthpMaxMwTh, Math.Max(0.0, deficitMw - thermalStoreDischargeMw));
                hthpElectricalMw = hthpThermalOutputMw / hthpCop;
                unmetThermalDeficitMw = Math.Max(0.0, deficitMw - thermalStoreDischargeMw - hthpThermalOutputMw);
            }

            double scenarioBElecMw = 0.0;
            if (s.HthpScenarioB && capturedHeatMw > 0.0)
            {
                double boostHeatMw = Math.Min(ScenarioBExtraHthpMw, totalNetworkDemandMw);
                scenarioBElecMw = boostHeatMw / hthpCop;
            }

            double netGridDrawMw = Math.Max(
                0.0,
                grossFacilityMw + hthpElectricalMw + scenarioBElecMw + ringPumpingKw / 1000.0 - pvOutputMw - orcOutputKw / 1000.0 - bessDischargeMw + bessChargeMw
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
            double heatShare = Math.Min(1.0, annualCapturedHeatMWh / annualNetDemandMWh);
            double avoidedHeatingCo2TonYr = AvoidedHeatingCo2TonYr * heatShare;
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
                    AnnualGrossCoveragePct = annualGrossCoveragePct,
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
                    PipeLossMw = pipeLossMw,
                    TotalNetworkDemandMw = totalNetworkDemandMw,
                    AnnualisedDemandMWh = annualisedDemandMWh,
                    NetThermalBalanceMw = netThermalBalanceMw,
                    SurplusMw = surplusMw,
                    DeficitMw = deficitMw,
                    ThermalStoreSoc = s.ThermalStoreSoc,
                    ThermalStoreEnergyMWh = ThermalStoreMWh * s.ThermalStoreSoc,
                    ThermalStoreUsableMWh = ThermalStoreUsableMWh * s.ThermalStoreSoc,
                    ThermalStoreChargeMw = thermalStoreChargeMw,
                    ThermalStoreDischargeMw = thermalStoreDischargeMw,
                    CommercialHeatSalesMw = commercialHeatSalesMw,
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
                    ScenarioBElecMw = scenarioBElecMw,
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
                    ColocationRentEurKwMo = ColocationRentEurKwMo,
                    ColocationRevenueM = ColocationRevenueM,
                    EbitdaAnnualM = EbitdaAnnualM,
                    EbitdaUtilityM = EbitdaUtilityM,
                    EbitdaDcM = EbitdaDcM,
                    SimplePaybackYr = SimplePaybackYr,
                    IrrPct = IrrPct,
                    Npv4PctM = Npv4PctM,
                    Npv8PctM = Npv8PctM,
                    UtilityIrrPct = UtilityIrrPct,
                    UtilityPaybackYr = UtilityPaybackYr,
                    UtilityLcohNetEurMwh = UtilityLcohNetEurMwh,
                    TcoUsciM = TcoUsciM,
                    TcoGasM = TcoGasM,
                    TcoAshpM = TcoAshpM,
                    BillUsciEurYr = BillUsciEurYr,
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
                "Reinforced-concrete vault (25 × 16 m, 100-yr EN 1990 life) at Level −4 bedrock beneath public plazas. Isolated by floating acoustic slabs, 1.0 m perimeter vibration trenches, and <110 dB water-mist suppression.",
                new string[]
                {
                    "2.0 MW IT capacity · 800 VDC SSTs (−5% loss; HTS busways optional)",
                    "43 m³ 15-min thermal buffer tank (ΔT = 10 K · 215 m³ district total)",
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
                        Kv("75°C Primary Flow", Fmt("{0:F2} L/s ({1:F0} L/min)", v.FlowPrimaryLmin / 60.0, v.FlowPrimaryLmin)),
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
                "2,500 m³ Stratified Pressurised Thermal Store",
                "Loop 2 · Thermal Buffering",
                "Insulated stratified water tank (ΔT = 35 K, 101.6 MWh_th gross / 91.4 MWh_th usable) sustaining average winter demand for 15 h with zero DC heat and 48 h alongside the 8.0 MW_th backup HTHPs.",
                new string[]
                {
                    "Volume: 2,500 m³ insulated steel/RC tank (100-yr civil life)",
                    "Capacity: 101.6 MWh_th gross / 91.4 MWh_th usable (ΔT = 35 K)",
                    "15 h winter carry standalone · 48 h peak resilience w/ 8.0 MW HTHP"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("State of Charge", Fmt("{0:F0}% ({1:F1} / 91.4 MWh usable)", sim.Thermal.ThermalStoreSoc * 100.0, sim.Thermal.ThermalStoreUsableMWh)),
                        Kv("Charge Rate", Fmt("{0:F2} MW_th", sim.Thermal.ThermalStoreChargeMw)),
                        Kv("Discharge Rate", Fmt("{0:F2} MW_th", sim.Thermal.ThermalStoreDischargeMw))
                    };
                }
            );

            map["hthp_bank"] = new SubsystemInfo(
                "hthp_bank",
                "8.0 MW_th Natural-Refrigerant Backup HTHP Bank",
                "Loop 2 · Peaking & Scenario B",
                "High-temperature heat pumps covering cautious −3°C cold-snap deficits (−4.31 MW_th) and Scenario B fallback (40°C → 75°C condensing at COP 4.0–6.0; ~8.5 MW / €5.1M for full boost, +€33/MWh).",
                new string[]
                {
                    "Rated thermal output: 8.0 MW_th (natural refrigerant)",
                    "Scenario B COP band: 4.0 – 6.0 (5.9–8.8 GWh_e/yr, LCOH → €126/MWh)",
                    "Covers −4.31 MW_th cautious deficit at −3°C & 48 h outage resilience"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Thermal Output", Fmt("{0:F2} MW_th", sim.Thermal.HthpThermalOutputMw)),
                        Kv("Electrical Draw", Fmt("{0:F2} MW_e", sim.Thermal.HthpElectricalMw + sim.Thermal.ScenarioBElecMw)),
                        Kv("Operating COP", Fmt("{0:F2}", sim.Thermal.HthpCop)),
                        Kv("Scenario B Fallback", sim.Thermal.ScenarioBActive ? "Armed (40°C → 75°C, +€33/MWh)" : "Standby")
                    };
                }
            );

            map["tces"] = new SubsystemInfo(
                "tces",
                "Optional R&D Pad — SrBr₂ / K₂CO₃ TCES (Excluded from Base)",
                "Optional R&D · Not in Base CapEx",
                "Thermochemical energy storage was evaluated during early concept work and excluded from the canonical RAKDA-USCI base CapEx and 3-tier surplus cascade.",
                new string[]
                {
                    "Status: Excluded from €136.66M base CapEx & LCOH model",
                    "Replaced by direct 70–75°C commercial sales + 0.4 MW_e Micro-ORC + Dry Coolers"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Base Cascade Allocation", "0.00 MW_th (Excluded from base scope)"),
                        Kv("Total Surplus Available", Fmt("{0:F2} MW_th", sim.Thermal.SurplusMw))
                    };
                }
            );

            map["ti_ptes"] = new SubsystemInfo(
                "ti_ptes",
                "Optional R&D Pad — TI-PTES Carnot Battery (Excluded from Base)",
                "Optional R&D · Not in Base CapEx",
                "Thermally-Integrated Pumped Thermal Energy Storage is excluded from the canonical RAKDA-USCI base CapEx and 3-tier surplus cascade (future R&D option only).",
                new string[]
                {
                    "Status: Excluded from €136.66M base CapEx & DCF model",
                    "Active grid firming delivered by 8 MW / 32 MWh LFP BESS"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Base Cascade Allocation", "0.00 MW_th (Excluded from base scope)"),
                        Kv("Active BESS Mode", sim.Electrical.BessMode)
                    };
                }
            );

            map["micro_orc"] = new SubsystemInfo(
                "micro_orc",
                "Summer Residual Micro-ORC Skid (~0.4 MW_e)",
                "Exergy Cascade Tier 2 · Power Recovery",
                "PFAS-free Organic Rankine Cycle turbine converting 75°C summer surplus (17.2% Carnot ceiling @ 15°C sink → 7.2–8.3% η_ORC) into 2.60–2.97 GWh_e/yr (~0.4 MW_e, €0.35M/yr / +€3.5M utility NPV).",
                new string[]
                {
                    "Carnot ceiling: 17.2% (75°C vs 15°C sink) → 7.2–8.3% η_ORC",
                    "Output: ~0.4 MW_e (2.60–2.97 GWh_e/yr · €0.35M/yr revenue)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Thermal Input (75°C)", Fmt("{0:F2} MW_th", sim.Thermal.OrcThermalInputMw)),
                        Kv("Electrical Generation", Fmt("{0:F1} kW_e", sim.Thermal.OrcOutputKw)),
                        Kv("Commercial Heat Sales", Fmt("{0:F2} MW_th", sim.Thermal.CommercialHeatSalesMw))
                    };
                }
            );

            map["mof_chiller"] = new SubsystemInfo(
                "mof_chiller",
                "Tier 1 Commercial Heat & Optional Adsorption Cooling",
                "Exergy Cascade Tier 1 · 75°C Direct Sales",
                "Direct 70–75°C heat sales to local commercial buyers (pools, laundries, greenhouses, thermally driven adsorption chillers) delivering 1.8×–12.5× ORC value.",
                new string[]
                {
                    "Priority Tier #1 of 44,816 MWh/yr summer surplus (35,852 MWh @ 75°C)",
                    "Direct heat value ~€95/MWh vs ~€10/MWh thermal input for ORC"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Tier 1 Commercial Sales", Fmt("{0:F2} MW_th", sim.Thermal.CommercialHeatSalesMw)),
                        Kv("Total Surplus Stream", Fmt("{0:F2} MW_th", sim.Thermal.SurplusMw))
                    };
                }
            );

            map["dry_coolers"] = new SubsystemInfo(
                "dry_coolers",
                "N+1 Low-Noise Dry Coolers (Cascade Tier 3)",
                "Exergy Cascade Tier 3 · Heat Rejection",
                "Zero-water adiabatic/dry heat rejection bank at the energy centre ensuring server uptime never depends on summer heat demand—saving 157,680–175,200 m³/yr of potable tower water.",
                new string[]
                {
                    "Zero evaporative water consumption (saves 157,680–175,200 m³/yr)",
                    "N+1 redundancy with remote acoustic shrouding at energy centre"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Heat Rejected", Fmt("{0:F2} MW_th", sim.Thermal.DryCoolerRejectionMw)),
                        Kv("Avoided Cooling Water", "157,680–175,200 m³/yr")
                    };
                }
            );

            map["rainwater_cistern"] = new SubsystemInfo(
                "rainwater_cistern",
                "10,000 m³ Subterranean RC Drought Cistern",
                "Loop 3 · Hydrological Resilience",
                "Reinforced-concrete cistern capturing runoff from 10 ha (100,000 m²) inert roofs (772.5 mm/yr, C_r=0.80, η_f=0.90 → 55,620 m³/yr, 152 m³/d), keeping ≥6,700 m³ (44-day drought reserve).",
                new string[]
                {
                    "Capacity: 10,000 m³ (6,700 m³ min across 3,318 m³ swing = 44 d reserve)",
                    "Water grades: Grade A UPW (≤3,265 m³/yr), Grade B toilets/laundry, Grade C parks",
                    "Survey (N=105): 3.87/5.00 recycled water score (72.8% comfortable)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Cistern Volume", Fmt("{0:F0} / 10,000 m³ ({1:F0}%)", sim.Hydrology.CisternVolumeM3, sim.Hydrology.CisternSoc * 100.0)),
                        Kv("Drought Reserve", Fmt("{0:F1} days @ 152 m³/d", sim.Hydrology.DroughtAutonomyDays)),
                        Kv("Annual Rain Harvest", Fmt("{0:F0} m³/yr", sim.Hydrology.AnnualRainHarvestM3))
                    };
                }
            );

            map["ultrapure_train"] = new SubsystemInfo(
                "ultrapure_train",
                "Grade A Semiconductor Ultrapure Water (UPW) Train",
                "Loop 3 · Microfluidic Polish",
                "Closed-loop Grade A UPW makeup train (≤3,265 m³/yr, <5.9% of harvested yield): dead-end UF + MCDI + mixed-bed DI + UV-C LED + BDD/Ti₄O₇ PFAS oxidation, isolated by EN 1717 physical air-gaps.",
                new string[]
                {
                    "Grade A = UPW (≤1.0 μS/cm, 1–18.2 MΩ·cm, ≤0.2–1.0 μm, ≤1 ppm SiO₂)",
                    "Grade B = toilets & laundry (no dishwashers) · Grade C = park irrigation"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Primary 75°C Flow", Fmt("{0:F1} L/s ({1:F0} L/min)", sim.Thermal.PrimaryGrossFlowLs, sim.Thermal.PrimaryGrossFlowLmin)),
                        Kv("Secondary 45°C Flow", Fmt("{0:F1} L/s ({1:F0} L/min)", sim.Thermal.SecondaryGrossFlowLs, sim.Thermal.SecondaryGrossFlowLmin)),
                        Kv("Annual Grade A Makeup", "≤ 3,265 m³/yr (< 5.9% of yield)")
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
                    "Local HCF+CPO E2E RTT: 143.2 μs wireless / 48.2 μs fibre (1.99 mm @ 50 km/h)",
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
                "Car-free public square sited directly above a Level −4 vault (~350 m² footprint), addressing the #1 resident survey concern (noise/vibration, 35.1%, N=105 cleaned).",
                new string[]
                {
                    "Zero vaults under residential foundations (100-yr EN 1990 concrete)",
                    "Survey (N=105): 3.20/5.00 DC comfort (47.1% pos / 17.3% neut / 35.6% low)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Active Vaults Below", Fmt("{0} / 5 online", sim.Electrical.ActiveVaultCount)),
                        Kv("Max Fire-Mist Noise", "< 110 dB (subterranean isolated)"),
                        Kv("Survey Top Concern Addressed", "Noise/Vibration (35.1%, N=105)")
                    };
                }
            );

            map["civic_hub"] = new SubsystemInfo(
                "civic_hub",
                "Schools, Health Centre & Community Hub (15.0 ha)",
                "2045 Circular District Anchor",
                "4 primary schools, 2 secondary schools, primary health centre, and sports hub served by the 30 km pre-insulated 70/35°C EN 253 network (13.0 W/m loss = 3,421 MWh/yr = 9.1%).",
                new string[]
                {
                    "15.0 ha civic/education + 14.0 ha retail/offices + 25.0 ha parks",
                    "30 km EN 253 70/35°C steel pipes (13.0 W/m = 9.1% loss; 259 MWh_e/yr pump)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Network Demand (incl. 0.39 MW loss)", Fmt("{0:F2} MW_th", sim.Thermal.TotalNetworkDemandMw)),
                        Kv("EN 253 Pumping Power", Fmt("{0:F1} kW_e", sim.Thermal.RingPumpingKw)),
                        Kv("Net Heat Coverage Ratio", Fmt("{0:F1}%", sim.Thermal.AnnualCoveragePct))
                    };
                }
            );

            map["residential_district"] = new SubsystemInfo(
                "residential_district",
                "7,500-Home Circular Residential District (20,000 Residents)",
                "180.0 ha Masterplan · 90.0 ha Residential",
                "Mixed-density 2045 urban district (3,095 buildings, 83.3 dph) heated by recovered data-centre waste heat via instantaneous HIUs (≥55°C tap, zero stored DHW).",
                new string[]
                {
                    "3,300 mid-rise · 1,200 low-rise · 2,100 townhouses · 900 detached",
                    "Annual demand: 37,612 MWh_th/yr · €536/yr bill vs €1,001 Gas / €1,691 ASHP",
                    "Avoided heating CO₂: 7,230–7,270 t/yr (6.8–7.5 mo embodied payback)"
                },
                delegate(SimResult sim)
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        Kv("Ambient Temp", Fmt("{0:F1} °C", sim.Inputs.AmbientTempC)),
                        Kv("Building Heat Demand", Fmt("{0:F2} MW_th", sim.Thermal.DistrictDemandMw)),
                        Kv("Annualised Demand", Fmt("{0:F0} MWh_th/yr", sim.Thermal.AnnualisedDemandMWh)),
                        Kv("Avoided Heating CO₂", Fmt("{0:F0} tCO₂/yr (7,230–7,270 nom.)", sim.Financial.AvoidedHeatingCo2TonYr))
                    };
                }
            );

            return map;
        }
    }
}
