using System;
using System.Collections.Generic;

namespace RakdaSim
{
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 One = new Vec3(1f, 1f, 1f);
        public static readonly Vec3 UnitX = new Vec3(1f, 0f, 0f);
        public static readonly Vec3 UnitY = new Vec3(0f, 1f, 0f);
        public static readonly Vec3 UnitZ = new Vec3(0f, 0f, 1f);

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 FromHex(int hex)
        {
            return new Vec3(
                ((hex >> 16) & 255) / 255f,
                ((hex >> 8) & 255) / 255f,
                (hex & 255) / 255f);
        }

        public float LengthSq()
        {
            return X * X + Y * Y + Z * Z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public Vec3 Normalized()
        {
            float len = Length();
            if (len < 1e-7f) return Zero;
            float inv = 1f / len;
            return new Vec3(X * inv, Y * inv, Z * inv);
        }

        public static Vec3 Add(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 Sub(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 Scale(Vec3 a, float s)
        {
            return new Vec3(a.X * s, a.Y * s, a.Z * s);
        }

        public static float Dot(Vec3 a, Vec3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public static float Length(Vec3 a)
        {
            return a.Length();
        }

        public static Vec3 Normalized(Vec3 a)
        {
            return a.Normalized();
        }

        public static Vec3 Lerp(Vec3 a, Vec3 b, float t)
        {
            return new Vec3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t);
        }

        public static float Distance(Vec3 a, Vec3 b)
        {
            return Sub(a, b).Length();
        }

        public static Vec3 Min(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.X < b.X ? a.X : b.X,
                a.Y < b.Y ? a.Y : b.Y,
                a.Z < b.Z ? a.Z : b.Z);
        }

        public static Vec3 Max(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.X > b.X ? a.X : b.X,
                a.Y > b.Y ? a.Y : b.Y,
                a.Z > b.Z ? a.Z : b.Z);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator -(Vec3 a)
        {
            return new Vec3(-a.X, -a.Y, -a.Z);
        }

        public static Vec3 operator *(Vec3 a, float s)
        {
            return new Vec3(a.X * s, a.Y * s, a.Z * s);
        }

        public static Vec3 operator *(float s, Vec3 a)
        {
            return new Vec3(a.X * s, a.Y * s, a.Z * s);
        }

        public static Vec3 operator /(Vec3 a, float s)
        {
            float inv = 1f / s;
            return new Vec3(a.X * inv, a.Y * inv, a.Z * inv);
        }
    }

    public struct Vec4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public static readonly Vec4 Zero = new Vec4(0f, 0f, 0f, 0f);
        public static readonly Vec4 One = new Vec4(1f, 1f, 1f, 1f);

        public Vec4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vec4(Vec3 v, float w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = w;
        }
    }

    public struct Mat4
    {
        // Column-major 4x4 matrix (OpenGL layout): element (row, col) = M[col * 4 + row]
        public float[] M;

        public Mat4(float[] m)
        {
            M = m;
        }

