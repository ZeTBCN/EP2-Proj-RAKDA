using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace RakdaSim
{
    public sealed unsafe class GpuMesh : IDisposable
    {
        internal uint Vao;
        internal uint Vbo;
        internal uint Ebo;
        internal int IndexCount;
        internal int TriangleCount;
        private bool _disposed;

        internal GpuMesh(uint vao, uint vbo, uint ebo, int indexCount)
        {
            Vao = vao;
            Vbo = vbo;
            Ebo = ebo;
            IndexCount = indexCount;
            TriangleCount = indexCount / 3;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            uint v = Vao;
            uint b = Vbo;
            uint e = Ebo;
            if (v != 0 && GlNative.glDeleteVertexArrays != null) GlNative.glDeleteVertexArrays(1, &v);
            if (b != 0 && GlNative.glDeleteBuffers != null) GlNative.glDeleteBuffers(1, &b);
            if (e != 0 && GlNative.glDeleteBuffers != null) GlNative.glDeleteBuffers(1, &e);
            Vao = 0;
            Vbo = 0;
            Ebo = 0;
        }
    }

    public sealed unsafe class GpuInstanceBatch : IDisposable
    {
        internal uint Vao;
        internal uint InstanceVbo;
        internal GpuMesh Mesh;
        internal int MaxInstances;
        internal int ActiveCount;
        private readonly float[] _staging;
        private bool _disposed;

        internal GpuInstanceBatch(GpuMesh mesh, int maxInstances)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (maxInstances < 1) maxInstances = 1;
            Mesh = mesh;
            MaxInstances = maxInstances;
            ActiveCount = 0;
            _staging = new float[maxInstances * 20];

            uint vao = 0;
            uint ivbo = 0;
            GlNative.glGenVertexArrays(1, &vao);
            GlNative.glGenBuffers(1, &ivbo);
            Vao = vao;
            InstanceVbo = ivbo;

            GlNative.glBindVertexArray(Vao);

            // Bind base mesh vertex & index buffers
            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, mesh.Vbo);
            GlNative.glBindBuffer(GlNative.GL_ELEMENT_ARRAY_BUFFER, mesh.Ebo);

            GlNative.glEnableVertexAttribArray(0);
            GlNative.glVertexAttribPointer(0, 3, GlNative.GL_FLOAT, 0, 32, new IntPtr(0));
            GlNative.glEnableVertexAttribArray(1);
            GlNative.glVertexAttribPointer(1, 3, GlNative.GL_FLOAT, 0, 32, new IntPtr(12));
            GlNative.glEnableVertexAttribArray(2);
            GlNative.glVertexAttribPointer(2, 2, GlNative.GL_FLOAT, 0, 32, new IntPtr(24));

            // Allocate dynamic per-instance buffer (16 floats Mat4 + 4 floats ColorTint = 80 bytes)
            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, InstanceVbo);
            GlNative.glBufferData(GlNative.GL_ARRAY_BUFFER, new IntPtr(maxInstances * 80), IntPtr.Zero, GlNative.GL_DYNAMIC_DRAW);

            for (uint col = 0; col < 4; col++)
            {
                uint loc = 3u + col;
                GlNative.glEnableVertexAttribArray(loc);
                GlNative.glVertexAttribPointer(loc, 4, GlNative.GL_FLOAT, 0, 80, new IntPtr((int)(col * 16)));
                GlNative.glVertexAttribDivisor(loc, 1);
            }

            GlNative.glEnableVertexAttribArray(7);
            GlNative.glVertexAttribPointer(7, 4, GlNative.GL_FLOAT, 0, 80, new IntPtr(64));
            GlNative.glVertexAttribDivisor(7, 1);

            GlNative.glBindVertexArray(0);
        }

        public void UpdateInstances(InstanceItem[] items, int count)
        {
            if (_disposed || items == null)
            {
                ActiveCount = 0;
                return;
            }
            if (count < 0) count = 0;
            if (count > items.Length) count = items.Length;
            if (count > MaxInstances) count = MaxInstances;
            ActiveCount = count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                int o = i * 20;
                float[] m = items[i].Transform.M;
                if (m != null && m.Length >= 16)
                {
                    Buffer.BlockCopy(m, 0, _staging, o * 4, 64);
                }
                else
                {
                    // Identity fallback
                    for (int k = 0; k < 16; k++) _staging[o + k] = 0f;
                    _staging[o] = 1f;
                    _staging[o + 5] = 1f;
                    _staging[o + 10] = 1f;
                    _staging[o + 15] = 1f;
                }

                Vec4 tint = items[i].ColorTint;
                if (tint.X == 0f && tint.Y == 0f && tint.Z == 0f && tint.W == 0f)
                {
                    _staging[o + 16] = 1f;
                    _staging[o + 17] = 1f;
                    _staging[o + 18] = 1f;
                    _staging[o + 19] = 0f;
                }
                else
                {
                    _staging[o + 16] = tint.X;
                    _staging[o + 17] = tint.Y;
                    _staging[o + 18] = tint.Z;
                    _staging[o + 19] = tint.W;
                }
            }

            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, InstanceVbo);
            fixed (float* ptr = _staging)
            {
                GlNative.glBufferSubData(GlNative.GL_ARRAY_BUFFER, IntPtr.Zero, new IntPtr(count * 80), new IntPtr(ptr));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            uint v = Vao;
            uint b = InstanceVbo;
            if (v != 0 && GlNative.glDeleteVertexArrays != null) GlNative.glDeleteVertexArrays(1, &v);
            if (b != 0 && GlNative.glDeleteBuffers != null) GlNative.glDeleteBuffers(1, &b);
            Vao = 0;
            InstanceVbo = 0;
        }
    }

    public sealed unsafe class GpuLineBatch : IDisposable
    {
        internal uint Vao;
        internal uint Vbo;
        internal int MaxVertices;
        internal int ActiveVertexCount;
        private bool _disposed;

        internal GpuLineBatch(int maxVertices)
        {
            if (maxVertices < 2) maxVertices = 2;
            MaxVertices = maxVertices;
            ActiveVertexCount = 0;

            uint vao = 0;
            uint vbo = 0;
            GlNative.glGenVertexArrays(1, &vao);
            GlNative.glGenBuffers(1, &vbo);
            Vao = vao;
            Vbo = vbo;

            GlNative.glBindVertexArray(Vao);
            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, Vbo);
            GlNative.glBufferData(GlNative.GL_ARRAY_BUFFER, new IntPtr(maxVertices * 28), IntPtr.Zero, GlNative.GL_DYNAMIC_DRAW);

            GlNative.glEnableVertexAttribArray(0);
            GlNative.glVertexAttribPointer(0, 3, GlNative.GL_FLOAT, 0, 28, new IntPtr(0));
            GlNative.glEnableVertexAttribArray(1);
            GlNative.glVertexAttribPointer(1, 4, GlNative.GL_FLOAT, 0, 28, new IntPtr(12));

            GlNative.glBindVertexArray(0);
        }

        public void UpdateLines(float[] posColor7FloatsPerVert, int vertexCount)
        {
            if (_disposed || posColor7FloatsPerVert == null)
            {
                ActiveVertexCount = 0;
                return;
            }
            int maxFromArr = posColor7FloatsPerVert.Length / 7;
            if (vertexCount < 0) vertexCount = 0;
            if (vertexCount > maxFromArr) vertexCount = maxFromArr;
            if (vertexCount > MaxVertices) vertexCount = MaxVertices;
            ActiveVertexCount = vertexCount;
            if (vertexCount == 0) return;

            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, Vbo);
            fixed (float* ptr = posColor7FloatsPerVert)
            {
                GlNative.glBufferSubData(GlNative.GL_ARRAY_BUFFER, IntPtr.Zero, new IntPtr(vertexCount * 28), new IntPtr(ptr));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            uint v = Vao;
            uint b = Vbo;
            if (v != 0 && GlNative.glDeleteVertexArrays != null) GlNative.glDeleteVertexArrays(1, &v);
            if (b != 0 && GlNative.glDeleteBuffers != null) GlNative.glDeleteBuffers(1, &b);
            Vao = 0;
            Vbo = 0;
        }
    }

    /// <summary>
    /// Native Hardware OpenGL 3.3 Core Renderer with 2048x2048 on-demand PCF shadows,
    /// HDR RGBA16F MSAA scene buffer, half-resolution selective bloom, ACES Filmic tone mapping,
    /// procedural atmospheric sky dome, 11 procedural PBR material patterns, geological cutaway plinth,
    /// camera orbit/pan/zoom/fly-to, ray-AABB picking, and offscreen FBO screenshot readback.
    /// Strictly C# 5 compatible.
    /// </summary>
    public sealed unsafe class Renderer : IDisposable
    {
        private struct MeshDrawCmd
        {
            public GpuMesh Mesh;
            public Mat4 Model;
            public MaterialDef Mat;
        }

        private struct InstDrawCmd
        {
            public GpuInstanceBatch Batch;
            public MaterialDef Mat;
        }

        private struct LineDrawCmd
        {
            public GpuLineBatch Batch;
            public bool Additive;
        }

        private const int ShadowMapSize = 2048;

        private readonly IntPtr _hwnd;
        private IntPtr _hdc;
        private IntPtr _hglrc;
        private int _width;
        private int _height;
        private bool _disposed;

        // Metrics
        private readonly string _gpuName;
        private float _lastFps;
        private float _lastFrameMs;
        private int _lastDrawCalls;
        private int _lastTriangles;
        private readonly Stopwatch _frameWatch = new Stopwatch();
        private double _fpsAccumSec;
        private int _fpsAccumFrames;

        public string GpuName { get { return _gpuName; } }
        public float LastFps { get { return _lastFps; } }
        public float LastFrameMs { get { return _lastFrameMs; } }
        public int LastDrawCalls { get { return _lastDrawCalls; } }
        public int LastTriangles { get { return _lastTriangles; } }

        // GL Programs & Uniform Locations
        private uint _emptyVao;
        private uint _pbrProg;
        private uint _pbrInstProg;
        private uint _shadowProg;
        private uint _shadowInstProg;
        private uint _skyProg;
        private uint _lineProg;
        private uint _brightProg;
        private uint _blurProg;
        private uint _compositeProg;

        // Cached Uniform Locations for PBR (0 = single, 1 = instanced)
        private sealed class PbrLocs
        {
            public int uViewProj, uModel, uLightSpace, uCameraPos;
            public int uSunDir, uSunColor, uSunIntensity;
            public int uHemiSky, uHemiGround, uHemiIntensity;
            public int uFogColor, uFogDensity, uTime, uNightWindowGlow, uApplyTonemap;
            public int uColor, uEmissive, uEmissiveIntensity, uRoughness, uMetallic, uAlpha;
            public int uPattern, uPatternScale, uShadowMap;
        }

        private PbrLocs _pbrLoc;
        private PbrLocs _pbrInstLoc;
        private int _shLightSpace, _shModel;
        private int _shInstLightSpace;

        private int _skyInvViewProjLoc, _skySunDirLoc, _skyZenithLoc, _skyMidLoc, _skyHorizLoc, _skyNadirLoc;
        private int _skySunGlowLoc, _skySunIntLoc, _skyHorizGlowLoc, _skyStarVisLoc, _skyCloudLoc, _skyTimeLoc, _skyTonemapLoc;

        private int _lineViewProj, _lineTonemap;
        private int _brightScene, _brightThreshold;
        private int _blurImage, _blurHoriz;
        private int _compScene, _compBloom, _compBloomStrength, _compExposure;

        // Framebuffers & Textures
        private bool _hasHdrPipeline;
        private bool _hasMsaaFbo;
        private uint _shadowFbo;
        private uint _shadowTex;

        private uint _msaaFbo;
        private uint _msaaColorRbo;
        private uint _msaaDepthRbo;

        private uint _sceneFbo;
        private uint _sceneColorTex;
        private uint _sceneDepthRbo;

        private uint _bloomFboA;
        private uint _bloomTexA;
        private uint _bloomFboB;
        private uint _bloomTexB;
        private int _bloomW;
        private int _bloomH;

        private uint _readbackFbo;
        private uint _readbackTex;
        private int _readbackW;
        private int _readbackH;

        // Camera state (spherical orbit with smooth damping + fly-to)
        private Vec3 _target;
        private float _yaw;
        private float _pitch;
        private float _distance;

        private Vec3 _curTarget;
        private float _curYaw;
        private float _curPitch;
        private float _curDistance;

        private Vec3 _cameraPos;
        private Mat4 _viewMat;
        private Mat4 _projMat;
        private Mat4 _viewProjMat;
        private Mat4 _invViewProjMat;

        private bool _flyActive;
        private Vec3 _flyStartPos;
        private Vec3 _flyEndPos;
        private Vec3 _flyStartTarget;
        private Vec3 _flyEndTarget;
        private float _flyElapsed;
        private float _flyDuration;

        // Lighting & Atmosphere state
        private bool _shadowsDirty = true;
        private Vec3 _sunPos = new Vec3(60f, 95f, 45f);
        private Vec3 _lastShadowSunPos = new Vec3(1e9f, 1e9f, 1e9f);
        private Vec3 _sunDir = new Vec3(0.45f, 0.78f, 0.42f).Normalized();
        private Vec3 _sunColor = new Vec3(1f, 0.96f, 0.90f);
        private float _sunIntensity = 1.9f;
        private Vec3 _hemiSky = new Vec3(0.62f, 0.77f, 1.0f);
        private Vec3 _hemiGround = new Vec3(0.11f, 0.16f, 0.25f);
        private float _hemiIntensity = 0.76f;
        private Vec3 _fogColor = new Vec3(0.14f, 0.25f, 0.39f);
        private float _fogDensity = 0.0019f;
        private float _nightWindowGlow = 0f;

        private Vec3 _skyZenith = Vec3.FromHex(0x0e2545);
        private Vec3 _skyMid = Vec3.FromHex(0x1f4875);
        private Vec3 _skyHoriz = Vec3.FromHex(0x527da6);
        private Vec3 _skyNadir = Vec3.FromHex(0x1c3552);
        private Vec3 _skySunGlow = Vec3.FromHex(0xfff5e6);
        private float _skySunIntensity = 0.85f;
        private float _skyHorizonGlow = 0.25f;
        private float _skyStarVis = 0f;
        private float _skyCloudCover = 0.35f;
        private Mat4 _lightSpaceMat = Mat4.Identity();

        // Ground X-Ray opacity state
        private bool _hasInitGroundOpacity;
        private float _curGroundOpacity = 0.78f;
        private float _targetGroundOpacity = 0.78f;
        private float _elapsedTime;

        // Built-in Geological Plinth & Selection Highlight resources
        private GpuMesh _bedrockMesh;
        private GpuMesh _baseFlangeMesh;
        private GpuMesh _bevelRimMesh;
        private GpuMesh _rimInlayMesh;
        private GpuMesh _rimTicksMesh;
        private GpuMesh _datumRingsMesh;
        private GpuMesh _strataSkirtMesh;
        private GpuMesh _l4BandMesh;
        private GpuMesh _groundDiskMesh;
        private GpuLineBatch _cavernGridLines;

        private readonly MaterialDef _bedrockMat = new MaterialDef();
        private readonly MaterialDef _metalTrimMat = new MaterialDef();
        private readonly MaterialDef _rimInlayMat = new MaterialDef();
        private readonly MaterialDef _rimTicksMat = new MaterialDef();
        private readonly MaterialDef _datumRingsMat = new MaterialDef();
        private readonly MaterialDef _strataMat = new MaterialDef();
        private readonly MaterialDef _l4BandMat = new MaterialDef();
        private readonly MaterialDef _groundMat = new MaterialDef();

        // Selection Highlight
        private PickTarget _selectedTarget;
        private float _selectionPulseTime;
        private GpuLineBatch _selectionLines;
        private readonly float[] _selectionVerts = new float[256 * 7];

        // Per-frame draw queues (zero allocations)
        private readonly List<MeshDrawCmd> _opaqueMeshes = new List<MeshDrawCmd>(256);
        private readonly List<MeshDrawCmd> _transMeshes = new List<MeshDrawCmd>(128);
        private readonly List<InstDrawCmd> _opaqueInst = new List<InstDrawCmd>(64);
        private readonly List<InstDrawCmd> _transInst = new List<InstDrawCmd>(32);
        private readonly List<LineDrawCmd> _lineCmds = new List<LineDrawCmd>(32);

        private int _frameDrawCalls;
        private int _frameTriangles;

        public Renderer(IntPtr hwnd, int width, int height)
        {
            _hwnd = hwnd;
            _width = width > 1 ? width : 1;
            _height = height > 1 ? height : 1;

            GlNative.CreateCoreContext(hwnd, out _hdc, out _hglrc);
            _gpuName = GlNative.GetString(GlNative.GL_RENDERER);

            GlNative.glEnable(GlNative.GL_DEPTH_TEST);
            GlNative.glDepthFunc(GlNative.GL_LEQUAL);
            GlNative.glEnable(GlNative.GL_CULL_FACE);
            GlNative.glCullFace(GlNative.GL_BACK);
            GlNative.glEnable(GlNative.GL_MULTISAMPLE);

            uint emptyVao = 0;
            GlNative.glGenVertexArrays(1, &emptyVao);
            _emptyVao = emptyVao;

            CompileAllShaders();
            CreateShadowResources();
            CreateHdrResources(_width, _height);
            BuildGeologicalPlinth();
            BuildSelectionHighlight();

            // Initialize camera to Preset 0 (Overview)
            CameraPreset p0 = WorldLayout.CameraPresets[0];
            SetCameraFromPosTarget(p0.Pos, p0.Target, true);
            SetSunFromState(new SimState());
            SetGroundOpacity(0.78f);

            _frameWatch.Start();
        }

        private void EnsureCurrent()
        {
            if (_hdc != IntPtr.Zero && _hglrc != IntPtr.Zero)
            {
                GlNative.wglMakeCurrent(_hdc, _hglrc);
            }
        }

        public GpuMesh UploadMesh(MeshBuilder mb)
        {
            if (mb == null) throw new ArgumentNullException("mb");
            float[] verts;
            uint[] indices;
            mb.BuildData(out verts, out indices);
            return UploadMesh(verts, indices);
        }

        public GpuMesh UploadMesh(float[] verts, uint[] indices)
        {
            EnsureCurrent();
            uint vao = 0, vbo = 0, ebo = 0;
            GlNative.glGenVertexArrays(1, &vao);
            GlNative.glGenBuffers(1, &vbo);
            GlNative.glGenBuffers(1, &ebo);

            GlNative.glBindVertexArray(vao);

            GlNative.glBindBuffer(GlNative.GL_ARRAY_BUFFER, vbo);
            if (verts != null && verts.Length > 0)
            {
                fixed (float* vp = verts)
                {
                    GlNative.glBufferData(GlNative.GL_ARRAY_BUFFER, new IntPtr(verts.Length * 4), new IntPtr(vp), GlNative.GL_STATIC_DRAW);
                }
            }
            else
            {
                GlNative.glBufferData(GlNative.GL_ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GlNative.GL_STATIC_DRAW);
            }

            GlNative.glBindBuffer(GlNative.GL_ELEMENT_ARRAY_BUFFER, ebo);
            int idxCount = indices != null ? indices.Length : 0;
            if (idxCount > 0)
            {
                fixed (uint* ip = indices)
                {
                    GlNative.glBufferData(GlNative.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(idxCount * 4), new IntPtr(ip), GlNative.GL_STATIC_DRAW);
                }
            }
            else
            {
                GlNative.glBufferData(GlNative.GL_ELEMENT_ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GlNative.GL_STATIC_DRAW);
            }

            GlNative.glEnableVertexAttribArray(0);
            GlNative.glVertexAttribPointer(0, 3, GlNative.GL_FLOAT, 0, 32, new IntPtr(0));
            GlNative.glEnableVertexAttribArray(1);
            GlNative.glVertexAttribPointer(1, 3, GlNative.GL_FLOAT, 0, 32, new IntPtr(12));
            GlNative.glEnableVertexAttribArray(2);
            GlNative.glVertexAttribPointer(2, 2, GlNative.GL_FLOAT, 0, 32, new IntPtr(24));

            GlNative.glBindVertexArray(0);
            return new GpuMesh(vao, vbo, ebo, idxCount);
        }

        public GpuInstanceBatch CreateInstanceBatch(GpuMesh mesh, int maxInstances)
        {
            EnsureCurrent();
            return new GpuInstanceBatch(mesh, maxInstances);
        }

        public GpuLineBatch CreateLineBatch(int maxVertices)
        {
            EnsureCurrent();
            return new GpuLineBatch(maxVertices);
        }

        public void Resize(int w, int h)
        {
            if (_disposed) return;
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            if (w == _width && h == _height) return;
            _width = w;
            _height = h;
            EnsureCurrent();
            DestroyHdrResources();
            CreateHdrResources(_width, _height);
            UpdateCameraMatrices();
        }

        public void SetGroundOpacity(float opacity01)
        {
            float clamped = Clamp(opacity01, 0.02f, 1.0f);
            if (Math.Abs(_targetGroundOpacity - clamped) > 0.005f)
            {
                MarkShadowsDirty();
            }
            _targetGroundOpacity = clamped;
            if (!_hasInitGroundOpacity)
            {
                _hasInitGroundOpacity = true;
                _curGroundOpacity = clamped;
                ApplyGroundOpacity(_curGroundOpacity);
            }
        }

        private void ApplyGroundOpacity(float opacity01)
        {
            _groundMat.Alpha = opacity01;
            _strataMat.Alpha = 0.16f + opacity01 * 0.64f;
            _l4BandMat.Alpha = 0.35f + (1f - opacity01) * 0.52f;
        }

        public void MarkShadowsDirty()
        {
            _shadowsDirty = true;
        }

        public void SetSunFromState(SimState state)
        {
            float tod = state != null ? (float)state.TimeOfDay : 13.5f;
            float rain = state != null ? Clamp((float)state.RainfallMultiplier, 0f, 2.5f) : 1.0f;
            float tempC = state != null ? (float)state.AmbientTempC : 10.0f;

            float dayPhase = Clamp((tod - 5.5f) / 13.0f, 0f, 1f);
            float rawElev = (tod >= 5.5f && tod <= 18.5f) ? (float)Math.Sin(dayPhase * Math.PI) : 0f;
            float daylight = (float)Math.Pow(rawElev, 0.85);

            float srDiff = (tod - 6.6f) / 1.35f;
            float ssDiff = (tod - 17.4f) / 1.45f;
            float sunrisePeak = (float)Math.Exp(-(srDiff * srDiff));
            float sunsetPeak = (float)Math.Exp(-(ssDiff * ssDiff));
            float goldenHour = Math.Min(1f, Math.Max(sunrisePeak, sunsetPeak));

            float sunAngle = ((tod - 6.0f) / 12.0f) * (float)Math.PI;
            float sx = (float)Math.Cos(sunAngle) * 98f;
            float sy = (float)Math.Sin(sunAngle) * 112f;
            float sz = 48f;

            if (rawElev > 0.02f)
            {
                _sunPos = new Vec3(sx, Math.Max(11f, sy), sz);
                _sunDir = new Vec3(sx, Math.Max(3f, sy), sz).Normalized();
            }
            else
            {
                _sunPos = new Vec3(-54f, 78f, -42f);
                _sunDir = new Vec3(sx, Math.Max(-14f, sy), sz).Normalized();
            }

            if ((_sunPos - _lastShadowSunPos).LengthSq() > 0.25f)
            {
                _lastShadowSunPos = _sunPos;
                Mat4 lightView = Mat4.LookAt(_sunPos, new Vec3(0f, -2f, 0f), Vec3.UnitY);
                Mat4 lightProj = Mat4.Ortho(-82f, 82f, -82f, 82f, 10f, 250f);
                _lightSpaceMat = Mat4.Multiply(lightProj, lightView);
                MarkShadowsDirty();
            }

            float cloudCover = Clamp(rain * 0.46f + (tempC < 2f ? 0.14f : 0f), 0.05f, 1.0f);
            float rainDim = Math.Max(0.52f, 1.0f - rain * 0.22f);
            float tempWarmth = Clamp((tempC + 5f) / 30f, 0f, 1f);

            Vec3 sunWarm = Vec3.FromHex(0xff9f5a);
            Vec3 sunMid = Vec3.FromHex(0xfff5e6);
            Vec3 moonCool = Vec3.FromHex(0x88b4f0);

            if (daylight > 0.06f)
            {
                _sunIntensity = (0.55f + daylight * 1.65f) * rainDim;
                if (daylight > 0.55f)
                {
                    _sunColor = sunMid;
                }
                else
                {
                    _sunColor = Vec3.Lerp(sunWarm, sunMid, Math.Min(1f, daylight / 0.55f));
                }
            }
            else
            {
                _sunIntensity = 0.44f * (0.75f + 0.25f * rainDim);
                _sunColor = moonCool;
            }

            Vec3 hemiSkyDay = Vec3.FromHex(0x9ec5ff);
            Vec3 hemiSkyGolden = Vec3.FromHex(0xf4b686);
            Vec3 hemiSkyNight = Vec3.FromHex(0x3a588a);
            Vec3 hemiGroundDay = Vec3.FromHex(0x1b2940);
            Vec3 hemiGroundNight = Vec3.FromHex(0x0c1526);

            _hemiIntensity = (0.42f + daylight * 0.52f) * (0.74f + 0.26f * rainDim);
            _hemiSky = Vec3.Lerp(hemiSkyNight, hemiSkyDay, daylight);
            if (goldenHour > 0.05f)
            {
                _hemiSky = Vec3.Lerp(_hemiSky, hemiSkyGolden, goldenHour * 0.42f * (1f - cloudCover * 0.5f));
            }
            _hemiGround = Vec3.Lerp(hemiGroundNight, hemiGroundDay, daylight);

            // Dusk/night warm architectural window glow
            _nightWindowGlow = Clamp(1.0f - daylight * 2.2f, 0f, 1f);

            // Atmospheric sky dome colors
            Vec3 zenithNight = Vec3.FromHex(0x040914);
            Vec3 zenithTwilight = Vec3.FromHex(0x0d1b38);
            Vec3 zenithCold = Vec3.FromHex(0x0b1f3d);
            Vec3 zenithWarm = Vec3.FromHex(0x123059);
            Vec3 zenithDay = Vec3.Lerp(Vec3.Lerp(zenithCold, zenithWarm, tempWarmth), Vec3.FromHex(0x0e2545), 0.35f);

            Vec3 midNight = Vec3.FromHex(0x081326);
            Vec3 midTwilight = Vec3.FromHex(0x233052);
            Vec3 midDay = Vec3.FromHex(0x1f4875);

            Vec3 horizNight = Vec3.FromHex(0x0f1e36);
            Vec3 horizGolden = Vec3.FromHex(0xd97b43);
            Vec3 horizDay = Vec3.FromHex(0x527da6);

            Vec3 nadirNight = Vec3.FromHex(0x050a14);
            Vec3 nadirTwilight = Vec3.FromHex(0x101f36);
            Vec3 nadirDay = Vec3.FromHex(0x1c3552);

            Vec3 zCol = zenithNight;
            Vec3 mCol = midNight;
            Vec3 hCol = horizNight;
            Vec3 nCol = nadirNight;

            if (daylight > 0.02f)
            {
                if (daylight < 0.35f)
                {
                    float k = daylight / 0.35f;
                    zCol = Vec3.Lerp(zenithNight, zenithTwilight, k);
                    mCol = Vec3.Lerp(midNight, midTwilight, k);
                    hCol = Vec3.Lerp(horizNight, horizDay, k * 0.6f);
                    nCol = Vec3.Lerp(nadirNight, nadirTwilight, k);
                }
                else
                {
                    float k = (daylight - 0.35f) / 0.65f;
                    zCol = Vec3.Lerp(zenithTwilight, zenithDay, k);
                    mCol = Vec3.Lerp(midTwilight, midDay, k);
                    hCol = Vec3.Lerp(horizDay * 0.75f, horizDay, k);
                    nCol = Vec3.Lerp(nadirTwilight, nadirDay, k);
                }
            }

            if (goldenHour > 0.02f)
            {
                float gw = goldenHour * (1f - cloudCover * 0.55f);
                hCol = Vec3.Lerp(hCol, horizGolden, Math.Min(0.82f, gw * 0.85f));
                mCol = Vec3.Lerp(mCol, sunWarm, Math.Min(0.28f, gw * 0.30f));
                nCol = Vec3.Lerp(nCol, nadirTwilight, Math.Min(0.35f, gw * 0.35f));
            }

            float overcastBlend = Clamp((rain - 0.45f) * 0.48f, 0f, 0.78f);
            if (overcastBlend > 0.01f)
            {
                float darkScale = 0.38f + daylight * 0.62f;
                zCol = Vec3.Lerp(zCol, Vec3.FromHex(0x182332) * darkScale, overcastBlend);
                mCol = Vec3.Lerp(mCol, Vec3.FromHex(0x29374a) * darkScale, overcastBlend);
                hCol = Vec3.Lerp(hCol, Vec3.FromHex(0x3e4f63) * darkScale, overcastBlend);
                nCol = Vec3.Lerp(nCol, Vec3.FromHex(0x1c2838) * darkScale, overcastBlend * 0.7f);
            }

            _skyZenith = zCol;
            _skyMid = mCol;
            _skyHoriz = hCol;
            _skyNadir = nCol;
            _skySunGlow = Vec3.Lerp(sunMid, sunWarm, Math.Min(1f, goldenHour * 0.9f + (1f - daylight) * 0.4f));
            _skySunIntensity = daylight;
            _skyHorizonGlow = Math.Min(1f, goldenHour * 0.95f + daylight * 0.25f);
            _skyStarVis = Math.Max(0f, 1.0f - daylight * 3.2f);
            _skyCloudCover = cloudCover;

            _fogColor = Vec3.Lerp(hCol, mCol, 0.55f);
            _fogDensity = 0.0017f + cloudCover * 0.0010f;
        }

        private void SetCameraFromPosTarget(Vec3 pos, Vec3 target, bool immediate)
        {
            Vec3 offset = pos - target;
            float dist = offset.Length();
            if (dist < 1f) dist = 1f;
            float pitch = (float)Math.Asin(Clamp(offset.Y / dist, -0.995f, 0.995f));
            float yaw = (float)Math.Atan2(offset.X, offset.Z);

            _target = target;
            _distance = Clamp(dist, 10f, 260f);
            _pitch = Clamp(pitch, -0.45f, 1.52f);
            _yaw = yaw;

            if (immediate)
            {
                _curTarget = _target;
                _curDistance = _distance;
                _curPitch = _pitch;
                _curYaw = _yaw;
                _flyActive = false;
                UpdateCameraMatrices();
            }
        }

        public void Orbit(float dx, float dy)
        {
            _flyActive = false;
            _yaw -= dx * 0.0065f;
            _pitch = Clamp(_pitch + dy * 0.0055f, -0.42f, 1.50f);
        }

        public void Pan(float dx, float dy)
        {
            _flyActive = false;
            Vec3 forward = (_curTarget - _cameraPos).Normalized();
            Vec3 right = Vec3.Cross(forward, Vec3.UnitY).Normalized();
            Vec3 up = Vec3.Cross(right, forward).Normalized();
            float panSpeed = _curDistance * 0.0016f;
            _target = _target - right * (dx * panSpeed) + up * (dy * panSpeed);
        }

        public void Zoom(float delta)
        {
            _flyActive = false;
            float factor = (float)Math.Pow(0.9992, delta);
            _distance = Clamp(_distance * factor, 10f, 260f);
        }

        public void FlyToPreset(int index)
        {
            if (index < 0 || index >= WorldLayout.CameraPresets.Length) return;
            CameraPreset p = WorldLayout.CameraPresets[index];
            _flyStartPos = _cameraPos;
            _flyEndPos = p.Pos;
            _flyStartTarget = _curTarget;
            _flyEndTarget = p.Target;
            _flyElapsed = 0f;
            _flyDuration = 0.72f;
            _flyActive = true;
            SetCameraFromPosTarget(p.Pos, p.Target, false);
        }

        public void SetSelectedTarget(PickTarget target)
        {
            _selectedTarget = target;
            _selectionPulseTime = 0f;
        }

        public string Pick(int mouseX, int mouseY, IList<PickTarget> targets, out PickTarget hitTarget)
        {
            hitTarget = null;
            if (targets == null || targets.Count == 0 || _width <= 0 || _height <= 0) return null;

            float ndcX = (2.0f * mouseX) / _width - 1.0f;
            float ndcY = 1.0f - (2.0f * mouseY) / _height;

            Vec3 pNear = Mat4.TransformPoint(_invViewProjMat, new Vec3(ndcX, ndcY, -1.0f));
            Vec3 pFar = Mat4.TransformPoint(_invViewProjMat, new Vec3(ndcX, ndcY, 1.0f));
            Vec3 rayDir = (pFar - pNear).Normalized();
            Vec3 rayOrig = pNear;

            float bestT = float.MaxValue;
            PickTarget best = null;

            for (int i = 0; i < targets.Count; i++)
            {
                PickTarget pt = targets[i];
                if (pt == null || string.IsNullOrEmpty(pt.InspectKey)) continue;

                float hitT;
                bool hit = false;
                Vec3 size = pt.Bounds.Max - pt.Bounds.Min;
                if (size.X > 1e-4f || size.Y > 1e-4f || size.Z > 1e-4f)
                {
                    hit = pt.Bounds.IntersectsRay(rayOrig, rayDir, out hitT);
                }
                else
                {
                    hit = IntersectRaySphere(rayOrig, rayDir, pt.Center, Math.Max(0.8f, pt.Radius), out hitT);
                }

                if (hit && hitT >= 0f && hitT < bestT)
                {
                    bestT = hitT;
                    best = pt;
                }
            }

            hitTarget = best;
            return best != null ? best.InspectKey : null;
        }

        private static bool IntersectRaySphere(Vec3 orig, Vec3 dir, Vec3 center, float radius, out float t)
        {
            Vec3 oc = orig - center;
            float b = Vec3.Dot(oc, dir);
            float c = Vec3.Dot(oc, oc) - radius * radius;
            float disc = b * b - c;
            if (disc < 0f)
            {
                t = 0f;
                return false;
            }
            float sq = (float)Math.Sqrt(disc);
            float t0 = -b - sq;
            if (t0 >= 0f) { t = t0; return true; }
            float t1 = -b + sq;
            if (t1 >= 0f) { t = t1; return true; }
            t = 0f;
            return false;
        }

        private void UpdateCameraMatrices()
        {
            float cp = (float)Math.Cos(_curPitch);
            float sp = (float)Math.Sin(_curPitch);
            float sy = (float)Math.Sin(_curYaw);
            float cy = (float)Math.Cos(_curYaw);

            _cameraPos = new Vec3(
                _curTarget.X + _curDistance * cp * sy,
                _curTarget.Y + _curDistance * sp,
                _curTarget.Z + _curDistance * cp * cy);

            float aspect = (float)_width / Math.Max(1, _height);
            _viewMat = Mat4.LookAt(_cameraPos, _curTarget, Vec3.UnitY);
            _projMat = Mat4.Perspective(46f * (float)(Math.PI / 180.0), aspect, 0.5f, 1100f);
            _viewProjMat = Mat4.Multiply(_projMat, _viewMat);
            Mat4.TryInvert(_viewProjMat, out _invViewProjMat);
        }

        public void BeginFrame(float dt)
        {
            if (_disposed) return;
            EnsureCurrent();

            float safeDt = Clamp(dt, 0f, 1.0f);
            _elapsedTime += Math.Min(0.05f, safeDt);
            _selectionPulseTime += Math.Min(0.05f, safeDt);

            // Smooth camera fly-to or damped spherical orbit
            if (_flyActive)
            {
                _flyElapsed += safeDt;
                float t = Math.Min(1f, _flyElapsed / _flyDuration);
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                Vec3 pos = Vec3.Lerp(_flyStartPos, _flyEndPos, ease);
                Vec3 tgt = Vec3.Lerp(_flyStartTarget, _flyEndTarget, ease);
                SetCameraFromPosTarget(pos, tgt, true);
                if (t >= 1f)
                {
                    _flyActive = false;
                }
                else
                {
                    // Restore final targets for next step
                    _flyActive = true;
                }
            }
            else
            {
                float damp = 1f - (float)Math.Exp(-14.0 * Math.Min(0.25f, safeDt));
                _curYaw += (_yaw - _curYaw) * damp;
                _curPitch += (_pitch - _curPitch) * damp;
                _curDistance += (_distance - _curDistance) * damp;
                _curTarget = Vec3.Lerp(_curTarget, _target, damp);
                UpdateCameraMatrices();
            }

            // Smooth X-Ray ground & geological strata opacity transition
            if (Math.Abs(_targetGroundOpacity - _curGroundOpacity) > 0.0008f)
            {
                float a = 1f - (float)Math.Exp(-10.5 * safeDt);
                _curGroundOpacity += (_targetGroundOpacity - _curGroundOpacity) * a;
                ApplyGroundOpacity(_curGroundOpacity);
            }
            else if (_curGroundOpacity != _targetGroundOpacity)
            {
                _curGroundOpacity = _targetGroundOpacity;
                ApplyGroundOpacity(_curGroundOpacity);
            }

            _opaqueMeshes.Clear();
            _transMeshes.Clear();
            _opaqueInst.Clear();
            _transInst.Clear();
            _lineCmds.Clear();
            _frameDrawCalls = 0;
            _frameTriangles = 0;

            // Enqueue built-in geological plinth & ground cutaway
            Mat4 id = Mat4.Identity();
            DrawMesh(_bedrockMesh, id, _bedrockMat);
            DrawMesh(_baseFlangeMesh, id, _metalTrimMat);
            DrawMesh(_bevelRimMesh, id, _metalTrimMat);
            DrawMesh(_rimInlayMesh, id, _rimInlayMat);
            DrawMesh(_rimTicksMesh, id, _rimTicksMat);
            DrawMesh(_datumRingsMesh, id, _datumRingsMat);
            DrawMesh(_strataSkirtMesh, id, _strataMat);
            DrawMesh(_l4BandMesh, id, _l4BandMat);
            DrawMesh(_groundDiskMesh, id, _groundMat);
            DrawLines(_cavernGridLines, false);

            // Enqueue animated holographic selection reticle if a subsystem is selected
            if (_selectedTarget != null)
            {
                UpdateSelectionReticleLines();
                DrawLines(_selectionLines, true);
            }
        }

        public void DrawMesh(GpuMesh mesh, Mat4 model, MaterialDef mat)
        {
            if (mesh == null || mesh.IndexCount <= 0 || mat == null) return;
            MeshDrawCmd cmd = new MeshDrawCmd();
            cmd.Mesh = mesh;
            cmd.Model = model;
            cmd.Mat = mat;
            if (mat.Alpha < 0.99f)
            {
                _transMeshes.Add(cmd);
            }
            else
            {
                _opaqueMeshes.Add(cmd);
            }
        }

        public void DrawInstanced(GpuInstanceBatch batch, MaterialDef mat)
        {
            if (batch == null || batch.ActiveCount <= 0 || batch.Mesh == null || mat == null) return;
            InstDrawCmd cmd = new InstDrawCmd();
            cmd.Batch = batch;
            cmd.Mat = mat;
            if (mat.Alpha < 0.99f)
            {
                _transInst.Add(cmd);
            }
            else
            {
                _opaqueInst.Add(cmd);
            }
        }

        public void DrawLines(GpuLineBatch lines, bool additive)
        {
            if (lines == null || lines.ActiveVertexCount < 2) return;
            LineDrawCmd cmd = new LineDrawCmd();
            cmd.Batch = lines;
            cmd.Additive = additive;
            _lineCmds.Add(cmd);
        }

        public void EndFrame()
        {
            if (_disposed) return;

            // 1. On-demand 2048x2048 directional shadow pass
            if (_shadowsDirty && _shadowFbo != 0)
            {
                RenderShadowPass();
                _shadowsDirty = false;
            }

            // 2. Bind HDR scene FBO (or default framebuffer fallback)
            if (_hasHdrPipeline)
            {
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _hasMsaaFbo ? _msaaFbo : _sceneFbo);
            }
            else
            {
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
            }

            GlNative.glViewport(0, 0, _width, _height);
            GlNative.glClearColor(_skyNadir.X, _skyNadir.Y, _skyNadir.Z, 1f);
            GlNative.glClear(GlNative.GL_COLOR_BUFFER_BIT | GlNative.GL_DEPTH_BUFFER_BIT);

            int inlineTonemap = _hasHdrPipeline ? 0 : 1;

            // 3. Procedural Atmospheric Sky Dome
            RenderSkyDome(inlineTonemap);

            // 4. Opaque 3D Geometry (Meshes + Instanced Batches)
            GlNative.glEnable(GlNative.GL_DEPTH_TEST);
            GlNative.glDepthMask(1);
            GlNative.glDisable(GlNative.GL_BLEND);

            RenderMeshList(_opaqueMeshes, inlineTonemap);
            RenderInstancedList(_opaqueInst, inlineTonemap);

            // 5. Transparent 3D Geometry (Geological Strata, X-Ray Ground, Glass Tanks, Water, Pipe Jackets)
            GlNative.glEnable(GlNative.GL_BLEND);
            GlNative.glBlendFunc(GlNative.GL_SRC_ALPHA, GlNative.GL_ONE_MINUS_SRC_ALPHA);
            GlNative.glDepthMask(0);

            RenderMeshList(_transMeshes, inlineTonemap);
            RenderInstancedList(_transInst, inlineTonemap);

            // 6. Glowing Lines & Beams (V2X Beams, Rain Streaks, Bedrock Grid, Selection Reticle)
            RenderLinesPass(inlineTonemap);

            GlNative.glDepthMask(1);
            GlNative.glDisable(GlNative.GL_BLEND);
            GlNative.glEnable(GlNative.GL_CULL_FACE);

            // 7. HDR Resolve + Half-Res Selective Bloom + ACES Tone Mapping to Screen
            if (_hasHdrPipeline)
            {
                RenderHdrBloomAndComposite(0);
            }

            GlNative.SwapBuffers(_hdc);

            // Frame timing & stats
            double sec = _frameWatch.Elapsed.TotalSeconds;
            _frameWatch.Restart();
            float ms = (float)(sec * 1000.0);
            _lastFrameMs = _lastFrameMs <= 0.001f ? ms : (_lastFrameMs * 0.85f + ms * 0.15f);

            _fpsAccumSec += sec;
            _fpsAccumFrames++;
            if (_fpsAccumSec >= 0.25)
            {
                _lastFps = (float)(_fpsAccumFrames / _fpsAccumSec);
                _fpsAccumSec = 0.0;
                _fpsAccumFrames = 0;
            }
            else if (_lastFps <= 0.1f && sec > 1e-5)
            {
                _lastFps = (float)(1.0 / sec);
            }

            _lastDrawCalls = _frameDrawCalls;
            _lastTriangles = _frameTriangles;
        }

        private void RenderShadowPass()
        {
            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _shadowFbo);
            GlNative.glViewport(0, 0, ShadowMapSize, ShadowMapSize);
            GlNative.glClear(GlNative.GL_DEPTH_BUFFER_BIT);
            GlNative.glEnable(GlNative.GL_DEPTH_TEST);
            GlNative.glDepthMask(1);
            GlNative.glDisable(GlNative.GL_BLEND);
            GlNative.glEnable(GlNative.GL_POLYGON_OFFSET_FILL);
            GlNative.glPolygonOffset(1.8f, 3.0f);
            GlNative.glDisable(GlNative.GL_CULL_FACE);

            // Single meshes
            if (_opaqueMeshes.Count > 0)
            {
                GlNative.glUseProgram(_shadowProg);
                SetMat4(_shLightSpace, _lightSpaceMat);
                for (int i = 0; i < _opaqueMeshes.Count; i++)
                {
                    MeshDrawCmd cmd = _opaqueMeshes[i];
                    if (!cmd.Mat.CastShadow) continue;
                    SetMat4(_shModel, cmd.Model);
                    GlNative.glBindVertexArray(cmd.Mesh.Vao);
                    GlNative.glDrawElements(GlNative.GL_TRIANGLES, cmd.Mesh.IndexCount, GlNative.GL_UNSIGNED_INT, IntPtr.Zero);
                    _frameDrawCalls++;
                    _frameTriangles += cmd.Mesh.TriangleCount;
                }
            }

            // Instanced batches
            if (_opaqueInst.Count > 0)
            {
                GlNative.glUseProgram(_shadowInstProg);
                SetMat4(_shInstLightSpace, _lightSpaceMat);
                for (int i = 0; i < _opaqueInst.Count; i++)
                {
                    InstDrawCmd cmd = _opaqueInst[i];
                    if (!cmd.Mat.CastShadow) continue;
                    GlNative.glBindVertexArray(cmd.Batch.Vao);
                    GlNative.glDrawElementsInstanced(GlNative.GL_TRIANGLES, cmd.Batch.Mesh.IndexCount, GlNative.GL_UNSIGNED_INT, IntPtr.Zero, cmd.Batch.ActiveCount);
                    _frameDrawCalls++;
                    _frameTriangles += cmd.Batch.Mesh.TriangleCount * cmd.Batch.ActiveCount;
                }
            }

            GlNative.glDisable(GlNative.GL_POLYGON_OFFSET_FILL);
            GlNative.glEnable(GlNative.GL_CULL_FACE);
            GlNative.glBindVertexArray(0);
        }

        private void RenderSkyDome(int inlineTonemap)
        {
            GlNative.glDisable(GlNative.GL_DEPTH_TEST);
            GlNative.glDepthMask(0);
            GlNative.glDisable(GlNative.GL_CULL_FACE);

            GlNative.glUseProgram(_skyProg);
            SetMat4(_skyInvViewProjLoc, _invViewProjMat);
            GlNative.glUniform3f(_skySunDirLoc, _sunDir.X, _sunDir.Y, _sunDir.Z);
            GlNative.glUniform3f(_skyZenithLoc, _skyZenith.X, _skyZenith.Y, _skyZenith.Z);
            GlNative.glUniform3f(_skyMidLoc, _skyMid.X, _skyMid.Y, _skyMid.Z);
            GlNative.glUniform3f(_skyHorizLoc, _skyHoriz.X, _skyHoriz.Y, _skyHoriz.Z);
            GlNative.glUniform3f(_skyNadirLoc, _skyNadir.X, _skyNadir.Y, _skyNadir.Z);
            GlNative.glUniform3f(_skySunGlowLoc, _skySunGlow.X, _skySunGlow.Y, _skySunGlow.Z);
            GlNative.glUniform1f(_skySunIntLoc, _skySunIntensity);
            GlNative.glUniform1f(_skyHorizGlowLoc, _skyHorizonGlow);
            GlNative.glUniform1f(_skyStarVisLoc, _skyStarVis);
            GlNative.glUniform1f(_skyCloudLoc, _skyCloudCover);
            GlNative.glUniform1f(_skyTimeLoc, _elapsedTime);
            GlNative.glUniform1i(_skyTonemapLoc, inlineTonemap);

            GlNative.glBindVertexArray(_emptyVao);
            GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
            _frameDrawCalls++;
            _frameTriangles += 1;

            GlNative.glDepthMask(1);
            GlNative.glEnable(GlNative.GL_DEPTH_TEST);
            GlNative.glEnable(GlNative.GL_CULL_FACE);
        }

        private void BindPbrGlobals(uint prog, PbrLocs loc, int inlineTonemap)
        {
            GlNative.glUseProgram(prog);
            SetMat4(loc.uViewProj, _viewProjMat);
            SetMat4(loc.uLightSpace, _lightSpaceMat);
            GlNative.glUniform3f(loc.uCameraPos, _cameraPos.X, _cameraPos.Y, _cameraPos.Z);
            GlNative.glUniform3f(loc.uSunDir, _sunDir.X, _sunDir.Y, _sunDir.Z);
            GlNative.glUniform3f(loc.uSunColor, _sunColor.X, _sunColor.Y, _sunColor.Z);
            GlNative.glUniform1f(loc.uSunIntensity, _sunIntensity);
            GlNative.glUniform3f(loc.uHemiSky, _hemiSky.X, _hemiSky.Y, _hemiSky.Z);
            GlNative.glUniform3f(loc.uHemiGround, _hemiGround.X, _hemiGround.Y, _hemiGround.Z);
            GlNative.glUniform1f(loc.uHemiIntensity, _hemiIntensity);
            GlNative.glUniform3f(loc.uFogColor, _fogColor.X, _fogColor.Y, _fogColor.Z);
            GlNative.glUniform1f(loc.uFogDensity, _fogDensity);
            GlNative.glUniform1f(loc.uTime, _elapsedTime);
            GlNative.glUniform1f(loc.uNightWindowGlow, _nightWindowGlow);
            GlNative.glUniform1i(loc.uApplyTonemap, inlineTonemap);

            GlNative.glActiveTexture(GlNative.GL_TEXTURE0);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _shadowTex);
            GlNative.glUniform1i(loc.uShadowMap, 0);
        }

        private void BindMaterial(PbrLocs loc, MaterialDef mat)
        {
            if (mat.DoubleSided)
            {
                GlNative.glDisable(GlNative.GL_CULL_FACE);
            }
            else
            {
                GlNative.glEnable(GlNative.GL_CULL_FACE);
            }

            GlNative.glUniform3f(loc.uColor, mat.Color.X, mat.Color.Y, mat.Color.Z);
            GlNative.glUniform3f(loc.uEmissive, mat.Emissive.X, mat.Emissive.Y, mat.Emissive.Z);
            GlNative.glUniform1f(loc.uEmissiveIntensity, mat.EmissiveIntensity);
            GlNative.glUniform1f(loc.uRoughness, Clamp(mat.Roughness, 0.05f, 1.0f));
            GlNative.glUniform1f(loc.uMetallic, Clamp(mat.Metallic, 0.0f, 1.0f));
            GlNative.glUniform1f(loc.uAlpha, Clamp(mat.Alpha, 0.0f, 1.0f));
            GlNative.glUniform1i(loc.uPattern, (int)mat.Pattern);
            GlNative.glUniform1f(loc.uPatternScale, mat.PatternScale > 0.01f ? mat.PatternScale : 1.0f);
        }

        private void RenderMeshList(List<MeshDrawCmd> list, int inlineTonemap)
        {
            if (list.Count == 0) return;
            BindPbrGlobals(_pbrProg, _pbrLoc, inlineTonemap);
            for (int i = 0; i < list.Count; i++)
            {
                MeshDrawCmd cmd = list[i];
                BindMaterial(_pbrLoc, cmd.Mat);
                SetMat4(_pbrLoc.uModel, cmd.Model);
                GlNative.glBindVertexArray(cmd.Mesh.Vao);
                GlNative.glDrawElements(GlNative.GL_TRIANGLES, cmd.Mesh.IndexCount, GlNative.GL_UNSIGNED_INT, IntPtr.Zero);
                _frameDrawCalls++;
                _frameTriangles += cmd.Mesh.TriangleCount;
            }
        }

        private void RenderInstancedList(List<InstDrawCmd> list, int inlineTonemap)
        {
            if (list.Count == 0) return;
            BindPbrGlobals(_pbrInstProg, _pbrInstLoc, inlineTonemap);
            for (int i = 0; i < list.Count; i++)
            {
                InstDrawCmd cmd = list[i];
                BindMaterial(_pbrInstLoc, cmd.Mat);
                GlNative.glBindVertexArray(cmd.Batch.Vao);
                GlNative.glDrawElementsInstanced(GlNative.GL_TRIANGLES, cmd.Batch.Mesh.IndexCount, GlNative.GL_UNSIGNED_INT, IntPtr.Zero, cmd.Batch.ActiveCount);
                _frameDrawCalls++;
                _frameTriangles += cmd.Batch.Mesh.TriangleCount * cmd.Batch.ActiveCount;
            }
        }

        private void RenderLinesPass(int inlineTonemap)
        {
            if (_lineCmds.Count == 0) return;
            GlNative.glUseProgram(_lineProg);
            SetMat4(_lineViewProj, _viewProjMat);
            GlNative.glUniform1i(_lineTonemap, inlineTonemap);
            GlNative.glDisable(GlNative.GL_CULL_FACE);

            for (int i = 0; i < _lineCmds.Count; i++)
            {
                LineDrawCmd cmd = _lineCmds[i];
                if (cmd.Additive)
                {
                    GlNative.glBlendFunc(GlNative.GL_SRC_ALPHA, GlNative.GL_ONE);
                }
                else
                {
                    GlNative.glBlendFunc(GlNative.GL_SRC_ALPHA, GlNative.GL_ONE_MINUS_SRC_ALPHA);
                }
                GlNative.glBindVertexArray(cmd.Batch.Vao);
                GlNative.glDrawArrays(GlNative.GL_LINES, 0, cmd.Batch.ActiveVertexCount);
                _frameDrawCalls++;
            }
        }

        private void RenderHdrBloomAndComposite(uint targetFbo)
        {
            // Step A: Resolve MSAA HDR buffer -> single-sample RGBA16F scene texture
            if (_hasMsaaFbo)
            {
                GlNative.glBindFramebuffer(GlNative.GL_READ_FRAMEBUFFER, _msaaFbo);
                GlNative.glBindFramebuffer(GlNative.GL_DRAW_FRAMEBUFFER, _sceneFbo);
                GlNative.glBlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GlNative.GL_COLOR_BUFFER_BIT, GlNative.GL_NEAREST);
            }

            GlNative.glDisable(GlNative.GL_DEPTH_TEST);
            GlNative.glDepthMask(0);
            GlNative.glDisable(GlNative.GL_CULL_FACE);
            GlNative.glDisable(GlNative.GL_BLEND);
            GlNative.glBindVertexArray(_emptyVao);

            // Step B: Half-resolution selective bright-pass extract
            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _bloomFboA);
            GlNative.glViewport(0, 0, _bloomW, _bloomH);
            GlNative.glUseProgram(_brightProg);
            GlNative.glActiveTexture(GlNative.GL_TEXTURE0);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _sceneColorTex);
            GlNative.glUniform1i(_brightScene, 0);
            GlNative.glUniform1f(_brightThreshold, 0.96f);
            GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
            _frameDrawCalls++;

            // Step C: 2x ping-pong 9-tap separable Gaussian blur at half-resolution
            GlNative.glUseProgram(_blurProg);
            GlNative.glUniform1i(_blurImage, 0);
            for (int iter = 0; iter < 2; iter++)
            {
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _bloomFboB);
                GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _bloomTexA);
                GlNative.glUniform1i(_blurHoriz, 1);
                GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
                _frameDrawCalls++;

                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _bloomFboA);
                GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _bloomTexB);
                GlNative.glUniform1i(_blurHoriz, 0);
                GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
                _frameDrawCalls++;
            }

            // Step D: Final HDR + Bloom Composite + ACES Filmic Tone Mapping + sRGB output
            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, targetFbo);
            GlNative.glViewport(0, 0, _width, _height);
            GlNative.glUseProgram(_compositeProg);

            GlNative.glActiveTexture(GlNative.GL_TEXTURE0);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _sceneColorTex);
            GlNative.glUniform1i(_compScene, 0);

            GlNative.glActiveTexture(GlNative.GL_TEXTURE1);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _bloomTexA);
            GlNative.glUniform1i(_compBloom, 1);

            GlNative.glUniform1f(_compBloomStrength, 0.32f);
            GlNative.glUniform1f(_compExposure, 1.14f);
            GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
            _frameDrawCalls++;

            GlNative.glDepthMask(1);
            GlNative.glEnable(GlNative.GL_DEPTH_TEST);
            GlNative.glEnable(GlNative.GL_CULL_FACE);
        }

        public Bitmap ReadFramebufferBitmap()
        {
            EnsureCurrent();
            int w = _width;
            int h = _height;
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            bool usedOffscreen = false;
            if (_hasHdrPipeline && GlNative.glGenFramebuffers != null)
            {
                EnsureReadbackFbo(w, h);
                if (_readbackFbo != 0)
                {
                    // Re-run lightweight 1-quad final ACES composite into dedicated RGBA8 readback FBO
                    // (immune to WDDM DWM flip-model back-buffer invalidation!)
                    GlNative.glDisable(GlNative.GL_DEPTH_TEST);
                    GlNative.glDisable(GlNative.GL_BLEND);
                    GlNative.glDisable(GlNative.GL_CULL_FACE);
                    GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, _readbackFbo);
                    GlNative.glViewport(0, 0, w, h);
                    GlNative.glUseProgram(_compositeProg);
                    GlNative.glActiveTexture(GlNative.GL_TEXTURE0);
                    GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _sceneColorTex);
                    GlNative.glUniform1i(_compScene, 0);
                    GlNative.glActiveTexture(GlNative.GL_TEXTURE1);
                    GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, _bloomTexA);
                    GlNative.glUniform1i(_compBloom, 1);
                    GlNative.glUniform1f(_compBloomStrength, 0.32f);
                    GlNative.glUniform1f(_compExposure, 1.14f);
                    GlNative.glBindVertexArray(_emptyVao);
                    GlNative.glDrawArrays(GlNative.GL_TRIANGLES, 0, 3);
                    usedOffscreen = true;
                }
            }

            if (!usedOffscreen)
            {
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
                GlNative.glReadBuffer(GlNative.GL_FRONT);
            }

            GlNative.glFinish();
            GlNative.glPixelStorei(GlNative.GL_PACK_ALIGNMENT, 1);

            int rowBytes = w * 3;
            byte[] raw = new byte[rowBytes * h];
            fixed (byte* ptr = raw)
            {
                GlNative.glReadPixels(0, 0, w, h, GlNative.GL_BGR, GlNative.GL_UNSIGNED_BYTE, new IntPtr(ptr));
            }

            if (usedOffscreen)
            {
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
                GlNative.glEnable(GlNative.GL_DEPTH_TEST);
                GlNative.glEnable(GlNative.GL_CULL_FACE);
            }

            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                // Flip rows vertically (OpenGL bottom-left -> GDI+ top-left)
                for (int y = 0; y < h; y++)
                {
                    int srcOffset = (h - 1 - y) * rowBytes;
                    IntPtr dstRow = new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride);
                    Marshal.Copy(raw, srcOffset, dstRow, rowBytes);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }

        private void EnsureReadbackFbo(int w, int h)
        {
            if (_readbackFbo != 0 && _readbackW == w && _readbackH == h) return;
            DestroyReadbackFbo();

            uint fbo = 0, tex = 0;
            GlNative.glGenFramebuffers(1, &fbo);
            GlNative.glGenTextures(1, &tex);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, tex);
            GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, (int)GlNative.GL_RGBA8, w, h, 0, GlNative.GL_RGBA, GlNative.GL_UNSIGNED_BYTE, IntPtr.Zero);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, GlNative.GL_NEAREST);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, GlNative.GL_NEAREST);

            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, fbo);
            GlNative.glFramebufferTexture2D(GlNative.GL_FRAMEBUFFER, GlNative.GL_COLOR_ATTACHMENT0, GlNative.GL_TEXTURE_2D, tex, 0);

            if (GlNative.glCheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) == GlNative.GL_FRAMEBUFFER_COMPLETE)
            {
                _readbackFbo = fbo;
                _readbackTex = tex;
                _readbackW = w;
                _readbackH = h;
            }
            else
            {
                GlNative.glDeleteFramebuffers(1, &fbo);
                GlNative.glDeleteTextures(1, &tex);
            }
            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
        }

        private void DestroyReadbackFbo()
        {
            if (_readbackFbo != 0 && GlNative.glDeleteFramebuffers != null)
            {
                uint f = _readbackFbo;
                GlNative.glDeleteFramebuffers(1, &f);
                _readbackFbo = 0;
            }
            if (_readbackTex != 0)
            {
                uint t = _readbackTex;
                GlNative.glDeleteTextures(1, &t);
                _readbackTex = 0;
            }
            _readbackW = 0;
            _readbackH = 0;
        }

        private void CreateShadowResources()
        {
            if (GlNative.glGenFramebuffers == null) return;
            uint fbo = 0, tex = 0;
            GlNative.glGenFramebuffers(1, &fbo);
            GlNative.glGenTextures(1, &tex);

            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, tex);
            GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, (int)GlNative.GL_DEPTH_COMPONENT24, ShadowMapSize, ShadowMapSize, 0, GlNative.GL_DEPTH_COMPONENT, GlNative.GL_FLOAT, IntPtr.Zero);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, GlNative.GL_NEAREST);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, GlNative.GL_NEAREST);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_S, GlNative.GL_CLAMP_TO_BORDER);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_T, GlNative.GL_CLAMP_TO_BORDER);
            float* border = stackalloc float[4];
            border[0] = 1f; border[1] = 1f; border[2] = 1f; border[3] = 1f;
            GlNative.glTexParameterfv(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_BORDER_COLOR, border);

            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, fbo);
            GlNative.glFramebufferTexture2D(GlNative.GL_FRAMEBUFFER, GlNative.GL_DEPTH_ATTACHMENT, GlNative.GL_TEXTURE_2D, tex, 0);
            GlNative.glDrawBuffer(GlNative.GL_NONE);
            GlNative.glReadBuffer(GlNative.GL_NONE);

            if (GlNative.glCheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) == GlNative.GL_FRAMEBUFFER_COMPLETE)
            {
                _shadowFbo = fbo;
                _shadowTex = tex;
            }
            else
            {
                GlNative.glDeleteFramebuffers(1, &fbo);
                GlNative.glDeleteTextures(1, &tex);
            }
            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
        }

        private void CreateHdrResources(int w, int h)
        {
            _hasHdrPipeline = false;
            _hasMsaaFbo = false;
            if (GlNative.glGenFramebuffers == null || GlNative.glGenRenderbuffers == null) return;

            try
            {
                // 1. Single-sample RGBA16F scene FBO
                uint sfbo = 0, stex = 0, sdepth = 0;
                GlNative.glGenFramebuffers(1, &sfbo);
                GlNative.glGenTextures(1, &stex);
                GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, stex);
                GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, (int)GlNative.GL_RGBA16F, w, h, 0, GlNative.GL_RGBA, GlNative.GL_FLOAT, IntPtr.Zero);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, GlNative.GL_LINEAR);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, GlNative.GL_LINEAR);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_S, GlNative.GL_CLAMP_TO_EDGE);
                GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_T, GlNative.GL_CLAMP_TO_EDGE);

                GlNative.glGenRenderbuffers(1, &sdepth);
                GlNative.glBindRenderbuffer(GlNative.GL_RENDERBUFFER, sdepth);
                GlNative.glRenderbufferStorage(GlNative.GL_RENDERBUFFER, GlNative.GL_DEPTH_COMPONENT24, w, h);

                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, sfbo);
                GlNative.glFramebufferTexture2D(GlNative.GL_FRAMEBUFFER, GlNative.GL_COLOR_ATTACHMENT0, GlNative.GL_TEXTURE_2D, stex, 0);
                GlNative.glFramebufferRenderbuffer(GlNative.GL_FRAMEBUFFER, GlNative.GL_DEPTH_ATTACHMENT, GlNative.GL_RENDERBUFFER, sdepth);

                if (GlNative.glCheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) != GlNative.GL_FRAMEBUFFER_COMPLETE)
                {
                    GlNative.glDeleteFramebuffers(1, &sfbo);
                    GlNative.glDeleteTextures(1, &stex);
                    GlNative.glDeleteRenderbuffers(1, &sdepth);
                    GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
                    return;
                }
                _sceneFbo = sfbo;
                _sceneColorTex = stex;
                _sceneDepthRbo = sdepth;

                // 2. Optional 4x MSAA HDR renderbuffer FBO for razor-sharp edges
                if (GlNative.glRenderbufferStorageMultisample != null && GlNative.glBlitFramebuffer != null)
                {
                    int maxSamples;
                    GlNative.glGetIntegerv(GlNative.GL_MAX_SAMPLES, out maxSamples);
                    int samples = Math.Min(4, maxSamples);
                    if (samples >= 2)
                    {
                        uint mfbo = 0, mcol = 0, mdep = 0;
                        GlNative.glGenFramebuffers(1, &mfbo);
                        GlNative.glGenRenderbuffers(1, &mcol);
                        GlNative.glGenRenderbuffers(1, &mdep);

                        GlNative.glBindRenderbuffer(GlNative.GL_RENDERBUFFER, mcol);
                        GlNative.glRenderbufferStorageMultisample(GlNative.GL_RENDERBUFFER, samples, GlNative.GL_RGBA16F, w, h);
                        GlNative.glBindRenderbuffer(GlNative.GL_RENDERBUFFER, mdep);
                        GlNative.glRenderbufferStorageMultisample(GlNative.GL_RENDERBUFFER, samples, GlNative.GL_DEPTH_COMPONENT24, w, h);

                        GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, mfbo);
                        GlNative.glFramebufferRenderbuffer(GlNative.GL_FRAMEBUFFER, GlNative.GL_COLOR_ATTACHMENT0, GlNative.GL_RENDERBUFFER, mcol);
                        GlNative.glFramebufferRenderbuffer(GlNative.GL_FRAMEBUFFER, GlNative.GL_DEPTH_ATTACHMENT, GlNative.GL_RENDERBUFFER, mdep);

                        if (GlNative.glCheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) == GlNative.GL_FRAMEBUFFER_COMPLETE)
                        {
                            _msaaFbo = mfbo;
                            _msaaColorRbo = mcol;
                            _msaaDepthRbo = mdep;
                            _hasMsaaFbo = true;
                        }
                        else
                        {
                            GlNative.glDeleteFramebuffers(1, &mfbo);
                            GlNative.glDeleteRenderbuffers(1, &mcol);
                            GlNative.glDeleteRenderbuffers(1, &mdep);
                        }
                    }
                }

                // 3. Half-resolution ping-pong HDR bloom FBOs
                _bloomW = Math.Max(1, w / 2);
                _bloomH = Math.Max(1, h / 2);
                if (!CreateColorFbo(_bloomW, _bloomH, GlNative.GL_RGBA16F, out _bloomFboA, out _bloomTexA) ||
                    !CreateColorFbo(_bloomW, _bloomH, GlNative.GL_RGBA16F, out _bloomFboB, out _bloomTexB))
                {
                    DestroyHdrResources();
                    return;
                }

                _hasHdrPipeline = true;
                GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
            }
            catch
            {
                DestroyHdrResources();
            }
        }

        private static bool CreateColorFbo(int w, int h, uint internalFormat, out uint fbo, out uint tex)
        {
            uint f = 0, t = 0;
            GlNative.glGenFramebuffers(1, &f);
            GlNative.glGenTextures(1, &t);
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, t);
            GlNative.glTexImage2D(GlNative.GL_TEXTURE_2D, 0, (int)internalFormat, w, h, 0, GlNative.GL_RGBA, GlNative.GL_FLOAT, IntPtr.Zero);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MIN_FILTER, GlNative.GL_LINEAR);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_MAG_FILTER, GlNative.GL_LINEAR);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_S, GlNative.GL_CLAMP_TO_EDGE);
            GlNative.glTexParameteri(GlNative.GL_TEXTURE_2D, GlNative.GL_TEXTURE_WRAP_T, GlNative.GL_CLAMP_TO_EDGE);

            GlNative.glBindFramebuffer(GlNative.GL_FRAMEBUFFER, f);
            GlNative.glFramebufferTexture2D(GlNative.GL_FRAMEBUFFER, GlNative.GL_COLOR_ATTACHMENT0, GlNative.GL_TEXTURE_2D, t, 0);
            bool ok = GlNative.glCheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) == GlNative.GL_FRAMEBUFFER_COMPLETE;
            if (!ok)
            {
                GlNative.glDeleteFramebuffers(1, &f);
                GlNative.glDeleteTextures(1, &t);
                fbo = 0;
                tex = 0;
                return false;
            }
            fbo = f;
            tex = t;
            return true;
        }

        private void DestroyHdrResources()
        {
            _hasHdrPipeline = false;
            _hasMsaaFbo = false;
            if (GlNative.glDeleteFramebuffers != null)
            {
                if (_msaaFbo != 0) { uint x = _msaaFbo; GlNative.glDeleteFramebuffers(1, &x); _msaaFbo = 0; }
                if (_sceneFbo != 0) { uint x = _sceneFbo; GlNative.glDeleteFramebuffers(1, &x); _sceneFbo = 0; }
                if (_bloomFboA != 0) { uint x = _bloomFboA; GlNative.glDeleteFramebuffers(1, &x); _bloomFboA = 0; }
                if (_bloomFboB != 0) { uint x = _bloomFboB; GlNative.glDeleteFramebuffers(1, &x); _bloomFboB = 0; }
            }
            if (GlNative.glDeleteRenderbuffers != null)
            {
                if (_msaaColorRbo != 0) { uint x = _msaaColorRbo; GlNative.glDeleteRenderbuffers(1, &x); _msaaColorRbo = 0; }
                if (_msaaDepthRbo != 0) { uint x = _msaaDepthRbo; GlNative.glDeleteRenderbuffers(1, &x); _msaaDepthRbo = 0; }
                if (_sceneDepthRbo != 0) { uint x = _sceneDepthRbo; GlNative.glDeleteRenderbuffers(1, &x); _sceneDepthRbo = 0; }
            }
            if (_sceneColorTex != 0) { uint x = _sceneColorTex; GlNative.glDeleteTextures(1, &x); _sceneColorTex = 0; }
            if (_bloomTexA != 0) { uint x = _bloomTexA; GlNative.glDeleteTextures(1, &x); _bloomTexA = 0; }
            if (_bloomTexB != 0) { uint x = _bloomTexB; GlNative.glDeleteTextures(1, &x); _bloomTexB = 0; }
            DestroyReadbackFbo();
        }

        private void BuildGeologicalPlinth()
        {
            float R = WorldLayout.DistrictRadius;

            // 1. Bedrock floor slab (-22..-20)
            MeshBuilder mb = new MeshBuilder();
            mb.AddCylinder(new Vec3(0f, -22f, 0f), R + 2.8f, R + 1.8f, 2.0f, 84, true);
            _bedrockMesh = UploadMesh(mb);
            _bedrockMat.Color = Vec3.FromHex(0x131d2c);
            _bedrockMat.Roughness = 0.88f;
            _bedrockMat.Metallic = 0.16f;
            _bedrockMat.Pattern = MaterialPattern.Concrete;
            _bedrockMat.PatternScale = 0.45f;
            _bedrockMat.CastShadow = false;

            // 2. Lower machined steel base flange
            mb = new MeshBuilder();
            mb.AddTorus(new Vec3(0f, -20.12f, 0f), R + 2.2f, 0.34f, 84, 10, true);
            _baseFlangeMesh = UploadMesh(mb);
            _metalTrimMat.Color = Vec3.FromHex(0x475569);
            _metalTrimMat.Roughness = 0.28f;
            _metalTrimMat.Metallic = 0.84f;
            _metalTrimMat.CastShadow = false;

            // 3. Bevelled metallic top rim collar
            mb = new MeshBuilder();
            mb.AddCylinder(new Vec3(0f, -0.52f, 0f), R + 2.30f, R + 2.30f, 0.48f, 84, false);
            mb.AddCylinder(new Vec3(0f, -0.04f, 0f), R + 2.30f, R + 1.95f, 0.18f, 84, false);
            mb.AddRing(new Vec3(0f, 0.14f, 0f), R + 0.95f, R + 1.95f, 84);
            _bevelRimMesh = UploadMesh(mb);

            // 4. Glowing cyan telemetry inlay on rim
            mb = new MeshBuilder();
            mb.AddTorus(new Vec3(0f, 0.145f, 0f), R + 1.48f, 0.085f, 84, 8, true);
            _rimInlayMesh = UploadMesh(mb);
            _rimInlayMat.Color = Vec3.FromHex(0x67e8f9);
            _rimInlayMat.Emissive = Vec3.FromHex(0x06b6d4);
            _rimInlayMat.EmissiveIntensity = 1.05f;
            _rimInlayMat.Roughness = 0.22f;
            _rimInlayMat.Metallic = 0.3f;
            _rimInlayMat.CastShadow = false;

            // 5. 36 azimuth ticks merged into a single mesh (1 draw call!)
            mb = new MeshBuilder();
            for (int i = 0; i < 36; i++)
            {
                float a = (float)(i * (Math.PI * 2.0 / 36.0));
                bool cardinal = (i % 9) == 0;
                float rad = R + 1.84f;
                float tw = cardinal ? 0.26f : 0.14f;
                float td = cardinal ? 1.05f : 0.62f;
                mb.AddBox(new Vec3((float)Math.Cos(a) * rad, 0.16f, (float)Math.Sin(a) * rad), new Vec3(tw, 0.04f, td), -a + (float)(Math.PI * 0.5));
            }
            _rimTicksMesh = UploadMesh(mb);
            _rimTicksMat.Color = Vec3.FromHex(0x94a3b8);
            _rimTicksMat.Emissive = Vec3.FromHex(0x0ea5e9);
            _rimTicksMat.EmissiveIntensity = 0.35f;
            _rimTicksMat.Roughness = 0.32f;
            _rimTicksMat.Metallic = 0.78f;
            _rimTicksMat.CastShadow = false;

            // 6. Level -4 bedrock datum rings merged into 1 draw call
            mb = new MeshBuilder();
            mb.AddRing(new Vec3(0f, -19.93f, 0f), WorldLayout.VaultRingRadius - 0.14f, WorldLayout.VaultRingRadius + 0.14f, 72);
            mb.AddRing(new Vec3(0f, -19.93f, 0f), 47.86f, 48.14f, 72);
            mb.AddRing(new Vec3(0f, -19.93f, 0f), R - 0.64f, R - 0.36f, 84);
            _datumRingsMesh = UploadMesh(mb);
            _datumRingsMat.Color = Vec3.FromHex(0x22d3ee);
            _datumRingsMat.Emissive = Vec3.FromHex(0x22d3ee);
            _datumRingsMat.EmissiveIntensity = 0.45f;
            _datumRingsMat.Alpha = 0.30f;
            _datumRingsMat.DoubleSided = true;
            _datumRingsMat.CastShadow = false;

            // 7. Dublin Geological Cutaway Strata Cylinder (Topsoil -> Boulder Clay -> Calp Limestone -> Granite Bedrock)
            mb = new MeshBuilder();
            mb.AddCylinder(new Vec3(0f, -20.03f, 0f), R + 1.58f, R + 1.12f, 19.65f, 84, false);
            _strataSkirtMesh = UploadMesh(mb);
            _strataMat.Color = new Vec3(0.92f, 0.94f, 0.97f);
            _strataMat.Roughness = 0.88f;
            _strataMat.Metallic = 0.06f;
            _strataMat.Alpha = 0.66f;
            _strataMat.Pattern = MaterialPattern.Strata;
            _strataMat.DoubleSided = true;
            _strataMat.CastShadow = false;

            // 8. Level -4 exterior cyan datum ring
            mb = new MeshBuilder();
            mb.AddTorus(new Vec3(0f, WorldLayout.VaultY, 0f), R + 1.56f, 0.09f, 84, 8, true);
            _l4BandMesh = UploadMesh(mb);
            _l4BandMat.Color = Vec3.FromHex(0x38bdf8);
            _l4BandMat.Emissive = Vec3.FromHex(0x0ea5e9);
            _l4BandMat.EmissiveIntensity = 0.78f;
            _l4BandMat.Alpha = 0.55f;
            _l4BandMat.CastShadow = false;

            // 9. X-Ray Ground Surface Disk
            mb = new MeshBuilder();
            mb.AddCylinder(new Vec3(0f, -0.39f, 0f), R + 0.75f, R + 0.75f, 0.34f, 84, true);
            _groundDiskMesh = UploadMesh(mb);
            _groundMat.Color = Vec3.FromHex(0x182538);
            _groundMat.Roughness = 0.86f;
            _groundMat.Metallic = 0.10f;
            _groundMat.Alpha = 0.78f;
            _groundMat.Pattern = MaterialPattern.PlazaTile;
            _groundMat.PatternScale = 0.22f;
            _groundMat.CastShadow = false;

            // 10. Level -4 Cartesian engineering grid lines on bedrock floor
            int gridN = 28;
            float half = 66f;
            float step = (half * 2f) / gridN;
            int lineVerts = (gridN + 1) * 4;
            float[] gridData = new float[lineVerts * 7];
            int ptr = 0;
            float gy = -19.95f;
            for (int i = 0; i <= gridN; i++)
            {
                float p = -half + i * step;
                bool major = (i % 7) == 0;
                float cr = major ? 0.14f : 0.08f;
                float cg = major ? 0.34f : 0.17f;
                float cb = major ? 0.52f : 0.28f;
                float ca = major ? 0.50f : 0.30f;

                // X line
                WriteLineVert(gridData, ref ptr, -half, gy, p, cr, cg, cb, ca);
                WriteLineVert(gridData, ref ptr,  half, gy, p, cr, cg, cb, ca);
                // Z line
                WriteLineVert(gridData, ref ptr, p, gy, -half, cr, cg, cb, ca);
                WriteLineVert(gridData, ref ptr, p, gy,  half, cr, cg, cb, ca);
            }
            _cavernGridLines = CreateLineBatch(lineVerts);
            _cavernGridLines.UpdateLines(gridData, lineVerts);
        }

        private void BuildSelectionHighlight()
        {
            _selectionLines = CreateLineBatch(256);
        }

        private void UpdateSelectionReticleLines()
        {
            if (_selectedTarget == null) return;
            Vec3 center = _selectedTarget.Center;
            Vec3 size = _selectedTarget.Bounds.Max - _selectedTarget.Bounds.Min;
            float baseY = (size.Y > 1e-4f) ? (_selectedTarget.Bounds.Min.Y + 0.08f) : (center.Y - 0.5f);
            float spanY = Clamp(size.Y, 1.4f, 10.0f);
            float rad = _selectedTarget.Radius > 0.5f
                ? _selectedTarget.Radius
                : (float)Math.Sqrt(size.X * size.X + size.Z * size.Z) * 0.52f + 0.45f;
            rad = Clamp(rad, 1.3f, 22f);

            float pop = 1.0f + 0.22f * (float)Math.Exp(-9.0 * _selectionPulseTime);
            float breath = 1.0f + 0.025f * (float)Math.Sin(_elapsedTime * 3.2);
            float r = rad * pop * breath;
            float r2 = r * 1.12f;

            float scanPhase = 0.5f - 0.5f * (float)Math.Cos(_elapsedTime * 2.2);
            float scanY = baseY + scanPhase * spanY;
            float rot = _elapsedTime * 0.6f;

            int ptr = 0;
            int segs = 48;
            // Lower glowing cyan ring
            for (int i = 0; i < segs; i++)
            {
                float a0 = (float)(i * (Math.PI * 2.0 / segs));
                float a1 = (float)((i + 1) * (Math.PI * 2.0 / segs));
                WriteLineVert(_selectionVerts, ref ptr, center.X + (float)Math.Cos(a0) * r, baseY, center.Z + (float)Math.Sin(a0) * r, 0.35f, 1.45f, 1.80f, 0.95f);
                WriteLineVert(_selectionVerts, ref ptr, center.X + (float)Math.Cos(a1) * r, baseY, center.Z + (float)Math.Sin(a1) * r, 0.35f, 1.45f, 1.80f, 0.95f);
            }
            // Upper scanning ring
            float scanAlpha = 0.30f + 0.55f * (float)Math.Sin(scanPhase * Math.PI);
            for (int i = 0; i < segs; i++)
            {
                float a0 = (float)(i * (Math.PI * 2.0 / segs));
                float a1 = (float)((i + 1) * (Math.PI * 2.0 / segs));
                WriteLineVert(_selectionVerts, ref ptr, center.X + (float)Math.Cos(a0) * r, scanY, center.Z + (float)Math.Sin(a0) * r, 0.55f, 1.50f, 1.90f, scanAlpha);
                WriteLineVert(_selectionVerts, ref ptr, center.X + (float)Math.Cos(a1) * r, scanY, center.Z + (float)Math.Sin(a1) * r, 0.55f, 1.50f, 1.90f, scanAlpha);
            }
            // 4 rotating corner reticle ticks
            for (int i = 0; i < 4; i++)
            {
                float a = rot + i * (float)(Math.PI * 0.5);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                WriteLineVert(_selectionVerts, ref ptr, center.X + ca * r,  baseY + 0.05f, center.Z + sa * r,  0.60f, 1.65f, 1.95f, 1.0f);
                WriteLineVert(_selectionVerts, ref ptr, center.X + ca * r2, baseY + 0.05f, center.Z + sa * r2, 0.60f, 1.65f, 1.95f, 1.0f);
            }

            _selectionLines.UpdateLines(_selectionVerts, ptr / 7);
        }

        private static void WriteLineVert(float[] buf, ref int ptr, float x, float y, float z, float r, float g, float b, float a)
        {
            buf[ptr++] = x;
            buf[ptr++] = y;
            buf[ptr++] = z;
            buf[ptr++] = r;
            buf[ptr++] = g;
            buf[ptr++] = b;
            buf[ptr++] = a;
        }

        private void SetMat4(int location, Mat4 m)
        {
            if (location < 0 || m.M == null) return;
            fixed (float* p = m.M)
            {
                GlNative.glUniformMatrix4fv(location, 1, 0, p);
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void CompileAllShaders()
        {
            string pbrFrag = BuildPbrFragmentShader();

            _pbrProg = LinkProgram(PbrVertexSingle, pbrFrag);
            _pbrLoc = FetchPbrLocs(_pbrProg);

            _pbrInstProg = LinkProgram(PbrVertexInstanced, pbrFrag);
            _pbrInstLoc = FetchPbrLocs(_pbrInstProg);

            _shadowProg = LinkProgram(ShadowVertexSingle, ShadowFragment);
            _shLightSpace = GlNative.glGetUniformLocation(_shadowProg, "uLightSpace");
            _shModel = GlNative.glGetUniformLocation(_shadowProg, "uModel");

            _shadowInstProg = LinkProgram(ShadowVertexInstanced, ShadowFragment);
            _shInstLightSpace = GlNative.glGetUniformLocation(_shadowInstProg, "uLightSpace");

            _skyProg = LinkProgram(FullscreenVertex, SkyFragment);
            _skyInvViewProjLoc = GlNative.glGetUniformLocation(_skyProg, "uInvViewProj");
            _skySunDirLoc = GlNative.glGetUniformLocation(_skyProg, "uSunDir");
            _skyZenithLoc = GlNative.glGetUniformLocation(_skyProg, "uZenithColor");
            _skyMidLoc = GlNative.glGetUniformLocation(_skyProg, "uMidSkyColor");
            _skyHorizLoc = GlNative.glGetUniformLocation(_skyProg, "uHorizonColor");
            _skyNadirLoc = GlNative.glGetUniformLocation(_skyProg, "uNadirColor");
            _skySunGlowLoc = GlNative.glGetUniformLocation(_skyProg, "uSunGlowColor");
            _skySunIntLoc = GlNative.glGetUniformLocation(_skyProg, "uSunIntensity");
            _skyHorizGlowLoc = GlNative.glGetUniformLocation(_skyProg, "uHorizonGlow");
            _skyStarVisLoc = GlNative.glGetUniformLocation(_skyProg, "uStarVisibility");
            _skyCloudLoc = GlNative.glGetUniformLocation(_skyProg, "uCloudCover");
            _skyTimeLoc = GlNative.glGetUniformLocation(_skyProg, "uTime");
            _skyTonemapLoc = GlNative.glGetUniformLocation(_skyProg, "uApplyTonemap");

            _lineProg = LinkProgram(LineVertex, LineFragment);
            _lineViewProj = GlNative.glGetUniformLocation(_lineProg, "uViewProj");
            _lineTonemap = GlNative.glGetUniformLocation(_lineProg, "uApplyTonemap");

            _brightProg = LinkProgram(FullscreenVertex, BrightFragment);
            _brightScene = GlNative.glGetUniformLocation(_brightProg, "uScene");
            _brightThreshold = GlNative.glGetUniformLocation(_brightProg, "uThreshold");

            _blurProg = LinkProgram(FullscreenVertex, BlurFragment);
            _blurImage = GlNative.glGetUniformLocation(_blurProg, "uImage");
            _blurHoriz = GlNative.glGetUniformLocation(_blurProg, "uHorizontal");

            _compositeProg = LinkProgram(FullscreenVertex, CompositeFragment);
            _compScene = GlNative.glGetUniformLocation(_compositeProg, "uScene");
            _compBloom = GlNative.glGetUniformLocation(_compositeProg, "uBloom");
            _compBloomStrength = GlNative.glGetUniformLocation(_compositeProg, "uBloomStrength");
            _compExposure = GlNative.glGetUniformLocation(_compositeProg, "uExposure");
        }

        private static PbrLocs FetchPbrLocs(uint prog)
        {
            PbrLocs l = new PbrLocs();
            l.uViewProj = GlNative.glGetUniformLocation(prog, "uViewProj");
            l.uModel = GlNative.glGetUniformLocation(prog, "uModel");
            l.uLightSpace = GlNative.glGetUniformLocation(prog, "uLightSpace");
            l.uCameraPos = GlNative.glGetUniformLocation(prog, "uCameraPos");
            l.uSunDir = GlNative.glGetUniformLocation(prog, "uSunDir");
            l.uSunColor = GlNative.glGetUniformLocation(prog, "uSunColor");
            l.uSunIntensity = GlNative.glGetUniformLocation(prog, "uSunIntensity");
            l.uHemiSky = GlNative.glGetUniformLocation(prog, "uHemiSky");
            l.uHemiGround = GlNative.glGetUniformLocation(prog, "uHemiGround");
            l.uHemiIntensity = GlNative.glGetUniformLocation(prog, "uHemiIntensity");
            l.uFogColor = GlNative.glGetUniformLocation(prog, "uFogColor");
            l.uFogDensity = GlNative.glGetUniformLocation(prog, "uFogDensity");
            l.uTime = GlNative.glGetUniformLocation(prog, "uTime");
            l.uNightWindowGlow = GlNative.glGetUniformLocation(prog, "uNightWindowGlow");
            l.uApplyTonemap = GlNative.glGetUniformLocation(prog, "uApplyTonemap");
            l.uColor = GlNative.glGetUniformLocation(prog, "uColor");
            l.uEmissive = GlNative.glGetUniformLocation(prog, "uEmissive");
            l.uEmissiveIntensity = GlNative.glGetUniformLocation(prog, "uEmissiveIntensity");
            l.uRoughness = GlNative.glGetUniformLocation(prog, "uRoughness");
            l.uMetallic = GlNative.glGetUniformLocation(prog, "uMetallic");
            l.uAlpha = GlNative.glGetUniformLocation(prog, "uAlpha");
            l.uPattern = GlNative.glGetUniformLocation(prog, "uPattern");
            l.uPatternScale = GlNative.glGetUniformLocation(prog, "uPatternScale");
            l.uShadowMap = GlNative.glGetUniformLocation(prog, "uShadowMap");
            return l;
        }

        private static uint CompileShader(uint type, string source)
        {
            uint sh = GlNative.glCreateShader(type);
            GlNative.glShaderSource(sh, 1, new string[] { source }, null);
            GlNative.glCompileShader(sh);
            int status;
            GlNative.glGetShaderiv(sh, GlNative.GL_COMPILE_STATUS, out status);
            if (status == 0)
            {
                StringBuilder log = new StringBuilder(2048);
                int len;
                GlNative.glGetShaderInfoLog(sh, log.Capacity, out len, log);
                GlNative.glDeleteShader(sh);
                throw new InvalidOperationException("GLSL compile error: " + log.ToString());
            }
            return sh;
        }

        private static uint LinkProgram(string vsSrc, string fsSrc)
        {
            uint vs = CompileShader(GlNative.GL_VERTEX_SHADER, vsSrc);
            uint fs = CompileShader(GlNative.GL_FRAGMENT_SHADER, fsSrc);
            uint prog = GlNative.glCreateProgram();
            GlNative.glAttachShader(prog, vs);
            GlNative.glAttachShader(prog, fs);
            GlNative.glLinkProgram(prog);
            GlNative.glDeleteShader(vs);
            GlNative.glDeleteShader(fs);

            int status;
            GlNative.glGetProgramiv(prog, GlNative.GL_LINK_STATUS, out status);
            if (status == 0)
            {
                StringBuilder log = new StringBuilder(2048);
                int len;
                GlNative.glGetProgramInfoLog(prog, log.Capacity, out len, log);
                GlNative.glDeleteProgram(prog);
                throw new InvalidOperationException("GLSL link error: " + log.ToString());
            }
            return prog;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            EnsureCurrent();

            if (_bedrockMesh != null) _bedrockMesh.Dispose();
            if (_baseFlangeMesh != null) _baseFlangeMesh.Dispose();
            if (_bevelRimMesh != null) _bevelRimMesh.Dispose();
            if (_rimInlayMesh != null) _rimInlayMesh.Dispose();
            if (_rimTicksMesh != null) _rimTicksMesh.Dispose();
            if (_datumRingsMesh != null) _datumRingsMesh.Dispose();
            if (_strataSkirtMesh != null) _strataSkirtMesh.Dispose();
            if (_l4BandMesh != null) _l4BandMesh.Dispose();
            if (_groundDiskMesh != null) _groundDiskMesh.Dispose();
            if (_cavernGridLines != null) _cavernGridLines.Dispose();
            if (_selectionLines != null) _selectionLines.Dispose();

            DestroyHdrResources();
            if (_shadowFbo != 0 && GlNative.glDeleteFramebuffers != null) { uint x = _shadowFbo; GlNative.glDeleteFramebuffers(1, &x); _shadowFbo = 0; }
            if (_shadowTex != 0) { uint x = _shadowTex; GlNative.glDeleteTextures(1, &x); _shadowTex = 0; }
            if (_emptyVao != 0 && GlNative.glDeleteVertexArrays != null) { uint x = _emptyVao; GlNative.glDeleteVertexArrays(1, &x); _emptyVao = 0; }

            DeleteProg(ref _pbrProg);
            DeleteProg(ref _pbrInstProg);
            DeleteProg(ref _shadowProg);
            DeleteProg(ref _shadowInstProg);
            DeleteProg(ref _skyProg);
            DeleteProg(ref _lineProg);
            DeleteProg(ref _brightProg);
            DeleteProg(ref _blurProg);
            DeleteProg(ref _compositeProg);

            GlNative.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            if (_hglrc != IntPtr.Zero)
            {
                GlNative.wglDeleteContext(_hglrc);
                _hglrc = IntPtr.Zero;
            }
            if (_hdc != IntPtr.Zero)
            {
                GlNative.ReleaseDC(_hwnd, _hdc);
                _hdc = IntPtr.Zero;
            }
        }

        private static void DeleteProg(ref uint prog)
        {
            if (prog != 0 && GlNative.glDeleteProgram != null)
            {
                GlNative.glDeleteProgram(prog);
                prog = 0;
            }
        }

        // =========================================================================
        // GLSL 330 CORE SHADERS
        // =========================================================================

        private const string PbrVertexSingle = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNorm;
layout(location = 2) in vec2 aUv;

uniform mat4 uViewProj;
uniform mat4 uModel;
uniform mat4 uLightSpace;

out vec3 vWorldPos;
out vec3 vWorldNorm;
out vec2 vUv;
out vec4 vLightPos;
out vec3 vTint;
out float vEmissiveBoost;

void main() {
    vec4 wp = uModel * vec4(aPos, 1.0);
    vWorldPos = wp.xyz;
    mat3 normalMat = transpose(inverse(mat3(uModel)));
    vWorldNorm = normalize(normalMat * aNorm);
    vUv = aUv;
    vLightPos = uLightSpace * vec4(wp.xyz + vWorldNorm * 0.08, 1.0);
    vTint = vec3(1.0);
    vEmissiveBoost = 0.0;
    gl_Position = uViewProj * wp;
}
";

        private const string PbrVertexInstanced = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNorm;
layout(location = 2) in vec2 aUv;
layout(location = 3) in vec4 aInst0;
layout(location = 4) in vec4 aInst1;
layout(location = 5) in vec4 aInst2;
layout(location = 6) in vec4 aInst3;
layout(location = 7) in vec4 aInstTint;

uniform mat4 uViewProj;
uniform mat4 uLightSpace;

out vec3 vWorldPos;
out vec3 vWorldNorm;
out vec2 vUv;
out vec4 vLightPos;
out vec3 vTint;
out float vEmissiveBoost;

void main() {
    mat4 instModel = mat4(aInst0, aInst1, aInst2, aInst3);
    vec4 wp = instModel * vec4(aPos, 1.0);
    vWorldPos = wp.xyz;
    mat3 normalMat = transpose(inverse(mat3(instModel)));
    vWorldNorm = normalize(normalMat * aNorm);
    vUv = aUv;
    vLightPos = uLightSpace * vec4(wp.xyz + vWorldNorm * 0.08, 1.0);
    vTint = aInstTint.rgb;
    vEmissiveBoost = aInstTint.a;
    gl_Position = uViewProj * wp;
}
";

        private static string BuildPbrFragmentShader()
        {
            return @"#version 330 core
in vec3 vWorldPos;
in vec3 vWorldNorm;
in vec2 vUv;
in vec4 vLightPos;
in vec3 vTint;
in float vEmissiveBoost;

uniform vec3 uCameraPos;
uniform vec3 uSunDir;
uniform vec3 uSunColor;
uniform float uSunIntensity;
uniform vec3 uHemiSky;
uniform vec3 uHemiGround;
uniform float uHemiIntensity;
uniform vec3 uFogColor;
uniform float uFogDensity;
uniform float uTime;
uniform float uNightWindowGlow;
uniform int uApplyTonemap;

uniform vec3 uColor;
uniform vec3 uEmissive;
uniform float uEmissiveIntensity;
uniform float uRoughness;
uniform float uMetallic;
uniform float uAlpha;
uniform int uPattern;
uniform float uPatternScale;
uniform sampler2D uShadowMap;

out vec4 FragColor;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise2(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float calcShadow(vec4 lightSpacePos, float ndotl) {
    vec3 proj = lightSpacePos.xyz / lightSpacePos.w;
    proj = proj * 0.5 + 0.5;
    if (proj.z > 1.0 || proj.x < 0.0 || proj.x > 1.0 || proj.y < 0.0 || proj.y > 1.0) return 0.0;
    float bias = max(0.00075 * (1.0 - ndotl), 0.00035);
    vec2 texelSize = 1.0 / vec2(textureSize(uShadowMap, 0));
    float shadow = 0.0;
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float pcfDepth = texture(uShadowMap, proj.xy + vec2(x, y) * texelSize * 1.35).r;
            shadow += (proj.z - bias > pcfDepth) ? 1.0 : 0.0;
        }
    }
    return shadow / 9.0;
}

vec3 toLinear(vec3 s) {
    return pow(max(s, vec3(0.0)), vec3(2.2));
}

vec3 acesToneMap(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    vec3 N = normalize(vWorldNorm);
    if (!gl_FrontFacing) N = -N;
    vec3 V = normalize(uCameraPos - vWorldPos);

    vec3 baseCol = uColor * vTint;
    float rough = uRoughness;
    float metal = uMetallic;
    vec3 extraEmissive = vec3(0.0);

    // 11 Procedural Material Patterns (zero external texture files)
    if (uPattern == 1) {
        // Architectural formwork concrete panels + subtle aggregate
        vec2 p = (abs(N.y) > 0.7) ? vWorldPos.xz : vec2(vWorldPos.x + vWorldPos.z, vWorldPos.y);
        p *= 0.42 * uPatternScale;
        vec2 cell = fract(p);
        float seam = smoothstep(0.035, 0.0, min(cell.x, cell.y)) + smoothstep(0.965, 1.0, max(cell.x, cell.y));
        float grain = noise2(p * 10.0) * 0.10 - 0.05;
        baseCol *= (0.94 + grain - seam * 0.16);
        rough = clamp(rough + seam * 0.12, 0.2, 0.98);
    } else if (uPattern == 2) {
        // CLT timber & glazed architectural WindowGrid facade with dusk/night warm interior glow
        if (abs(N.y) < 0.65) {
            float horizCoord = (abs(N.x) > abs(N.z)) ? vWorldPos.z : vWorldPos.x;
            vec2 wp = vec2(horizCoord * 0.72 * uPatternScale, vWorldPos.y * 0.62 * uPatternScale);
            vec2 cellId = floor(wp);
            vec2 cellUv = fract(wp);
            float inWinX = step(0.16, cellUv.x) * step(cellUv.x, 0.84);
            float inWinY = step(0.22, cellUv.y) * step(cellUv.y, 0.82);
            float isWin = inWinX * inWinY;
            if (isWin > 0.5) {
                float litRnd = hash21(cellId + vec2(3.7, 9.1));
                vec3 glassCol = mix(vec3(0.09, 0.16, 0.24), vec3(0.18, 0.28, 0.40), cellUv.y);
                baseCol = mix(glassCol, baseCol * 0.45, 0.18);
                rough = 0.14;
                metal = 0.55;
                float litMask = step(0.32, litRnd) * (0.14 + 0.86 * uNightWindowGlow);
                vec3 warmLit = mix(vec3(1.0, 0.76, 0.42), vec3(0.95, 0.88, 0.72), fract(litRnd * 17.3));
                extraEmissive += warmLit * litMask * (0.65 + 0.55 * litRnd);
            } else {
                float grain = noise2(wp * 4.5) * 0.08 - 0.04;
                baseCol *= (0.96 + grain);
            }
        }
    } else if (uPattern == 3) {
        // Monocrystalline Solar PV silicon cells + silver busbars
        vec2 p = vUv * vec2(6.0, 4.0) * uPatternScale;
        vec2 g = fract(p);
        float border = smoothstep(0.055, 0.0, min(g.x, g.y)) + smoothstep(0.945, 1.0, max(g.x, g.y));
        float busbar = smoothstep(0.025, 0.0, abs(g.x - 0.5));
        vec3 silicon = mix(vec3(0.05, 0.11, 0.26), vec3(0.09, 0.18, 0.38), noise2(floor(p) * 1.7));
        baseCol = mix(silicon * vTint, vec3(0.76, 0.84, 0.94), clamp(border * 0.72 + busbar * 0.38, 0.0, 1.0));
        rough = mix(0.16, 0.35, border);
        metal = mix(0.45, 0.88, clamp(border + busbar, 0.0, 1.0));
    } else if (uPattern == 4) {
        // Dublin Geological Cutaway Strata: Topsoil -> Boulder Clay -> Bedded Calp Limestone -> Granite Bedrock
        float depth = clamp((-vWorldPos.y - 0.35) / 19.65, 0.0, 1.0);
        float ang = atan(vWorldPos.z, vWorldPos.x);
        float wave = sin(ang * 6.0 + depth * 9.0) * 0.015 + cos(ang * 13.0) * 0.007;
        float d = clamp(depth + wave, 0.0, 1.0);
        float fine = noise2(vec2(ang * 22.0, d * 55.0));
        float med = noise2(vec2(ang * 9.0, d * 24.0));
        vec3 sCol;
        if (d < 0.095) {
            sCol = mix(vec3(0.22, 0.18, 0.14), vec3(0.16, 0.13, 0.10), fine);
        } else if (d < 0.33) {
            vec3 clay = mix(vec3(0.32, 0.27, 0.22), vec3(0.24, 0.21, 0.18), med);
            float cobble = smoothstep(0.76, 0.84, noise2(vec2(ang * 32.0, d * 48.0)));
            sCol = mix(clay, vec3(0.46, 0.44, 0.41), cobble * 0.65);
        } else if (d < 0.74) {
            float bed = sin(d * 135.0 + med * 4.0);
            vec3 calp = mix(vec3(0.11, 0.15, 0.21), vec3(0.22, 0.28, 0.36), smoothstep(-0.35, 0.45, bed));
            float joint = smoothstep(0.95, 0.99, abs(sin(ang * 34.0 + floor(d * 22.0) * 2.1)));
            sCol = mix(calp, vec3(0.06, 0.09, 0.13), joint * 0.55);
        } else {
            float speck = hash21(floor(vec2(ang * 140.0, d * 140.0)));
            sCol = mix(vec3(0.09, 0.13, 0.19), vec3(0.26, 0.32, 0.40), smoothstep(0.65, 0.95, speck) * 0.7);
        }
        // Dashed cyan Level -4 engineering datum line at y = -16m
        float datumDist = abs(vWorldPos.y - (-16.0));
        float dash = step(0.45, fract(ang * 18.0));
        float datumLine = smoothstep(0.16, 0.04, datumDist) * dash;
        baseCol = sCol * baseCol;
        extraEmissive += vec3(0.13, 0.83, 0.93) * datumLine * 0.55;
    } else if (uPattern == 5) {
        // 45-degree yellow/black industrial trench hazard striping
        float s = fract((vWorldPos.x + vWorldPos.z) * 1.15 * uPatternScale);
        float stripe = step(0.5, s);
        baseCol = mix(vec3(0.08, 0.09, 0.12), baseCol, stripe);
    } else if (uPattern == 6) {
        // AI server rack perforated grille + glowing cyan LED status rows
        vec2 p = vUv * vec2(1.0, 14.0) * uPatternScale;
        float row = fract(p.y);
        float rowId = floor(p.y);
        float ledX = step(0.12, vUv.x) * step(vUv.x, 0.88);
        float ledBand = smoothstep(0.28, 0.42, row) * smoothstep(0.72, 0.58, row) * ledX;
        float pulse = 0.65 + 0.35 * sin(uTime * 3.0 + rowId * 1.7 + vWorldPos.x);
        baseCol = mix(vec3(0.05, 0.08, 0.13), vec3(0.11, 0.16, 0.24), step(0.18, row) * 0.4);
        rough = 0.38;
        metal = 0.65;
        vec3 ledCol = (dot(uEmissive, uEmissive) > 0.001) ? uEmissive : vec3(0.13, 0.83, 0.93);
        extraEmissive += ledCol * ledBand * pulse * max(0.35, uEmissiveIntensity + vEmissiveBoost);
    } else if (uPattern == 7) {
        // Thermal store vertical stratification (45C teal -> 60C amber -> 75C orange-red)
        float t = clamp(vUv.y, 0.0, 1.0);
        vec3 cool = vec3(0.08, 0.55, 0.68);
        vec3 mid = vec3(0.95, 0.62, 0.16);
        vec3 hot = vec3(1.0, 0.34, 0.12);
        baseCol = (t < 0.5) ? mix(cool, mid, t * 2.0) : mix(mid, hot, (t - 0.5) * 2.0);
        baseCol *= vTint;
    } else if (uPattern == 8) {
        // Animated shimmering water caustic ripples
        vec2 p = vWorldPos.xz * 0.55 * uPatternScale;
        float w1 = sin(p.x * 2.4 + uTime * 1.6) * cos(p.y * 2.1 - uTime * 1.3);
        float w2 = sin((p.x + p.y) * 3.2 - uTime * 2.1);
        float caustic = smoothstep(0.35, 0.95, (w1 + w2) * 0.5 + 0.5);
        baseCol = mix(baseCol, baseCol * 1.35 + vec3(0.15, 0.32, 0.42), caustic * 0.55);
        rough = 0.12;
        metal = 0.25;
    } else if (uPattern == 9) {
        // Asphalt transit avenue with lane dashes & subtle autonomous cyan guideway
        float r = length(vWorldPos.xz);
        float ang = atan(vWorldPos.z, vWorldPos.x);
        float d16 = abs(r - 16.0);
        float d38 = abs(r - 38.0);
        float ringD = min(d16, d38);
        float dash = step(0.48, fract(ang * (d16 < d38 ? 24.0 : 48.0)));
        float centerLine = smoothstep(0.14, 0.04, ringD) * dash;
        float edgeGuide = smoothstep(0.12, 0.03, abs(ringD - 1.35));
        float grain = noise2(vWorldPos.xz * 2.5) * 0.06 - 0.03;
        baseCol = baseCol * (0.97 + grain) + vec3(0.45, 0.50, 0.58) * centerLine * 0.45;
        extraEmissive += vec3(0.08, 0.65, 0.82) * edgeGuide * 0.35;
    } else if (uPattern == 10) {
        // Architectural stone / masterplan plaza tiling
        vec2 p = vWorldPos.xz * 0.65 * uPatternScale;
        vec2 g = fract(p);
        float joint = smoothstep(0.04, 0.0, min(g.x, g.y));
        float checker = mod(floor(p.x) + floor(p.y), 2.0);
        float rad = length(vWorldPos.xz);
        float ringHint = smoothstep(0.14, 0.03, abs(fract(rad / 18.0) - 0.5) * 18.0) * 0.12;
        baseCol = baseCol * (0.94 + checker * 0.06 - joint * 0.10) + vec3(0.10, 0.24, 0.36) * ringHint;
    }

    // Decode authored sRGB pattern/albedo & lighting colors into physical linear space
    vec3 baseColLin = toLinear(baseCol);
    vec3 sunColLin = toLinear(uSunColor);
    vec3 hemiSkyLin = toLinear(uHemiSky);
    vec3 hemiGroundLin = toLinear(uHemiGround);
    vec3 fogLin = toLinear(uFogColor);

    // Direct Sun + 9-tap PCF Shadow
    vec3 L = normalize(uSunDir);
    float ndotl = max(dot(N, L), 0.0);
    float shadow = (ndotl > 0.001 && vWorldPos.y > -20.5) ? calcShadow(vLightPos, ndotl) : 0.0;
    float sunVis = (vWorldPos.y < -1.5) ? 0.30 : 1.0;

    vec3 H = normalize(L + V);
    float shininess = mix(112.0, 10.0, rough * rough);
    float specNorm = (shininess + 8.0) / 25.1327;
    float spec = pow(max(dot(N, H), 0.0), shininess) * specNorm * (1.0 - rough * 0.78);

    vec3 diffuseCol = baseColLin * (1.0 - metal * 0.85);
    vec3 f0 = mix(vec3(0.04), baseColLin, metal);
    float fresnel = pow(1.0 - max(dot(N, V), 0.0), 4.0);
    vec3 F = f0 + (vec3(1.0) - f0) * fresnel * (1.0 - rough * 0.65);

    vec3 sunLight = (diffuseCol * 0.54 + F * spec * 0.62) * sunColLin * uSunIntensity * ndotl * (1.0 - shadow * 0.74) * sunVis;

    // Opposing cool atmospheric rim/fill directional light
    vec3 Lfill = normalize(vec3(-0.65, 0.42, -0.62));
    float fillNdotL = max(dot(N, Lfill), 0.0);
    vec3 fillLight = diffuseCol * toLinear(vec3(0.45, 0.68, 0.98)) * 0.42 * fillNdotL;

    // Hemisphere Sky + Ground IBL Ambient & Specular Reflection
    float hemiMix = N.y * 0.5 + 0.5;
    vec3 ambientSky = mix(hemiGroundLin, hemiSkyLin, hemiMix);
    vec3 R = reflect(-V, N);
    float reflMix = clamp(R.y * 0.5 + 0.5, 0.0, 1.0);
    vec3 envRefl = mix(hemiGroundLin * 1.25, hemiSkyLin * 1.35 + vec3(0.025, 0.045, 0.08), reflMix);
    vec3 ibl = diffuseCol * ambientSky * uHemiIntensity * 1.15 + F * envRefl * (1.0 - rough * 0.80) * (0.55 + uHemiIntensity * 0.55);

    // 4 Subterranean Level -4 Cavern Accent Point Lights (linear-calibrated to avoid whiteout hotspots)
    vec3 cavern = vec3(0.0);
    if (vWorldPos.y < 1.5) {
        vec3 p0 = vec3(0.0, -10.5, 0.0) - vWorldPos;
        float d0 = length(p0);
        float att0 = 1.0 / (1.0 + 0.0020 * d0 * d0);
        cavern += (diffuseCol * 0.62 + F * 0.28) * toLinear(vec3(0.18, 0.85, 0.96)) * 1.55 * max(dot(N, p0 / max(d0, 0.01)), 0.14) * att0;

        vec3 p1 = vec3(0.0, -14.4, 0.0) - vWorldPos;
        float d1 = length(p1);
        float att1 = 1.0 / (1.0 + 0.0038 * d1 * d1);
        cavern += (diffuseCol * 0.58 + F * 0.22) * toLinear(vec3(1.0, 0.56, 0.32)) * 0.88 * max(dot(N, p1 / max(d1, 0.01)), 0.12) * att1;

        vec3 p2 = vec3(30.0, -11.8, 24.0) - vWorldPos;
        float d2 = length(p2);
        float att2 = 1.0 / (1.0 + 0.0026 * d2 * d2);
        cavern += (diffuseCol * 0.55 + F * 0.20) * toLinear(vec3(0.24, 0.76, 0.98)) * 0.85 * max(dot(N, p2 / max(d2, 0.01)), 0.16) * att2;

        vec3 p3 = vec3(-30.0, -11.8, -24.0) - vWorldPos;
        float d3 = length(p3);
        float att3 = 1.0 / (1.0 + 0.0026 * d3 * d3);
        cavern += (diffuseCol * 0.55 + F * 0.20) * toLinear(vec3(0.20, 0.85, 0.78)) * 0.78 * max(dot(N, p3 / max(d3, 0.01)), 0.16) * att3;
    }

    // Emissive contribution decoded to linear space (preserving HDR intensity for bloom)
    vec3 emColLin = (dot(uEmissive, uEmissive) > 0.0001) ? toLinear(uEmissive) : baseColLin;
    float totalEmInt = max(0.0, uEmissiveIntensity + vEmissiveBoost);
    float extraPeak = max(max(extraEmissive.r, extraEmissive.g), extraEmissive.b);
    vec3 extraEmLin = (extraPeak > 0.0001)
        ? (toLinear(extraEmissive / max(extraPeak, 1.0)) * max(extraPeak, 1.0))
        : vec3(0.0);
    vec3 emissiveTerm = (uPattern == 6) ? (extraEmLin * 1.15) : (emColLin * totalEmInt * 1.10 + extraEmLin);

    vec3 color = sunLight + fillLight + ibl + cavern + emissiveTerm;

    // Exponential atmospheric depth fog in linear space
    float dist = length(uCameraPos - vWorldPos);
    float fogFactor = 1.0 - exp(-pow(dist * uFogDensity, 2.0));
    color = mix(color, fogLin, clamp(fogFactor, 0.0, 0.65));

    if (uApplyTonemap == 1) {
        color = acesToneMap(color * 1.14);
        color = pow(color, vec3(1.0 / 2.2));
    }

    FragColor = vec4(color, uAlpha);
}
";
        }

        private const string ShadowVertexSingle = @"#version 330 core
layout(location = 0) in vec3 aPos;
uniform mat4 uLightSpace;
uniform mat4 uModel;
void main() {
    gl_Position = uLightSpace * uModel * vec4(aPos, 1.0);
}
";

        private const string ShadowVertexInstanced = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 3) in vec4 aInst0;
