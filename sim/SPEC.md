# RAKDA Ltd. (Group A-10) — EP2 3D Simulation Engineering Specification
Source of Truth: *RAKDA-USCI.docx* (`calc.py` + `innovations.py`, RAKDA USCI, Nov 2026) · TCD 1E17 *The Circular City (2045)*

## 1. District Masterplan (180.0 ha · 20,000 Residents · 7,500 Homes)
- **Detached houses (2–3 fl.):** 900 units, 900 bldgs (22.5 ha), 3,200 residents
- **Townhouses / semi-detached (2–3 fl.):** 2,100 units, 2,100 bldgs (30.0 ha), 6,800 residents
- **Low-rise apartments (3–4 fl.):** 1,200 units, 40 blocks (12.0 ha), 3,000 residents
- **Mid-rise apartments (5–7 fl.):** 3,300 units, 55 blocks (22.0 ha), 7,000 residents
- **Total residential:** 7,500 units, 3,095 buildings (90.0 ha, 83.3 dph); 8,000-home sensitivity = 40,120 MWh_th/yr (196% net coverage)
- **Roads, autonomous transit & pedestrian corridors:** 36.0 ha (20.0%)
- **District parks & blue-green corridors:** 25.0 ha (13.9%)
- **Schools & community facilities:** 15.0 ha (8.3%) — 4 primary, 2 secondary, health centre, sports hub (`15.54 ha` school gap flagged)
- **Retail, offices & civic/utilities:** 14.0 ha (7.8%)

## 2. Subterranean Data Centre Architecture (10.0 MW IT Load)
- **Layout:** 5 × 2.0 MW subterranean reinforced-concrete vaults at Level −4 bedrock (~350 m² IT/hall civil footprint, 25 × 16 m internal, 8–12 m depth), sited beneath public plazas (zero vaults under residential foundations). **100-year** structural design life (EN 1990:2002).
- **Safety & Acoustic Isolation:** Floating acoustic slabs with elastomeric bearings, 1.0 m vibration-isolation trenches, low-noise water-mist fire suppression (< 110 dB).
- **Power Delivery & CRU Compliance:** **800 VDC** Solid-State Transformers (SSTs, −5% conversion loss vs 54 V; HTS busways optional pending cryogenic draw verification), PUE = 1.015 (88,914 MWh_e/yr gross; 77,958 MWh_e/yr net grid after 10,956 MWh_e/yr BTM PV). CRU/EirGrid LEU assets: 12 MW_p rooftop solar PV + 8 MW / 32 MWh LFP BESS (€15.30M).
- **Compute Density:** 2045 rack power 250–1,200 kW/rack (40 / 17 / 9 racks total); 25–250 ZettaOPS AI compute output (>99.9% leasable).

## 3. Three-Loop Thermal, Exergy & Hydrological Architecture
- **Loop 1 — Primary In-Silicon Microfluidic Cooling (75°C / ΔT = 25 K):**
  - 98% heat capture at full load → 9.80 MW_th continuous (85,848 MWh_th/yr).
  - Primary 75°C flow (80% share): **76.5 L/s** (4,590.0 L/min gross).
  - Secondary 45°C flow (20% share): **32.0 L/s** (1,920.0 L/min gross); preheats returning 35°C district water toward 42°C.
  - 15-min vault thermal buffers: **215 m³** total (**43 m³** per vault, ΔT = 10 K, €0.38M).
  - Instantaneous building HIUs deliver ≥ 55°C DHW (60°C sizing basis) with zero stored DHW (eliminating Legionella).
- **Loop 2 — Pre-Insulated 70/35°C EN 253 District Heating Network, Storage & Surplus Cascade:**
  - District thermal demand (7,500 homes): **37,612 MWh_th/yr** (`26,362` MWh DHW = `70.1%` / `3.01 MW` baseload + `11,250` MWh space heating; `5.01 MWh/home/yr`).
  - Pre-insulated 70/35°C EN 253:2019 steel pipe pair (30 km, R = 6.53 m·K/W): **13.0 W/m** = **3,421 MWh_th/yr (9.1% loss)**, **0.39 MW** winter loss; **259 MWh_e/yr** pumping (21.9 kW primary + 68 MWh/yr network).
  - Heat coverage & break-even: **228% gross / 209% net** base; **130% gross / 120% net** cautious (`u = 0.70, f_cap = 0.80` → 49,056 MWh_th/yr); break-even **`u = 47.8%`** (**`58.6%`** at `f_cap = 0.80`).
  - Winter design day (**−3°C**): **9.52 MW_th** building demand + **0.39 MW_th** pipe loss = **9.91 MW_th** vs **9.80 MW_th** output = **−0.11 MW_th** (met by store); cautious case gap = **4.31 MW_th**.
  - Stratified thermal store: **2,500 m³** (ΔT = 35 K, **101.6 MWh_th** gross / **91.4 MWh_th** usable, **15 h** winter carry, **48 h** peak resilience with **8.0 MW_th** natural-refrigerant backup HTHPs).
  - Scenario B Fallback (45°C silicon limit → 75°C cond., 5 K pinch at 40°C): **COP 4.0–6.0** (`COP_Carnot = 9.95`), full network boost **5.9–8.8 GWh_e/yr**, requires ~8.5 MW (€5.1M) added HTHPs (**+€33/MWh**, LCOH → **€126/MWh**).
  - Summer Surplus (**44,816 MWh_th/yr** total; **35,852 MWh_th/yr** @ 75°C) & 3-Tier Cascade:
    1. Direct 70–75°C commercial heat sales (1.8×–12.5× ORC value).
    2. PFAS-free Micro-ORC (**17.2% Carnot** @ 15°C sink → **7.2–8.3% η_ORC**, **2.60–2.97 GWh_e/yr**, **~0.4 MW_e**, **€0.35M/yr** / +€3.5M utility NPV).
    3. N+1 low-noise dry coolers (zero evaporative water loss). *(TI-PTES & SrBr₂ TCES removed from base CapEx/cascade; optional R&D only).*
