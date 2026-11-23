using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RakdaSim
{
    /// <summary>
    /// Zero-dependency Win32 / WGL / OpenGL 3.3 Core Profile loader using built-in
    /// opengl32.dll, gdi32.dll, and user32.dll. Strictly C# 5 compatible.
    /// </summary>
    public static unsafe class GlNative
    {
        // OpenGL Constants
        public const uint GL_FALSE = 0;
        public const uint GL_TRUE = 1;
        public const uint GL_NO_ERROR = 0;

        public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;

        public const uint GL_LINES = 0x0001;
        public const uint GL_TRIANGLES = 0x0004;

        public const uint GL_LEQUAL = 0x0203;
        public const uint GL_LESS = 0x0201;

        public const uint GL_SRC_ALPHA = 0x0302;
        public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const uint GL_ONE = 1;
        public const uint GL_ZERO = 0;

        public const uint GL_FRONT = 0x0404;
        public const uint GL_BACK = 0x0405;
        public const uint GL_NONE = 0;

        public const uint GL_CULL_FACE = 0x0B44;
        public const uint GL_DEPTH_TEST = 0x0B71;
        public const uint GL_BLEND = 0x0BE2;
        public const uint GL_MULTISAMPLE = 0x809D;
        public const uint GL_POLYGON_OFFSET_FILL = 0x8037;

        public const uint GL_PACK_ALIGNMENT = 0x0D05;
        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_UNSIGNED_INT = 0x1405;
        public const uint GL_FLOAT = 0x1406;

        public const uint GL_DEPTH_COMPONENT = 0x1902;
        public const uint GL_RGB = 0x1907;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_BGR = 0x80E0;

        public const uint GL_RGBA8 = 0x8058;
        public const uint GL_RGBA16F = 0x881A;
        public const uint GL_DEPTH_COMPONENT24 = 0x81A6;

        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_WRAP_S = 0x2802;
        public const uint GL_TEXTURE_WRAP_T = 0x2803;
        public const uint GL_TEXTURE_BORDER_COLOR = 0x1004;
        public const int GL_NEAREST = 0x2600;
        public const int GL_LINEAR = 0x2601;
        public const int GL_CLAMP_TO_EDGE = 0x812F;
        public const int GL_CLAMP_TO_BORDER = 0x812D;

        public const uint GL_TEXTURE0 = 0x84C0;
        public const uint GL_TEXTURE1 = 0x84C1;
        public const uint GL_TEXTURE2 = 0x84C2;

        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_DYNAMIC_DRAW = 0x88E8;

        public const uint GL_FRAGMENT_SHADER = 0x8B30;
        public const uint GL_VERTEX_SHADER = 0x8B31;
        public const uint GL_COMPILE_STATUS = 0x8B81;
        public const uint GL_LINK_STATUS = 0x8B82;

        public const uint GL_FRAMEBUFFER = 0x8D40;
        public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
        public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
        public const uint GL_RENDERBUFFER = 0x8D41;
        public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint GL_DEPTH_ATTACHMENT = 0x8D00;
        public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const uint GL_MAX_SAMPLES = 0x8D57;

        public const uint GL_VENDOR = 0x1F00;
        public const uint GL_RENDERER = 0x1F01;
        public const uint GL_VERSION = 0x1F02;

        // WGL ARB Constants
        private const int WGL_DRAW_TO_WINDOW_ARB = 0x2001;
        private const int WGL_SUPPORT_OPENGL_ARB = 0x2010;
        private const int WGL_DOUBLE_BUFFER_ARB = 0x2011;
        private const int WGL_PIXEL_TYPE_ARB = 0x2013;
        private const int WGL_TYPE_RGBA_ARB = 0x202B;
        private const int WGL_COLOR_BITS_ARB = 0x2014;
        private const int WGL_DEPTH_BITS_ARB = 0x2022;
        private const int WGL_STENCIL_BITS_ARB = 0x2023;
        private const int WGL_ACCELERATION_ARB = 0x2003;
        private const int WGL_FULL_ACCELERATION_ARB = 0x2027;
        private const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        private const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
        private const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
        private const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;

        private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        private const uint PFD_SUPPORT_OPENGL = 0x00000020;
        private const uint PFD_DOUBLEBUFFER = 0x00000001;
        private const byte PFD_TYPE_RGBA = 0;
        private const byte PFD_MAIN_PLANE = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        // Win32 / GDI / WGL P/Invokes
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int ChoosePixelFormat(IntPtr hdc, [In] ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int DescribePixelFormat(IntPtr hdc, int iPixelFormat, uint nBytes, ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, [In] ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool SwapBuffers(IntPtr hdc);

        [DllImport("opengl32.dll", SetLastError = true)]
        public static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("opengl32.dll", SetLastError = true)]
        public static extern bool wglDeleteContext(IntPtr hglrc);

        [DllImport("opengl32.dll", SetLastError = true)]
        public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

        [DllImport("opengl32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr wglGetProcAddress(string lpszProc);

        // OpenGL 1.1 core built-ins from opengl32.dll
        [DllImport("opengl32.dll")] public static extern void glViewport(int x, int y, int width, int height);
        [DllImport("opengl32.dll")] public static extern void glClearColor(float r, float g, float b, float a);
        [DllImport("opengl32.dll")] public static extern void glClear(uint mask);
        [DllImport("opengl32.dll")] public static extern void glEnable(uint cap);
        [DllImport("opengl32.dll")] public static extern void glDisable(uint cap);
        [DllImport("opengl32.dll")] public static extern void glDepthFunc(uint func);
        [DllImport("opengl32.dll")] public static extern void glDepthMask(byte flag);
        [DllImport("opengl32.dll")] public static extern void glBlendFunc(uint sfactor, uint dfactor);
        [DllImport("opengl32.dll")] public static extern void glCullFace(uint mode);
        [DllImport("opengl32.dll")] public static extern void glPolygonOffset(float factor, float units);
        [DllImport("opengl32.dll")] public static extern void glDrawArrays(uint mode, int first, int count);
        [DllImport("opengl32.dll")] public static extern void glDrawElements(uint mode, int count, uint type, IntPtr indices);
        [DllImport("opengl32.dll")] public static extern void glGenTextures(int n, uint* textures);
        [DllImport("opengl32.dll")] public static extern void glBindTexture(uint target, uint texture);
        [DllImport("opengl32.dll")] public static extern void glDeleteTextures(int n, uint* textures);
        [DllImport("opengl32.dll")] public static extern void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")] public static extern void glTexParameteri(uint target, uint pname, int param);
        [DllImport("opengl32.dll")] public static extern void glTexParameterfv(uint target, uint pname, float* param);
        [DllImport("opengl32.dll")] public static extern void glDrawBuffer(uint mode);
        [DllImport("opengl32.dll")] public static extern void glReadBuffer(uint mode);
        [DllImport("opengl32.dll")] public static extern void glPixelStorei(uint pname, int param);
        [DllImport("opengl32.dll")] public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")] public static extern void glGetIntegerv(uint pname, out int data);
        [DllImport("opengl32.dll")] public static extern uint glGetError();
        [DllImport("opengl32.dll")] public static extern void glLineWidth(float width);
        [DllImport("opengl32.dll")] public static extern void glFlush();
        [DllImport("opengl32.dll")] public static extern void glFinish();
        [DllImport("opengl32.dll")] private static extern IntPtr glGetString(uint name);

        public static string GetString(uint name)
        {
            IntPtr p = glGetString(name);
            if (p == IntPtr.Zero) return "Unknown OpenGL";
            return Marshal.PtrToStringAnsi(p);
        }

        // WGL Extension Delegates
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool WglChoosePixelFormatARBDelegate(IntPtr hdc, int[] piAttribIList, float[] pfAttribFList, uint nMaxFormats, out int piFormats, out uint nNumFormats);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr WglCreateContextAttribsARBDelegate(IntPtr hdc, IntPtr hShareContext, int[] attribList);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool WglSwapIntervalEXTDelegate(int interval);

        // OpenGL 3.3 Core Function Delegates
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGenVertexArraysDel(int n, uint* arrays);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindVertexArrayDel(uint array);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteVertexArraysDel(int n, uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGenBuffersDel(int n, uint* buffers);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindBufferDel(uint target, uint buffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBufferDataDel(uint target, IntPtr size, IntPtr data, uint usage);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBufferSubDataDel(uint target, IntPtr offset, IntPtr size, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteBuffersDel(int n, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlEnableVertexAttribArrayDel(uint index);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlVertexAttribPointerDel(uint index, int size, uint type, byte normalized, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlVertexAttribDivisorDel(uint index, uint divisor);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDrawElementsInstancedDel(uint mode, int count, uint type, IntPtr indices, int instanceCount);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCreateShaderDel(uint type);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlShaderSourceDel(uint shader, int count, string[] source, int[] length);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlCompileShaderDel(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetShaderivDel(uint shader, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetShaderInfoLogDel(uint shader, int maxLength, out int length, StringBuilder infoLog);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteShaderDel(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCreateProgramDel();
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlAttachShaderDel(uint program, uint shader);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlLinkProgramDel(uint program);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetProgramivDel(uint program, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetProgramInfoLogDel(uint program, int maxLength, out int length, StringBuilder infoLog);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUseProgramDel(uint program);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteProgramDel(uint program);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GlGetUniformLocationDel(uint program, string name);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform1iDel(int location, int v0);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform1fDel(int location, float v0);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform2fDel(int location, float v0, float v1);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform3fDel(int location, float v0, float v1, float v2);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform4fDel(int location, float v0, float v1, float v2, float v3);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniformMatrix4fvDel(int location, int count, byte transpose, float* value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlActiveTextureDel(uint texture);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGenFramebuffersDel(int n, uint* ids);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindFramebufferDel(uint target, uint framebuffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteFramebuffersDel(int n, uint* ids);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlFramebufferTexture2DDel(uint target, uint attachment, uint textarget, uint texture, int level);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlFramebufferRenderbufferDel(uint target, uint attachment, uint renderbuffertarget, uint renderbuffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCheckFramebufferStatusDel(uint target);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBlitFramebufferDel(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGenRenderbuffersDel(int n, uint* ids);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindRenderbufferDel(uint target, uint renderbuffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteRenderbuffersDel(int n, uint* ids);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlRenderbufferStorageDel(uint target, uint internalformat, int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlRenderbufferStorageMultisampleDel(uint target, int samples, uint internalformat, int width, int height);

        // Loaded GL 3.3 Core Entry Points
        public static GlGenVertexArraysDel glGenVertexArrays;
        public static GlBindVertexArrayDel glBindVertexArray;
        public static GlDeleteVertexArraysDel glDeleteVertexArrays;

        public static GlGenBuffersDel glGenBuffers;
        public static GlBindBufferDel glBindBuffer;
        public static GlBufferDataDel glBufferData;
        public static GlBufferSubDataDel glBufferSubData;
        public static GlDeleteBuffersDel glDeleteBuffers;

        public static GlEnableVertexAttribArrayDel glEnableVertexAttribArray;
        public static GlVertexAttribPointerDel glVertexAttribPointer;
        public static GlVertexAttribDivisorDel glVertexAttribDivisor;
        public static GlDrawElementsInstancedDel glDrawElementsInstanced;

        public static GlCreateShaderDel glCreateShader;
        public static GlShaderSourceDel glShaderSource;
        public static GlCompileShaderDel glCompileShader;
        public static GlGetShaderivDel glGetShaderiv;
        public static GlGetShaderInfoLogDel glGetShaderInfoLog;
        public static GlDeleteShaderDel glDeleteShader;

        public static GlCreateProgramDel glCreateProgram;
        public static GlAttachShaderDel glAttachShader;
        public static GlLinkProgramDel glLinkProgram;
        public static GlGetProgramivDel glGetProgramiv;
        public static GlGetProgramInfoLogDel glGetProgramInfoLog;
        public static GlUseProgramDel glUseProgram;
        public static GlDeleteProgramDel glDeleteProgram;

        public static GlGetUniformLocationDel glGetUniformLocation;
        public static GlUniform1iDel glUniform1i;
        public static GlUniform1fDel glUniform1f;
        public static GlUniform2fDel glUniform2f;
        public static GlUniform3fDel glUniform3f;
        public static GlUniform4fDel glUniform4f;
        public static GlUniformMatrix4fvDel glUniformMatrix4fv;
        public static GlActiveTextureDel glActiveTexture;

        public static GlGenFramebuffersDel glGenFramebuffers;
        public static GlBindFramebufferDel glBindFramebuffer;
        public static GlDeleteFramebuffersDel glDeleteFramebuffers;
        public static GlFramebufferTexture2DDel glFramebufferTexture2D;
        public static GlFramebufferRenderbufferDel glFramebufferRenderbuffer;
        public static GlCheckFramebufferStatusDel glCheckFramebufferStatus;
        public static GlBlitFramebufferDel glBlitFramebuffer;

        public static GlGenRenderbuffersDel glGenRenderbuffers;
        public static GlBindRenderbufferDel glBindRenderbuffer;
        public static GlDeleteRenderbuffersDel glDeleteRenderbuffers;
        public static GlRenderbufferStorageDel glRenderbufferStorage;
        public static GlRenderbufferStorageMultisampleDel glRenderbufferStorageMultisample;

        private static T LoadProc<T>(string name) where T : class
        {
            IntPtr p = wglGetProcAddress(name);
            if (p == IntPtr.Zero || p == new IntPtr(1) || p == new IntPtr(2) || p == new IntPtr(3) || p == new IntPtr(-1))
            {
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(p, typeof(T)) as T;
        }

        private static T LoadRequired<T>(string name) where T : class
        {
            T fn = LoadProc<T>(name);
            if (fn == null)
            {
                throw new InvalidOperationException("Required OpenGL 3.3 function missing: " + name);
            }
            return fn;
        }

        private static PIXELFORMATDESCRIPTOR MakeStandardPfd()
        {
            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
            pfd.nSize = (ushort)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR));
            pfd.nVersion = 1;
            pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
            pfd.iPixelType = PFD_TYPE_RGBA;
            pfd.cColorBits = 32;
            pfd.cAlphaBits = 8;
            pfd.cDepthBits = 24;
            pfd.cStencilBits = 8;
            pfd.iLayerType = PFD_MAIN_PLANE;
            return pfd;
        }

        /// <summary>
        /// Creates a hardware-accelerated OpenGL 3.3 Core Profile context on targetHwnd,
        /// makes it current, disables vsync for native performance, and loads all GL 3.3 entry points.
        /// </summary>
        public static void CreateCoreContext(IntPtr targetHwnd, out IntPtr hdc, out IntPtr hglrc)
        {
            WglChoosePixelFormatARBDelegate wglChoosePixelFormatARB = null;
            WglCreateContextAttribsARBDelegate wglCreateContextAttribsARB = null;
            WglSwapIntervalEXTDelegate wglSwapIntervalEXT = null;

            // Step 1: Bootstrap WGL extensions via a temporary hidden window
            IntPtr dummyHwnd = CreateWindowEx(0, "STATIC", "", 0x80000000u, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (dummyHwnd != IntPtr.Zero)
            {
                IntPtr dummyDc = GetDC(dummyHwnd);
                if (dummyDc != IntPtr.Zero)
                {
                    PIXELFORMATDESCRIPTOR dummyPfd = MakeStandardPfd();
                    int dummyPf = ChoosePixelFormat(dummyDc, ref dummyPfd);
                    if (dummyPf != 0 && SetPixelFormat(dummyDc, dummyPf, ref dummyPfd))
                    {
                        IntPtr dummyGl = wglCreateContext(dummyDc);
                        if (dummyGl != IntPtr.Zero)
                        {
                            if (wglMakeCurrent(dummyDc, dummyGl))
                            {
                                wglChoosePixelFormatARB = LoadProc<WglChoosePixelFormatARBDelegate>("wglChoosePixelFormatARB");
                                wglCreateContextAttribsARB = LoadProc<WglCreateContextAttribsARBDelegate>("wglCreateContextAttribsARB");
                                wglSwapIntervalEXT = LoadProc<WglSwapIntervalEXTDelegate>("wglSwapIntervalEXT");
                                wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                            }
                            wglDeleteContext(dummyGl);
                        }
                    }
                    ReleaseDC(dummyHwnd, dummyDc);
                }
                DestroyWindow(dummyHwnd);
            }

            // Step 2: Configure pixel format on target window DC
            hdc = GetDC(targetHwnd);
            if (hdc == IntPtr.Zero)
            {
                throw new InvalidOperationException("GetDC failed for OpenGL viewport handle.");
            }

            PIXELFORMATDESCRIPTOR pfd = MakeStandardPfd();
            int pixelFormat = 0;

            if (wglChoosePixelFormatARB != null)
            {
                int[] attribs = new int[]
                {
                    WGL_DRAW_TO_WINDOW_ARB, 1,
                    WGL_SUPPORT_OPENGL_ARB, 1,
                    WGL_DOUBLE_BUFFER_ARB, 1,
                    WGL_ACCELERATION_ARB, WGL_FULL_ACCELERATION_ARB,
                    WGL_PIXEL_TYPE_ARB, WGL_TYPE_RGBA_ARB,
                    WGL_COLOR_BITS_ARB, 32,
                    WGL_DEPTH_BITS_ARB, 24,
                    WGL_STENCIL_BITS_ARB, 8,
                    0
                };
                uint numFormats;
                if (wglChoosePixelFormatARB(hdc, attribs, null, 1, out pixelFormat, out numFormats) && numFormats > 0)
                {
                    DescribePixelFormat(hdc, pixelFormat, (uint)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR)), ref pfd);
                }
                else
                {
                    pixelFormat = 0;
                }
            }

            if (pixelFormat == 0)
            {
                pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            }
            if (pixelFormat == 0 || !SetPixelFormat(hdc, pixelFormat, ref pfd))
            {
                ReleaseDC(targetHwnd, hdc);
                hdc = IntPtr.Zero;
                throw new InvalidOperationException("Failed to set OpenGL pixel format on viewport.");
            }

            // Step 3: Create OpenGL 3.3 Core Profile context
            hglrc = IntPtr.Zero;
            if (wglCreateContextAttribsARB != null)
            {
                int[] ctxAttribs = new int[]
                {
                    WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
                    WGL_CONTEXT_MINOR_VERSION_ARB, 3,
                    WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
                    0
                };
                hglrc = wglCreateContextAttribsARB(hdc, IntPtr.Zero, ctxAttribs);
            }
            if (hglrc == IntPtr.Zero)
            {
                hglrc = wglCreateContext(hdc);
            }
            if (hglrc == IntPtr.Zero || !wglMakeCurrent(hdc, hglrc))
            {
                if (hglrc != IntPtr.Zero) wglDeleteContext(hglrc);
                ReleaseDC(targetHwnd, hdc);
                hdc = IntPtr.Zero;
                hglrc = IntPtr.Zero;
                throw new InvalidOperationException("Failed to create and activate OpenGL 3.3 Core context.");
            }

            // Disable vsync for crisp low-latency native FPS
            if (wglSwapIntervalEXT != null)
            {
                try { wglSwapIntervalEXT(0); } catch { }
            }

            // Step 4: Load OpenGL 3.3 Core entry points
            LoadCoreFunctions();
        }

        private static void LoadCoreFunctions()
        {
            glGenVertexArrays = LoadRequired<GlGenVertexArraysDel>("glGenVertexArrays");
            glBindVertexArray = LoadRequired<GlBindVertexArrayDel>("glBindVertexArray");
            glDeleteVertexArrays = LoadRequired<GlDeleteVertexArraysDel>("glDeleteVertexArrays");

            glGenBuffers = LoadRequired<GlGenBuffersDel>("glGenBuffers");
            glBindBuffer = LoadRequired<GlBindBufferDel>("glBindBuffer");
            glBufferData = LoadRequired<GlBufferDataDel>("glBufferData");
            glBufferSubData = LoadRequired<GlBufferSubDataDel>("glBufferSubData");
            glDeleteBuffers = LoadRequired<GlDeleteBuffersDel>("glDeleteBuffers");

            glEnableVertexAttribArray = LoadRequired<GlEnableVertexAttribArrayDel>("glEnableVertexAttribArray");
            glVertexAttribPointer = LoadRequired<GlVertexAttribPointerDel>("glVertexAttribPointer");
            glVertexAttribDivisor = LoadRequired<GlVertexAttribDivisorDel>("glVertexAttribDivisor");
            glDrawElementsInstanced = LoadRequired<GlDrawElementsInstancedDel>("glDrawElementsInstanced");

            glCreateShader = LoadRequired<GlCreateShaderDel>("glCreateShader");
            glShaderSource = LoadRequired<GlShaderSourceDel>("glShaderSource");
            glCompileShader = LoadRequired<GlCompileShaderDel>("glCompileShader");
            glGetShaderiv = LoadRequired<GlGetShaderivDel>("glGetShaderiv");
            glGetShaderInfoLog = LoadRequired<GlGetShaderInfoLogDel>("glGetShaderInfoLog");
            glDeleteShader = LoadRequired<GlDeleteShaderDel>("glDeleteShader");

            glCreateProgram = LoadRequired<GlCreateProgramDel>("glCreateProgram");
            glAttachShader = LoadRequired<GlAttachShaderDel>("glAttachShader");
            glLinkProgram = LoadRequired<GlLinkProgramDel>("glLinkProgram");
            glGetProgramiv = LoadRequired<GlGetProgramivDel>("glGetProgramiv");
            glGetProgramInfoLog = LoadRequired<GlGetProgramInfoLogDel>("glGetProgramInfoLog");
            glUseProgram = LoadRequired<GlUseProgramDel>("glUseProgram");
            glDeleteProgram = LoadRequired<GlDeleteProgramDel>("glDeleteProgram");

            glGetUniformLocation = LoadRequired<GlGetUniformLocationDel>("glGetUniformLocation");
            glUniform1i = LoadRequired<GlUniform1iDel>("glUniform1i");
            glUniform1f = LoadRequired<GlUniform1fDel>("glUniform1f");
            glUniform2f = LoadRequired<GlUniform2fDel>("glUniform2f");
            glUniform3f = LoadRequired<GlUniform3fDel>("glUniform3f");
            glUniform4f = LoadRequired<GlUniform4fDel>("glUniform4f");
            glUniformMatrix4fv = LoadRequired<GlUniformMatrix4fvDel>("glUniformMatrix4fv");
            glActiveTexture = LoadRequired<GlActiveTextureDel>("glActiveTexture");

            glGenFramebuffers = LoadProc<GlGenFramebuffersDel>("glGenFramebuffers");
            glBindFramebuffer = LoadProc<GlBindFramebufferDel>("glBindFramebuffer");
            glDeleteFramebuffers = LoadProc<GlDeleteFramebuffersDel>("glDeleteFramebuffers");
            glFramebufferTexture2D = LoadProc<GlFramebufferTexture2DDel>("glFramebufferTexture2D");
            glFramebufferRenderbuffer = LoadProc<GlFramebufferRenderbufferDel>("glFramebufferRenderbuffer");
            glCheckFramebufferStatus = LoadProc<GlCheckFramebufferStatusDel>("glCheckFramebufferStatus");
            glBlitFramebuffer = LoadProc<GlBlitFramebufferDel>("glBlitFramebuffer");

            glGenRenderbuffers = LoadProc<GlGenRenderbuffersDel>("glGenRenderbuffers");
            glBindRenderbuffer = LoadProc<GlBindRenderbufferDel>("glBindRenderbuffer");
            glDeleteRenderbuffers = LoadProc<GlDeleteRenderbuffersDel>("glDeleteRenderbuffers");
            glRenderbufferStorage = LoadProc<GlRenderbufferStorageDel>("glRenderbufferStorage");
            glRenderbufferStorageMultisample = LoadProc<GlRenderbufferStorageMultisampleDel>("glRenderbufferStorageMultisample");
        }
    }
}