layout(location = 4) in vec4 aInst1;
layout(location = 5) in vec4 aInst2;
layout(location = 6) in vec4 aInst3;
uniform mat4 uLightSpace;
void main() {
    mat4 instModel = mat4(aInst0, aInst1, aInst2, aInst3);
    gl_Position = uLightSpace * instModel * vec4(aPos, 1.0);
}
";

        private const string ShadowFragment = @"#version 330 core
void main() {}
";

        private const string FullscreenVertex = @"#version 330 core
out vec2 vUv;
out vec2 vNdc;
void main() {
    vec2 pos = vec2((gl_VertexID == 1) ? 3.0 : -1.0, (gl_VertexID == 2) ? 3.0 : -1.0);
    vNdc = pos;
    vUv = pos * 0.5 + 0.5;
    gl_Position = vec4(pos, 0.0, 1.0);
}
";

        private const string SkyFragment = @"#version 330 core
in vec2 vUv;
in vec2 vNdc;

uniform mat4 uInvViewProj;
uniform vec3 uSunDir;
uniform vec3 uZenithColor;
uniform vec3 uMidSkyColor;
uniform vec3 uHorizonColor;
uniform vec3 uNadirColor;
uniform vec3 uSunGlowColor;
uniform float uSunIntensity;
uniform float uHorizonGlow;
uniform float uStarVisibility;
uniform float uCloudCover;
uniform float uTime;
uniform int uApplyTonemap;

