# RAKDA Ltd. (Group A-10) — 2045 Circular Urban Digital Twin (`docs/`)

Live GitHub Pages static web deployment for **Trinity College Dublin 1E17 Engineering Practice 2 (*The Circular City 2045*)**, grounded strictly in [SPEC.md](./SPEC.md) (*RAKDA USCI (Nov 2026)* / `RAKDA-USCI.docx`, 27 Nov 2026) · **[Live Site](https://zetbcn.github.io/EP2-Proj-RAKDA/)** · **[rakda.ltd](https://rakda.ltd/)**.

> **Note on Native Desktop App:** The standalone Windows OpenGL 3.3 desktop application (`RAKDA_3D_Sim.exe` and C# source in `sim/native/`) lives in [`../sim/`](../sim/README.md) so that `/docs` remains a clean static web deployment root for GitHub Pages. All web sources (`index.html`, `styles.css`, `SPEC.md`, `serve.mjs`, `start-sim.bat`, `src/*`, `tests/*`) are kept 100% byte-identical between `docs/` and `sim/` and verified in CI via `sim/scripts/check-sync.mjs`.

---

## Quick Launch (Local Web Server)

### Option A — Node.js Batch Launcher (Windows)
Double-click **`start-sim.bat`**. It automatically opens `http://localhost:8170` in your default browser and starts the local zero-dependency Node static server.

### Option B — Node.js Terminal
```bash
node serve.mjs
# Optional custom port:
node serve.mjs --port 8200
```
Then open `http://localhost:8170` in Chrome or Edge.

> **Note:** The 3D viewport loads Three.js `0.170.0` via `cdn.jsdelivr.net` importmap (using PMREM `RoomEnvironment`, `UnrealBloomPass`, static geometry batching, 3D selection reticle, and WebGL context-loss recovery). If offline, the HUD displays a fallback warning banner while keeping all physics equations, scenarios, and telemetry interactive.

---

## Interactive Controls & Keyboard Shortcuts

| Key / Control | Action |
|---|---|
| **Left Mouse Drag** | Orbit camera around the 180 ha circular district & Level −4 vaults |
| **Right Mouse Drag / Scroll** | Pan & zoom |
| **Click any 3D Asset** | Open live RAKDA USCI (Nov 2026) specs & telemetry in the **Subsystem Inspector** |
| **Theme Toggle (`☀ Paper` / `☾ Slate`)** | Switch HUD between Dark Slate and **`rakda.ltd` Light Paper** (`#FAF7F2` / `#1A0F3C` / `#FF5C9A`), persisted in `localStorage` |
| **`1` – `6`** | Fly smoothly between the 6 camera presets (Overview, Vaults, Hub, Vault 3, Pods, Top-Down) |
| **`X`** | Toggle ground X-Ray cutaway opacity (`18%` ↔ `92%`) |
| **`Space`** | Play / Pause simulation animation |
| **`H`** | Toggle clean Presentation Mode (hides side drawers for live demos) |
| **`R`** | Reset camera to District Overview |

---

## Engineering Scenarios & Architecture

1. **Nominal 2045 Design** — 10.0 MW IT load (`PUE = 1.015`, `88,914 MWh_e/yr` gross, `77,958 MWh_e/yr` net after `10,956 MWh_e/yr` PV), `9.80 MW_th` (`85,848 MWh_th/yr` = `228%` gross / `209%` net of `37,612 MWh_th/yr` demand + `3,421 MWh_th/yr` EN 253 pipe loss at `13.0 W/m`), `76.5 L/s` primary `75°C` flow, `32.0 L/s` secondary `45°C` flow, break-even `u = 47.8%`.
2. **Winter Design Day (−3°C)** — `9.52 MW_th` building peak + `0.39 MW_th` EN 253 loss = `9.91 MW_th` vs `9.80 MW_th` (`−0.11 MW_th` gap covered by the `2,500 m³` thermal store).
3. **Cautious Winter (−3°C, u=0.70, f_cap=0.80)** — `5.60 MW_th` captured vs `9.91 MW_th` peak (`−4.31 MW_th` gap) covered by the `2,500 m³` store (`101.6 MWh_th` gross / `91.4 MWh_th` usable, `15 h` carry) + `8.0 MW_th` backup HTHPs (`48 h` resilience; Scenario B `40→75°C` at `COP 4.0–6.0`, `+€33/MWh`).
4. **Vault 3 Fault + SDN Reroute** — Vault 3 goes offline; the 3.0 km NANF HCF optical ring reroutes V2X beams (`143.2 μs` E2E) and SDN shifts workload (`70.1×` more efficient than pumping hot water: `250 W` vs `17,530 W`).
5. **Summer Surplus Cascade** — `44,816 MWh_th/yr` annual surplus (`35,852 MWh_th/yr` @ `75°C`) routed across direct `70–75°C` commercial heat sales, PFAS-free `~0.4 MW_e` Micro-ORC (`17.2%` Carnot @ `15°C` sink → `7.2–8.3%` efficiency, `2.60–2.97 GWh_e/yr`), and N+1 dry coolers.
6. **Dublin 44-Day Drought** — `0×` rainfall input; `10,000 m³` RC cistern (`6,700 m³` min storage = `44 d` reserve at `152 m³/d`) & Grade A UPW loop (`≤3,265 m³/yr`) avoid `157,680–175,200 m³/yr` cooling-tower potable water.
7. **Cautious Base (`u=0.70, f_cap=0.80`)** — `49,056 MWh_th/yr` output (`130%` gross / `120%` net coverage; break-even `u = 58.6%`).

---

## Verification

```bash
node --check src/physics.js src/scene.js src/district.js src/vaults.js src/flows.js src/ui.js src/main.js serve.mjs
node --test tests/physics.test.mjs
```
