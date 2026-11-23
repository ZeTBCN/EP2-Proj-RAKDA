using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

[assembly: AssemblyTitle("RAKDA EP2 3D Simulation Launcher")]
[assembly: AssemblyDescription("Standalone local HTTP static server and launcher for the RAKDA Ltd. (Group A-10) 2045 Circular Urban Digital Twin.")]
[assembly: AssemblyProduct("RAKDA EP2 3D Simulation")]
[assembly: AssemblyCompany("RAKDA Ltd. (Group A-10)")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace RakdaLauncher
{
    internal static class Program
    {
        private const int DefaultPort = 8170;
        private const int FallbackPortScanCount = 30;
        private const int MaxHeaderBytes = 32768;
        private const int StreamBufferSize = 65536;
        private const int KeepAliveMaxRequests = 64;
        private const int KeepAliveTimeoutSeconds = 5;

        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html; charset=utf-8" },
            { ".css", "text/css; charset=utf-8" },
            { ".js", "text/javascript; charset=utf-8" },
            { ".mjs", "text/javascript; charset=utf-8" },
            { ".json", "application/json; charset=utf-8" },
            { ".svg", "image/svg+xml" },
            { ".png", "image/png" },
            { ".ico", "image/x-icon" },
            { ".md", "text/markdown; charset=utf-8" },
            { ".txt", "text/plain; charset=utf-8" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".webp", "image/webp" },
            { ".woff2", "font/woff2" },
            { ".wasm", "application/wasm" }
        };

        private static readonly ManualResetEvent ShutdownEvent = new ManualResetEvent(false);
        private static volatile bool _shuttingDown;
        private static TcpListener _listener;

        private delegate bool ConsoleCtrlHandlerDelegate(int ctrlType);
        private static ConsoleCtrlHandlerDelegate _ctrlHandler;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);

        private sealed class HttpRequestInfo
        {
            public string RequestLine;
            public bool KeepAlive;
            public string IfNoneMatch;
        }

        private static int Main(string[] args)
        {
            int preferredPort;
            bool noBrowser;
            bool showHelp;
            string parseError;

            if (!ParseArgs(args, out preferredPort, out noBrowser, out showHelp, out parseError))
            {
                Console.Error.WriteLine("[ERROR] " + parseError);
                PrintUsage();
                return 2;
            }

            if (showHelp)
            {
                PrintUsage();
                return 0;
            }

            string rootDir = ResolveRootDirectory();
            string rootPrefix = EnsureTrailingSeparator(rootDir);

            int boundPort;
            TcpListener listener = StartListenerWithFallback(preferredPort, out boundPort);
            if (listener == null)
            {
                Console.Error.WriteLine("[ERROR] Unable to bind local HTTP server on 127.0.0.1.");
                return 1;
            }

            _listener = listener;
            RegisterShutdownHandlers();

            string url = "http://localhost:" + boundPort;
            TrySetConsoleTitle("RAKDA EP2 3D Simulation — " + url);

            PrintBanner(url, preferredPort, boundPort, rootDir, noBrowser);

            if (!File.Exists(Path.Combine(rootDir, "index.html")))
            {
                Console.WriteLine("[WARN] index.html was not found in " + rootDir);
                Console.WriteLine("       Place RAKDA_3D_Sim.exe in the simulation root folder.");
            }

            Thread acceptThread = new Thread(delegate() { AcceptLoop(listener, rootDir, rootPrefix); });
            acceptThread.IsBackground = true;
            acceptThread.Name = "HttpAcceptLoop";
            acceptThread.Start();

            if (!noBrowser)
            {
                TryOpenBrowser(url);
            }

            ShutdownEvent.WaitOne();

            StopListener();
            Console.WriteLine("[INFO] Server stopped cleanly.");
            return 0;
        }

        private static bool ParseArgs(string[] args, out int port, out bool noBrowser, out bool showHelp, out string error)
        {
            port = ResolveEnvPort();
            noBrowser = false;
            showHelp = false;
            error = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--no-browser", StringComparison.OrdinalIgnoreCase))
                {
                    noBrowser = true;
                }
                else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase))
                {
                    showHelp = true;
                }
                else if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "-p", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        error = "Missing value after " + arg + ".";
                        return false;
                    }
                    int parsed;
                    if (!TryParsePort(args[++i], out parsed))
                    {
                        error = "Invalid port number: " + args[i] + " (expected 1-65535).";
                        return false;
                    }
                    port = parsed;
                }
                else if (arg.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = arg.Substring("--port=".Length);
                    int parsed;
                    if (!TryParsePort(raw, out parsed))
                    {
                        error = "Invalid port number: " + raw + " (expected 1-65535).";
                        return false;
                    }
                    port = parsed;
                }
                else
                {
                    error = "Unknown argument: " + arg;
                    return false;
                }
            }

            return true;
        }

        private static int ResolveEnvPort()
        {
            string envPort = Environment.GetEnvironmentVariable("PORT");
            int parsed;
            if (!string.IsNullOrEmpty(envPort) && TryParsePort(envPort.Trim(), out parsed))
            {
                return parsed;
            }
            return DefaultPort;
        }

        private static bool TryParsePort(string value, out int port)
        {
            if (int.TryParse(value, out port) && port >= 1 && port <= 65535)
            {
                return true;
            }
            port = 0;
            return false;
        }

        private static string ResolveRootDirectory()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string dir = !string.IsNullOrEmpty(exePath)
                ? Path.GetDirectoryName(exePath)
                : AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }
            return path + Path.DirectorySeparatorChar;
        }

        private static TcpListener StartListenerWithFallback(int preferredPort, out int boundPort)
        {
            int maxScanPort = Math.Min(65535, preferredPort + FallbackPortScanCount - 1);
            for (int candidate = preferredPort; candidate <= maxScanPort; candidate++)
            {
                TcpListener listener = TryStartListener(candidate, out boundPort);
                if (listener != null)
                {
                    return listener;
                }
            }

            // Fall back to an OS-assigned ephemeral port if the sequential range is busy.
            return TryStartListener(0, out boundPort);
        }

        private static TcpListener TryStartListener(int port, out int boundPort)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start(64);
                boundPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                return listener;
            }
            catch (SocketException)
            {
                if (listener != null)
                {
                    try { listener.Stop(); } catch { }
                }
                boundPort = 0;
                return null;
            }
        }

        private static void RegisterShutdownHandlers()
        {
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                RequestShutdown();
            };

            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
            {
                RequestShutdown();
            };

            try
            {
                _ctrlHandler = delegate(int ctrlType)
                {
                    RequestShutdown();
                    return true;
                };
                SetConsoleCtrlHandler(_ctrlHandler, true);
            }
            catch
            {
                // Ignore if running without an attached console.
            }
        }

        private static void RequestShutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;
            StopListener();
            ShutdownEvent.Set();
        }

        private static void StopListener()
        {
            TcpListener listener = Interlocked.Exchange(ref _listener, null);
            if (listener != null)
            {
                try { listener.Stop(); } catch { }
            }
        }

        private static void AcceptLoop(TcpListener listener, string rootDir, string rootPrefix)
        {
            while (!_shuttingDown)
            {
                TcpClient client = null;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (_shuttingDown) break;
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                TcpClient accepted = client;
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    HandleClient(accepted, rootDir, rootPrefix);
                });
            }
        }

        private static void HandleClient(TcpClient client, string rootDir, string rootPrefix)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = KeepAliveTimeoutSeconds * 1000;
                    client.SendTimeout = 10000;
                    client.NoDelay = true;
                    client.SendBufferSize = StreamBufferSize;

                    byte[] copyBuffer = new byte[StreamBufferSize];
                    byte[] pendingBuffer = new byte[0];

                    using (NetworkStream stream = client.GetStream())
                    {
                        for (int reqCount = 0; reqCount < KeepAliveMaxRequests && !_shuttingDown; reqCount++)
                        {
                            HttpRequestInfo req = ReadRequest(stream, ref pendingBuffer);
                            if (req == null || string.IsNullOrEmpty(req.RequestLine))
                            {
                                break;
                            }

                            bool keepAlive = req.KeepAlive && (reqCount + 1 < KeepAliveMaxRequests) && !_shuttingDown;

                            string[] parts = req.RequestLine.Split(' ');
                            if (parts.Length < 2)
                            {
                                SendTextResponse(stream, 400, "Bad Request", "400 Bad Request", false, false, null);
                                break;
                            }

                            string method = parts[0].ToUpperInvariant();
                            string rawTarget = parts[1];

                            if (method != "GET" && method != "HEAD")
                            {
                                SendTextResponse(stream, 405, "Method Not Allowed", "405 Method Not Allowed", false, keepAlive, "Allow: GET, HEAD\r\n");
                                if (!keepAlive) break;
                                continue;
                            }

                            bool isHead = (method == "HEAD");
                            bool okToKeepAlive = ServeStaticRequest(stream, rawTarget, req.IfNoneMatch, isHead, keepAlive, rootDir, rootPrefix, copyBuffer);
                            if (!okToKeepAlive)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    // Client closed connection or keep-alive idle timeout expired.
                }
                catch (SocketException)
                {
                    // Client disconnected.
                }
                catch (Exception)
                {
                    // Guard worker thread from unhandled exceptions.
                }
            }
        }

        private static HttpRequestInfo ReadRequest(NetworkStream stream, ref byte[] pendingBuffer)
        {
            byte[] readChunk = new byte[4096];
            using (MemoryStream ms = new MemoryStream())
            {
                if (pendingBuffer.Length > 0)
                {
                    ms.Write(pendingBuffer, 0, pendingBuffer.Length);
                    pendingBuffer = new byte[0];
                }

                while (ms.Length < MaxHeaderBytes)
                {
                    byte[] current = ms.ToArray();
                    int delimLen;
                    int headerEnd = FindHeaderEnd(current, out delimLen);
                    if (headerEnd >= 0)
                    {
                        int bodyStart = headerEnd + delimLen;
                        if (bodyStart < current.Length)
                        {
                            int leftover = current.Length - bodyStart;
                            pendingBuffer = new byte[leftover];
                            Buffer.BlockCopy(current, bodyStart, pendingBuffer, 0, leftover);
                        }

                        string headerText = Encoding.ASCII.GetString(current, 0, headerEnd);
                        return ParseRequestHeaders(headerText);
                    }

                    int read = stream.Read(readChunk, 0, Math.Min(readChunk.Length, MaxHeaderBytes - (int)ms.Length));
                    if (read <= 0)
                    {
                        return null;
                    }
                    ms.Write(readChunk, 0, read);
                }
            }
            return null;
        }

        private static HttpRequestInfo ParseRequestHeaders(string headerText)
        {
            string[] lines = headerText.Split(new char[] { '\n' });
            if (lines.Length == 0) return null;

            HttpRequestInfo info = new HttpRequestInfo();
            info.RequestLine = lines[0].Trim('\r', '\n');
            info.KeepAlive = false;
            info.IfNoneMatch = null;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim('\r', '\n');
                int colon = line.IndexOf(':');
                if (colon <= 0) continue;

                string name = line.Substring(0, colon).Trim();
                string val = line.Substring(colon + 1).Trim();

                if (string.Equals(name, "Connection", StringComparison.OrdinalIgnoreCase))
                {
                    if (val.IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        info.KeepAlive = true;
                    }
                    else if (val.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        info.KeepAlive = false;
                    }
                }
                else if (string.Equals(name, "If-None-Match", StringComparison.OrdinalIgnoreCase))
                {
                    info.IfNoneMatch = val;
                }
            }

            return info;
        }

        private static int FindHeaderEnd(byte[] data, out int delimLen)
        {
            for (int i = 0; i <= data.Length - 4; i++)
            {
                if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                {
                    delimLen = 4;
                    return i;
                }
            }
            for (int i = 0; i <= data.Length - 2; i++)
            {
                if (data[i] == '\n' && data[i + 1] == '\n')
                {
                    delimLen = 2;
                    return i;
                }
            }
            delimLen = 0;
            return -1;
        }

        private static bool ServeStaticRequest(
            NetworkStream stream,
            string rawTarget,
            string ifNoneMatch,
            bool isHead,
            bool keepAlive,
            string rootDir,
            string rootPrefix,
            byte[] copyBuffer)
        {
            string pathPart = rawTarget;
            int queryIndex = pathPart.IndexOf('?');
            if (queryIndex >= 0)
            {
                pathPart = pathPart.Substring(0, queryIndex);
            }
            int hashIndex = pathPart.IndexOf('#');
            if (hashIndex >= 0)
            {
                pathPart = pathPart.Substring(0, hashIndex);
            }

            string decodedPath;
            try
            {
                decodedPath = Uri.UnescapeDataString(pathPart);
            }
            catch (UriFormatException)
            {
                SendTextResponse(stream, 400, "Bad Request", "400 Bad Request", isHead, false, null);
                return false;
            }

            if (string.IsNullOrEmpty(decodedPath) || decodedPath.IndexOf('\0') >= 0)
            {
                SendTextResponse(stream, 400, "Bad Request", "400 Bad Request", isHead, false, null);
                return false;
            }

            if (decodedPath == "/")
            {
                decodedPath = "/index.html";
            }

            // Strict path-traversal check on segments before canonical path resolution.
            string[] segments = decodedPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "..")
                {
                    SendTextResponse(stream, 403, "Forbidden", "403 Forbidden", isHead, keepAlive, null);
                    return keepAlive;
                }
            }

            string relativePath = decodedPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(relativePath))
            {
                relativePath = "index.html";
            }

            if (Path.IsPathRooted(relativePath) || relativePath.IndexOf(':') >= 0)
            {
                SendTextResponse(stream, 403, "Forbidden", "403 Forbidden", isHead, keepAlive, null);
                return keepAlive;
            }

            string targetPath;
            try
            {
                targetPath = Path.GetFullPath(Path.Combine(rootDir, relativePath));
            }
            catch (Exception)
            {
                SendTextResponse(stream, 403, "Forbidden", "403 Forbidden", isHead, keepAlive, null);
                return keepAlive;
            }

            if (!string.Equals(targetPath, rootDir, StringComparison.OrdinalIgnoreCase) &&
                !targetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                SendTextResponse(stream, 403, "Forbidden", "403 Forbidden", isHead, keepAlive, null);
                return keepAlive;
            }

            FileInfo fi = new FileInfo(targetPath);
            if (!fi.Exists)
            {
                SendTextResponse(stream, 404, "Not Found", "404 Not Found", isHead, keepAlive, null);
                return keepAlive;
            }

            try
            {
                string etag = "\"" + fi.Length.ToString("x") + "-" + fi.LastWriteTimeUtc.Ticks.ToString("x") + "\"";
                if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch.Trim(), etag, StringComparison.Ordinal))
                {
                    StringBuilder notMod = new StringBuilder();
                    notMod.Append("HTTP/1.1 304 Not Modified\r\n");
                    notMod.Append("ETag: ").Append(etag).Append("\r\n");
                    notMod.Append("Cache-Control: no-cache\r\n");
                    AppendConnectionHeaders(notMod, keepAlive);
                    notMod.Append("\r\n");

                    byte[] nmBytes = Encoding.ASCII.GetBytes(notMod.ToString());
                    stream.Write(nmBytes, 0, nmBytes.Length);
                    stream.Flush();
                    return keepAlive;
                }

                string ext = Path.GetExtension(targetPath);
                string contentType;
                if (string.IsNullOrEmpty(ext) || !MimeTypes.TryGetValue(ext, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                using (FileStream fs = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan))
                {
                    long fileLength = fs.Length;

                    StringBuilder header = new StringBuilder();
                    header.Append("HTTP/1.1 200 OK\r\n");
                    header.Append("Content-Type: ").Append(contentType).Append("\r\n");
                    header.Append("Content-Length: ").Append(fileLength).Append("\r\n");
                    header.Append("ETag: ").Append(etag).Append("\r\n");
                    header.Append("Cache-Control: no-cache\r\n");
                    header.Append("X-Content-Type-Options: nosniff\r\n");
                    AppendConnectionHeaders(header, keepAlive);
                    header.Append("\r\n");

                    byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
                    stream.Write(headerBytes, 0, headerBytes.Length);

                    if (!isHead && fileLength > 0)
                    {
                        long remaining = fileLength;
                        while (remaining > 0)
                        {
                            int toRead = (int)Math.Min(copyBuffer.Length, remaining);
                            int n = fs.Read(copyBuffer, 0, toRead);
                            if (n <= 0) break;
                            stream.Write(copyBuffer, 0, n);
                            remaining -= n;
                        }
                    }
                    stream.Flush();
                }

                return keepAlive;
            }
            catch (UnauthorizedAccessException)
            {
                SendTextResponse(stream, 403, "Forbidden", "403 Forbidden", isHead, keepAlive, null);
                return keepAlive;
            }
            catch (Exception)
            {
                SendTextResponse(stream, 500, "Internal Server Error", "500 Internal Server Error", isHead, false, null);
                return false;
            }
        }

        private static void AppendConnectionHeaders(StringBuilder header, bool keepAlive)
        {
            if (keepAlive)
            {
                header.Append("Connection: keep-alive\r\n");
                header.Append("Keep-Alive: timeout=").Append(KeepAliveTimeoutSeconds).Append(", max=").Append(KeepAliveMaxRequests).Append("\r\n");
            }
            else
            {
                header.Append("Connection: close\r\n");
            }
        }

        private static void SendTextResponse(NetworkStream stream, int statusCode, string reasonPhrase, string body, bool isHead, bool keepAlive, string extraHeaders)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(reasonPhrase).Append("\r\n");
            header.Append("Content-Type: text/plain; charset=utf-8\r\n");
            header.Append("Content-Length: ").Append(bodyBytes.Length).Append("\r\n");
            header.Append("Cache-Control: no-cache\r\n");
            if (!string.IsNullOrEmpty(extraHeaders))
            {
                header.Append(extraHeaders);
            }
            AppendConnectionHeaders(header, keepAlive);
            header.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (!isHead && bodyBytes.Length > 0)
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }
            stream.Flush();
        }

        private static void TryOpenBrowser(string url)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(url);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Could not auto-open browser: " + ex.Message);
                Console.WriteLine("       Open " + url + " manually in Chrome or Edge.");
            }
        }

        private static void TrySetConsoleTitle(string title)
        {
            try
            {
                Console.Title = title;
            }
            catch
            {
                // Ignore when stdout is redirected / non-interactive.
            }
        }

        private static void PrintBanner(string url, int preferredPort, int boundPort, string rootDir, bool noBrowser)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("  RAKDA Ltd. (Group A-10) - 2045 Circular Urban Digital Twin");
            Console.WriteLine("================================================================");
            Console.WriteLine("  Status   : Running (standalone built-in HTTP server)");
            Console.WriteLine("  URL      : " + url);
            if (boundPort != preferredPort)
            {
                Console.WriteLine("  Note     : Port " + preferredPort + " was busy; automatically switched to " + boundPort);
            }
            Console.WriteLine("  Root Dir : " + rootDir);
            Console.WriteLine("  Browser  : " + (noBrowser ? "Disabled (--no-browser)" : "Opening default browser..."));
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("  Keep this window open while using the 3D simulation.");
            Console.WriteLine("  Press Ctrl+C or close this window to stop the server.");
            Console.WriteLine("================================================================");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: RAKDA_3D_Sim.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --port <n>, -p <n>   Preferred local port (default: 8170 or PORT env var)");
            Console.WriteLine("  --no-browser         Do not automatically open the default web browser");
            Console.WriteLine("  --help, -h           Show this help message");
        }
    }
}