out vec4 FragColor;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float hash31(vec3 p) {
    p = fract(p * vec3(443.897, 441.423, 437.195));
    p += dot(p, p.yzx + 19.19);
    return fract((p.x + p.y) * p.z);
}

float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    float sum = 0.0;
    float amp = 0.5;
    mat2 rot = mat2(0.80, -0.60, 0.60, 0.80);
    for (int i = 0; i < 4; i++) {
        sum += amp * valueNoise(p);
        p = rot * p * 2.04 + vec2(13.1, 7.7);
        amp *= 0.5;
    }
    return sum;
}

float starLayer(vec3 dir, float scale, float threshold) {
    vec3 cell = floor(dir * scale);
    vec3 local = fract(dir * scale) - 0.5;
    float rnd = hash31(cell);
    if (rnd < threshold) return 0.0;
    vec3 jitter = vec3(
        hash31(cell + 11.7) - 0.5,
        hash31(cell + 27.3) - 0.5,
        hash31(cell + 43.1) - 0.5
    ) * 0.58;
    float d = length(local - jitter);
    float star = smoothstep(0.16, 0.015, d);
    float twinkle = 0.72 + 0.28 * sin(uTime * (1.2 + rnd * 2.4) + rnd * 62.83);
    return star * twinkle * smoothstep(threshold, 1.0, rnd);
}

