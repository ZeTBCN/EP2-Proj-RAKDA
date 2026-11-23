# RAKDA Ltd. (Group A-10) — EP2 3D Simulation Engineering Specification
Source of Truth: *RAKDA Ltd. Report — Draft 3.2* (21 Nov 2026) · TCD 1E17 *The Circular City (2045)*

## 1. District Masterplan (180.0 ha · 20,000 Residents · 7,500 Homes)
- **Detached houses (2–3 fl.):** 900 units, 900 bldgs (22.5 ha), 3,200 residents
- **Townhouses / semi-detached (2–3 fl.):** 2,100 units, 2,100 bldgs (30.0 ha), 6,800 residents
- **Low-rise apartments (3–4 fl.):** 1,200 units, 40 blocks (12.0 ha), 3,000 residents
- **Mid-rise apartments (5–7 fl.):** 3,300 units, 55 blocks (22.0 ha), 7,000 residents
- **Total residential:** 7,500 units, 3,095 buildings (90.0 ha, 83.3 dph)
- **Roads, autonomous transit & pedestrian corridors:** 36.0 ha (20.0%)
- **District parks & blue-green corridors:** 25.0 ha (13.9%)
- **Schools & community facilities:** 15.0 ha (8.3%) — 4 primary, 2 secondary, health centre, sports hub
- **Retail, offices & civic/utilities:** 14.0 ha (7.8%)

## 2. Subterranean Data Centre Architecture (10.0 MW IT Load)
- **Layout:** 5 × 2.0 MW subterranean reinforced-concrete vaults at Level −4 bedrock (~350 m² total civil footprint), sited beneath public plazas (not residential foundations).
- **Safety & Acoustic Isolation:** Floating acoustic slabs, 1.0 m vibration-isolation trenches, low-noise water-mist fire suppression (< 110 dB).
- **Power Delivery & CRU Compliance:** 800 VDC Solid-State Transformers (SSTs) + High-Temperature Superconducting (HTS) busways (−98% copper), PUE = 1.015 (88,914 MWh_e/yr gross; 77,958 MWh_e/yr net grid after 10,956 MWh_e/yr BTM PV). CRU/EirGrid LEU assets: 12 MW_p rooftop solar PV + 8 MW / 32 MWh LFP BESS.
- **Compute Density:** 2045 rack power 250–1,200 kW/rack (40 / 17 / 9 racks total); 25–250 ZettaOPS AI compute output (>99.9% leasable).

## 3. Three-Loop Thermal, Exergy & Hydrological Architecture
- **Loop 1 — Primary In-Silicon Microfluidic Cooling (75°C / ΔT = 25 K):**
  - 98% heat capture at full load → 9.80 MW_th continuous (85,848 MWh_th/yr).
  - Primary 75°C water flow (80% gross basis, ΔT = 25 K): 4,635.3 L/min (77.26 L/s); net 7.84 MW_th = 4,543 L/min.
  - 15-min vault thermal buffers: 217.0 m³ total (43.4 m³ per vault, ΔT = 10 K).
  - Direct EN 253 PHE DHW pasteurisation ≥ 60°C (Scenario A).
