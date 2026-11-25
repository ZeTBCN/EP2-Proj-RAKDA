using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace RakdaSim
{
    public sealed class MainForm : Form
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly float[] UiScalePresets = new float[] { 0.80f, 0.90f, 1.00f, 1.15f, 1.30f, 1.50f };
        private const double TargetFps = 60.0;
        private const double TargetFrameMs = 1000.0 / TargetFps;

        // Active global UI scale factor (0.80 - 1.50)
        internal static float UiScale = 1.00f;

        internal static int S(float px)
        {
            int v = (int)Math.Round(px * UiScale);
            return v < 1 ? 1 : v;
        }

        internal static int S0(float px)
        {
            return (int)Math.Round(px * UiScale);
        }

        // Dark Engineering Palette
        internal static readonly Color ColBgBase = Color.FromArgb(11, 18, 32);        // #0b1220
        internal static readonly Color ColPanelBg = Color.FromArgb(15, 23, 42);       // #0f172a
        internal static readonly Color ColCardBg = Color.FromArgb(19, 30, 51);        // #131e33
        internal static readonly Color ColControlBg = Color.FromArgb(24, 36, 59);     // #18243b
        internal static readonly Color ColBtnBg = Color.FromArgb(30, 41, 59);         // #1e293b
        internal static readonly Color ColBtnHover = Color.FromArgb(36, 56, 86);
        internal static readonly Color ColBtnActiveBg = Color.FromArgb(14, 62, 86);
        internal static readonly Color ColBorder = Color.FromArgb(42, 58, 82);
        internal static readonly Color ColBorderAccent = Color.FromArgb(28, 126, 152);
        internal static readonly Color ColTextMain = Color.FromArgb(241, 245, 249);   // #f1f5f9
        internal static readonly Color ColTextSec = Color.FromArgb(203, 213, 225);    // #cbd5e1
        internal static readonly Color ColTextMuted = Color.FromArgb(148, 163, 184);  // #94a3b8
        internal static readonly Color ColCyan = Color.FromArgb(34, 211, 238);        // #22d3ee
        internal static readonly Color ColSky = Color.FromArgb(14, 165, 233);         // #0ea5e9
        internal static readonly Color ColOrange = Color.FromArgb(255, 107, 44);      // #ff6b2c
        internal static readonly Color ColEmerald = Color.FromArgb(16, 185, 129);     // #10b981
        internal static readonly Color ColViolet = Color.FromArgb(192, 132, 252);     // #c084fc
        internal static readonly Color ColAmber = Color.FromArgb(245, 158, 11);       // #f59e0b
        internal static readonly Color ColRed = Color.FromArgb(239, 68, 68);          // #ef4444
        internal static readonly Color ColRedSoft = Color.FromArgb(254, 202, 202);
        internal static readonly Color ColEmeraldSoft = Color.FromArgb(110, 231, 183);

        // Dynamic Scaled Fonts (Pixel-sized so OS DPI never distorts proportions)
        internal static Font FontBrandTitle;
        internal static Font FontBrandSub;
        internal static Font FontBadge;
        internal static Font FontSection;
        internal static Font FontTag;
        internal static Font FontBody;
        internal static Font FontBodyBold;
        internal static Font FontSmall;
        internal static Font FontKpiLabel;
        internal static Font FontKpiVal;
        internal static Font FontMono;
        internal static Font FontMonoBold;
        internal static Font FontInspectorTitle;

        private static readonly List<Font> _retiredFonts = new List<Font>();

        static MainForm()
        {
            RebuildFonts(1.00f);
        }

        private static void RebuildFonts(float scale)
        {
            UiScale = scale;
            // Hold old fonts until form disposes so no transient paint ever touches a disposed Font
            if (FontBrandTitle != null) _retiredFonts.Add(FontBrandTitle);
            if (FontBrandSub != null) _retiredFonts.Add(FontBrandSub);
            if (FontBadge != null) _retiredFonts.Add(FontBadge);
            if (FontSection != null) _retiredFonts.Add(FontSection);
            if (FontTag != null) _retiredFonts.Add(FontTag);
            if (FontBody != null) _retiredFonts.Add(FontBody);
            if (FontBodyBold != null) _retiredFonts.Add(FontBodyBold);
            if (FontSmall != null) _retiredFonts.Add(FontSmall);
            if (FontKpiLabel != null) _retiredFonts.Add(FontKpiLabel);
            if (FontKpiVal != null) _retiredFonts.Add(FontKpiVal);
            if (FontMono != null) _retiredFonts.Add(FontMono);
            if (FontMonoBold != null) _retiredFonts.Add(FontMonoBold);
            if (FontInspectorTitle != null) _retiredFonts.Add(FontInspectorTitle);

            FontBrandTitle = new Font("Segoe UI Semibold", 12.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontBrandSub = new Font("Segoe UI", 10.5f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            FontBadge = new Font("Segoe UI Semibold", 10.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontSection = new Font("Segoe UI Semibold", 11f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontTag = new Font("Segoe UI", 9.5f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            FontBody = new Font("Segoe UI", 11f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            FontBodyBold = new Font("Segoe UI Semibold", 11f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontSmall = new Font("Segoe UI", 10f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            FontKpiLabel = new Font("Segoe UI Semibold", 9f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontKpiVal = new Font("Consolas", 12.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontMono = new Font("Consolas", 10.5f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            FontMonoBold = new Font("Consolas", 10.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            FontInspectorTitle = new Font("Segoe UI Semibold", 12.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        // DWM dark titlebar, DarkMode scrollbar, and 1ms multimedia timer interop
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        // Simulation state
        private readonly bool _automatedMode;
        private readonly bool _uncapped;
        private bool _timerPeriodActive = false;
        private SimState _state;
        private SimResult _sim;
        private readonly LayerVisibility _layers;
        private readonly List<PickTarget> _pickTargets;

        private bool _playing = true;
        private float _simSpeed = 1.0f;
        private bool _autoDay = false;
        private float _groundOpacity = 0.78f;
        private bool _presentationMode = false;
        private bool _leftVisible = true;
        private bool _rightVisible = true;
        private bool _suppressUiEvents = false;
        private string _inspectedKey = null;
        private string _renderedInspectedKey = null;
        private int _activeCamIdx = 0;

        // OpenGL & Scene
        private Renderer _renderer;
        private SubterraneanScene _subterraneanScene;
        private SurfaceScene _surfaceScene;
        private FlowsScene _flowsScene;
        private bool _rendererReady = false;
        private string _rendererInitError = null;
        private bool _glInitialized = false;

        // Frame loop & 60 FPS pacing
        private readonly Stopwatch _frameWatch;
        private double _lastTickSec = 0.0;
        private float _simElapsed = 0f;
        private double _hudTimerSec = 0.0;
        private double _dayUiTimerSec = 0.0;
        private float _pacedFps = 0f;
        private float _pacedFrameMs = 0f;

        // Layout root & major regions
        private Panel _rootContainer;
        private HeaderBarPanel _topHeader;
        private FooterDockPanel _bottomDock;
        private DarkScrollPanel _leftPanel;
        private DarkScrollPanel _rightPanel;
        private Panel _viewportHost;
        private GlViewportControl _glViewport;
        private Label _errorBanner;

        // Top header controls
        private KpiPill _kpiIt;
        private KpiPill _kpiPue;
        private KpiPill _kpiCov;
        private KpiPill _kpiRtt;
        private KpiPill _kpiCo2;
        private KpiPill _kpiNpv;
        private KpiPill _kpiGpuFps;
        private DarkButton _btnToggleLeft;
        private DarkButton _btnToggleRight;
        private DarkButton _btnPresentTop;

        // Left panel controls
        private SectionCard _cardScenarios;
        private SectionCard _cardParams;
        private SectionCard _cardFaults;
        private SectionCard _cardLayers;
        private SectionCard _cardCam;
        private Panel _leftSpacer;
        private Label _lblFaultHint;
        private readonly Dictionary<string, DarkButton> _presetButtons;
        private readonly List<DarkButton> _presetButtonOrder;
        private AccentDescriptionBox _scenarioDescBox;
        private DarkSlider _sliderUtil;
        private DarkSlider _sliderFcap;
        private DarkSlider _sliderTemp;
        private DarkSlider _sliderRain;
        private DarkSlider _sliderTime;
        private DarkSlider _sliderXray;
        private DarkToggleSwitch _toggleSdn;
        private DarkToggleSwitch _toggleScenarioB;
        private DarkButton[] _vaultFaultButtons;
        private DarkCheckPill _chkSurface;
        private DarkCheckPill _chkHeatmap;
        private DarkCheckPill _chkLoop1;
        private DarkCheckPill _chkLoop2;
        private DarkCheckPill _chkLoop3;
        private DarkCheckPill _chkOptical;
        private DarkCheckPill _chkVaults;
        private DarkButton[] _cameraButtons;

        // Right panel controls
        private SectionCard _cardInspector;
        private Label _inspTitleLbl;
        private Label _inspDescLbl;
        private Label _inspSpecsLbl;
        private KvTableControl _inspMetricsKv;

        private SectionCard _cardThermal;
        private DarkProgressBar _barCaptured;
        private DarkProgressBar _barDemand;
        private KvTableControl _kvThermalCore;
        private Panel _divTherm;
        private KvTableControl _kvThermalCascade;

        private SectionCard _cardGridWater;
        private DarkProgressBar _barCistern;
        private KvTableControl _kvGridWater;

        private SectionCard _cardFinance;
        private KvTableControl _kvFinance;
        private Panel _rightSpacer;

        // Bottom dock controls
        private DarkButton _btnPlay;
        private DarkButton _btnSpeed;
        private DarkButton _btnDayCycle;
        private DarkButton _btnXray;
        private DarkButton _btnReset;
        private DarkButton _btnScale;
        private DarkButton _btnPresentDock;
        private Label _lblShortcuts;

        public bool IsRendererReady
        {
            get { return _rendererReady; }
        }

        public string RendererInitError
        {
            get { return _rendererInitError; }
        }

        public MainForm(bool automatedMode)
            : this(automatedMode, 1.00f, false)
        {
        }

        public MainForm(bool automatedMode, float initialUiScale, bool uncapped)
        {
            _automatedMode = automatedMode;
            _uncapped = uncapped;
            float clampedScale = initialUiScale;
            if (clampedScale < 0.80f) clampedScale = 0.80f;
            if (clampedScale > 1.50f) clampedScale = 1.50f;
            RebuildFonts(clampedScale);

            _state = PhysicsEngine.CreateDefaultState();
            _sim = PhysicsEngine.Compute(_state);
            _layers = new LayerVisibility();
            _pickTargets = new List<PickTarget>(64);
            _presetButtons = new Dictionary<string, DarkButton>(StringComparer.OrdinalIgnoreCase);
            _presetButtonOrder = new List<DarkButton>(8);
            _frameWatch = Stopwatch.StartNew();

            try
            {
                if (TimeBeginPeriod(1) == 0)
                {
                    _timerPeriodActive = true;
                }
            }
            catch
            {
                _timerPeriodActive = false;
            }

            SuspendLayout();

            Text = "RAKDA Ltd. (Group A-10) \u2014 2045 Circular Urban Digital Twin [Native OpenGL 3.3]";
            BackColor = ColBgBase;
            ForeColor = ColTextMain;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            MinimumSize = new Size(1024, 680);

            if (_automatedMode)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(40, 40);
                ClientSize = new Size(1440, 900);
            }
            else
            {
                ClientSize = new Size(1440, 900);
                StartPosition = FormStartPosition.CenterScreen;
                Rectangle wa = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : new Rectangle(0, 0, 1920, 1080);
                if (wa.Width <= 1480 || wa.Height <= 940)
                {
                    WindowState = FormWindowState.Maximized;
                }
            }

            BuildLayout();

            ResumeLayout(true);

            SyncControlsFromState();
            RenderTelemetry();
            ApplyUiScaleLayout();

            if (!_automatedMode)
            {
                Application.Idle += OnApplicationIdle;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            }
            catch
            {
                // Ignore if DWM attribute is unavailable
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsureGlInitialized();
            if (_rendererReady)
            {
                RenderSingleFrame(0f);
                UpdateGpuFpsPill();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_automatedMode)
            {
                Application.Idle -= OnApplicationIdle;
            }
            DisposeGlResources();
            if (_timerPeriodActive)
            {
                try { TimeEndPeriod(1); } catch { }
                _timerPeriodActive = false;
            }
            base.OnFormClosing(e);
        }

        private void EnsureGlInitialized()
        {
            if (_glInitialized) return;
            _glInitialized = true;

            if (_glViewport == null || !_glViewport.IsHandleCreated)
            {
                _glViewport.CreateControl();
            }

            int w = Math.Max(1, _glViewport.Width);
            int h = Math.Max(1, _glViewport.Height);

            try
            {
                _renderer = new Renderer(_glViewport.Handle, w, h);
                _subterraneanScene = new SubterraneanScene(_renderer);
                _surfaceScene = new SurfaceScene(_renderer);
                _flowsScene = new FlowsScene(_renderer);

                _renderer.SetGroundOpacity(_groundOpacity);
                _renderer.SetSunFromState(_state);
                _renderer.FlyToPreset(_activeCamIdx);
                _renderer.MarkShadowsDirty();

                _glViewport.AttachRenderer(_renderer);
                _rendererReady = true;

                UpdateGpuFpsPill();
            }
            catch (Exception ex)
            {
                _rendererReady = false;
                _rendererInitError = ex.Message;
                ShowErrorBanner("OpenGL 3.3 Hardware Viewport Unavailable (" + ex.Message + "). Live RAKDA USCI physics controls & telemetry remain active.");
            }
        }

        private void ShowErrorBanner(string msg)
        {
            if (_errorBanner == null) return;
            _errorBanner.Text = msg;
            _errorBanner.Visible = true;
            _errorBanner.BringToFront();
        }

        private void DisposeGlResources()
        {
            if (_glViewport != null)
            {
                _glViewport.AttachRenderer(null);
            }
            if (_flowsScene != null)
            {
                try { _flowsScene.Dispose(); } catch { }
                _flowsScene = null;
            }
            if (_surfaceScene != null)
            {
                try { _surfaceScene.Dispose(); } catch { }
                _surfaceScene = null;
            }
            if (_subterraneanScene != null)
            {
                try { _subterraneanScene.Dispose(); } catch { }
                _subterraneanScene = null;
            }
            if (_renderer != null)
            {
                try { _renderer.Dispose(); } catch { }
                _renderer = null;
            }
            _rendererReady = false;
        }

        // =========================================================================
        // Dynamic UI Scaling (80% - 150%)
        // =========================================================================

        public void SetUiScale(float newScale)
        {
            if (newScale < 0.80f) newScale = 0.80f;
            if (newScale > 1.50f) newScale = 1.50f;
            if (Math.Abs(newScale - UiScale) < 0.005f) return;

            RebuildFonts(newScale);
            ApplyUiScaleLayout();
        }

        private void CycleUiScale()
        {
            int nextIdx = 0;
            for (int i = 0; i < UiScalePresets.Length; i++)
            {
                if (UiScalePresets[i] > UiScale + 0.01f)
                {
                    nextIdx = i;
                    break;
                }
            }
            SetUiScale(UiScalePresets[nextIdx]);
        }

        private void StepUiScale(int delta)
        {
            int closest = 0;
            float bestDiff = 99f;
            for (int i = 0; i < UiScalePresets.Length; i++)
            {
                float d = Math.Abs(UiScalePresets[i] - UiScale);
                if (d < bestDiff)
                {
                    bestDiff = d;
                    closest = i;
                }
            }
            int target = closest + delta;
            if (target < 0) target = 0;
            if (target >= UiScalePresets.Length) target = UiScalePresets.Length - 1;
            SetUiScale(UiScalePresets[target]);
        }

        private void ApplyUiScaleLayout()
        {
            if (_rootContainer == null) return;

            _rootContainer.SuspendLayout();
            _leftPanel.SuspendLayout();
            _rightPanel.SuspendLayout();

            // Refresh explicit Label fonts
            _lblFaultHint.Font = FontSmall;
            _lblShortcuts.Font = FontSmall;
            _errorBanner.Font = FontBodyBold;
            _errorBanner.Height = S(36);
            _inspTitleLbl.Font = FontInspectorTitle;
            _inspDescLbl.Font = FontSmall;
            _inspSpecsLbl.Font = FontSmall;

            // Update Scale button readout
            _btnScale.Text = string.Format(Inv, "Scale: {0}%", (int)Math.Round(UiScale * 100f));

            // Resize docked regions
            _leftPanel.Width = S(292);
            _rightPanel.Width = S(326);
            _bottomDock.Height = S(44);

            LayoutTopHeader();
            LayoutBottomDock();
            LayoutLeftPanel();
            ReflowRightPanel();

            _rightPanel.ResumeLayout(true);
            _leftPanel.ResumeLayout(true);
            _rootContainer.ResumeLayout(true);
            _rootContainer.PerformLayout();

            // Force OpenGL viewport resize so aspect ratio & FBO stay razor-sharp
            if (_rendererReady && _renderer != null && _glViewport != null && _glViewport.Width > 0 && _glViewport.Height > 0)
            {
                _renderer.Resize(_glViewport.Width, _glViewport.Height);
            }

            Invalidate(true);
        }

        // =========================================================================
        // UI Construction
        // =========================================================================

        private void BuildLayout()
        {
            _rootContainer = new Panel();
            _rootContainer.Dock = DockStyle.Fill;
            _rootContainer.BackColor = ColBgBase;
            Controls.Add(_rootContainer);

            BuildTopHeader();
            BuildBottomDock();
            BuildLeftPanel();
            BuildRightPanel();
            BuildCenterViewport();

            _rootContainer.Controls.Add(_viewportHost);
            _rootContainer.Controls.Add(_rightPanel);
            _rootContainer.Controls.Add(_leftPanel);
            _rootContainer.Controls.Add(_bottomDock);
            _rootContainer.Controls.Add(_topHeader);

            _topHeader.Resize += delegate(object sender, EventArgs args) { LayoutTopHeader(); };
            _bottomDock.Resize += delegate(object sender, EventArgs args) { LayoutBottomDock(); };
        }

        private void BuildTopHeader()
        {
            _topHeader = new HeaderBarPanel();
            _topHeader.Dock = DockStyle.Top;
            _topHeader.Height = S(62);
            _topHeader.BackColor = ColPanelBg;

            _kpiIt = new KpiPill("IT LOAD", "10.00 MW", ColCyan);
            _kpiPue = new KpiPill("PUE", "1.015", ColEmerald);
            _kpiCov = new KpiPill("NET HEAT COV.", "209.2%", ColOrange);
            _kpiRtt = new KpiPill("6G V2X RTT", "143.2 \u03bcs", ColViolet);
            _kpiCo2 = new KpiPill("AVOIDED CO\u2082", "18,490 t/yr", ColEmerald);
            _kpiNpv = new KpiPill("NPV @ 4%", "+\u20ac189.0M", ColAmber);
            _kpiGpuFps = new KpiPill("OPENGL 3.3 GPU", "Init...", ColCyan);
            _kpiGpuFps.AccentBorder = true;

            _btnToggleLeft = new DarkButton("Controls");
            _btnToggleLeft.Active = true;
            _btnToggleLeft.Click += delegate(object s, EventArgs e)
            {
                _leftVisible = !_leftVisible;
                _leftPanel.Visible = _leftVisible;
                _btnToggleLeft.Active = _leftVisible;
                ResizeRendererToViewport();
            };

            _btnToggleRight = new DarkButton("Telemetry");
            _btnToggleRight.Active = true;
            _btnToggleRight.Click += delegate(object s, EventArgs e)
            {
                _rightVisible = !_rightVisible;
                _rightPanel.Visible = _rightVisible;
                _btnToggleRight.Active = _rightVisible;
                ResizeRendererToViewport();
            };

            _btnPresentTop = new DarkButton("Present (H)");
            _btnPresentTop.Primary = true;
            _btnPresentTop.Click += delegate(object s, EventArgs e) { TogglePresentationMode(); };

            _topHeader.Controls.Add(_kpiIt);
            _topHeader.Controls.Add(_kpiPue);
            _topHeader.Controls.Add(_kpiCov);
            _topHeader.Controls.Add(_kpiRtt);
            _topHeader.Controls.Add(_kpiCo2);
            _topHeader.Controls.Add(_kpiNpv);
            _topHeader.Controls.Add(_kpiGpuFps);
            _topHeader.Controls.Add(_btnToggleLeft);
            _topHeader.Controls.Add(_btnToggleRight);
            _topHeader.Controls.Add(_btnPresentTop);
        }

        private void LayoutTopHeader()
        {
            if (_topHeader == null) return;
            int w = _topHeader.ClientSize.Width;
            if (w <= 0) w = ClientSize.Width;

            int gap = S(6);
            int btnH = S(30);
            int btnY = S(16);
            int wPresent = S(88);
            int wTele = S(78);
            int wCtrl = S(72);

            int xRight = w - S(12);
            _btnPresentTop.SetBounds(xRight - wPresent, btnY, wPresent, btnH);
            xRight -= wPresent + gap;
            _btnToggleRight.SetBounds(xRight - wTele, btnY, wTele, btnH);
            xRight -= wTele + gap;
            _btnToggleLeft.SetBounds(xRight - wCtrl, btnY, wCtrl, btnH);
            xRight -= wCtrl + S(12);

            bool showSideBtns = !_presentationMode;
            _btnToggleLeft.Visible = showSideBtns;
            _btnToggleRight.Visible = showSideBtns;
            if (_presentationMode)
            {
                xRight = w - wPresent - S(24);
            }

            KpiPill[] pills = new KpiPill[] { _kpiIt, _kpiPue, _kpiCov, _kpiRtt, _kpiCo2, _kpiNpv, _kpiGpuFps };
            int[] baseWidths = new int[] { 86, 64, 86, 86, 98, 92, 168 };
            int[] widths = new int[baseWidths.Length];
            int totalKpiW = 0;
            for (int i = 0; i < baseWidths.Length; i++)
            {
                widths[i] = S(baseWidths[i]);
                totalKpiW += widths[i];
            }
            totalKpiW += gap * (widths.Length - 1);

            int brandMinRight = S(356);
            int hPill = S(42);
            int singleRowH = S(62);
            int twoRowH = S(108);

            if (xRight - brandMinRight >= totalKpiW)
            {
                // Sleek single-row header
                if (_topHeader.Height != singleRowH)
                {
                    _topHeader.Height = singleRowH;
                }
                int yPill = S(10);
                int cx = xRight - totalKpiW;
                for (int i = 0; i < pills.Length; i++)
                {
                    pills[i].Visible = true;
                    pills[i].SetBounds(cx, yPill, widths[i], hPill);
                    cx += widths[i] + gap;
                }
            }
            else
            {
                // Responsive two-row header: keeps ALL 7 KPI pills legible with zero overlap at 130%-150% scale!
                if (_topHeader.Height != twoRowH)
                {
                    _topHeader.Height = twoRowH;
                }
                int yRow2 = S(58);
                int availRow2 = Math.Max(100, w - S(24));
                float shrink = totalKpiW > availRow2 ? ((float)availRow2 / (float)totalKpiW) : 1.0f;
                int usedW = 0;
                int[] row2W = new int[widths.Length];
                for (int i = 0; i < widths.Length; i++)
                {
                    row2W[i] = Math.Max(S(54), (int)Math.Floor(widths[i] * shrink));
                    usedW += row2W[i];
                }
                int row2Gap = Math.Max(3, (int)Math.Floor(gap * shrink));
                usedW += row2Gap * (widths.Length - 1);

                // Distribute any remaining width gracefully across pills so row 2 looks crisp & balanced
                int extra = availRow2 - usedW;
                if (extra > 0 && shrink >= 0.999f)
                {
                    int perPill = Math.Min(S(24), extra / pills.Length);
                    for (int i = 0; i < row2W.Length; i++) row2W[i] += perPill;
                }

                int cx = S(12);
                for (int i = 0; i < pills.Length; i++)
                {
                    pills[i].Visible = true;
                    pills[i].SetBounds(cx, yRow2, row2W[i], hPill);
                    cx += row2W[i] + row2Gap;
                }
            }

            _topHeader.Invalidate();
        }

        private void BuildBottomDock()
        {
            _bottomDock = new FooterDockPanel();
            _bottomDock.Dock = DockStyle.Bottom;
            _bottomDock.Height = S(44);
            _bottomDock.BackColor = ColPanelBg;

            _btnPlay = new DarkButton("|| Pause");
            _btnPlay.Click += delegate(object s, EventArgs e) { TogglePlay(); };

            _btnSpeed = new DarkButton("1.0x Speed");
            _btnSpeed.Click += delegate(object s, EventArgs e) { CycleSpeed(); };

            _btnDayCycle = new DarkButton("Day Cycle: Off");
            _btnDayCycle.Click += delegate(object s, EventArgs e) { ToggleDayCycle(); };

            _btnXray = new DarkButton("X-Ray (X)");
            _btnXray.Click += delegate(object s, EventArgs e) { ToggleXray(); };

            _btnReset = new DarkButton("Reset View (R)");
            _btnReset.Click += delegate(object s, EventArgs e) { SelectCameraPreset(0); };

            _btnScale = new DarkButton("Scale: 100%");
            _btnScale.Click += delegate(object s, EventArgs e) { CycleUiScale(); };

            _btnPresentDock = new DarkButton("Present (H)");
            _btnPresentDock.Primary = true;
            _btnPresentDock.Click += delegate(object s, EventArgs e) { TogglePresentationMode(); };

            _lblShortcuts = new Label();
            _lblShortcuts.UseMnemonic = false;
            _lblShortcuts.Text = "Shortcuts:  1-6 Views  \u00b7  X X-Ray  \u00b7  Space Play  \u00b7  Ctrl +/-/0 Scale  \u00b7  H Present  \u00b7  R Reset  \u00b7  Click 3D";
            _lblShortcuts.Font = FontSmall;
            _lblShortcuts.ForeColor = ColTextMuted;
            _lblShortcuts.BackColor = Color.Transparent;
            _lblShortcuts.AutoSize = true;

            _bottomDock.Controls.Add(_btnPlay);
            _bottomDock.Controls.Add(_btnSpeed);
            _bottomDock.Controls.Add(_btnDayCycle);
            _bottomDock.Controls.Add(_btnXray);
            _bottomDock.Controls.Add(_btnReset);
            _bottomDock.Controls.Add(_btnScale);
            _bottomDock.Controls.Add(_btnPresentDock);
            _bottomDock.Controls.Add(_lblShortcuts);
        }

        private void LayoutBottomDock()
        {
            if (_bottomDock == null) return;
            int w = _bottomDock.ClientSize.Width;
            if (w <= 0) w = ClientSize.Width;

            int y = S(7);
            int h = S(30);
            int x = S(12);
            int gap = S(6);

            DarkButton[] btns = new DarkButton[] { _btnPlay, _btnSpeed, _btnDayCycle, _btnXray, _btnReset, _btnScale, _btnPresentDock };
            int[] baseBws = new int[] { 78, 84, 104, 82, 98, 92, 86 };

            int sumW = 0;
            for (int i = 0; i < baseBws.Length; i++) sumW += S(baseBws[i]);
            sumW += gap * (baseBws.Length - 1);
            int avail = Math.Max(200, w - S(24));
            float shrink = sumW > avail ? ((float)avail / (float)sumW) : 1.0f;

            for (int i = 0; i < btns.Length; i++)
            {
                int bw = Math.Max(S(56), (int)Math.Floor(S(baseBws[i]) * shrink));
                btns[i].SetBounds(x, y, bw, h);
                x += bw + Math.Max(3, (int)Math.Floor(gap * shrink));
            }

            _lblShortcuts.Font = FontSmall;
            int remW = w - x - S(12);
            Size pref = TextRenderer.MeasureText(_lblShortcuts.Text, FontSmall);
            _lblShortcuts.Location = new Point(x + S(6), (S(44) - pref.Height) / 2);
            _lblShortcuts.Visible = remW >= pref.Width + S(4);
        }

        private void BuildCenterViewport()
        {
            _viewportHost = new Panel();
            _viewportHost.Dock = DockStyle.Fill;
            _viewportHost.BackColor = ColBgBase;
            _viewportHost.Padding = new Padding(0);

            _glViewport = new GlViewportControl();
            _glViewport.Dock = DockStyle.Fill;
            _glViewport.ClickedPick += OnViewportPick;
            _viewportHost.Controls.Add(_glViewport);

            _errorBanner = new Label();
            _errorBanner.UseMnemonic = false;
            _errorBanner.AutoSize = false;
            _errorBanner.Dock = DockStyle.Top;
            _errorBanner.Height = S(36);
            _errorBanner.BackColor = Color.FromArgb(127, 29, 29);
            _errorBanner.ForeColor = Color.FromArgb(254, 226, 226);
            _errorBanner.Font = FontBodyBold;
            _errorBanner.TextAlign = ContentAlignment.MiddleCenter;
            _errorBanner.Visible = false;
            _viewportHost.Controls.Add(_errorBanner);
        }

        private void BuildLeftPanel()
        {
            _leftPanel = new DarkScrollPanel();
            _leftPanel.Dock = DockStyle.Left;
            _leftPanel.Width = S(292);
            _leftPanel.BackColor = ColPanelBg;
            _leftPanel.BorderSide = BorderSideMode.Right;

            // 1. Engineering Scenarios (7 Presets)
            _cardScenarios = new SectionCard("ENGINEERING SCENARIOS", "7 Presets", ColTextSec);
            ScenarioPreset[] presets = PhysicsEngine.ScenarioList;
            for (int i = 0; i < presets.Length; i++)
            {
                ScenarioPreset p = presets[i];
                DarkButton btn = new DarkButton(p.ShortLabel);
                btn.SubLabel = p.Badge;
                btn.PresetStyle = true;

                string presetId = p.Id;
                btn.Click += delegate(object s, EventArgs e) { ApplyPreset(presetId); };

                _cardScenarios.Controls.Add(btn);
                _presetButtons[presetId] = btn;
                _presetButtonOrder.Add(btn);
            }

            _scenarioDescBox = new AccentDescriptionBox();
            _cardScenarios.Controls.Add(_scenarioDescBox);
            _leftPanel.Controls.Add(_cardScenarios);

            // 2. Live Parameters (6 Sliders)
            _cardParams = new SectionCard("LIVE PARAMETERS", "Real-Time", ColCyan);

            _sliderUtil = new DarkSlider("DC Utilization", 10.0, 100.0, 1.0, 100.0);
            _sliderUtil.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.Utilization = _sliderUtil.Value / 100.0;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardParams.Controls.Add(_sliderUtil);

            _sliderFcap = new DarkSlider("Heat Capture (f_cap)", 75.0, 98.0, 1.0, 98.0);
            _sliderFcap.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.HeatCaptureEff = _sliderFcap.Value / 100.0;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardParams.Controls.Add(_sliderFcap);

            _sliderTemp = new DarkSlider("Ambient Temp (Dublin)", -8.0, 30.0, 0.5, 10.0);
            _sliderTemp.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.AmbientTempC = _sliderTemp.Value;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardParams.Controls.Add(_sliderTemp);

            _sliderRain = new DarkSlider("Rainfall Multiplier", 0.0, 2.0, 0.05, 1.0);
            _sliderRain.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.RainfallMultiplier = _sliderRain.Value;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardParams.Controls.Add(_sliderRain);

            _sliderTime = new DarkSlider("Time of Day (Solar PV)", 0.0, 23.9, 0.25, 13.5);
            _sliderTime.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.TimeOfDay = _sliderTime.Value;
                RefreshPhysics();
            };
            _cardParams.Controls.Add(_sliderTime);

            _sliderXray = new DarkSlider("Ground X-Ray Opacity", 5.0, 100.0, 1.0, 78.0);
            _sliderXray.ValueChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                SetGroundOpacity((float)(_sliderXray.Value / 100.0));
            };
            _cardParams.Controls.Add(_sliderXray);
            _leftPanel.Controls.Add(_cardParams);

            // 3. Faults & SDN Control
            _cardFaults = new SectionCard("FAULTS & SDN CONTROL", "70.1\u00d7 vs EN 253", ColViolet);

            _toggleSdn = new DarkToggleSwitch("SDN Electro-Thermal Shifting", true);
            _toggleSdn.CheckedChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.SdnEnabled = _toggleSdn.Checked;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardFaults.Controls.Add(_toggleSdn);

            _toggleScenarioB = new DarkToggleSwitch("Scenario B HTHP (40\u00b0C \u2192 75\u00b0C)", false);
            _toggleScenarioB.CheckedChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                _state.HthpScenarioB = _toggleScenarioB.Checked;
                _state.ScenarioId = "custom";
                RefreshPhysics();
            };
            _cardFaults.Controls.Add(_toggleScenarioB);

            _lblFaultHint = new Label();
            _lblFaultHint.UseMnemonic = false;
            _lblFaultHint.Text = "Toggle Vault Fault (Click to Trip / Restore):";
            _lblFaultHint.Font = FontSmall;
            _lblFaultHint.ForeColor = ColTextMuted;
            _lblFaultHint.BackColor = Color.Transparent;
            _cardFaults.Controls.Add(_lblFaultHint);

            _vaultFaultButtons = new DarkButton[5];
            for (int i = 0; i < 5; i++)
            {
                DarkButton vbtn = new DarkButton("V" + (i + 1).ToString(Inv) + " OK");
                vbtn.HealthyBadge = true;
                vbtn.MonoFont = true;
                int idx = i;
                vbtn.Click += delegate(object s, EventArgs e) { ToggleVaultFault(idx); };
                _cardFaults.Controls.Add(vbtn);
                _vaultFaultButtons[i] = vbtn;
            }
            _leftPanel.Controls.Add(_cardFaults);

            // 4. 3D System Layers
            _cardLayers = new SectionCard("3D SYSTEM LAYERS", "7 Layers", ColTextSec);
            _chkSurface = AddLayerPill(_cardLayers, "Surface District", true, delegate(bool v) { _layers.Surface = v; MarkShadowsDirtySafe(); });
            _chkHeatmap = AddLayerPill(_cardLayers, "Thermal Heat-Map", false, delegate(bool v) { _layers.Heatmap = v; });
            _chkLoop1 = AddLayerPill(_cardLayers, "Loop 1 (75\u00b0C)", true, delegate(bool v) { _layers.Loop1 = v; });
            _chkLoop2 = AddLayerPill(_cardLayers, "Loop 2 (EN 253)", true, delegate(bool v) { _layers.Loop2 = v; });
            _chkLoop3 = AddLayerPill(_cardLayers, "Loop 3 (Water/Rain)", true, delegate(bool v) { _layers.Loop3 = v; });
            _chkOptical = AddLayerPill(_cardLayers, "Optical Ring", true, delegate(bool v) { _layers.Optical = v; });
            _chkVaults = AddLayerPill(_cardLayers, "Level \u22124 Vaults", true, delegate(bool v) { _layers.Vaults = v; MarkShadowsDirtySafe(); });
            _leftPanel.Controls.Add(_cardLayers);

            // 5. Camera Presets
            _cardCam = new SectionCard("CAMERA PRESETS", "Keys 1\u20136", ColCyan);
            string[] camLabels = new string[]
            {
                "1 \u00b7 District Overview",
                "2 \u00b7 Level \u22124 Vaults",
                "3 \u00b7 Central Hub",
                "4 \u00b7 Vault 3 Close-Up",
                "5 \u00b7 Street Pods",
                "6 \u00b7 Top Masterplan"
            };
            _cameraButtons = new DarkButton[6];
            for (int i = 0; i < 6; i++)
            {
                DarkButton cbtn = new DarkButton(camLabels[i]);
                cbtn.AlignLeft = true;
                cbtn.Active = (i == 0);
                int idx = i;
                cbtn.Click += delegate(object s, EventArgs e) { SelectCameraPreset(idx); };
                _cardCam.Controls.Add(cbtn);
                _cameraButtons[i] = cbtn;
            }
            _leftPanel.Controls.Add(_cardCam);

            _leftSpacer = new Panel();
            _leftSpacer.BackColor = Color.Transparent;
            _leftPanel.Controls.Add(_leftSpacer);
        }

        private DarkCheckPill AddLayerPill(SectionCard parent, string text, bool initial, Action<bool> onChanged)
        {
            DarkCheckPill pill = new DarkCheckPill(text, initial);
            pill.CheckedChanged += delegate(object s, EventArgs e)
            {
                if (_suppressUiEvents) return;
                onChanged(pill.Checked);
            };
            parent.Controls.Add(pill);
            return pill;
        }

        private void LayoutLeftPanel()
        {
            if (_leftPanel == null) return;
            int sbW = SystemInformation.VerticalScrollBarWidth;
            int pad = S(10);
            int cardW = Math.Max(S(200), _leftPanel.Width - sbW - pad * 2);
            int innerX = S(10);
            int innerW = cardW - innerX * 2;
            int btnGap = S(6);
            int colW = (innerW - btnGap) / 2;

            int scrollY = _leftPanel.AutoScrollPosition.Y;
            int y = pad + scrollY;

            // Card 1: Scenarios
            int sy = S(34);
            int presetH = S(38);
            int presetStep = S(44);
            int count = _presetButtonOrder.Count;
            for (int i = 0; i < count; i++)
            {
                int row = i / 2;
                int col = i % 2;
                bool isLastOdd = (i == count - 1) && (count % 2 == 1);
                int bw = isLastOdd ? innerW : colW;
                int bx = innerX + (isLastOdd ? 0 : col * (colW + btnGap));
                int by = sy + row * presetStep;
                _presetButtonOrder[i].SetBounds(bx, by, bw, presetH);
            }
            int presetRows = (count + 1) / 2;
            sy += presetRows * presetStep + S(4);

            Size descMeasure = TextRenderer.MeasureText(
                string.IsNullOrEmpty(_scenarioDescBox.DescriptionText) ? "Sample description line one and two and three." : _scenarioDescBox.DescriptionText,
                FontSmall,
                new Size(Math.Max(40, innerW - S(14)), 500),
                TextFormatFlags.WordBreak);
            int descH = Math.Max(S(50), descMeasure.Height + S(12));
            _scenarioDescBox.SetBounds(innerX, sy, innerW, descH);
            _cardScenarios.SetBounds(pad, y, cardW, sy + descH + S(10));
            y += _cardScenarios.Height + pad;

            // Card 2: Live Parameters
            int py = S(34);
            int sliderH = S(38);
            int sliderStep = S(41);
            DarkSlider[] sliders = new DarkSlider[] { _sliderUtil, _sliderFcap, _sliderTemp, _sliderRain, _sliderTime, _sliderXray };
            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].SetBounds(innerX, py, innerW, sliderH);
                py += sliderStep;
            }
            _cardParams.SetBounds(pad, y, cardW, py + S(6));
            y += _cardParams.Height + pad;

            // Card 3: Faults & SDN
            int fy = S(34);
            int swH = S(24);
            _toggleSdn.SetBounds(innerX, fy, innerW, swH);
            fy += swH + S(4);
            _toggleScenarioB.SetBounds(innerX, fy, innerW, swH);
            fy += swH + S(6);
            _lblFaultHint.SetBounds(innerX, fy, innerW, S(16));
            fy += S(19);
            int vfGap = S(4);
            int vfW = (innerW - vfGap * 4) / 5;
            int vfH = S(26);
            for (int i = 0; i < 5; i++)
            {
                _vaultFaultButtons[i].SetBounds(innerX + i * (vfW + vfGap), fy, vfW, vfH);
            }
            fy += vfH + S(10);
            _cardFaults.SetBounds(pad, y, cardW, fy);
            y += _cardFaults.Height + pad;

            // Card 4: 3D System Layers
            int ly = S(34);
            int pillH = S(25);
            int pillStep = S(29);
            _chkSurface.SetBounds(innerX, ly, colW, pillH);
            _chkHeatmap.SetBounds(innerX + colW + btnGap, ly, colW, pillH);
            ly += pillStep;
            _chkLoop1.SetBounds(innerX, ly, colW, pillH);
            _chkLoop2.SetBounds(innerX + colW + btnGap, ly, colW, pillH);
            ly += pillStep;
            _chkLoop3.SetBounds(innerX, ly, colW, pillH);
            _chkOptical.SetBounds(innerX + colW + btnGap, ly, colW, pillH);
            ly += pillStep;
            _chkVaults.SetBounds(innerX, ly, innerW, pillH);
            ly += pillH + S(10);
            _cardLayers.SetBounds(pad, y, cardW, ly);
            y += _cardLayers.Height + pad;

            // Card 5: Camera Presets
            int cy = S(34);
            int camH = S(27);
            int camStep = S(32);
            for (int i = 0; i < 6; i++)
            {
                int row = i / 2;
                int col = i % 2;
                _cameraButtons[i].SetBounds(innerX + col * (colW + btnGap), cy + row * camStep, colW, camH);
            }
            _cardCam.SetBounds(pad, y, cardW, cy + 3 * camStep + S(8));
            y += _cardCam.Height;

            _leftSpacer.SetBounds(0, y, 10, S(12));
        }

        private void BuildRightPanel()
        {
            _rightPanel = new DarkScrollPanel();
            _rightPanel.Dock = DockStyle.Right;
            _rightPanel.Width = S(326);
            _rightPanel.BackColor = ColPanelBg;
            _rightPanel.BorderSide = BorderSideMode.Left;

            // 1. Subsystem Inspector Card
            _cardInspector = new SectionCard("SUBSYSTEM INSPECTOR", "Click 3D Asset", ColCyan);
            _cardInspector.AccentBorder = true;

            _inspTitleLbl = new Label();
            _inspTitleLbl.UseMnemonic = false;
            _inspTitleLbl.Font = FontInspectorTitle;
            _inspTitleLbl.ForeColor = ColTextMain;
            _inspTitleLbl.BackColor = Color.Transparent;
            _inspTitleLbl.Text = "Click any 3D Vault, Tank, Pod, PV, or Plaza";
            _cardInspector.Controls.Add(_inspTitleLbl);

            _inspDescLbl = new Label();
            _inspDescLbl.UseMnemonic = false;
            _inspDescLbl.Font = FontSmall;
            _inspDescLbl.ForeColor = ColTextSec;
            _inspDescLbl.BackColor = Color.Transparent;
            _inspDescLbl.Text = "Select a subterranean vault, 2,500 m\u00b3 thermal store, 10,000 m\u00b3 cistern, HTHP bank, Micro-ORC, dry coolers, or surface asset in the 3D scene to inspect RAKDA USCI (Nov 2026) specs and live telemetry.";
            _cardInspector.Controls.Add(_inspDescLbl);

            _inspSpecsLbl = new Label();
            _inspSpecsLbl.UseMnemonic = false;
            _inspSpecsLbl.Font = FontSmall;
            _inspSpecsLbl.ForeColor = ColTextMuted;
            _inspSpecsLbl.BackColor = Color.Transparent;
            _inspSpecsLbl.Text = "";
            _inspSpecsLbl.Visible = false;
            _cardInspector.Controls.Add(_inspSpecsLbl);

            _inspMetricsKv = new KvTableControl();
            _inspMetricsKv.Visible = false;
            _cardInspector.Controls.Add(_inspMetricsKv);

            _rightPanel.Controls.Add(_cardInspector);

            // 2. Thermal & Surplus Cascade Card
            _cardThermal = new SectionCard("THERMAL & SURPLUS CASCADE", "Loops 1 & 2", ColOrange);

            _barCaptured = new DarkProgressBar("Captured Heat (98% nom.)", ColOrange);
            _cardThermal.Controls.Add(_barCaptured);

            _barDemand = new DarkProgressBar("Network Demand (Incl. 0.39 MW)", ColCyan);
            _cardThermal.Controls.Add(_barDemand);

            _kvThermalCore = new KvTableControl();
            _cardThermal.Controls.Add(_kvThermalCore);

            _divTherm = new Panel();
            _divTherm.BackColor = ColBorder;
            _cardThermal.Controls.Add(_divTherm);

            _kvThermalCascade = new KvTableControl();
            _cardThermal.Controls.Add(_kvThermalCascade);

            _rightPanel.Controls.Add(_cardThermal);

            // 3. Power, Hydrology & Optical Card
            _cardGridWater = new SectionCard("POWER, HYDROLOGY & OPTICAL", "CRU \u00b7 Loop 3 \u00b7 6G", ColCyan);

            _barCistern = new DarkProgressBar("10,000 m\u00b3 Cistern (44 d Res.)", ColSky);
            _cardGridWater.Controls.Add(_barCistern);

            _kvGridWater = new KvTableControl();
            _cardGridWater.Controls.Add(_kvGridWater);

            _rightPanel.Controls.Add(_cardGridWater);

            // 4. 30-Yr Economics & Survey Card
            _cardFinance = new SectionCard("30-YR ECONOMICS & SURVEY", "Case 2 \u00b7 N=105", ColAmber);

            _kvFinance = new KvTableControl();
            _cardFinance.Controls.Add(_kvFinance);

            _rightPanel.Controls.Add(_cardFinance);

            _rightSpacer = new Panel();
            _rightSpacer.BackColor = Color.Transparent;
            _rightPanel.Controls.Add(_rightSpacer);
        }

        private void ReflowRightPanel()
        {
            if (_rightPanel == null) return;
            int sbW = SystemInformation.VerticalScrollBarWidth;
            int pad = S(10);
            int cardW = Math.Max(S(220), _rightPanel.Width - sbW - pad * 2);
            int innerX = S(10);
            int innerW = cardW - innerX * 2;
            int rowH = S(19);
            int barH = S(29);

            // 1. Inspector Card (dynamic height)
            int iy = S(34);
            Size titleSz = TextRenderer.MeasureText(_inspTitleLbl.Text, FontInspectorTitle, new Size(innerW, 500), TextFormatFlags.WordBreak);
            _inspTitleLbl.SetBounds(innerX, iy, innerW, Math.Max(S(18), titleSz.Height + S(2)));
            iy += _inspTitleLbl.Height + S(4);

            Size descSz = TextRenderer.MeasureText(_inspDescLbl.Text, FontSmall, new Size(innerW, 500), TextFormatFlags.WordBreak);
            _inspDescLbl.SetBounds(innerX, iy, innerW, Math.Max(S(28), descSz.Height + S(4)));
            iy += _inspDescLbl.Height + S(4);

            if (_inspSpecsLbl.Visible && !string.IsNullOrEmpty(_inspSpecsLbl.Text))
            {
                Size specSz = TextRenderer.MeasureText(_inspSpecsLbl.Text, FontSmall, new Size(innerW, 500), TextFormatFlags.WordBreak);
                _inspSpecsLbl.SetBounds(innerX, iy, innerW, Math.Max(S(18), specSz.Height + S(4)));
                iy += _inspSpecsLbl.Height + S(6);
            }

            if (_inspMetricsKv.Visible && _inspMetricsKv.RowCount > 0)
            {
                int mH = _inspMetricsKv.RowCount * rowH + S(2);
                _inspMetricsKv.SetBounds(innerX, iy, innerW, mH);
                iy += mH + S(6);
            }
            _cardInspector.Width = cardW;
            _cardInspector.Height = iy + S(6);

            // 2. Thermal Card
            int ty = S(34);
            _barCaptured.SetBounds(innerX, ty, innerW, barH);
            ty += barH + S(3);
            _barDemand.SetBounds(innerX, ty, innerW, barH);
            ty += barH + S(6);
            int coreH = Math.Max(5, _kvThermalCore.RowCount) * rowH;
            _kvThermalCore.SetBounds(innerX, ty, innerW, coreH);
            ty += coreH + S(8);
            _divTherm.SetBounds(innerX, ty - S(4), innerW, 1);
            int cascRows = Math.Max(4, _kvThermalCascade.RowCount);
            int cascH = cascRows * rowH;
            _kvThermalCascade.SetBounds(innerX, ty + S(2), innerW, cascH);
            _cardThermal.Width = cardW;
            _cardThermal.Height = ty + cascH + S(12);

            // 3. Power, Hydrology & Optical Card
            int gy = S(34);
            _barCistern.SetBounds(innerX, gy, innerW, barH);
            gy += barH + S(6);
            int gwH = Math.Max(7, _kvGridWater.RowCount) * rowH;
            _kvGridWater.SetBounds(innerX, gy, innerW, gwH);
            _cardGridWater.Width = cardW;
            _cardGridWater.Height = gy + gwH + S(12);

            // 4. Economics Card
            int finH = Math.Max(5, _kvFinance.RowCount) * rowH;
            _kvFinance.SetBounds(innerX, S(34), innerW, finH);
            _cardFinance.Width = cardW;
            _cardFinance.Height = S(34) + finH + S(12);

            // Vertical stacking respecting current scroll position
            int scrollY = _rightPanel.AutoScrollPosition.Y;
            int y = pad + scrollY;
            _cardInspector.Location = new Point(pad, y);
            y += _cardInspector.Height + pad;
            _cardThermal.Location = new Point(pad, y);
            y += _cardThermal.Height + pad;
            _cardGridWater.Location = new Point(pad, y);
            y += _cardGridWater.Height + pad;
            _cardFinance.Location = new Point(pad, y);
            y += _cardFinance.Height;
            _rightSpacer.SetBounds(0, y, 10, S(12));
        }

        // =========================================================================
        // Simulation Actions & Keyboard Shortcuts
        // =========================================================================

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool ctrl = (keyData & Keys.Control) != 0;
            Keys key = keyData & Keys.KeyCode;

            if (ctrl)
            {
                if (key == Keys.Oemplus || key == Keys.Add)
                {
                    StepUiScale(1);
                    return true;
                }
                if (key == Keys.OemMinus || key == Keys.Subtract)
                {
                    StepUiScale(-1);
                    return true;
                }
                if (key == Keys.D0 || key == Keys.NumPad0)
                {
                    SetUiScale(1.00f);
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (key >= Keys.D1 && key <= Keys.D6)
            {
                SelectCameraPreset((int)(key - Keys.D1));
                return true;
            }
            if (key >= Keys.NumPad1 && key <= Keys.NumPad6)
            {
                SelectCameraPreset((int)(key - Keys.NumPad1));
                return true;
            }
            if (key == Keys.X)
            {
                ToggleXray();
                return true;
            }
            if (key == Keys.Space)
            {
                TogglePlay();
                return true;
            }
            if (key == Keys.H)
            {
                TogglePresentationMode();
                return true;
            }
            if (key == Keys.R)
            {
                SelectCameraPreset(0);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ResizeRendererToViewport()
        {
            if (_rendererReady && _renderer != null && _glViewport != null && _glViewport.Width > 0 && _glViewport.Height > 0)
            {
                _renderer.Resize(_glViewport.Width, _glViewport.Height);
            }
        }

        private void ApplyPreset(string presetId)
        {
            ScenarioPreset preset = PhysicsEngine.GetPreset(presetId);
            if (preset == null) return;
            _state = preset.Params.Clone();
            _state.ScenarioId = preset.Id;
            RefreshPhysics();
            LayoutLeftPanel();
        }

        private void ToggleVaultFault(int idx)
        {
            if (idx < 0 || idx >= 5) return;
            _state.VaultFaults[idx] = !_state.VaultFaults[idx];
            _state.ScenarioId = "custom";
            RefreshPhysics();
        }

        private void SetGroundOpacity(float opacity01)
        {
            if (opacity01 < 0.05f) opacity01 = 0.05f;
            if (opacity01 > 1.0f) opacity01 = 1.0f;
            _groundOpacity = opacity01;
            if (_rendererReady && _renderer != null)
            {
                _renderer.SetGroundOpacity(_groundOpacity);
            }
            SyncControlsFromState();
        }

        private void SelectCameraPreset(int idx)
        {
            if (idx < 0 || idx >= 6) return;
            _activeCamIdx = idx;
            for (int i = 0; i < _cameraButtons.Length; i++)
            {
                _cameraButtons[i].Active = (i == idx);
            }
            if (_rendererReady && _renderer != null)
            {
                _renderer.FlyToPreset(idx);
            }
        }

        private void TogglePlay()
        {
            _playing = !_playing;
            RenderTelemetry();
        }

        private void CycleSpeed()
        {
            if (Math.Abs(_simSpeed - 1.0f) < 0.05f) _simSpeed = 2.0f;
            else if (Math.Abs(_simSpeed - 2.0f) < 0.05f) _simSpeed = 0.5f;
            else _simSpeed = 1.0f;
            RenderTelemetry();
        }

        private void ToggleDayCycle()
        {
            _autoDay = !_autoDay;
            RenderTelemetry();
        }

        private void ToggleXray()
        {
            SetGroundOpacity(_groundOpacity > 0.40f ? 0.18f : 0.92f);
        }

        private void TogglePresentationMode()
        {
            _presentationMode = !_presentationMode;
            _leftPanel.Visible = !_presentationMode && _leftVisible;
            _rightPanel.Visible = !_presentationMode && _rightVisible;
            _btnPresentTop.Active = _presentationMode;
            _btnPresentDock.Active = _presentationMode;
            LayoutTopHeader();
            ResizeRendererToViewport();
        }

        private void MarkShadowsDirtySafe()
        {
            if (_rendererReady && _renderer != null)
            {
                _renderer.MarkShadowsDirty();
            }
        }

        private void RefreshPhysics()
        {
            _sim = PhysicsEngine.Compute(_state);
            SyncControlsFromState();
            RenderTelemetry();
            if (_rendererReady && _renderer != null)
            {
                _renderer.SetSunFromState(_state);
            }
        }

        private void OnViewportPick(int mouseX, int mouseY)
        {
            if (!_rendererReady || _renderer == null) return;

            _pickTargets.Clear();
            if (_layers.Vaults && _subterraneanScene != null)
            {
                _subterraneanScene.CollectPickTargets(_pickTargets);
            }
            if (_layers.Surface && _surfaceScene != null)
            {
                _surfaceScene.CollectPickTargets(_pickTargets);
            }

            PickTarget hitTarget;
            string key = _renderer.Pick(mouseX, mouseY, _pickTargets, out hitTarget);
            if (!string.IsNullOrEmpty(key) && PhysicsEngine.GetSubsystemInfo(key) != null)
            {
                _renderer.SetSelectedTarget(hitTarget);
                SetInspectedKey(key);
            }
            else
            {
                _renderer.SetSelectedTarget(null);
            }
        }

        private void SetInspectedKey(string key)
        {
            if (string.IsNullOrEmpty(key) || PhysicsEngine.GetSubsystemInfo(key) == null) return;
            _inspectedKey = key;
            UpdateInspector();
        }

        private void SelectSubsystemForTarget(string key)
        {
            SetInspectedKey(key);
            if (!_rendererReady || _renderer == null) return;

            _pickTargets.Clear();
            if (_layers.Vaults && _subterraneanScene != null) _subterraneanScene.CollectPickTargets(_pickTargets);
            if (_layers.Surface && _surfaceScene != null) _surfaceScene.CollectPickTargets(_pickTargets);

            PickTarget match = null;
            for (int i = 0; i < _pickTargets.Count; i++)
            {
                if (string.Equals(_pickTargets[i].InspectKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    match = _pickTargets[i];
                    break;
                }
            }
            _renderer.SetSelectedTarget(match);
        }

        // =========================================================================
        // State -> UI Synchronization
        // =========================================================================

        private static string FormatClock(double hoursFloat)
        {
            double wrapped = ((hoursFloat % 24.0) + 24.0) % 24.0;
            int hh = (int)Math.Floor(wrapped);
            int mm = ((int)Math.Round((wrapped - hh) * 60.0)) % 60;
            return string.Format(Inv, "{0:00}:{1:00}", hh, mm);
        }

        private static string FormatInt(double val)
        {
            return Math.Round(val).ToString("N0", Inv);
        }

        private void SyncControlsFromState()
        {
            _suppressUiEvents = true;
            try
            {
                foreach (KeyValuePair<string, DarkButton> kv in _presetButtons)
                {
                    kv.Value.Active = string.Equals(kv.Key, _state.ScenarioId, StringComparison.OrdinalIgnoreCase);
                }

                ScenarioPreset activePreset = PhysicsEngine.GetPreset(_state.ScenarioId);
                _scenarioDescBox.DescriptionText = activePreset != null
                    ? activePreset.Description
                    : "Custom interactive engineering configuration (live RAKDA USCI equations).";

                int utilPct = (int)Math.Round(_state.Utilization * 100.0);
                _sliderUtil.SetValueSilently(utilPct, string.Format(Inv, "{0}% ({1:F1} MW)", utilPct, _state.Utilization * 10.0));

                int fcapPct = (int)Math.Round(_state.HeatCaptureEff * 100.0);
                _sliderFcap.SetValueSilently(fcapPct, string.Format(Inv, "{0}%", fcapPct));

                _sliderTemp.SetValueSilently(_state.AmbientTempC, string.Format(Inv, "{0:F1} \u00b0C", _state.AmbientTempC));

                _sliderRain.SetValueSilently(
                    _state.RainfallMultiplier,
                    string.Format(Inv, "{0:F2}\u00d7 ({1:F0} mm/yr)", _state.RainfallMultiplier, 772.5 * _state.RainfallMultiplier));

                _sliderTime.SetValueSilently(_state.TimeOfDay, FormatClock(_state.TimeOfDay));

                int xrayPct = (int)Math.Round(_groundOpacity * 100.0);
                _sliderXray.SetValueSilently(xrayPct, string.Format(Inv, "{0}%", xrayPct));

                _toggleSdn.Checked = _state.SdnEnabled;
                _toggleScenarioB.Checked = _state.HthpScenarioB;

                for (int i = 0; i < 5; i++)
                {
                    bool faulted = _state.VaultFaults != null && i < _state.VaultFaults.Length && _state.VaultFaults[i];
                    DarkButton btn = _vaultFaultButtons[i];
                    btn.FaultedBadge = faulted;
                    btn.HealthyBadge = !faulted;
                    btn.Text = faulted ? ("V" + (i + 1).ToString(Inv) + " FLT") : ("V" + (i + 1).ToString(Inv) + " OK");
                }

                _chkSurface.Checked = _layers.Surface;
                _chkHeatmap.Checked = _layers.Heatmap;
                _chkLoop1.Checked = _layers.Loop1;
                _chkLoop2.Checked = _layers.Loop2;
                _chkLoop3.Checked = _layers.Loop3;
                _chkOptical.Checked = _layers.Optical;
                _chkVaults.Checked = _layers.Vaults;

                _btnXray.Active = (_groundOpacity <= 0.40f);
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        private void RenderTelemetry()
        {
            ElectricalTelemetry e = _sim.Electrical;
            ThermalTelemetry t = _sim.Thermal;
            HydrologyTelemetry h = _sim.Hydrology;
            OpticalTelemetry o = _sim.Optical;
            FinancialTelemetry f = _sim.Financial;
            SurveyStats sv = _sim.Survey;

            string co2Rounded = FormatInt(f.TotalAvoidedCo2TonYr);

            // Top KPI pills
            _kpiIt.ValueText = string.Format(Inv, "{0:F2} MW", e.TotalItMw);
            _kpiPue.ValueText = string.Format(Inv, "{0:F3}", e.Pue);
            _kpiCov.ValueText = string.Format(Inv, "{0:F1}%", t.AnnualCoveragePct);
            _kpiRtt.ValueText = string.Format(Inv, "{0:F1} \u03bcs", o.ActiveLocalE2eRttUs);
            _kpiCo2.ValueText = co2Rounded + " t/yr";
            _kpiNpv.ValueText = string.Format(Inv, "+\u20ac{0:F1}M", f.Npv4PctM);

            // Bottom dock states
            _btnPlay.Text = _playing ? "|| Pause" : "> Play";
            _btnSpeed.Text = string.Format(Inv, "{0:F1}\u00d7 Speed", _simSpeed);
            _btnDayCycle.Text = _autoDay ? "Day Cycle: On" : "Day Cycle: Off";
            _btnDayCycle.Active = _autoDay;

            // Thermal & Surplus Cascade Card
            _barCaptured.Update(string.Format(Inv, "{0:F2} MW_th", t.CapturedHeatMw), (float)((t.CapturedHeatMw / 10.0) * 100.0));
            _barDemand.Update(string.Format(Inv, "{0:F2} MW_th", t.TotalNetworkDemandMw), (float)((t.TotalNetworkDemandMw / 10.0) * 100.0));

            bool isSurplus = t.NetThermalBalanceMw >= 0.0;
            string balSign = isSurplus ? "+" : "\u2212";
            Color balCol = isSurplus ? ColEmerald : ColAmber;

            _kvThermalCore.SetRows(new KvRow[]
            {
                new KvRow("Net Thermal Balance", string.Format(Inv, "{0}{1:F2} MW_th", balSign, Math.Abs(t.NetThermalBalanceMw)), balCol),
                new KvRow("Annual Heat / Net Cov.", string.Format(Inv, "{0} MWh ({1:F0}%/{2:F0}%)", FormatInt(t.AnnualCapturedHeatMWh), t.AnnualGrossCoveragePct, t.AnnualCoveragePct), ColTextMain),
                new KvRow("Primary 75\u00b0C Flow", string.Format(Inv, "{0:F1} L/s ({1:F0} L/m)", t.PrimaryGrossFlowLs, t.PrimaryGrossFlowLmin), ColTextMain),
                new KvRow("Secondary 45\u00b0C Flow", string.Format(Inv, "{0:F1} L/s ({1:F0} L/m)", t.SecondaryGrossFlowLs, t.SecondaryGrossFlowLmin), ColTextMain),
                new KvRow("2,500 m\u00b3 Thermal Store", string.Format(Inv, "{0:F0}% ({1:F1} MWh usable)", t.ThermalStoreSoc * 100.0, t.ThermalStoreUsableMWh), ColTextMain)
            });

            if (isSurplus)
            {
                _kvThermalCascade.SetRows(new KvRow[]
                {
                    new KvRow("2,500 m\u00b3 Store Charge", string.Format(Inv, "{0:F2} MW_th", t.ThermalStoreChargeMw), ColTextMain),
                    new KvRow("Tier 1: 75\u00b0C Heat Sales", string.Format(Inv, "{0:F2} MW_th", t.CommercialHeatSalesMw), ColOrange),
                    new KvRow("Tier 2: Micro-ORC (~0.4 MW)", string.Format(Inv, "{0:F1} kW_e", t.OrcOutputKw), ColEmerald),
                    new KvRow("Tier 3: N+1 Dry Coolers", string.Format(Inv, "{0:F2} MW_th", t.DryCoolerRejectionMw), ColTextMain)
                });
            }
            else
            {
                _kvThermalCascade.SetRows(new KvRow[]
                {
                    new KvRow("Thermal Store Discharge", string.Format(Inv, "{0:F2} MW_th", t.ThermalStoreDischargeMw), ColOrange),
                    new KvRow("8.0 MW_th Backup HTHP", string.Format(Inv, "{0:F2} MW_th (COP {1:F2})", t.HthpThermalOutputMw, t.HthpCop), ColAmber),
                    new KvRow("HTHP Electric Draw", string.Format(Inv, "{0:F2} MW_e", t.HthpElectricalMw + t.ScenarioBElecMw), ColTextMain),
                    new KvRow("Unmet Deficit", string.Format(Inv, "{0:F2} MW_th", t.UnmetThermalDeficitMw), t.UnmetThermalDeficitMw > 0.01 ? ColRed : ColEmerald)
                });
            }

            // Power, Hydrology & Optical Card
            _barCistern.Update(string.Format(Inv, "{0:F0} m\u00b3 ({1:F1} d)", h.CisternVolumeM3, h.DroughtAutonomyDays), (float)(h.CisternSoc * 100.0));
            _kvGridWater.SetRows(new KvRow[]
            {
                new KvRow("Gross / Net Grid Annual", string.Format(Inv, "{0} / {1} MWh", FormatInt(e.AnnualGrossMWh), FormatInt(e.AnnualNetGridMWh)), ColTextMain),
                new KvRow("12 MW_p PV / Net Import", string.Format(Inv, "{0:F2} / {1:F2} MW", e.PvOutputMw, e.NetGridDrawMw), ColTextMain),
                new KvRow("8 MW / 32 MWh BESS", e.BessMode, ColTextMain),
                new KvRow("Rain Harvest (10 ha inert)", string.Format(Inv, "{0} m\u00b3/yr", FormatInt(h.AnnualRainHarvestM3)), ColTextMain),
                new KvRow("Avoided Tower Water", "157.7\u2013175.2k m\u00b3/yr", ColCyan),
                new KvRow("Local V2X vs Cloud", string.Format(Inv, "{0:F2} mm vs {1:F2} m", o.LocalBlindTravelMm, o.CloudBlindTravelM), ColViolet),
                new KvRow("Local Breakout / WAN", string.Format(Inv, "{0:F2}% / {1:F0} MWh", o.LocalBreakoutPct, o.WanPowerSavedMWhYr), ColTextMain)
            });

            // 30-Yr Economics & Survey Card
            _kvFinance.SetRows(new KvRow[]
            {
                new KvRow("CapEx (Utility + DC)", string.Format(Inv, "\u20ac{0:F2}M (\u20ac{1:F2}+\u20ac{2:F2})", f.CapexTotalM, f.CapexDistrictM, f.CapexDcOperatorM), ColTextMain),
                new KvRow("Case 2 EBITDA / Payback", string.Format(Inv, "+\u20ac{0:F1}M/yr \u00b7 {1:F1} yr", f.EbitdaAnnualM, f.SimplePaybackYr), ColEmerald),
                new KvRow("Case 2 IRR / NPV @ 8%", string.Format(Inv, "{0:F1}% \u00b7 +\u20ac{1:F0}M", f.IrrPct, f.Npv8PctM), ColTextMain),
                new KvRow("Case 1 Utility IRR/LCOH", string.Format(Inv, "{0:F1}% \u00b7 \u20ac{1:F0}/MWh", f.UtilityIrrPct, f.UtilityLcohNetEurMwh), ColTextMain),
                new KvRow("30-Yr TCO (USCI/Gas/HP)", string.Format(Inv, "\u20ac{0:F0}/\u20ac{1:F0}/\u20ac{2:F0}M", f.TcoUsciM, f.TcoGasM, f.TcoAshpM), ColTextMain),
                new KvRow("Avoided Carbon", co2Rounded + " tCO\u2082/yr", ColEmerald),
                new KvRow("Survey Comfort / H\u2082O", string.Format(Inv, "{0:F2}/5 \u00b7 {1:F2}/5 (N={2})", sv.DcComfortScore, sv.RecycledWaterScore, sv.SampleSize), ColTextMain)
            });

            UpdateInspector();
        }

        private void UpdateInspector()
        {
            if (string.IsNullOrEmpty(_inspectedKey)) return;
            SubsystemInfo info = PhysicsEngine.GetSubsystemInfo(_inspectedKey);
            if (info == null) return;

            bool changedKey = !string.Equals(_renderedInspectedKey, _inspectedKey, StringComparison.OrdinalIgnoreCase);
            if (changedKey)
            {
                _renderedInspectedKey = _inspectedKey;
                _cardInspector.TagText = info.Category;
                _inspTitleLbl.Text = info.Title;
                _inspDescLbl.Text = info.Description;

                if (info.Specs != null && info.Specs.Length > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < info.Specs.Length; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append("\u25b8 ");
                        sb.Append(info.Specs[i]);
                    }
                    _inspSpecsLbl.Text = sb.ToString();
                    _inspSpecsLbl.Visible = true;
                }
                else
                {
                    _inspSpecsLbl.Visible = false;
                }
            }

            List<KeyValuePair<string, string>> metrics = info.GetLiveMetrics(_sim);
            KvRow[] rows = new KvRow[metrics.Count];
            for (int i = 0; i < metrics.Count; i++)
            {
                Color c = ColTextMain;
                string v = metrics[i].Value ?? "";
                if (v.IndexOf("FAULTED", StringComparison.OrdinalIgnoreCase) >= 0) c = ColRed;
                else if (v.IndexOf("ONLINE", StringComparison.OrdinalIgnoreCase) >= 0) c = ColEmerald;
                else if (v.IndexOf("Armed", StringComparison.OrdinalIgnoreCase) >= 0) c = ColAmber;
                rows[i] = new KvRow(metrics[i].Key, v, c);
            }
            _inspMetricsKv.SetRows(rows);
            _inspMetricsKv.Visible = rows.Length > 0;

            if (changedKey)
            {
                ReflowRightPanel();
            }
        }

        private void UpdateGpuFpsPill()
        {
            if (!_rendererReady || _renderer == null)
            {
                _kpiGpuFps.LabelText = "OPENGL FALLBACK";
                _kpiGpuFps.ValueText = "No GL Context";
                return;
            }

            string gpu = _renderer.GpuName;
            if (string.IsNullOrEmpty(gpu)) gpu = "OpenGL 3.3 GPU";
            string shortGpu = ShortenGpuName(gpu);
            _kpiGpuFps.LabelText = shortGpu;

            float fps = _pacedFps > 0.5f ? _pacedFps : _renderer.LastFps;
            float ms = _pacedFrameMs > 0.05f ? _pacedFrameMs : _renderer.LastFrameMs;
            if (!_uncapped && fps > 60.5f && _automatedMode)
            {
                fps = 60.0f;
                ms = (float)TargetFrameMs;
            }

            if (fps > 0.5f)
            {
                _kpiGpuFps.ValueText = string.Format(Inv, "{0:F0} FPS \u00b7 {1:F1}ms", fps, ms);
            }
            else
            {
                _kpiGpuFps.ValueText = "GL 3.3 Active";
            }
        }

        private static string ShortenGpuName(string raw)
        {
            string s = raw.Trim();
            s = s.Replace("NVIDIA GeForce ", "");
            s = s.Replace("AMD Radeon ", "");
            s = s.Replace("Intel(R) ", "Intel ");
            s = s.Replace("/PCIe/SSE2", "");
            s = s.Replace(" (TM)", "");
            s = s.Replace("(R)", "");
            if (s.Length > 22) s = s.Substring(0, 22);
            return s.ToUpperInvariant();
        }

        // =========================================================================
        // 60 FPS Capped Native Render Loop (Application.Idle + Stopwatch Pacing)
        // =========================================================================

        private static bool IsAppIdle()
        {
            NativeMessage msg;
            return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
        }

        private void PaceToDeadline(double targetEndSec)
        {
            if (_uncapped) return;
            while (true)
            {
                double remMs = (targetEndSec - _frameWatch.Elapsed.TotalSeconds) * 1000.0;
                if (remMs <= 0.0) break;
                if (remMs > 3.0)
                {
                    int sleepMs = (int)(remMs - 2.5);
                    if (sleepMs > 10) sleepMs = 10;
                    if (sleepMs > 0)
                    {
                        Thread.Sleep(sleepMs);
                    }
                }
                else
                {
                    Thread.SpinWait(40);
                    Thread.Yield();
                }
            }
        }

        private void PaceFrameTo60Fps(double frameStartSec)
        {
            PaceToDeadline(frameStartSec + (TargetFrameMs * 0.001));
        }

        private void OnApplicationIdle(object sender, EventArgs e)
        {
            if (!_rendererReady || IsDisposed || WindowState == FormWindowState.Minimized)
            {
                return;
            }

            while (IsAppIdle())
            {
                if (IsDisposed || WindowState == FormWindowState.Minimized || !_rendererReady)
                {
                    break;
                }

                double frameStartSec = _frameWatch.Elapsed.TotalSeconds;
                float rawDt = (float)(frameStartSec - _lastTickSec);
                if (rawDt <= 0f) rawDt = 0.001f;
                if (rawDt > 0.05f) rawDt = 0.05f;
                _lastTickSec = frameStartSec;

                RenderSingleFrame(rawDt);
                PaceFrameTo60Fps(frameStartSec);

                double totalFrameMs = (_frameWatch.Elapsed.TotalSeconds - frameStartSec) * 1000.0;
                if (totalFrameMs > 0.01)
                {
                    float instantFps = (float)(1000.0 / totalFrameMs);
                    _pacedFrameMs = _pacedFrameMs <= 0.01f ? (float)totalFrameMs : (_pacedFrameMs * 0.85f + (float)totalFrameMs * 0.15f);
                    _pacedFps = _pacedFps <= 0.1f ? instantFps : (_pacedFps * 0.85f + instantFps * 0.15f);
                }

                _hudTimerSec += rawDt;
                if (_hudTimerSec >= 0.25)
                {
                    _hudTimerSec = 0.0;
                    UpdateGpuFpsPill();
                }
            }
        }

        private void RenderSingleFrame(float rawDt)
        {
            if (!_rendererReady || _renderer == null) return;

            float simDt = _playing ? rawDt * _simSpeed : 0f;
            _simElapsed += simDt;

            if (_playing && _autoDay)
            {
                _state.TimeOfDay = (_state.TimeOfDay + simDt * 0.45) % 24.0;
                _sim = PhysicsEngine.Compute(_state);
                _renderer.SetSunFromState(_state);

                _dayUiTimerSec += rawDt;
                if (_dayUiTimerSec >= 0.15)
                {
                    _dayUiTimerSec = 0.0;
                    SyncControlsFromState();
                    RenderTelemetry();
                }
            }

            if (_layers.Vaults && _subterraneanScene != null)
            {
                _subterraneanScene.Update(_sim, simDt, _simElapsed);
            }
            if (_layers.Surface && _surfaceScene != null)
            {
                _surfaceScene.Update(_sim, simDt, _simElapsed, _layers.Heatmap);
            }
            if ((_layers.Loop1 || _layers.Loop2 || _layers.Loop3 || _layers.Optical) && _flowsScene != null)
            {
                _flowsScene.Update(_sim, simDt, _simElapsed, _layers);
            }

            _renderer.BeginFrame(rawDt);
            if (_layers.Vaults && _subterraneanScene != null)
            {
                _subterraneanScene.Render(_renderer, _layers);
            }
            if (_layers.Surface && _surfaceScene != null)
            {
                _surfaceScene.Render(_renderer, _layers);
            }
            if ((_layers.Loop1 || _layers.Loop2 || _layers.Loop3 || _layers.Optical) && _flowsScene != null)
            {
                _flowsScene.Render(_renderer, _layers);
            }
            _renderer.EndFrame();
        }

        // =========================================================================
        // Automated Benchmarking & Screenshot Capture (CLI Flags)
        // =========================================================================

        public void RunBenchmark()
        {
            EnsureGlInitialized();
            if (!_rendererReady)
            {
                throw new InvalidOperationException("OpenGL renderer not ready.");
            }

            Console.WriteLine("=== RAKDA 2045 Native OpenGL 3.3 GPU Benchmark ===");
            Console.WriteLine("GL_RENDERER: " + _renderer.GpuName);
            Console.WriteLine(string.Format(
                Inv,
                "Viewport: {0}x{1} (Window {2}x{3}) | UI Scale: {4}% | Frame Cap: {5}",
                _glViewport.Width,
                _glViewport.Height,
                ClientSize.Width,
                ClientSize.Height,
                (int)Math.Round(UiScale * 100f),
                _uncapped ? "Uncapped (--uncapped)" : "60.0 FPS"));

            // Phase 1: Overview (120 frames)
            ApplyPreset("nominal");
            _layers.Surface = true;
            _layers.Heatmap = false;
            _layers.Loop1 = true;
            _layers.Loop2 = true;
            _layers.Loop3 = true;
            _layers.Optical = true;
            _layers.Vaults = true;
            SetGroundOpacity(0.78f);
            SelectCameraPreset(0);
            _renderer.SetSelectedTarget(null);
            _renderer.MarkShadowsDirty();

            // Warmup & camera settle (unpaced so startup is instant)
            for (int i = 0; i < 60; i++) RenderSingleFrame(0.033f);

            Stopwatch sw1 = Stopwatch.StartNew();
            const int frames = 120;
            double nextDeadline1 = _frameWatch.Elapsed.TotalSeconds;
            for (int i = 0; i < frames; i++)
            {
                nextDeadline1 += TargetFrameMs * 0.001;
                RenderSingleFrame(0.016667f);
                PaceToDeadline(nextDeadline1);
            }
            sw1.Stop();
            double ovMs = sw1.Elapsed.TotalMilliseconds / frames;
            double ovFps = 1000.0 / Math.Max(0.001, ovMs);
            int ovCalls = _renderer.LastDrawCalls;
            int ovTris = _renderer.LastTriangles;

            _pacedFps = (float)ovFps;
            _pacedFrameMs = (float)ovMs;

            Console.WriteLine(string.Format(
                Inv,
                "[Overview 120f] FPS: {0:F1} | Avg: {1:F2} ms | DrawCalls: {2} | Triangles: {3:N0}",
                ovFps, ovMs, ovCalls, ovTris));

            // Phase 2: X-Ray Vaults (120 frames)
            SetGroundOpacity(0.16f);
            SelectCameraPreset(1);
            _renderer.MarkShadowsDirty();

            for (int i = 0; i < 60; i++) RenderSingleFrame(0.033f);

            Stopwatch sw2 = Stopwatch.StartNew();
            double nextDeadline2 = _frameWatch.Elapsed.TotalSeconds;
            for (int i = 0; i < frames; i++)
            {
                nextDeadline2 += TargetFrameMs * 0.001;
                RenderSingleFrame(0.016667f);
                PaceToDeadline(nextDeadline2);
            }
            sw2.Stop();
            double xrMs = sw2.Elapsed.TotalMilliseconds / frames;
            double xrFps = 1000.0 / Math.Max(0.001, xrMs);
            int xrCalls = _renderer.LastDrawCalls;
            int xrTris = _renderer.LastTriangles;

            Console.WriteLine(string.Format(
                Inv,
                "[X-Ray Vaults 120f] FPS: {0:F1} | Avg: {1:F2} ms | DrawCalls: {2} | Triangles: {3:N0}",
                xrFps, xrMs, xrCalls, xrTris));
        }

        public void CaptureScreenshots(string outputDir)
        {
            EnsureGlInitialized();
            if (!_rendererReady)
            {
                throw new InvalidOperationException("OpenGL renderer not ready.");
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Console.WriteLine("=== RAKDA 2045 Native Screenshot Capture ===");
            Console.WriteLine("Output Directory: " + outputDir);
            Console.WriteLine(string.Format(Inv, "GL_RENDERER: {0} | UI Scale: {1}%", _renderer.GpuName, (int)Math.Round(UiScale * 100f)));

            CaptureOneShot(
                Path.Combine(outputDir, "native_01_overview.png"),
                "nominal",
                0,
                0.78f,
                false,
                "residential_district",
                false);

            CaptureOneShot(
                Path.Combine(outputDir, "native_02_xray_vaults.png"),
                "nominal",
                1,
                0.15f,
                false,
                "thermal_store",
                true);

            CaptureOneShot(
                Path.Combine(outputDir, "native_03_vault3_fault_sdn.png"),
                "vault_fault_sdn",
                3,
                0.18f,
                false,
                "vault_2",
                true);

            CaptureOneShot(
                Path.Combine(outputDir, "native_04_summer_exergy.png"),
                "summer_exergy",
                2,
                0.18f,
                false,
                "micro_orc",
                true);

            CaptureOneShot(
                Path.Combine(outputDir, "native_05_street_pods.png"),
                "nominal",
                4,
                0.95f,
                false,
                "autonomous_pods",
                true);
        }

        private void CaptureOneShot(
            string filePath,
            string presetId,
            int cameraPresetIdx,
            float groundOpacity,
            bool heatmapVisible,
            string inspectKey,
            bool showSelectionRing)
        {
            ApplyPreset(presetId);
            _layers.Surface = true;
            _layers.Heatmap = heatmapVisible;
            _layers.Loop1 = true;
            _layers.Loop2 = true;
            _layers.Loop3 = true;
            _layers.Optical = true;
            _layers.Vaults = true;
            SetGroundOpacity(groundOpacity);
            SelectCameraPreset(cameraPresetIdx);

            if (showSelectionRing && !string.IsNullOrEmpty(inspectKey))
            {
                SelectSubsystemForTarget(inspectKey);
            }
            else
            {
                _renderer.SetSelectedTarget(null);
                if (!string.IsNullOrEmpty(inspectKey))
                {
                    SetInspectedKey(inspectKey);
                }
            }

            _renderer.MarkShadowsDirty();

            // Settle camera flight & advance animations smoothly (fast, no artificial sleep)
            for (int i = 0; i < 95; i++)
            {
                RenderSingleFrame(0.033f);
            }

            // Update GPU/FPS pill before painting HUD
            UpdateGpuFpsPill();
            _rootContainer.PerformLayout();
            _rootContainer.Refresh();
            Application.DoEvents();

            // Render final crisp GL frame immediately before framebuffer readback
            RenderSingleFrame(0.016f);

            int fullW = _rootContainer.ClientSize.Width;
            int fullH = _rootContainer.ClientSize.Height;

            using (Bitmap composite = new Bitmap(fullW, fullH, PixelFormat.Format24bppRgb))
            {
                // 1. Capture entire WinForms dark HUD via DrawToBitmap
                _rootContainer.DrawToBitmap(composite, new Rectangle(0, 0, fullW, fullH));

                // 2. Capture native OpenGL 3.3 framebuffer and composite cleanly into exact viewport rect
                using (Bitmap glBmp = _renderer.ReadFramebufferBitmap())
                {
                    if (glBmp != null)
                    {
                        Point vpLoc = _rootContainer.PointToClient(_glViewport.PointToScreen(Point.Empty));
                        Rectangle dstRect = new Rectangle(vpLoc.X, vpLoc.Y, _glViewport.Width, _glViewport.Height);
                        using (Graphics g = Graphics.FromImage(composite))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(glBmp, dstRect);
                        }
                    }
                }

                composite.Save(filePath, ImageFormat.Png);
            }

            FileInfo fi = new FileInfo(filePath);
            Console.WriteLine(string.Format(
                Inv,
                "Saved {0} ({1}x{2}, {3:N0} bytes) | Scale: {4}% | DrawCalls: {5} | Tris: {6:N0}",
                Path.GetFileName(filePath),
                fullW,
                fullH,
                fi.Length,
                (int)Math.Round(UiScale * 100f),
                _renderer.LastDrawCalls,
                _renderer.LastTriangles));
        }

        // =========================================================================
        // Custom Owner-Drawn Controls & Rendering Helpers
        // =========================================================================

        internal static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class GlViewportControl : Control
        {
            private Renderer _renderer;
            private bool _leftDown = false;
            private bool _rightDown = false;
            private Point _downPoint;
            private Point _lastPoint;
            private float _totalDragDist = 0f;

            public event Action<int, int> ClickedPick;

            public GlViewportControl()
            {
                SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);
                BackColor = ColBgBase;
                Cursor = Cursors.Hand;
            }

            public void AttachRenderer(Renderer r)
            {
                _renderer = r;
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // Intentionally empty: OpenGL owns the framebuffer completely (zero flicker).
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (_renderer == null)
                {
                    using (SolidBrush br = new SolidBrush(ColBgBase))
                    {
                        e.Graphics.FillRectangle(br, ClientRectangle);
                    }
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (_renderer != null && Width > 0 && Height > 0)
                {
                    _renderer.Resize(Width, Height);
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                _downPoint = e.Location;
                _lastPoint = e.Location;
                _totalDragDist = 0f;

                if (e.Button == MouseButtons.Left)
                {
                    _leftDown = true;
                    Capture = true;
                }
                else if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
                {
                    _rightDown = true;
                    Cursor = Cursors.SizeAll;
                    Capture = true;
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_renderer == null) return;

                float dx = e.X - _lastPoint.X;
                float dy = e.Y - _lastPoint.Y;
                _lastPoint = e.Location;
                _totalDragDist += (float)Math.Sqrt(dx * dx + dy * dy);

                if (_leftDown)
                {
                    _renderer.Orbit(dx, dy);
                }
                else if (_rightDown)
                {
                    _renderer.Pan(dx, dy);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                bool wasLeft = _leftDown && e.Button == MouseButtons.Left;
                _leftDown = false;
                _rightDown = false;
                Capture = false;
                Cursor = Cursors.Hand;

                if (wasLeft)
                {
                    float directDist = (float)Math.Sqrt(
                        (e.X - _downPoint.X) * (e.X - _downPoint.X) +
                        (e.Y - _downPoint.Y) * (e.Y - _downPoint.Y));
                    if (directDist < 5f && _totalDragDist < 8f)
                    {
                        Action<int, int> handler = ClickedPick;
                        if (handler != null)
                        {
                            handler(e.X, e.Y);
                        }
                    }
                }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                if (_renderer != null)
                {
                    _renderer.Zoom(e.Delta / 120f);
                }
            }
        }

        private enum BorderSideMode
        {
            None,
            Left,
            Right
        }

        private sealed class DarkScrollPanel : Panel
        {
            public BorderSideMode BorderSide = BorderSideMode.None;

            public DarkScrollPanel()
            {
                DoubleBuffered = true;
                AutoScroll = true;
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                try
                {
                    SetWindowTheme(Handle, "DarkMode_Explorer", null);
                }
                catch
                {
                    // Safe fallback on OS versions without DarkMode_Explorer
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (Pen p = new Pen(ColBorder, 1f))
                {
                    if (BorderSide == BorderSideMode.Right)
                    {
                        e.Graphics.DrawLine(p, Width - 1, 0, Width - 1, Height);
                    }
                    else if (BorderSide == BorderSideMode.Left)
                    {
                        e.Graphics.DrawLine(p, 0, 0, 0, Height);
                    }
                }
            }
        }

        private sealed class HeaderBarPanel : Panel
        {
            public HeaderBarPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Subtle bottom border
                using (Pen bp = new Pen(ColBorder, 1f))
                {
                    g.DrawLine(bp, 0, Height - 1, Width, Height - 1);
                }

                // RAKDA · A-10 gradient badge
                Rectangle badgeRect = new Rectangle(S(12), S(16), S(86), S(26));
                using (GraphicsPath bp = CreateRoundedRect(badgeRect, S(6)))
                using (LinearGradientBrush gb = new LinearGradientBrush(badgeRect, Color.FromArgb(14, 165, 233), Color.FromArgb(99, 102, 241), 25f))
                {
                    g.FillPath(gb, bp);
                }
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush tb = new SolidBrush(Color.White))
                    {
                        g.DrawString("RAKDA \u00b7 A-10", FontBadge, tb, badgeRect, sf);
                    }
                }

                // Title & subtitle
                using (SolidBrush mainBr = new SolidBrush(ColTextMain))
                using (SolidBrush mutedBr = new SolidBrush(ColTextMuted))
                {
                    g.DrawString("10.0 MW Subterranean DC \u00b7 Circular District", FontBrandTitle, mainBr, S(106), S(12));
                    g.DrawString("TCD 1E17 EP2 \u00b7 RAKDA USCI (Nov 2026) OpenGL 3.3 Twin", FontBrandSub, mutedBr, S(106), S(31));
                }
            }
        }

        private sealed class FooterDockPanel : Panel
        {
            public FooterDockPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (Pen bp = new Pen(ColBorder, 1f))
                {
                    e.Graphics.DrawLine(bp, 0, 0, Width, 0);
                }
            }
        }

        private sealed class KpiPill : Control
        {
            private string _labelText;
            private string _valueText;
            private readonly Color _accent;
            public bool AccentBorder = false;

            public string LabelText
            {
                get { return _labelText; }
                set
                {
                    if (_labelText != value)
                    {
                        _labelText = value;
                        Invalidate();
                    }
                }
            }

            public string ValueText
            {
                get { return _valueText; }
                set
                {
                    if (_valueText != value)
                    {
                        _valueText = value;
                        Invalidate();
                    }
                }
            }

            public KpiPill(string label, string val, Color accent)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _labelText = label;
                _valueText = val;
                _accent = accent;
                BackColor = ColPanelBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(r, S(7)))
                using (SolidBrush bg = new SolidBrush(ColCardBg))
                using (Pen border = new Pen(AccentBorder ? ColBorderAccent : ColBorder, 1f))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(border, path);
                }

                using (SolidBrush lb = new SolidBrush(ColTextMuted))
                using (SolidBrush vb = new SolidBrush(_accent))
                using (StringFormat sf = new StringFormat())
                {
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    int px = S(7);
                    g.DrawString(_labelText, FontKpiLabel, lb, new RectangleF(px, S(5), Width - px * 2, S(14)), sf);
                    g.DrawString(_valueText, FontKpiVal, vb, new RectangleF(px, S(20), Width - px * 2, S(18)), sf);
                }
            }
        }

        private sealed class SectionCard : Panel
        {
            private readonly string _title;
            private string _tagText;
            private readonly Color _tagColor;
            public bool AccentBorder = false;

            public string TagText
            {
                get { return _tagText; }
                set
                {
                    if (_tagText != value)
                    {
                        _tagText = value;
                        Invalidate();
                    }
                }
            }

            public SectionCard(string title, string tagText, Color tagColor)
            {
                DoubleBuffered = true;
                _title = title;
                _tagText = tagText;
                _tagColor = tagColor;
                BackColor = ColCardBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(r, S(9)))
                using (SolidBrush bg = new SolidBrush(ColCardBg))
                using (Pen border = new Pen(AccentBorder ? ColBorderAccent : ColBorder, 1f))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(border, path);
                }

                int pad = S(10);

                // Header pill tag first so title knows exact right bound
                int tagLeft = Width - pad;
                if (!string.IsNullOrEmpty(_tagText))
                {
                    string displayTag = _tagText.Length > 22 ? _tagText.Substring(0, 21) + "\u2026" : _tagText;
                    SizeF sz = g.MeasureString(displayTag, FontTag);
                    int tw = Math.Min(Width / 2, (int)Math.Ceiling(sz.Width) + S(12));
                    int th = S(18);
                    tagLeft = Width - tw - pad;
                    Rectangle tagRect = new Rectangle(tagLeft, S(7), tw, th);
                    using (GraphicsPath tp = CreateRoundedRect(tagRect, S(8)))
                    using (SolidBrush tbg = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    using (Pen tpen = new Pen(Color.FromArgb(95, _tagColor.R, _tagColor.G, _tagColor.B), 1f))
                    using (SolidBrush tfg = new SolidBrush(_tagColor))
                    using (StringFormat sf = new StringFormat())
                    {
                        g.FillPath(tbg, tp);
                        g.DrawPath(tpen, tp);
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        g.DrawString(displayTag, FontTag, tfg, tagRect, sf);
                    }
                }

                // Header title (cleanly bounded away from tag)
                using (SolidBrush tb = new SolidBrush(ColTextSec))
                using (StringFormat sfTitle = new StringFormat())
                {
                    sfTitle.FormatFlags = StringFormatFlags.NoWrap;
                    sfTitle.Trimming = StringTrimming.EllipsisCharacter;
                    g.DrawString(_title, FontSection, tb, new RectangleF(pad, S(9), Math.Max(20, tagLeft - pad - S(4)), S(18)), sfTitle);
                }
            }
        }

        private sealed class AccentDescriptionBox : Control
        {
            private string _desc = "";

            public string DescriptionText
            {
                get { return _desc; }
                set
                {
                    if (_desc != value)
                    {
                        _desc = value ?? "";
                        Invalidate();
                    }
                }
            }

            public AccentDescriptionBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = ColCardBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(r, S(5)))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(14, 22, 38)))
                {
                    g.FillPath(bg, path);
                }

                // Cyan left accent bar
                using (SolidBrush bar = new SolidBrush(ColCyan))
                {
                    g.FillRectangle(bar, 0, S(2), Math.Max(2, S(3)), Height - S(4));
                }

                Rectangle textRect = new Rectangle(S(8), S(4), Width - S(12), Height - S(8));
                TextRenderer.DrawText(
                    g,
                    _desc,
                    FontSmall,
                    textRect,
                    ColTextSec,
                    TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
            }
        }

        private sealed class DarkButton : Control
        {
            private bool _hovered = false;
            private bool _active = false;
            private bool _primary = false;
            private bool _faultedBadge = false;
            private bool _healthyBadge = false;
            public bool PresetStyle = false;
            public bool AlignLeft = false;
            public bool MonoFont = false;
            public string SubLabel = null;

            public bool Active
            {
                get { return _active; }
                set { if (_active != value) { _active = value; Invalidate(); } }
            }

            public bool Primary
            {
                get { return _primary; }
                set { if (_primary != value) { _primary = value; Invalidate(); } }
            }

            public bool FaultedBadge
            {
                get { return _faultedBadge; }
                set { if (_faultedBadge != value) { _faultedBadge = value; Invalidate(); } }
            }

            public bool HealthyBadge
            {
                get { return _healthyBadge; }
                set { if (_healthyBadge != value) { _healthyBadge = value; Invalidate(); } }
            }

            public DarkButton(string text)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Text = text;
                Cursor = Cursors.Hand;
                BackColor = ColCardBg;
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color fillCol = ColBtnBg;
                Color borderCol = ColBorder;
                Color fgCol = ColTextMain;

                if (_faultedBadge)
                {
                    fillCol = _hovered ? Color.FromArgb(145, 34, 34) : Color.FromArgb(110, 26, 26);
                    borderCol = ColRed;
                    fgCol = ColRedSoft;
                }
                else if (_healthyBadge)
                {
                    fillCol = _hovered ? Color.FromArgb(14, 90, 68) : Color.FromArgb(10, 66, 52);
                    borderCol = Color.FromArgb(16, 150, 108);
                    fgCol = ColEmeraldSoft;
                }
                else if (_active)
                {
                    fillCol = ColBtnActiveBg;
                    borderCol = ColCyan;
                    fgCol = Color.White;
                }
                else if (_primary)
                {
                    fillCol = _hovered ? Color.FromArgb(18, 86, 122) : Color.FromArgb(16, 62, 92);
                    borderCol = Color.FromArgb(34, 178, 212);
                    fgCol = Color.FromArgb(224, 242, 254);
                }
                else if (_hovered)
                {
                    fillCol = ColBtnHover;
                    borderCol = ColCyan;
                }

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(r, S(6)))
                using (SolidBrush bg = new SolidBrush(fillCol))
                using (Pen bp = new Pen(borderCol, 1f))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(bp, path);
                }

                if (PresetStyle && !string.IsNullOrEmpty(SubLabel))
                {
                    using (SolidBrush tb = new SolidBrush(fgCol))
                    using (SolidBrush sb = new SolidBrush(_active ? ColCyan : ColTextMuted))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        int px = S(7);
                        g.DrawString(Text, FontBodyBold, tb, new RectangleF(px, S(4), Width - px * 2, S(16)), sf);
                        g.DrawString(SubLabel, FontTag, sb, new RectangleF(px, S(20), Width - px * 2, S(14)), sf);
                    }
                }
                else
                {
                    Font f = MonoFont ? FontMonoBold : FontBody;
                    using (SolidBrush tb = new SolidBrush(fgCol))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = AlignLeft ? StringAlignment.Near : StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        Rectangle tr = AlignLeft ? new Rectangle(S(7), 0, Width - S(10), Height) : new Rectangle(S(2), 0, Width - S(4), Height);
                        g.DrawString(Text, f, tb, tr, sf);
                    }
                }
            }
        }

        private sealed class DarkSlider : Control
        {
            private readonly string _label;
            private readonly double _min;
            private readonly double _max;
            private readonly double _step;
            private double _value;
            private string _readout = "";
            private bool _dragging = false;
            private bool _hovered = false;

            public event EventHandler ValueChanged;

            public double Value
            {
                get { return _value; }
            }

            public DarkSlider(string label, double min, double max, double step, double initial)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _label = label;
                _min = min;
                _max = max;
                _step = step;
                _value = initial;
                Cursor = Cursors.Hand;
                BackColor = ColCardBg;
            }

            public void SetValueSilently(double val, string readout)
            {
                double clamped = Math.Max(_min, Math.Min(_max, val));
                bool changed = Math.Abs(_value - clamped) > 1e-6 || _readout != readout;
                _value = clamped;
                _readout = readout ?? "";
                if (changed) Invalidate();
            }

            private void UpdateFromX(int mouseX)
            {
                int pad = S(7);
                int trackW = Math.Max(1, Width - pad * 2);
                double t = (double)(mouseX - pad) / (double)trackW;
                if (t < 0.0) t = 0.0;
                if (t > 1.0) t = 1.0;

                double raw = _min + t * (_max - _min);
                double snapped = Math.Round((raw - _min) / _step) * _step + _min;
                if (snapped < _min) snapped = _min;
                if (snapped > _max) snapped = _max;

                if (Math.Abs(snapped - _value) > 1e-6)
                {
                    _value = snapped;
                    Invalidate();
                    EventHandler h = ValueChanged;
                    if (h != null) h(this, EventArgs.Empty);
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    _dragging = true;
                    Capture = true;
                    UpdateFromX(e.X);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_dragging)
                {
                    UpdateFromX(e.X);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                _dragging = false;
                Capture = false;
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Top label & right monospace readout
                using (SolidBrush lb = new SolidBrush(ColTextSec))
                using (SolidBrush rb = new SolidBrush(ColCyan))
                using (StringFormat sfRight = new StringFormat())
                {
                    sfRight.Alignment = StringAlignment.Far;
                    sfRight.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(_label, FontBody, lb, 0, 1);
                    g.DrawString(_readout, FontMono, rb, new RectangleF(0, 1, Width, S(16)), sfRight);
                }

                // Track
                int pad = S(7);
                int trackY = S(24);
                int trackH = Math.Max(4, S(5));
                int trackW = Math.Max(4, Width - pad * 2);
                double frac = (_max > _min) ? (_value - _min) / (_max - _min) : 0.0;
                int fillW = (int)Math.Round(trackW * frac);

                Rectangle trackRect = new Rectangle(pad, trackY, trackW, trackH);
                using (GraphicsPath tp = CreateRoundedRect(trackRect, Math.Max(2, trackH / 2)))
                using (SolidBrush tbg = new SolidBrush(ColBtnBg))
                {
                    g.FillPath(tbg, tp);
                }

                if (fillW > 1)
                {
                    Rectangle fillRect = new Rectangle(pad, trackY, fillW, trackH);
                    using (GraphicsPath fp = CreateRoundedRect(fillRect, Math.Max(2, trackH / 2)))
                    using (LinearGradientBrush fg = new LinearGradientBrush(
                        new Rectangle(pad, trackY, trackW, trackH), ColSky, ColCyan, 0f))
                    {
                        g.FillPath(fg, fp);
                    }
                }

                // Thumb
                int cx = pad + fillW;
                int cy = trackY + trackH / 2;
                int rad = (_dragging || _hovered) ? S(6) : S(5);
                using (SolidBrush thumb = new SolidBrush(ColCyan))
                using (Pen ring = new Pen(_dragging ? Color.White : ColBgBase, 1.5f))
                {
                    g.FillEllipse(thumb, cx - rad, cy - rad, rad * 2, rad * 2);
                    g.DrawEllipse(ring, cx - rad, cy - rad, rad * 2, rad * 2);
                }
            }
        }

        private sealed class DarkToggleSwitch : Control
        {
            private readonly string _label;
            private bool _checked;

            public event EventHandler CheckedChanged;

            public bool Checked
            {
                get { return _checked; }
                set
                {
                    if (_checked != value)
                    {
                        _checked = value;
                        Invalidate();
                    }
                }
            }

            public DarkToggleSwitch(string label, bool initial)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _label = label;
                _checked = initial;
                Cursor = Cursors.Hand;
                BackColor = ColCardBg;
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                _checked = !_checked;
                Invalidate();
                EventHandler h = CheckedChanged;
                if (h != null) h(this, EventArgs.Empty);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                int sw = S(34);
                int sh = S(18);
                using (SolidBrush lb = new SolidBrush(ColTextMain))
                using (StringFormat sf = new StringFormat())
                {
                    sf.LineAlignment = StringAlignment.Center;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    g.DrawString(_label, FontBody, lb, new RectangleF(0, 0, Width - sw - S(8), Height), sf);
                }

                Rectangle pill = new Rectangle(Width - sw - 1, (Height - sh) / 2, sw, sh);
                using (GraphicsPath pp = CreateRoundedRect(pill, sh / 2))
                using (SolidBrush bg = new SolidBrush(_checked ? Color.FromArgb(14, 116, 144) : ColBtnBg))
                using (Pen bp = new Pen(_checked ? ColCyan : ColBorder, 1f))
                {
                    g.FillPath(bg, pp);
                    g.DrawPath(bp, pp);
                }

                int knobD = S(12);
                int inset = S(3);
                int kx = _checked ? (pill.Right - knobD - inset) : (pill.X + inset);
                int ky = pill.Y + (sh - knobD) / 2;
                using (SolidBrush kb = new SolidBrush(_checked ? Color.White : ColTextMuted))
                {
                    g.FillEllipse(kb, kx, ky, knobD, knobD);
                }
            }
        }

        private sealed class DarkCheckPill : Control
        {
            private readonly string _label;
            private bool _checked;
            private bool _hovered;

            public event EventHandler CheckedChanged;

            public bool Checked
            {
                get { return _checked; }
                set
                {
                    if (_checked != value)
                    {
                        _checked = value;
                        Invalidate();
                    }
                }
            }

            public DarkCheckPill(string label, bool initial)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _label = label;
                _checked = initial;
                Cursor = Cursors.Hand;
                BackColor = ColCardBg;
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                _checked = !_checked;
                Invalidate();
                EventHandler h = CheckedChanged;
                if (h != null) h(this, EventArgs.Empty);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(r, S(5)))
                using (SolidBrush bg = new SolidBrush(_hovered ? ColBtnHover : ColControlBg))
                using (Pen bp = new Pen(_checked ? Color.FromArgb(70, 34, 211, 238) : ColBorder, 1f))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(bp, path);
                }

                int boxSz = S(12);
                Rectangle box = new Rectangle(S(6), (Height - boxSz) / 2, boxSz, boxSz);
                using (GraphicsPath boxP = CreateRoundedRect(box, S(3)))
                using (SolidBrush bb = new SolidBrush(_checked ? ColCyan : ColPanelBg))
                using (Pen bpen = new Pen(_checked ? ColCyan : ColTextMuted, 1f))
                {
                    g.FillPath(bb, boxP);
                    g.DrawPath(bpen, boxP);
                }

                if (_checked)
                {
                    using (Pen chk = new Pen(ColBgBase, Math.Max(1.5f, 1.8f * UiScale)))
                    {
                        g.DrawLine(chk, box.X + boxSz * 0.21f, box.Y + boxSz * 0.52f, box.X + boxSz * 0.42f, box.Y + boxSz * 0.74f);
                        g.DrawLine(chk, box.X + boxSz * 0.42f, box.Y + boxSz * 0.74f, box.X + boxSz * 0.80f, box.Y + boxSz * 0.27f);
                    }
                }

                using (SolidBrush tb = new SolidBrush(_checked ? ColTextMain : ColTextMuted))
                using (StringFormat sf = new StringFormat())
                {
                    sf.LineAlignment = StringAlignment.Center;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    int tx = box.Right + S(5);
                    g.DrawString(_label, FontTag, tb, new RectangleF(tx, 0, Width - tx - S(2), Height), sf);
                }
            }
        }

        private sealed class DarkProgressBar : Control
        {
            private readonly string _label;
            private readonly Color _fillColor;
            private string _valueText = "";
            private float _pct = 0f;

            public DarkProgressBar(string label, Color fillColor)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _label = label;
                _fillColor = fillColor;
                BackColor = ColCardBg;
            }

            public void Update(string valueText, float pct)
            {
                float clamped = pct < 0f ? 0f : (pct > 100f ? 100f : pct);
                if (_valueText != valueText || Math.Abs(_pct - clamped) > 0.05f)
                {
                    _valueText = valueText ?? "";
                    _pct = clamped;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using (SolidBrush lb = new SolidBrush(ColTextSec))
                using (SolidBrush vb = new SolidBrush(ColCyan))
                using (StringFormat sfRight = new StringFormat())
                {
                    sfRight.Alignment = StringAlignment.Far;
                    sfRight.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(_label, FontSmall, lb, 0, 1);
                    g.DrawString(_valueText, FontMonoBold, vb, new RectangleF(0, 0, Width, S(16)), sfRight);
                }

                int barY = S(18);
                int barH = Math.Max(5, S(7));
                Rectangle track = new Rectangle(0, barY, Width - 1, barH);
                using (GraphicsPath tp = CreateRoundedRect(track, Math.Max(2, barH / 2)))
                using (SolidBrush tbg = new SolidBrush(ColBtnBg))
                using (Pen tpen = new Pen(ColBorder, 1f))
                {
                    g.FillPath(tbg, tp);
                    g.DrawPath(tpen, tp);
                }

                int fillW = (int)Math.Round((Width - 2) * (_pct / 100.0f));
                if (fillW > 2)
                {
                    Rectangle fill = new Rectangle(1, barY + 1, fillW, Math.Max(3, barH - 2));
                    using (GraphicsPath fp = CreateRoundedRect(fill, Math.Max(2, (barH - 2) / 2)))
                    using (SolidBrush fbg = new SolidBrush(_fillColor))
                    {
                        g.FillPath(fbg, fp);
                    }
                }
            }
        }

        private struct KvRow
        {
            public string Key;
            public string Value;
            public Color ValueColor;

            public KvRow(string key, string value, Color valueColor)
            {
                Key = key;
                Value = value;
                ValueColor = valueColor;
            }
        }

        private sealed class KvTableControl : Control
        {
            private KvRow[] _rows = new KvRow[0];

            public int RowCount
            {
                get { return _rows.Length; }
            }

            public KvTableControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = ColCardBg;
            }

            public void SetRows(KvRow[] rows)
            {
                if (rows == null) rows = new KvRow[0];
                bool diff = rows.Length != _rows.Length;
                if (!diff)
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (_rows[i].Key != rows[i].Key ||
                            _rows[i].Value != rows[i].Value ||
                            _rows[i].ValueColor != rows[i].ValueColor)
                        {
                            diff = true;
                            break;
                        }
                    }
                }
                if (diff)
                {
                    _rows = rows;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                int rowH = S(19);
                using (SolidBrush kBr = new SolidBrush(ColTextMuted))
                using (StringFormat sfRight = new StringFormat())
                using (StringFormat sfLeft = new StringFormat())
                {
                    sfRight.Alignment = StringAlignment.Far;
                    sfRight.Trimming = StringTrimming.EllipsisCharacter;
                    sfRight.FormatFlags = StringFormatFlags.NoWrap;

                    sfLeft.Alignment = StringAlignment.Near;
                    sfLeft.Trimming = StringTrimming.EllipsisCharacter;
                    sfLeft.FormatFlags = StringFormatFlags.NoWrap;

                    for (int i = 0; i < _rows.Length; i++)
                    {
                        int y = i * rowH;
                        g.DrawString(_rows[i].Key, FontSmall, kBr, new RectangleF(0, y + S(2), Width * 0.56f, rowH), sfLeft);
                        using (SolidBrush vBr = new SolidBrush(_rows[i].ValueColor))
                        {
                            g.DrawString(_rows[i].Value, FontMono, vBr, new RectangleF(0, y + 1, Width, rowH), sfRight);
                        }
                    }
                }
            }
        }
    }
}