vec3 toLinear(vec3 s) {
    return pow(max(s, vec3(0.0)), vec3(2.2));
}

vec3 acesToneMap(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    vec4 nearP = uInvViewProj * vec4(vNdc, -1.0, 1.0);
    vec4 farP  = uInvViewProj * vec4(vNdc,  1.0, 1.0);
    vec3 dir = normalize(farP.xyz / farP.w - nearP.xyz / nearP.w);
    float y = dir.y;

    vec3 zenithLin = toLinear(uZenithColor);
    vec3 midLin = toLinear(uMidSkyColor);
    vec3 horizLin = toLinear(uHorizonColor);
    vec3 nadirLin = toLinear(uNadirColor);
    vec3 sunGlowLin = toLinear(uSunGlowColor);

    vec3 col;
    if (y >= 0.0) {
        float tMid = smoothstep(0.0, 0.26, y);
        float tZenith = smoothstep(0.16, 0.86, y);
        col = mix(horizLin, midLin, tMid);
        col = mix(col, zenithLin, tZenith);
    } else {
        float tDown = smoothstep(0.0, -0.55, y);
        col = mix(horizLin * 0.70, nadirLin, tDown);
    }

    float sunDot = max(dot(dir, normalize(uSunDir)), 0.0);
    float horizonBand = exp(-abs(y) * 7.5);
    float clearSky = 1.0 - uCloudCover * 0.72;

    float sunAzimuthGlow = pow(sunDot, 3.0) * horizonBand * uHorizonGlow * (0.45 + 0.55 * clearSky);
    col += sunGlowLin * sunAzimuthGlow * 0.55;

    if (y > -0.05 && uSunIntensity > 0.01) {
        float halo = pow(sunDot, 22.0) * 0.26 * uSunIntensity * clearSky;
        float disc = smoothstep(0.9985, 0.9996, sunDot) * 0.58 * uSunIntensity * clearSky;
        col += sunGlowLin * (halo + disc);
    }

    if (y > 0.01 && uCloudCover > 0.02) {
        vec2 skyUv = dir.xz / (y + 0.22);
        vec2 drift = vec2(uTime * 0.0085, uTime * 0.0032);
        float n = fbm(skyUv * 1.15 + drift);
        float density = smoothstep(0.66 - uCloudCover * 0.42, 0.92 - uCloudCover * 0.22, n);
        float horizonFade = smoothstep(0.01, 0.22, y);
        vec3 cloudLit = mix(midLin * 1.25, sunGlowLin * 0.65 + horizLin * 0.50, pow(sunDot, 2.5) * 0.45 + uHorizonGlow * 0.25);
        vec3 cloudShade = mix(zenithLin * 0.95, horizLin * 0.55, 0.45);
        vec3 cloudCol = mix(cloudShade, cloudLit, smoothstep(0.25, 0.85, n) * (0.35 + 0.65 * uSunIntensity));
        col = mix(col, cloudCol, density * horizonFade * clamp(uCloudCover * 0.82, 0.0, 0.78));
    }

    if (y > 0.02 && uStarVisibility > 0.01) {
        float starMask = smoothstep(0.03, 0.28, y) * uStarVisibility * (1.0 - uCloudCover * 0.85);
        float s1 = starLayer(dir, 145.0, 0.968);
        float s2 = starLayer(dir + vec3(0.17, 0.31, 0.09), 235.0, 0.982) * 0.72;
        vec3 starTint = toLinear(mix(vec3(0.75, 0.86, 1.0), vec3(1.0, 0.92, 0.80), hash31(floor(dir * 145.0) + 5.1)));
        col += starTint * (s1 + s2) * starMask * 0.95;
    }

    if (uApplyTonemap == 1) {
        col = acesToneMap(col * 1.14);
        col = pow(col, vec3(1.0 / 2.2));
    }

    FragColor = vec4(col, 1.0);
}
";

        private const string LineVertex = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;