        public static Mat4 Identity()
        {
            float[] m = new float[16];
            m[0] = 1f;
            m[5] = 1f;
            m[10] = 1f;
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 Translation(float x, float y, float z)
        {
            Mat4 r = Identity();
            r.M[12] = x;
            r.M[13] = y;
            r.M[14] = z;
            return r;
        }

        public static Mat4 Translation(Vec3 v)
        {
            return Translation(v.X, v.Y, v.Z);
        }

        public static Mat4 Scale(float x, float y, float z)
        {
            float[] m = new float[16];
            m[0] = x;
            m[5] = y;
            m[10] = z;
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 Scale(Vec3 v)
        {
            return Scale(v.X, v.Y, v.Z);
        }

        public static Mat4 Scale(float s)
        {
            return Scale(s, s, s);
        }

        public static Mat4 RotationX(float rad)
        {
            float c = (float)Math.Cos(rad);
            float s = (float)Math.Sin(rad);
            Mat4 r = Identity();
            r.M[5] = c;
            r.M[6] = s;
            r.M[9] = -s;
            r.M[10] = c;
            return r;
        }

        public static Mat4 RotationY(float rad)
        {
            float c = (float)Math.Cos(rad);
            float s = (float)Math.Sin(rad);
            Mat4 r = Identity();
            r.M[0] = c;
            r.M[2] = -s;
            r.M[8] = s;
            r.M[10] = c;
            return r;
        }

        public static Mat4 RotationZ(float rad)
        {
            float c = (float)Math.Cos(rad);
            float s = (float)Math.Sin(rad);
            Mat4 r = Identity();
            r.M[0] = c;
            r.M[1] = s;
            r.M[4] = -s;
            r.M[5] = c;
            return r;
        }

        public static Mat4 TRS(Vec3 pos, Vec3 rotEulerRad, Vec3 scale)
        {
            // T * Ry * Rx * Rz * S
            float cx = (float)Math.Cos(rotEulerRad.X);
            float sx = (float)Math.Sin(rotEulerRad.X);
            float cy = (float)Math.Cos(rotEulerRad.Y);
            float sy = (float)Math.Sin(rotEulerRad.Y);
            float cz = (float)Math.Cos(rotEulerRad.Z);
            float sz = (float)Math.Sin(rotEulerRad.Z);

            float r00 = cy * cz + sy * sx * sz;
            float r10 = cx * sz;
            float r20 = -sy * cz + cy * sx * sz;

            float r01 = -cy * sz + sy * sx * cz;
            float r11 = cx * cz;
            float r21 = sy * sz + cy * sx * cz;

            float r02 = sy * cx;
            float r12 = -sx;
            float r22 = cy * cx;

            float[] m = new float[16];
            m[0] = r00 * scale.X;
            m[1] = r10 * scale.X;
            m[2] = r20 * scale.X;
            m[3] = 0f;

            m[4] = r01 * scale.Y;
            m[5] = r11 * scale.Y;
            m[6] = r21 * scale.Y;
            m[7] = 0f;

            m[8] = r02 * scale.Z;
            m[9] = r12 * scale.Z;
            m[10] = r22 * scale.Z;
            m[11] = 0f;

            m[12] = pos.X;
            m[13] = pos.Y;
            m[14] = pos.Z;
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 FromDirection(Vec3 pos, Vec3 dir, Vec3 scale)
        {
            Vec3 zAxis = dir.Normalized();
            if (zAxis.LengthSq() < 1e-6f) zAxis = Vec3.UnitZ;
            Vec3 upRef = Math.Abs(zAxis.Y) > 0.98f ? Vec3.UnitX : Vec3.UnitY;
            Vec3 xAxis = Vec3.Cross(upRef, zAxis).Normalized();
            Vec3 yAxis = Vec3.Cross(zAxis, xAxis);

            float[] m = new float[16];
            m[0] = xAxis.X * scale.X;
            m[1] = xAxis.Y * scale.X;
            m[2] = xAxis.Z * scale.X;
            m[3] = 0f;

            m[4] = yAxis.X * scale.Y;
            m[5] = yAxis.Y * scale.Y;
            m[6] = yAxis.Z * scale.Y;
            m[7] = 0f;

            m[8] = zAxis.X * scale.Z;
            m[9] = zAxis.Y * scale.Z;
            m[10] = zAxis.Z * scale.Z;
            m[11] = 0f;

            m[12] = pos.X;
            m[13] = pos.Y;
            m[14] = pos.Z;
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 LookAt(Vec3 eye, Vec3 target, Vec3 up)
        {
            Vec3 f = Vec3.Sub(target, eye).Normalized();
            if (f.LengthSq() < 1e-6f) f = new Vec3(0f, 0f, -1f);
            Vec3 s = Vec3.Cross(f, up).Normalized();
            if (s.LengthSq() < 1e-6f) s = Vec3.UnitX;
            Vec3 u = Vec3.Cross(s, f);

            float[] m = new float[16];
            m[0] = s.X;
            m[1] = u.X;
            m[2] = -f.X;
            m[3] = 0f;

            m[4] = s.Y;
            m[5] = u.Y;
            m[6] = -f.Y;
            m[7] = 0f;

            m[8] = s.Z;
            m[9] = u.Z;
            m[10] = -f.Z;
            m[11] = 0f;

            m[12] = -Vec3.Dot(s, eye);
            m[13] = -Vec3.Dot(u, eye);
            m[14] = Vec3.Dot(f, eye);
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 Perspective(float fovRad, float aspect, float near, float far)
        {
            float f = 1f / (float)Math.Tan(fovRad * 0.5f);
            float nf = 1f / (near - far);
            float[] m = new float[16];
            m[0] = f / (aspect > 1e-5f ? aspect : 1f);
            m[5] = f;
            m[10] = (far + near) * nf;
            m[11] = -1f;
            m[14] = (2f * far * near) * nf;
            return new Mat4(m);
        }

        public static Mat4 Ortho(float left, float right, float bottom, float top, float near, float far)
        {
            float rl = 1f / (right - left);
            float tb = 1f / (top - bottom);
            float fn = 1f / (far - near);
            float[] m = new float[16];
            m[0] = 2f * rl;
            m[5] = 2f * tb;
            m[10] = -2f * fn;
            m[12] = -(right + left) * rl;
            m[13] = -(top + bottom) * tb;
            m[14] = -(far + near) * fn;
            m[15] = 1f;
            return new Mat4(m);
        }

        public static Mat4 Multiply(Mat4 a, Mat4 b)
        {
            float[] am = a.M;
            float[] bm = b.M;
            float[] r = new float[16];
            for (int col = 0; col < 4; col++)
            {
                int c = col * 4;
                float b0 = bm[c];
                float b1 = bm[c + 1];
                float b2 = bm[c + 2];
                float b3 = bm[c + 3];
                r[c]     = am[0] * b0 + am[4] * b1 + am[8]  * b2 + am[12] * b3;
                r[c + 1] = am[1] * b0 + am[5] * b1 + am[9]  * b2 + am[13] * b3;
                r[c + 2] = am[2] * b0 + am[6] * b1 + am[10] * b2 + am[14] * b3;
                r[c + 3] = am[3] * b0 + am[7] * b1 + am[11] * b2 + am[15] * b3;
            }
            return new Mat4(r);
        }

        public static void MultiplyInto(Mat4 a, Mat4 b, float[] dst)
        {
            float[] am = a.M;
            float[] bm = b.M;
            for (int col = 0; col < 4; col++)
            {
                int c = col * 4;
                float b0 = bm[c];
                float b1 = bm[c + 1];
                float b2 = bm[c + 2];
                float b3 = bm[c + 3];
                dst[c]     = am[0] * b0 + am[4] * b1 + am[8]  * b2 + am[12] * b3;
                dst[c + 1] = am[1] * b0 + am[5] * b1 + am[9]  * b2 + am[13] * b3;
                dst[c + 2] = am[2] * b0 + am[6] * b1 + am[10] * b2 + am[14] * b3;
                dst[c + 3] = am[3] * b0 + am[7] * b1 + am[11] * b2 + am[15] * b3;
            }
        }

        public static Mat4 operator *(Mat4 a, Mat4 b)
        {
            return Multiply(a, b);
        }

        public static Vec3 TransformPoint(Mat4 m, Vec3 p)
        {
            float[] a = m.M;
            float x = a[0] * p.X + a[4] * p.Y + a[8]  * p.Z + a[12];
            float y = a[1] * p.X + a[5] * p.Y + a[9]  * p.Z + a[13];
            float z = a[2] * p.X + a[6] * p.Y + a[10] * p.Z + a[14];
            float w = a[3] * p.X + a[7] * p.Y + a[11] * p.Z + a[15];
            if (Math.Abs(w) > 1e-6f && Math.Abs(w - 1f) > 1e-6f)
            {
                float invW = 1f / w;
                return new Vec3(x * invW, y * invW, z * invW);
            }
            return new Vec3(x, y, z);
        }

        public static Vec3 TransformDirection(Mat4 m, Vec3 d)
        {
            float[] a = m.M;
            return new Vec3(
                a[0] * d.X + a[4] * d.Y + a[8]  * d.Z,
                a[1] * d.X + a[5] * d.Y + a[9]  * d.Z,
                a[2] * d.X + a[6] * d.Y + a[10] * d.Z);
        }

        public static bool TryInvert(Mat4 mat, out Mat4 inv)
        {
            float[] m = mat.M;
            float[] o = new float[16];

            o[0] = m[5]  * m[10] * m[15] - m[5]  * m[11] * m[14] - m[9]  * m[6]  * m[15] + m[9]  * m[7]  * m[14] + m[13] * m[6]  * m[11] - m[13] * m[7]  * m[10];
            o[4] = -m[4] * m[10] * m[15] + m[4]  * m[11] * m[14] + m[8]  * m[6]  * m[15] - m[8]  * m[7]  * m[14] - m[12] * m[6]  * m[11] + m[12] * m[7]  * m[10];
            o[8] = m[4]  * m[9]  * m[15] - m[4]  * m[11] * m[13] - m[8]  * m[5]  * m[15] + m[8]  * m[7]  * m[13] + m[12] * m[5]  * m[11] - m[12] * m[7]  * m[9];
            o[12]= -m[4] * m[9]  * m[14] + m[4]  * m[10] * m[13] + m[8]  * m[5]  * m[14] - m[8]  * m[6]  * m[13] - m[12] * m[5]  * m[10] + m[12] * m[6]  * m[9];

            o[1] = -m[1] * m[10] * m[15] + m[1]  * m[11] * m[14] + m[9]  * m[2]  * m[15] - m[9]  * m[3]  * m[14] - m[13] * m[2]  * m[11] + m[13] * m[3]  * m[10];
            o[5] = m[0]  * m[10] * m[15] - m[0]  * m[11] * m[14] - m[8]  * m[2]  * m[15] + m[8]  * m[3]  * m[14] + m[12] * m[2]  * m[11] - m[12] * m[3]  * m[10];
            o[9] = -m[0] * m[9]  * m[15] + m[0]  * m[11] * m[13] + m[8]  * m[1]  * m[15] - m[8]  * m[3]  * m[13] - m[12] * m[1]  * m[11] + m[12] * m[3]  * m[9];
            o[13]= m[0]  * m[9]  * m[14] - m[0]  * m[10] * m[13] - m[8]  * m[1]  * m[14] + m[8]  * m[2]  * m[13] + m[12] * m[1]  * m[10] - m[12] * m[2]  * m[9];

            o[2] = m[1]  * m[6]  * m[15] - m[1]  * m[7]  * m[14] - m[5]  * m[2]  * m[15] + m[5]  * m[3]  * m[14] + m[13] * m[2]  * m[7]  - m[13] * m[3]  * m[6];
            o[6] = -m[0] * m[6]  * m[15] + m[0]  * m[7]  * m[14] + m[4]  * m[2]  * m[15] - m[4]  * m[3]  * m[14] - m[12] * m[2]  * m[7]  + m[12] * m[3]  * m[6];
            o[10]= m[0]  * m[5]  * m[15] - m[0]  * m[7]  * m[13] - m[4]  * m[1]  * m[15] + m[4]  * m[3]  * m[13] + m[12] * m[1]  * m[7]  - m[12] * m[3]  * m[5];
            o[14]= -m[0] * m[5]  * m[14] + m[0]  * m[6]  * m[13] + m[4]  * m[1]  * m[14] - m[4]  * m[2]  * m[13] - m[12] * m[1]  * m[6]  + m[12] * m[2]  * m[5];

            o[3] = -m[1] * m[6]  * m[11] + m[1]  * m[7]  * m[10] + m[5]  * m[2]  * m[11] - m[5]  * m[3]  * m[10] - m[9]  * m[2]  * m[7]  + m[9]  * m[3]  * m[6];
            o[7] = m[0]  * m[6]  * m[11] - m[0]  * m[7]  * m[10] - m[4]  * m[2]  * m[11] + m[4]  * m[3]  * m[10] + m[8]  * m[2]  * m[7]  - m[8]  * m[3]  * m[6];
            o[11]= -m[0] * m[5]  * m[11] + m[0]  * m[7]  * m[9]  + m[4]  * m[1]  * m[11] - m[4]  * m[3]  * m[9]  - m[8]  * m[1]  * m[7]  + m[8]  * m[3]  * m[5];
            o[15]= m[0]  * m[5]  * m[10] - m[0]  * m[6]  * m[9]  - m[4]  * m[1]  * m[10] + m[4]  * m[2]  * m[9]  + m[8]  * m[1]  * m[6]  - m[8]  * m[2]  * m[5];

            float det = m[0] * o[0] + m[1] * o[4] + m[2] * o[8] + m[3] * o[12];
            if (Math.Abs(det) < 1e-10f)
            {
                inv = Identity();
                return false;
            }
            float invDet = 1f / det;
            for (int i = 0; i < 16; i++) o[i] *= invDet;
            inv = new Mat4(o);
            return true;
        }
    }

    public struct Aabb
    {
        public Vec3 Min;
        public Vec3 Max;

        public Aabb(Vec3 min, Vec3 max)
        {
            Min = min;
            Max = max;
        }

        public bool IntersectsRay(Vec3 origin, Vec3 dir, out float t)
        {
            float tmin = 0f;
            float tmax = 1e9f;

            // X slab
            if (Math.Abs(dir.X) < 1e-8f)
            {
                if (origin.X < Min.X || origin.X > Max.X) { t = 0f; return false; }
            }
            else
            {
                float inv = 1f / dir.X;
                float t1 = (Min.X - origin.X) * inv;
                float t2 = (Max.X - origin.X) * inv;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) { t = 0f; return false; }
            }

            // Y slab
            if (Math.Abs(dir.Y) < 1e-8f)
            {
                if (origin.Y < Min.Y || origin.Y > Max.Y) { t = 0f; return false; }
            }
            else
            {
                float inv = 1f / dir.Y;
                float t1 = (Min.Y - origin.Y) * inv;
                float t2 = (Max.Y - origin.Y) * inv;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) { t = 0f; return false; }
            }

            // Z slab
            if (Math.Abs(dir.Z) < 1e-8f)
            {
                if (origin.Z < Min.Z || origin.Z > Max.Z) { t = 0f; return false; }
            }
            else
            {
                float inv = 1f / dir.Z;
                float t1 = (Min.Z - origin.Z) * inv;
                float t2 = (Max.Z - origin.Z) * inv;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) { t = 0f; return false; }
            }

            t = tmin;
            return tmax >= 0f;
        }
    }

    public sealed class VaultPos
    {
        public int Id;
        public string Label;
        public float Angle;
        public float X;
        public float Y;
        public float Z;
    }

    public sealed class CameraPreset
    {
        public string Id;
        public string Key;
        public string Label;
        public Vec3 Pos;
        public Vec3 Target;
    }

    public static class WorldLayout
    {
        public const float DistrictRadius = 72f;
        public const float VaultY = -16f;
        public const float VaultRingRadius = 24f;

        public static readonly VaultPos[] VaultPositions;
        public static readonly CameraPreset[] CameraPresets;

        static WorldLayout()
        {
            VaultPositions = new VaultPos[5];
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)(-Math.PI * 0.5 + i * (Math.PI * 2.0 / 5.0));
                VaultPos vp = new VaultPos();
                vp.Id = i;
                vp.Label = "Vault " + (i + 1).ToString();
                vp.Angle = angle;
                vp.X = (float)Math.Cos(angle) * VaultRingRadius;
                vp.Y = VaultY;
                vp.Z = (float)Math.Sin(angle) * VaultRingRadius;
                VaultPositions[i] = vp;
            }

            VaultPos v3 = VaultPositions[2];
            CameraPresets = new CameraPreset[6];

            CameraPresets[0] = new CameraPreset
            {
                Id = "overview",
                Key = "1",
                Label = "1 · Full District Overview",
                Pos = new Vec3(88f, 62f, 92f),
                Target = new Vec3(0f, -3f, 0f)
            };
            CameraPresets[1] = new CameraPreset
            {
                Id = "subterranean",
                Key = "2",
                Label = "2 · Subterranean Vaults",
                Pos = new Vec3(48f, -3f, 52f),
                Target = new Vec3(0f, -16f, 0f)
            };
            CameraPresets[2] = new CameraPreset
            {
                Id = "hub",
                Key = "3",
                Label = "3 · Central Utility Hub",
                Pos = new Vec3(24f, -4f, 27f),
                Target = new Vec3(0f, -15f, 0f)
            };
            CameraPresets[3] = new CameraPreset
            {
                Id = "vault3",
                Key = "4",
                Label = "4 · Vault 3 Close-Up",
                Pos = new Vec3(v3.X + 15f, VaultY + 12f, v3.Z + 18f),
                Target = new Vec3(v3.X, VaultY + 1f, v3.Z)
            };
            CameraPresets[4] = new CameraPreset
            {
                Id = "pods",
                Key = "5",
                Label = "5 · Street-Level Pods",
                Pos = new Vec3(22f, 6.2f, 14f),
                Target = new Vec3(2f, 1.8f, -6f)
            };
            CameraPresets[5] = new CameraPreset
            {
                Id = "topdown",
                Key = "6",
                Label = "6 · Top-Down Masterplan",
                Pos = new Vec3(0f, 146f, 20f),
                Target = new Vec3(0f, 0f, 0f)
            };
        }
    }

