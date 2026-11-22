# RAKDA Ltd. (Group A-10) — 2045 Circular Urban Digital Twin

Interactive 3D Engineering Simulation & Digital Twin for **Trinity College Dublin 1E17 Engineering Practice 2 (*The Circular City 2045*)**, grounded strictly in [SPEC.md](./SPEC.md) (*RAKDA Ltd. Report — Draft 3.2*, 21 Nov 2026).

---

## Quick Launch (Windows)

### Option A — Standalone Windows Executable (Recommended, No Node.js Required)
Double-click **`RAKDA_3D_Sim.exe`** in this folder. It starts an embedded local HTTP static file server on `http://localhost:8170` (automatically selecting a free port if `8170` is busy) and opens the 3D simulation in your default browser.

CLI options (PowerShell / Command Prompt):
```powershell
.\RAKDA_3D_Sim.exe                        # Default port 8170 (or PORT env var) + auto-open browser
.\RAKDA_3D_Sim.exe --port 8200            # Custom preferred port
.\RAKDA_3D_Sim.exe --no-browser           # Start server without opening a browser
.\RAKDA_3D_Sim.exe --help                 # Show CLI usage
```
Keep the launcher console window open while using the simulation; press `Ctrl+C` or close the window to stop the server cleanly.

#### Rebuilding `RAKDA_3D_Sim.exe`
The standalone C# launcher source and build scripts live in `launcher/` and compile with the built-in Windows .NET Framework 4.x compiler (`csc.exe`, zero external SDKs required):
```powershell
.\launcher\build.ps1
# or from Command Prompt:
launcher\build.bat
```

### Option B — Node.js Batch Launcher
Double-click **`start-sim.bat`**. It automatically opens `http://localhost:8170` in your default browser and starts the local zero-dependency Node static server.

### Option C — Node.js Terminal
```bash
node serve.mjs
# Optional custom port:
node serve.mjs --port 8200
```
Then open `http://localhost:8170` in Chrome or Edge.

> **Note:** The 3D viewport loads Three.js `0.170.0` via `cdn.jsdelivr.net` importmap. If offline, the HUD displays a clear fallback warning banner while keeping all physics equations, scenarios, and telemetry interactive.

---

## Interactive Controls & Keyboard Shortcuts

| Key / Control | Action |
|---|---|
| **Left Mouse Drag** | Orbit camera around the 180 ha circular district & Level −4 vaults |
| **Right Mouse Drag / Scroll** | Pan & zoom |
| **Click any 3D Asset** | Open live Draft 3.2 specs & telemetry in the **Subsystem Inspector** |
| **`1` – `6`** | Fly smoothly between the 6 camera presets (Overview, Vaults, Hub, Vault 3, Pods, Top-Down) |
| **`X`** | Toggle ground X-Ray cutaway opacity (`18%` ↔ `92%`) |
| **`Space`** | Play / Pause simulation animation |
| **`H`** | Toggle clean Presentation Mode (hides side drawers for live demos) |
| **`R`** | Reset camera to District Overview |

---

## Engineering Scenarios & Architecture

1. **Nominal 2045 Design** — 10.0 MW IT load (`PUE = 1.015`, `88,914 MWh_e/yr` gross, `77,958 MWh_e/yr` net after `10,956 MWh_e/yr` PV), `9.80 MW_th` (`85,848 MWh_th/yr` = `228.3%` annual district demand), `4,635.3 L/min` primary `75°C` flow (`4,543 L/min` net), `1,923.1 L/min` secondary `45°C` flow.
2. **Winter Cold Snap (−5°C)** — `9.52 MW_th` peak demand satisfied directly (`+0.28 MW_th` surplus).
3. **Cold Snap + 60% Stress** — `5.88 MW_th` captured vs `9.52 MW_th` peak (`−3.64 MW_th` deficit) covered seamlessly by the `2,500 m³` thermal store (`~87.1 MWh_th`) and `8.0 MW_th` natural-refrigerant backup HTHPs (`COP 4.6–6.9`).
4. **Vault 3 Fault + SDN Reroute** — Vault 3 goes offline; the 3.0 km NANF HCF optical ring reroutes V2X beams to healthy vaults and SDN shifts workload (`74.9×` more efficient than pumping hot water).
5. **Summer Exergy Cascade** — `1.45 MW_th` DHW baseline creates `8.35 MW_th` surplus routed across `35 MWh_th` SrBr₂/K₂CO₃ TCES, `2 MW_e / 16 MWh_e` TI-PTES Carnot battery, `~422 kW_e` micro-ORC (`≤ 440 kW_e` cap), MOF chillers, and N+1 dry coolers.
6. **Dublin 60-Day Drought** — `0×` rainfall input; `10,000 m³` RC drought cistern sustains 45–66 day dry spells while closed-loop cooling avoids `166,500 m³/yr` cooling-tower potable water.
7. **Conservative Stress (`u=0.70, f_cap=0.80`)** — `49,056 MWh_th/yr` output (`130.4%` coverage of `37,610 MWh_th/yr` district demand).

---

## Verification

```bash
node --check src/physics.js src/scene.js src/district.js src/vaults.js src/flows.js src/ui.js src/main.js serve.mjs
node --test tests/physics.test.mjs
```