uniform mat4 uViewProj;
out vec4 vColor;
void main() {
    vColor = aColor;
    gl_Position = uViewProj * vec4(aPos, 1.0);
}
";

        private const string LineFragment = @"#version 330 core
in vec4 vColor;
uniform int uApplyTonemap;
out vec4 FragColor;

vec3 toLinear(vec3 s) {
    return pow(max(s, vec3(0.0)), vec3(2.2));
}

vec3 acesToneMap(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    float peak = max(max(vColor.r, vColor.g), vColor.b);
    vec3 c = (peak > 1.0)
        ? (pow(max(vColor.rgb / peak, vec3(0.0)), vec3(2.2)) * peak)
        : pow(max(vColor.rgb, vec3(0.0)), vec3(2.2));
    if (uApplyTonemap == 1) {
        c = pow(acesToneMap(c * 1.14), vec3(1.0 / 2.2));
    }
    FragColor = vec4(c, vColor.a);
}
";

        private const string BrightFragment = @"#version 330 core
in vec2 vUv;
uniform sampler2D uScene;
uniform float uThreshold;
out vec4 FragColor;
void main() {
    vec3 c = texture(uScene, vUv).rgb;
    float lum = max(max(c.r, max(c.g, c.b)), dot(c, vec3(0.2126, 0.7152, 0.0722)));
    float knee = 0.18;
    float soft = clamp((lum - uThreshold + knee) / (2.0 * knee), 0.0, 1.0);
    soft = soft * soft * knee;
    float contrib = max(lum - uThreshold, soft) / max(lum, 0.0001);
    FragColor = vec4(c * clamp(contrib, 0.0, 2.5), 1.0);
}
";

        private const string BlurFragment = @"#version 330 core