    public enum MaterialPattern
    {
        Solid = 0,
        Concrete = 1,
        WindowGrid = 2,
        SolarPv = 3,
        Strata = 4,
        Hazard = 5,
        RackLed = 6,
        ThermalStrat = 7,
        WaterCaustic = 8,
        Asphalt = 9,
        PlazaTile = 10
    }

    public sealed class MaterialDef
    {
        public Vec3 Color = new Vec3(0.75f, 0.80f, 0.86f);
        public Vec3 Emissive = new Vec3(0f, 0f, 0f);
        public float EmissiveIntensity = 0f;
        public float Roughness = 0.55f;
        public float Metallic = 0.15f;
        public float Alpha = 1.0f;
        public MaterialPattern Pattern = MaterialPattern.Solid;
        public float PatternScale = 1.0f;
        public bool DoubleSided = false;
        public bool CastShadow = true;
    }

    public struct InstanceItem
    {
        public Mat4 Transform;
        public Vec4 ColorTint; // rgb tint, a = emissiveBoost

        public InstanceItem(Mat4 transform, Vec4 colorTint)
        {
            Transform = transform;
            ColorTint = colorTint;
        }
    }

    public sealed class PickTarget
    {
        public string InspectKey;
        public Aabb Bounds;
        public Vec3 Center;
        public float Radius;
    }

