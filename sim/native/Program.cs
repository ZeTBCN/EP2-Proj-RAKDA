using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RakdaSim
{
    internal static class Program
    {
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static int Main(string[] args)
        {
            bool showHelp = false;
            bool runBenchmark = false;
            bool uncapped = false;
            float uiScale = 1.00f;
            string screenshotDir = null;

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (string.IsNullOrEmpty(arg)) continue;

                    if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase))
                    {
                        showHelp = true;
                    }
                    else if (string.Equals(arg, "--benchmark", StringComparison.OrdinalIgnoreCase))
                    {
                        runBenchmark = true;
                    }
                    else if (string.Equals(arg, "--uncapped", StringComparison.OrdinalIgnoreCase))
                    {
                        uncapped = true;
                    }
                    else if (string.Equals(arg, "--ui-scale", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length)
                        {
                            uiScale = ParseUiScale(args[++i], uiScale);
                        }
                    }
                    else if (arg.StartsWith("--ui-scale=", StringComparison.OrdinalIgnoreCase))
                    {
                        string valStr = arg.Substring("--ui-scale=".Length).Trim('"');
                        uiScale = ParseUiScale(valStr, uiScale);
                    }
                    else if (string.Equals(arg, "--screenshot", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                        {
                            screenshotDir = args[++i];
                        }
                        else
                        {
                            screenshotDir = Environment.CurrentDirectory;
                        }
                    }
                    else if (arg.StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
                    {
                        screenshotDir = arg.Substring("--screenshot=".Length).Trim('"');
                        if (string.IsNullOrEmpty(screenshotDir))
                        {
                            screenshotDir = Environment.CurrentDirectory;
                        }
                    }
                }
            }

            bool isCliMode = showHelp || runBenchmark || !string.IsNullOrEmpty(screenshotDir);

            if (isCliMode)
            {
                TryAttachParentConsole();
            }

            if (showHelp)
            {
                PrintUsage();
                return 0;
            }

            EnableDpiAwareness();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (isCliMode)
            {
                return RunAutomatedMode(runBenchmark, screenshotDir, uiScale, uncapped);
            }

            try
            {
                using (MainForm form = new MainForm(false, uiScale, uncapped))
                {
                    Application.Run(form);
                }
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Fatal error launching RAKDA 2045 Native Digital Twin:\n\n" + ex.Message,
                    "RAKDA 3D Sim Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }
        }

        private static float ParseUiScale(string raw, float fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            string cleaned = raw.Trim().TrimEnd('%');
            double val;
            if (!double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                return fallback;
            }
            if (val > 3.0)
            {
                val = val / 100.0;
            }
            if (val < 0.80) val = 0.80;
            if (val > 1.50) val = 1.50;
            return (float)val;
        }

        private static void EnableDpiAwareness()
        {
            try
            {
                // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4)
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                {
                    return;
                }
            }
            catch
            {
                // Safe fallback on OS versions prior to Windows 10 1703
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
                // Ignore if DPI API is unavailable
            }
        }

        private static void TryAttachParentConsole()
        {
            try
            {
                if (!Console.IsOutputRedirected)
                {
                    AttachConsole(ATTACH_PARENT_PROCESS);
                }
            }
            catch
            {
                // Safe fallback if console attachment is unavailable
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("RAKDA Ltd. (Group A-10) - 2045 Circular Urban Digital Twin (Native OpenGL 3.3)");
            Console.WriteLine("Usage: RAKDA_3D_Sim.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  (no arguments)        Launch interactive native 3D desktop application (60 FPS cap).");
            Console.WriteLine("  --ui-scale <percent>  Set initial HUD scale (80-150, e.g. --ui-scale 130). Default: 100.");
            Console.WriteLine("  --benchmark           Run 120-frame Overview + 120-frame X-Ray Vaults benchmark and exit.");
            Console.WriteLine("  --uncapped            Disable the 60 FPS frame cap (raw GPU speed for benchmarking).");
            Console.WriteLine("  --screenshot <dir>    Render and save 5 full-window HUD+OpenGL PNGs to <dir> and exit.");
            Console.WriteLine("  --help, -h            Display this usage information and exit.");
        }

        private static int RunAutomatedMode(bool runBenchmark, string screenshotDir, float uiScale, bool uncapped)
        {
            try
            {
                using (MainForm form = new MainForm(true, uiScale, uncapped))
                {
                    form.Show();
                    Application.DoEvents();

                    if (!form.IsRendererReady)
                    {
                        Console.Error.WriteLine("ERROR: Native OpenGL 3.3 renderer failed to initialize: " + form.RendererInitError);
                        return 2;
                    }

                    if (runBenchmark)
                    {
                        form.RunBenchmark();
                    }

                    if (!string.IsNullOrEmpty(screenshotDir))
                    {
                        string fullDir = Path.GetFullPath(screenshotDir);
                        form.CaptureScreenshots(fullDir);
                    }

                    form.Close();
                }

                Console.Out.Flush();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: Automated execution failed: " + ex.ToString());
                return 1;
            }
        }
    }
}