in vec2 vUv;
uniform sampler2D uImage;
uniform int uHorizontal;
out vec4 FragColor;
void main() {
    const float w0 = 0.227027;
    const float w1 = 0.1945946;
    const float w2 = 0.1216216;
    const float w3 = 0.054054;
    const float w4 = 0.016216;
    vec2 texOffset = 1.0 / vec2(textureSize(uImage, 0));
    vec2 dir = (uHorizontal == 1) ? vec2(texOffset.x, 0.0) : vec2(0.0, texOffset.y);
    vec3 res = texture(uImage, vUv).rgb * w0;
    res += texture(uImage, vUv + dir * 1.0).rgb * w1;
    res += texture(uImage, vUv - dir * 1.0).rgb * w1;
    res += texture(uImage, vUv + dir * 2.0).rgb * w2;
    res += texture(uImage, vUv - dir * 2.0).rgb * w2;
    res += texture(uImage, vUv + dir * 3.0).rgb * w3;
    res += texture(uImage, vUv - dir * 3.0).rgb * w3;
    res += texture(uImage, vUv + dir * 4.0).rgb * w4;
    res += texture(uImage, vUv - dir * 4.0).rgb * w4;
    FragColor = vec4(res, 1.0);
}
";

        private const string CompositeFragment = @"#version 330 core
in vec2 vUv;
uniform sampler2D uScene;
uniform sampler2D uBloom;
uniform float uBloomStrength;
uniform float uExposure;
out vec4 FragColor;

vec3 toLinear(vec3 s) {
    return pow(max(s, vec3(0.0)), vec3(2.2));
}

vec3 acesToneMap(vec3 x) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    vec3 hdr = texture(uScene, vUv).rgb + texture(uBloom, vUv).rgb * uBloomStrength;
    vec3 mapped = acesToneMap(hdr * uExposure);
    vec3 srgb = pow(mapped, vec3(1.0 / 2.2));
    FragColor = vec4(srgb, 1.0);
}
";
    }
}
