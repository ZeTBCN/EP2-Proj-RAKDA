# RAKDA Ltd. (Group A-10) — 2045 Circular Urban Digital Twin (`sim/`)

Interactive 3D Engineering Simulation & Digital Twin for **Trinity College Dublin 1E17 Engineering Practice 2 (*The Circular City 2045*)**, grounded strictly in [SPEC.md](./SPEC.md) (*RAKDA USCI (Nov 2026)* / `RAKDA-USCI.docx`, 27 Nov 2026) · **[rakda.ltd](https://rakda.ltd/)**.

---

## Quick Launch (Windows)

### Option A — 100% Native Windows OpenGL 3.3 Desktop App (Recommended)
Double-click **`RAKDA_3D_Sim.exe`** in this folder. It launches directly as a standalone **native Windows desktop application** with embedded hardware-accelerated **OpenGL 3.3 Core** 3D rendering, Per-Monitor V2 DPI awareness, dynamic UI scaling (`80%`–`150%`), and smooth **60 FPS** frame pacing (direct GPU rendering — **zero browser, zero WebView2, zero Node.js, and zero internet required**).

CLI options (PowerShell / Command Prompt):
```powershell
.\RAKDA_3D_Sim.exe                            # Open interactive 1440x900 native desktop app (60 FPS cap)
.\RAKDA_3D_Sim.exe --ui-scale 130             # Launch with scaled HUD (presets: 80, 90, 100, 115, 130, 150)
.\RAKDA_3D_Sim.exe --benchmark                # Run 120-frame Overview + X-Ray GPU benchmark (60 FPS capped)
.\RAKDA_3D_Sim.exe --benchmark --uncapped     # Run raw uncapped GPU throughput benchmark
.\RAKDA_3D_Sim.exe --screenshot <output_dir>  # Render & export 5 full-window PNG verification captures
.\RAKDA_3D_Sim.exe --help                     # Show CLI usage
```

#### Rebuilding `RAKDA_3D_Sim.exe`
The native C# WinForms + OpenGL 3.3 desktop application source and physics test suite live in `native/` and compile directly with the built-in Windows .NET Framework 4.x x64 compiler (`csc.exe`, zero external SDKs required):
```powershell
.\native\build.ps1
# or from Command Prompt:
native\build.bat
```
This compiles and runs `native\PhysicsTests.exe` (10/10 RAKDA USCI (Nov 2026) engineering test suites) and outputs `RAKDA_3D_Sim.exe` in `sim/`.

### Option B — Browser / Web Edition (Node.js)
The upgraded Three.js web version (synced identically with `../docs/` for GitHub Pages) runs in Chrome or Edge:
```bash
# Double-click start-sim.bat, or run:
node serve.mjs
# Optional custom port:
node serve.mjs --port 8200
```
Then open `http://localhost:8170` in Chrome or Edge. Includes a header **Theme Toggle** between **Dark Slate** and **`rakda.ltd` Light Paper** (`#FAF7F2` / `#1A0F3C` / `#FF5C9A`).

> **Note:** The native desktop app (`RAKDA_3D_Sim.exe`) runs **100% offline** with direct OpenGL 3.3 GPU acceleration. The browser edition loads Three.js `0.170.0` via `cdn.jsdelivr.net` importmap (displaying an offline fallback banner if disconnected while keeping physics and telemetry active).

---

## Interactive Controls & Keyboard Shortcuts

| Key / Control | Action |
|---|---|
| **Left Mouse Drag** | Orbit camera around the 180 ha circular district & Level −4 vaults |
| **Right Mouse Drag / Scroll** | Pan & zoom |
| **Click any 3D Asset** | Open live RAKDA USCI (Nov 2026) specs & telemetry in the **Subsystem Inspector** |
| **`1` – `6`** | Fly smoothly between the 6 camera presets (Overview, Vaults, Hub, Vault 3, Pods, Top-Down) |
| **`X`** | Toggle ground X-Ray cutaway opacity (`18%` ↔ `92%`) |
| **`Space`** | Play / Pause simulation animation |
| **`H`** | Toggle clean Presentation Mode (hides side drawers for live demos) |
| **`R`** | Reset camera to District Overview |
| **`Ctrl +` / `Ctrl −` / `Ctrl 0`** | Zoom native HUD UI scale (`80%`–`150%`) or reset to `100%` (native app) |

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

```powershell
# Native Desktop App build + 10/10 C# RAKDA USCI physics test suite
.\native\build.ps1
.\RAKDA_3D_Sim.exe --benchmark

# Web edition syntax, physics unit tests & docs/sim sync parity
node --check src/physics.js src/scene.js src/district.js src/vaults.js src/flows.js src/ui.js src/main.js serve.mjs scripts/check-sync.mjs
node --test tests/physics.test.mjs
node scripts/check-sync.mjs
```