    public sealed class LayerVisibility
    {
        public bool Surface = true;
        public bool Heatmap = false;
        public bool Loop1 = true;
        public bool Loop2 = true;
        public bool Loop3 = true;
        public bool Optical = true;
        public bool Vaults = true;
    }

    public sealed class MeshBuilder
    {
        private readonly List<float> _verts = new List<float>(2048);
        private readonly List<uint> _indices = new List<uint>(1024);

        public int VertexCount
        {
            get { return _verts.Count / 8; }
        }

        public int IndexCount
        {
            get { return _indices.Count; }
        }

        private uint PushVertex(float px, float py, float pz, float nx, float ny, float nz, float u, float v)
        {
            uint idx = (uint)(_verts.Count / 8);
            _verts.Add(px);
            _verts.Add(py);
            _verts.Add(pz);
            _verts.Add(nx);
            _verts.Add(ny);
            _verts.Add(nz);
            _verts.Add(u);
            _verts.Add(v);
            return idx;
        }

        private void PushTri(uint a, uint b, uint c)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }

        private void PushQuad(uint a, uint b, uint c, uint d)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
            _indices.Add(a);
            _indices.Add(c);
            _indices.Add(d);
        }

        private static Vec3 RotYVec(float x, float y, float z, float cosY, float sinY)
        {
            return new Vec3(x * cosY + z * sinY, y, -x * sinY + z * cosY);
        }

        public void AddBox(Vec3 center, Vec3 size, float rotY = 0f)
        {
            float hx = size.X * 0.5f;
            float hy = size.Y * 0.5f;
            float hz = size.Z * 0.5f;
            float cy = (float)Math.Cos(rotY);
            float sy = (float)Math.Sin(rotY);

            // 6 faces, CCW outward winding
            AddBoxFace(center, cy, sy, new Vec3(-hx, -hy,  hz), new Vec3( hx, -hy,  hz), new Vec3( hx,  hy,  hz), new Vec3(-hx,  hy,  hz), new Vec3(0f, 0f, 1f));
            AddBoxFace(center, cy, sy, new Vec3( hx, -hy, -hz), new Vec3(-hx, -hy, -hz), new Vec3(-hx,  hy, -hz), new Vec3( hx,  hy, -hz), new Vec3(0f, 0f, -1f));
            AddBoxFace(center, cy, sy, new Vec3( hx, -hy,  hz), new Vec3( hx, -hy, -hz), new Vec3( hx,  hy, -hz), new Vec3( hx,  hy,  hz), new Vec3(1f, 0f, 0f));
            AddBoxFace(center, cy, sy, new Vec3(-hx, -hy, -hz), new Vec3(-hx, -hy,  hz), new Vec3(-hx,  hy,  hz), new Vec3(-hx,  hy, -hz), new Vec3(-1f, 0f, 0f));
            AddBoxFace(center, cy, sy, new Vec3(-hx,  hy,  hz), new Vec3( hx,  hy,  hz), new Vec3( hx,  hy, -hz), new Vec3(-hx,  hy, -hz), new Vec3(0f, 1f, 0f));
            AddBoxFace(center, cy, sy, new Vec3(-hx, -hy, -hz), new Vec3( hx, -hy, -hz), new Vec3( hx, -hy,  hz), new Vec3(-hx, -hy,  hz), new Vec3(0f, -1f, 0f));
        }

        private void AddBoxFace(Vec3 c, float cy, float sy, Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, Vec3 n)
        {
            Vec3 rn = RotYVec(n.X, n.Y, n.Z, cy, sy);
            Vec3 v0 = RotYVec(p0.X, p0.Y, p0.Z, cy, sy) + c;
            Vec3 v1 = RotYVec(p1.X, p1.Y, p1.Z, cy, sy) + c;
            Vec3 v2 = RotYVec(p2.X, p2.Y, p2.Z, cy, sy) + c;
            Vec3 v3 = RotYVec(p3.X, p3.Y, p3.Z, cy, sy) + c;
            uint i0 = PushVertex(v0.X, v0.Y, v0.Z, rn.X, rn.Y, rn.Z, 0f, 0f);
            uint i1 = PushVertex(v1.X, v1.Y, v1.Z, rn.X, rn.Y, rn.Z, 1f, 0f);
            uint i2 = PushVertex(v2.X, v2.Y, v2.Z, rn.X, rn.Y, rn.Z, 1f, 1f);
            uint i3 = PushVertex(v3.X, v3.Y, v3.Z, rn.X, rn.Y, rn.Z, 0f, 1f);
            PushQuad(i0, i1, i2, i3);
        }

        public void AddCylinder(Vec3 baseCenter, float rBottom, float rTop, float height, int segs = 16, bool capped = true)
        {
            if (segs < 3) segs = 3;
            float slope = (rBottom - rTop) / (height > 1e-5f ? height : 1f);
            float ny = slope;
            float nLen = (float)Math.Sqrt(1f + ny * ny);
            float nHoriz = 1f / nLen;
            float nVert = ny / nLen;

            uint sideStart = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float u = (float)i / segs;
                float a = u * (float)(Math.PI * 2.0);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                float nx = ca * nHoriz;
                float nz = sa * nHoriz;

                PushVertex(baseCenter.X + ca * rBottom, baseCenter.Y,          baseCenter.Z + sa * rBottom, nx, nVert, nz, u, 0f);
                PushVertex(baseCenter.X + ca * rTop,    baseCenter.Y + height, baseCenter.Z + sa * rTop,    nx, nVert, nz, u, 1f);
            }

            for (int i = 0; i < segs; i++)
            {
                uint b0 = sideStart + (uint)(i * 2);
                uint t0 = b0 + 1;
                uint b1 = b0 + 2;
                uint t1 = b0 + 3;
                // Outward CCW when viewed from outside (+cos, +sin)
                PushTri(b0, t0, t1);
                PushTri(b0, t1, b1);
            }

            if (capped)
            {
                if (rTop > 1e-5f)
                {
                    AddDisc(new Vec3(baseCenter.X, baseCenter.Y + height, baseCenter.Z), rTop, segs);
                }
                if (rBottom > 1e-5f)
                {
                    uint cIdx = PushVertex(baseCenter.X, baseCenter.Y, baseCenter.Z, 0f, -1f, 0f, 0.5f, 0.5f);
                    uint ringStart = (uint)VertexCount;
                    for (int i = 0; i <= segs; i++)
                    {
                        float a = (float)i / segs * (float)(Math.PI * 2.0);
                        float ca = (float)Math.Cos(a);
                        float sa = (float)Math.Sin(a);
                        PushVertex(baseCenter.X + ca * rBottom, baseCenter.Y, baseCenter.Z + sa * rBottom, 0f, -1f, 0f, 0.5f + ca * 0.5f, 0.5f + sa * 0.5f);
                    }
                    for (int i = 0; i < segs; i++)
                    {
                        PushTri(cIdx, ringStart + (uint)i, ringStart + (uint)(i + 1));
                    }
                }
            }
        }

        public void AddCylinderAxis(Vec3 p0, Vec3 p1, float r, int segs = 10)
        {
            if (segs < 3) segs = 3;
            Vec3 diff = p1 - p0;
            float len = diff.Length();
            if (len < 1e-5f) return;
            Vec3 axis = diff / len;
            Vec3 upRef = Math.Abs(axis.Y) > 0.96f ? Vec3.UnitX : Vec3.UnitY;
            Vec3 right = Vec3.Cross(upRef, axis).Normalized();
            Vec3 fwd = Vec3.Cross(axis, right);

            uint start = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float u = (float)i / segs;
                float a = u * (float)(Math.PI * 2.0);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                Vec3 n = right * ca + fwd * sa;
                Vec3 v0 = p0 + n * r;
                Vec3 v1 = p1 + n * r;
                PushVertex(v0.X, v0.Y, v0.Z, n.X, n.Y, n.Z, u, 0f);
                PushVertex(v1.X, v1.Y, v1.Z, n.X, n.Y, n.Z, u, 1f);
            }
            for (int i = 0; i < segs; i++)
            {
                uint b0 = start + (uint)(i * 2);
                PushTri(b0, b0 + 2, b0 + 3);
                PushTri(b0, b0 + 3, b0 + 1);
            }

            // End caps
            uint c0 = PushVertex(p0.X, p0.Y, p0.Z, -axis.X, -axis.Y, -axis.Z, 0.5f, 0.5f);
            uint r0 = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * (float)(Math.PI * 2.0);
                Vec3 n = right * (float)Math.Cos(a) + fwd * (float)Math.Sin(a);
                Vec3 v = p0 + n * r;
                PushVertex(v.X, v.Y, v.Z, -axis.X, -axis.Y, -axis.Z, 0.5f, 0.5f);
            }
            for (int i = 0; i < segs; i++) PushTri(c0, r0 + (uint)(i + 1), r0 + (uint)i);

            uint c1 = PushVertex(p1.X, p1.Y, p1.Z, axis.X, axis.Y, axis.Z, 0.5f, 0.5f);
            uint r1 = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * (float)(Math.PI * 2.0);
                Vec3 n = right * (float)Math.Cos(a) + fwd * (float)Math.Sin(a);
                Vec3 v = p1 + n * r;
                PushVertex(v.X, v.Y, v.Z, axis.X, axis.Y, axis.Z, 0.5f, 0.5f);
            }
            for (int i = 0; i < segs; i++) PushTri(c1, r1 + (uint)i, r1 + (uint)(i + 1));
        }

        public void AddCone(Vec3 baseCenter, float radius, float height, int segs = 10)
        {
            AddCylinder(baseCenter, radius, 0f, height, segs, true);
        }

        public void AddSphere(Vec3 center, float radius, int rings = 10, int segs = 14)
        {
            if (rings < 3) rings = 3;
            if (segs < 4) segs = 4;
            uint start = (uint)VertexCount;
            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float phi = v * (float)Math.PI;
                float sp = (float)Math.Sin(phi);
                float cp = (float)Math.Cos(phi);
                for (int s = 0; s <= segs; s++)
                {
                    float u = (float)s / segs;
                    float theta = u * (float)(Math.PI * 2.0);
                    float nx = sp * (float)Math.Cos(theta);
                    float ny = cp;
                    float nz = sp * (float)Math.Sin(theta);
                    PushVertex(
                        center.X + nx * radius,
                        center.Y + ny * radius,
                        center.Z + nz * radius,
                        nx, ny, nz, u, 1f - v);
                }
            }
            int rowStride = segs + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segs; s++)
                {
                    uint a = start + (uint)(r * rowStride + s);
                    uint b = a + (uint)rowStride;
                    uint c = b + 1;
                    uint d = a + 1;
                    PushTri(a, d, c);
                    PushTri(a, c, b);
                }
            }
        }

        public void AddTorus(Vec3 center, float majorR, float minorR, int radialSegs = 24, int tubeSegs = 10, bool horizontal = true)
        {
            if (radialSegs < 6) radialSegs = 6;
            if (tubeSegs < 4) tubeSegs = 4;
            uint start = (uint)VertexCount;

            for (int i = 0; i <= radialSegs; i++)
            {
                float u = (float)i / radialSegs;
                float a = u * (float)(Math.PI * 2.0);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);

                for (int j = 0; j <= tubeSegs; j++)
                {
                    float v = (float)j / tubeSegs;
                    float b = v * (float)(Math.PI * 2.0);
                    float cb = (float)Math.Cos(b);
                    float sb = (float)Math.Sin(b);

                    float rr = majorR + minorR * cb;
                    float px, py, pz, nx, ny, nz;
                    if (horizontal)
                    {
                        px = center.X + rr * ca;
                        py = center.Y + minorR * sb;
                        pz = center.Z + rr * sa;
                        nx = cb * ca;
                        ny = sb;
                        nz = cb * sa;
                    }
                    else
                    {
                        px = center.X + rr * ca;
                        py = center.Y + rr * sa;
                        pz = center.Z + minorR * sb;
                        nx = cb * ca;
                        ny = cb * sa;
                        nz = sb;
                    }
                    PushVertex(px, py, pz, nx, ny, nz, u, v);
                }
            }

            int stride = tubeSegs + 1;
            for (int i = 0; i < radialSegs; i++)
            {
                for (int j = 0; j < tubeSegs; j++)
                {
                    uint a = start + (uint)(i * stride + j);
                    uint b = a + (uint)stride;
                    uint c = b + 1;
                    uint d = a + 1;
                    if (horizontal)
                    {
                        PushTri(a, d, c);
                        PushTri(a, c, b);
                    }
                    else
                    {
                        PushTri(a, b, c);
                        PushTri(a, c, d);
                    }
                }
            }
        }

        public void AddTube(Vec3[] pts, float radius, int tubeSegs = 8, bool closed = false)
        {
            if (pts == null || pts.Length < 2) return;
            if (tubeSegs < 4) tubeSegs = 4;

            int count = pts.Length;
            int ringCount = closed ? count + 1 : count;
            Vec3[] tangents = new Vec3[ringCount];
            Vec3[] normals = new Vec3[ringCount];
            Vec3[] binormals = new Vec3[ringCount];

            for (int i = 0; i < ringCount; i++)
            {
                int idx = i % count;
                Vec3 prev = pts[(idx - 1 + count) % count];
                Vec3 next = pts[(idx + 1) % count];
                Vec3 t;
                if (!closed && idx == 0) t = pts[1] - pts[0];
                else if (!closed && idx == count - 1) t = pts[count - 1] - pts[count - 2];
                else t = next - prev;
                t = t.Normalized();
                if (t.LengthSq() < 1e-6f) t = Vec3.UnitZ;
                tangents[i] = t;
            }

            // Parallel transport (Bishop) frame for twist-free 3D conduits
            Vec3 refUp = Math.Abs(tangents[0].Y) > 0.94f ? Vec3.UnitX : Vec3.UnitY;
            normals[0] = Vec3.Cross(refUp, tangents[0]).Normalized();
            binormals[0] = Vec3.Cross(tangents[0], normals[0]).Normalized();

            for (int i = 1; i < ringCount; i++)
            {
                Vec3 t = tangents[i];
                Vec3 n = normals[i - 1] - t * Vec3.Dot(normals[i - 1], t);
                if (n.LengthSq() < 1e-6f)
                {
                    Vec3 alt = Math.Abs(t.Y) > 0.94f ? Vec3.UnitX : Vec3.UnitY;
                    n = Vec3.Cross(alt, t);
                }
                normals[i] = n.Normalized();
                binormals[i] = Vec3.Cross(t, normals[i]).Normalized();
            }

            uint start = (uint)VertexCount;
            for (int i = 0; i < ringCount; i++)
            {
                Vec3 p = pts[i % count];
                Vec3 n = normals[i];
                Vec3 b = binormals[i];
                float v = (float)i / (ringCount - 1);
                for (int j = 0; j <= tubeSegs; j++)
                {
                    float u = (float)j / tubeSegs;
                    float a = u * (float)(Math.PI * 2.0);
                    float ca = (float)Math.Cos(a);
                    float sa = (float)Math.Sin(a);
                    Vec3 radial = n * ca + b * sa;
                    Vec3 vp = p + radial * radius;
                    PushVertex(vp.X, vp.Y, vp.Z, radial.X, radial.Y, radial.Z, u, v);
                }
            }

            int stride = tubeSegs + 1;
            for (int i = 0; i < ringCount - 1; i++)
            {
                for (int j = 0; j < tubeSegs; j++)
                {
                    uint a = start + (uint)(i * stride + j);
                    uint b = a + (uint)stride;
                    uint c = b + 1;
                    uint d = a + 1;
                    PushTri(a, b, c);
                    PushTri(a, c, d);
                }
            }
        }

        public void AddDisc(Vec3 center, float radius, int segs = 32)
        {
            if (segs < 3) segs = 3;
            uint cIdx = PushVertex(center.X, center.Y, center.Z, 0f, 1f, 0f, 0.5f, 0.5f);
            uint ringStart = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * (float)(Math.PI * 2.0);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                PushVertex(center.X + ca * radius, center.Y, center.Z + sa * radius, 0f, 1f, 0f, 0.5f + ca * 0.5f, 0.5f + sa * 0.5f);
            }
            for (int i = 0; i < segs; i++)
            {
                // Upward +Y normal -> CCW viewed from +Y
                PushTri(cIdx, ringStart + (uint)(i + 1), ringStart + (uint)i);
            }
        }

        public void AddRing(Vec3 center, float innerR, float outerR, int segs = 48, float startAngle = 0f, float sweepAngle = 6.2831853f)
        {
            if (segs < 3) segs = 3;
            uint start = (uint)VertexCount;
            for (int i = 0; i <= segs; i++)
            {
                float u = (float)i / segs;
                float a = startAngle + u * sweepAngle;
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                PushVertex(center.X + ca * innerR, center.Y, center.Z + sa * innerR, 0f, 1f, 0f, u, 0f);
                PushVertex(center.X + ca * outerR, center.Y, center.Z + sa * outerR, 0f, 1f, 0f, u, 1f);
            }
            for (int i = 0; i < segs; i++)
            {
                uint a = start + (uint)(i * 2);
                uint b = a + 1;
                uint c = a + 3;
                uint d = a + 2;
                // Upward +Y normal -> CCW viewed from +Y
                PushTri(a, d, c);
                PushTri(a, c, b);
            }
        }

        public void AddGabledRoof(Vec3 baseCenter, float w, float h, float d, float rotY = 0f)
        {
            float hw = w * 0.5f;
            float hd = d * 0.5f;
            float cy = (float)Math.Cos(rotY);
            float sy = (float)Math.Sin(rotY);

            // Ridge runs along X
            Vec3 fl = RotYVec(-hw, 0f,  hd, cy, sy) + baseCenter;
            Vec3 fr = RotYVec( hw, 0f,  hd, cy, sy) + baseCenter;
            Vec3 bl = RotYVec(-hw, 0f, -hd, cy, sy) + baseCenter;
            Vec3 br = RotYVec( hw, 0f, -hd, cy, sy) + baseCenter;
            Vec3 rl = RotYVec(-hw,  h,  0f, cy, sy) + baseCenter;
            Vec3 rr = RotYVec( hw,  h,  0f, cy, sy) + baseCenter;

            // Front slope (+Z)
            Vec3 nFront = Vec3.Cross(fr - fl, rr - fl).Normalized();
            PushQuad(
                PushVertex(fl.X, fl.Y, fl.Z, nFront.X, nFront.Y, nFront.Z, 0f, 0f),
                PushVertex(fr.X, fr.Y, fr.Z, nFront.X, nFront.Y, nFront.Z, 1f, 0f),
                PushVertex(rr.X, rr.Y, rr.Z, nFront.X, nFront.Y, nFront.Z, 1f, 1f),
                PushVertex(rl.X, rl.Y, rl.Z, nFront.X, nFront.Y, nFront.Z, 0f, 1f));

            // Back slope (-Z)
            Vec3 nBack = Vec3.Cross(bl - br, rl - br).Normalized();
            PushQuad(
                PushVertex(br.X, br.Y, br.Z, nBack.X, nBack.Y, nBack.Z, 0f, 0f),
                PushVertex(bl.X, bl.Y, bl.Z, nBack.X, nBack.Y, nBack.Z, 1f, 0f),
                PushVertex(rl.X, rl.Y, rl.Z, nBack.X, nBack.Y, nBack.Z, 1f, 1f),
                PushVertex(rr.X, rr.Y, rr.Z, nBack.X, nBack.Y, nBack.Z, 0f, 1f));

            // Left gable (-X)
            Vec3 nLeft = Vec3.Cross(fl - bl, rl - bl).Normalized();
            PushTri(
                PushVertex(bl.X, bl.Y, bl.Z, nLeft.X, nLeft.Y, nLeft.Z, 0f, 0f),
                PushVertex(fl.X, fl.Y, fl.Z, nLeft.X, nLeft.Y, nLeft.Z, 1f, 0f),
                PushVertex(rl.X, rl.Y, rl.Z, nLeft.X, nLeft.Y, nLeft.Z, 0.5f, 1f));

            // Right gable (+X)
            Vec3 nRight = Vec3.Cross(br - fr, rr - fr).Normalized();
            PushTri(
                PushVertex(fr.X, fr.Y, fr.Z, nRight.X, nRight.Y, nRight.Z, 0f, 0f),
                PushVertex(br.X, br.Y, br.Z, nRight.X, nRight.Y, nRight.Z, 1f, 0f),
                PushVertex(rr.X, rr.Y, rr.Z, nRight.X, nRight.Y, nRight.Z, 0.5f, 1f));
        }

        public void TransformAll(Mat4 m)
        {
            float[] a = m.M;
            // Cofactor matrix of upper 3x3 for proper normal transformation under non-uniform scale
            float c00 = a[5] * a[10] - a[9] * a[6];
            float c10 = -(a[1] * a[10] - a[9] * a[2]);
            float c20 = a[1] * a[6] - a[5] * a[2];

            float c01 = -(a[4] * a[10] - a[8] * a[6]);
            float c11 = a[0] * a[10] - a[8] * a[2];
            float c21 = -(a[0] * a[6] - a[4] * a[2]);

            float c02 = a[4] * a[9] - a[8] * a[5];
            float c12 = -(a[0] * a[9] - a[8] * a[1]);
            float c22 = a[0] * a[5] - a[4] * a[1];

            int count = _verts.Count;
            for (int i = 0; i < count; i += 8)
            {
                float px = _verts[i];
                float py = _verts[i + 1];
                float pz = _verts[i + 2];
                _verts[i]     = a[0] * px + a[4] * py + a[8]  * pz + a[12];
                _verts[i + 1] = a[1] * px + a[5] * py + a[9]  * pz + a[13];
                _verts[i + 2] = a[2] * px + a[6] * py + a[10] * pz + a[14];

                float nx = _verts[i + 3];
                float ny = _verts[i + 4];
                float nz = _verts[i + 5];
                float tnx = c00 * nx + c01 * ny + c02 * nz;
                float tny = c10 * nx + c11 * ny + c12 * nz;
                float tnz = c20 * nx + c21 * ny + c22 * nz;
                float nLen = (float)Math.Sqrt(tnx * tnx + tny * tny + tnz * tnz);
                if (nLen > 1e-7f)
                {
                    float inv = 1f / nLen;
                    _verts[i + 3] = tnx * inv;
                    _verts[i + 4] = tny * inv;
                    _verts[i + 5] = tnz * inv;
                }
            }
        }

        public void BuildData(out float[] vertices, out uint[] indices)
        {
            vertices = _verts.ToArray();
            indices = _indices.ToArray();
        }
    }
}