- **Loop 2 — Hybrid 5GDHC District Heating, Storage & Exergy Cascade:**
  - District thermal demand (7,500 homes): 37,610 MWh_th/yr (exact 37,612 MWh_th/yr; base coverage 228.3%; conservative u=0.70, f_cap=0.80 → 49,056 MWh_th/yr = 130.4%).
  - Winter cold-snap peak: 9.52 MW_th vs 9.80 MW_th output (+0.28 MW_th surplus); at 60% stress load = 5.88 MW_th (−3.64 MW_th deficit covered by 2,500 m³ diurnal/cold-snap insulated thermal store + 8.0 MW_th natural-refrigerant backup HTHPs).
  - Secondary 45°C water flow (20% gross, ΔT = 15 K): 1,923.1 L/min (32.05 L/s).
  - Scenario B Fallback (45°C → 70°C HTHP): COP 4.6–6.9.
  - Hybrid 5GDHC twin-pipe HDPE ring (~30 km, near-ambient 15–25°C, loss 7.0 W/m = 1,840 MWh_th/yr or 4.9%; primary pumping 22.1 kW_e).
  - Summer Surplus (30,148 MWh_th) & Exergy-Priority Cascade:
    - SrBr₂/K₂CO₃ Thermochemical Energy Storage (TCES): ~35 MWh_th
    - TI-PTES Carnot Battery: 2 MW_e / 16 MWh_e (wind/peak arbitrage)
    - Summer residual micro-ORC: ~418–440 kW_e (≤ 2,412 MWh_e/yr ceiling, ~2,000 MWh_e/yr practical) + MOF chillers + N+1 dry coolers.
- **Loop 3 — Rainwater Harvesting & Ultrapure Water Train:**
  - Dublin Airport rainfall: 772.5 mm/yr (C_r = 0.80, η_f = 0.90) → 55,620–62,379 m³/yr per 100k m² inert roof catchment (100–160k m² total).
  - Avoided cooling-tower potable water: 158,000–175,000 m³/yr.
  - Subterranean RC drought cistern: 10,000 m³ (sized for 45–66 day Dublin dry spells).
  - Semiconductor Grade C ultrapure train: UF + MCDI/2D + DI (≤ 1.0 μS/cm, 1–18.2 MΩ·cm) + UV-C LED + BDD/Ti₄O₇ PFAS mineralisation.

## 4. Optical Ring, Autonomous Pods & SDN Electro-Thermal Shifting
- **3.0 km NANF Hollow-Core Fiber (HCF) + Co-Packaged Optics (CPO) Ring** interconnecting all 5 vaults:
  - Inter-vault ring hop (1.00 km): 8.16 μs RTT (±1.2 μs jitter).
  - Subterranean vault to street/pod (0.25 km HCF+CPO): 3.17 μs net RTT, **143.2 μs** 6G V2X/XR E2E RTT (±2.0 μs jitter), vs Regional Core Cloud (75 km SMF): 7.64 ms transport RTT (and 90 ms full cloud control loop = 1.25 m blind travel at 50 km/h vs < 2 mm local).
  - 87.34% local traffic breakout (saves ~455 MWh_e/yr WAN telecom power).
- **SDN / SRv6 Electro-Thermal Workload Shifting:** Shifts flexible AI compute across the 5 vaults in real time to match spatial heat demand — **74.9×** more energy-efficient than pumping hot water across the district.

## 5. Financial & Carbon KPIs (Case 2 Whole-System, 2045 Real, 30-Yr 2045–2075)
- **Initial CapEx:** €137.08M total (€57.73M District Utility + €79.35M DC Operator incl. €15.30M CRU PV/BESS) vs Counterfactual A (7,500 ASHPs + PUE 1.56 DC) €215.59M.
- **EBITDA:** +€22.20M/yr (€34.20M revenue − €12.00M OpEx; mid-life RepEx €25.70M across 30 yr).
- **Returns:** Simple Payback **6.2 yr**, 30-yr IRR **15.7%**, NPV @ 4% **+€232.98M** (discounted payback 7.2 yr), NPV @ 8% **+€105.07M** (8.9 yr).
- **Avoided Carbon:** ~8,020 tCO₂/yr displaced heating + ~15,822 tCO₂/yr avoided PUE grid power (47,742 MWh_e/yr vs PUE 1.56).
- **Resident Survey (N = 109):** DC comfort 3.19/5.00 (64.8% pos/neutral); Recycled water 3.86/5.00 (72.0% pos); #1 heating priority: Cheapest Bill (41.8%) + 24/7 Reliability (31.6%) = 73.5%; #1 concern: Noise/Vibration (33.7%) → directly mitigated by plaza siting, floating slabs, 1.0 m trenches, <110 dB water-mist.