- **Loop 3 — Rainwater Harvesting & Ultrapure Water Train:**
  - Dublin Airport rainfall: 772.5 mm/yr (`C_r = 0.80, η_f = 0.90`, 10 ha inert roofs) → **55,620 m³/yr** (**152 m³/d**).
  - Subterranean RC drought cistern: **10,000 m³** (`6,700 m³` minimum storage across 3,318 m³ seasonal swing, **44-day** drought reserve).
  - Avoided cooling-tower potable water: **157,680–175,200 m³/yr**.
  - Three-grade segregation: **Grade A = UPW** (`≤ 3,265 m³/yr`, `< 5.9%` yield; UF + MCDI + DI + UV-C LED + BDD/Ti₄O₇ PFAS oxidation, `≤ 1.0 μS/cm`, `1–18.2 MΩ·cm`), **Grade B = toilets & laundry** (no dishwashers), **Grade C = park irrigation**.

## 4. Optical Ring, Autonomous Pods & SDN Electro-Thermal Shifting
- **3.0 km NANF Hollow-Core Fiber (HCF) + Co-Packaged Optics (CPO) Ring** interconnecting all 5 vaults:
  - Inter-vault ring hop (1.00 km): 8.16 μs RTT (±1.2 μs jitter).
  - Subterranean vault to street/pod (0.25 km HCF+CPO): **3.17 μs** net RTT, **143.2 μs** wireless E2E RTT (±2.5 μs jitter), **48.2 μs** fixed-fibre E2E RTT, vs Regional Core Cloud (75 km SMF): 7.64 ms wireless / 6.34 ms fibre RTT (90 ms full cloud control loop = 1.25 m blind travel at 50 km/h vs < 2 mm local).
  - **87.34%** local traffic breakout (17.47 / 20.0 Tbps; saves ~455 MWh_e/yr WAN telecom power).
- **SDN / SRv6 Electro-Thermal Workload Shifting:** Shifts flexible AI compute across the 5 vaults in real time — **70.1×** more energy-efficient than pumping hot water across 1.2 km of EN 253 pipe (**250 W** optical transceivers vs **17,530 W** pump + thermal loss).

## 5. Financial, Carbon & Survey KPIs (2045 / 2026 Real, 30-Yr 2045–2075)
- **Initial CapEx:** **€136.66M (€136.7M)** total (**€57.31M** Public Heat Utility + **€79.35M** DC Operator incl. €15.30M CRU PV/BESS).
- **Colocation & EBITDA:** **€145/kW/mo** rent (€17.40M/yr, CBRE 2026); **€19.6M/yr** EBITDA (Utility **+€3.64M/yr**, DC **+€15.97M/yr**; €0.33M/yr water/WAN societal credits excluded from EBITDA).
- **DCF Returns:**
  - **Case 2 (Whole Project):** **13.7% IRR**, **7.0 yr** simple payback, NPV **+€189M** (4%) / **+€77M** (8%). Stress (+30% CapEx, 60% util.): **4.3% IRR**.
  - **Case 1 (Heat Utility):** **4.2% IRR**, **15.8 yr** payback, NPV **+€1.1M** (4%) / **−€18.7M** (8%), Net LCOH **€93/MWh** (**€68/MWh** w/ 30% grant; Stress: **€129/MWh**).
- **30-Yr Societal TCO & Home Bills (5.01 MWh/yr):** USCI **€520M** (€356M PV) vs Gas **€826M** (€520M PV) vs ASHP **€936M** (€610M PV). Annual household heating bill: USCI **€536/yr** vs Gas **€1,001/yr** vs ASHP **€1,691/yr**.
- **Carbon (A1–A4 & Operational, Ali 2026b):** Embodied **4,628 t CO₂e** CEM I (**4,178 t** GGBS, **3,908 t** LC3). Avoided heating **7,230–7,270 t/yr** vs gas (**6.8–7.5 mo** payback); avoided PUE **7,780–14,700 t/yr**; residual DC grid **12,707–24,011 t/yr**.
- **Resident Survey (N = 105 cleaned / 109 raw):** DC comfort **3.20/5** (47.1% pos / 17.3% neut / 35.6% low; Apts 3.92, Students 3.8, Houses 2.87); Recycled water **3.87/5** (72.8% pos); #1 heating priority: Cheapest Bill (**42.6%**) + 24/7 Reliability (**33.0%**); Top concerns: Noise/Vibration (**35.1%**), Fire/Electrical (**26.6%**), Water Contamination (**26.6%**).
