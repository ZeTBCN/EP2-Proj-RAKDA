using System;
using System.Collections.Generic;

namespace RakdaSim
{
    public sealed class FlowsScene : IDisposable
    {
        private sealed class BakedCurve
        {
            public Vec3[] Points;
            public Vec3[] Tangents;
            public int Segments;

            public void Sample(float t, out Vec3 pos, out Vec3 tan)
            {
                float f = t * Segments;
                int i0 = (int)f;
                if (i0 < 0) i0 = 0;
                if (i0 > Segments) i0 = Segments;
                float frac = f - i0;
                int i1 = i0 < Segments ? i0 + 1 : Segments;

                Vec3 p0 = Points[i0];
                Vec3 p1 = Points[i1];
                pos = new Vec3(
                    p0.X + (p1.X - p0.X) * frac,
                    p0.Y + (p1.Y - p0.Y) * frac,
                    p0.Z + (p1.Z - p0.Z) * frac);

                Vec3 t0 = Tangents[i0];
                Vec3 t1 = Tangents[i1];
                float tx = t0.X + (t1.X - t0.X) * frac;
                float ty = t0.Y + (t1.Y - t0.Y) * frac;
                float tz = t0.Z + (t1.Z - t0.Z) * frac;
                float inv = 1f / ((float)Math.Sqrt(tx * tx + ty * ty + tz * tz) + 1e-6f);
                tan = new Vec3(tx * inv, ty * inv, tz * inv);
            }
        }

        private sealed class ConduitPipe : IDisposable
        {
            public GpuMesh JacketMesh;
            public GpuMesh CoreMesh;
            public MaterialDef JacketMat;
            public MaterialDef CoreMat;
            public float BaseJacketAlpha;
            public bool Visible = true;

            public void SetState(bool active, Vec3 color, float intensity, float jacketAlpha)
            {
                JacketMat.Color = color;
                CoreMat.Color = color;
                CoreMat.Emissive = color;
                JacketMat.Alpha = active ? jacketAlpha : 0.06f;
                CoreMat.Alpha = active ? 0.90f : 0.16f;
                CoreMat.EmissiveIntensity = active ? intensity : 0.12f;
            }

            public void SetActive(bool active, float intensity, float jacketAlpha)
            {
                JacketMat.Alpha = active ? jacketAlpha : 0.06f;
                CoreMat.Alpha = active ? 0.90f : 0.16f;
                CoreMat.EmissiveIntensity = active ? intensity : 0.12f;
            }

            public void Dispose()
            {
                if (JacketMesh != null) JacketMesh.Dispose();
                if (CoreMesh != null) CoreMesh.Dispose();
            }
        }

        private struct StreamSlot
        {
            public BakedCurve Curve;
            public int StartIdx;
            public int Count;
            public float BaseScale;
            public bool Reverse;
            public Vec3 Color;
            public float EmissiveBoost;
        }

        private struct RainSeed
        {
            public float X;
            public float Y;
            public float Z;
            public float Speed;
            public float Len;
        }

        private readonly GpuMesh _packetUnitMesh;
        private readonly GpuMesh _ringUnitMesh;
        private readonly MaterialDef _packetMat;
        private readonly MaterialDef _ringMat;

        // Loop 1: 75°C Supply + 50°C Return
        private readonly ConduitPipe[] _l1SupplyPipes;
        private readonly ConduitPipe[] _l1ReturnPipes;
        private readonly StreamSlot[] _l1SupplySlots;
        private readonly StreamSlot[] _l1ReturnSlots;
        private readonly StreamSlot[] _l1RingSlots;
        private readonly GpuInstanceBatch _l1PacketBatch;
        private readonly InstanceItem[] _l1PacketItems;
        private readonly GpuInstanceBatch _l1RingBatch;
        private readonly InstanceItem[] _l1RingItems;

        // Loop 2: 5GDHC Twin-Pipe Ring + Radial Spurs + Exergy Cascade + Deficit Discharge
        private readonly ConduitPipe _l2WarmPipe;
        private readonly ConduitPipe _l2CoolPipe;
        private readonly ConduitPipe _l2InnerPipe;
        private readonly ConduitPipe _l2SpursPipe;
        private readonly ConduitPipe _l2RiserPipe;
        private readonly ConduitPipe _l2TcesPipe;
        private readonly ConduitPipe _l2PtesPipe;
        private readonly ConduitPipe _l2OrcPipe;
        private readonly ConduitPipe _l2MofPipe;
        private readonly ConduitPipe _l2DryPipe;
        private readonly ConduitPipe _l2HthpPipe;
        private readonly StreamSlot[] _l2PacketSlots;
        private readonly StreamSlot[] _l2RingSlots;
        private readonly GpuInstanceBatch _l2PacketBatch;
        private readonly InstanceItem[] _l2PacketItems;
        private readonly GpuInstanceBatch _l2RingBatch;
        private readonly InstanceItem[] _l2RingItems;

        // Loop 3: Roof Downspouts -> Cistern -> Ultrapure -> Vaults + 3D Rain
        private readonly ConduitPipe _l3CatchmentPipe;
        private readonly ConduitPipe _l3CisternToUpPipe;
        private readonly ConduitPipe[] _l3VaultPipes;
        private readonly StreamSlot[] _l3PacketSlots;
        private readonly GpuInstanceBatch _l3PacketBatch;
        private readonly InstanceItem[] _l3PacketItems;

        private const int RainCount = 640;
        private readonly RainSeed[] _rainSeeds;
        private readonly GpuLineBatch _rainLines;
        private readonly float[] _rainVerts;
        private int _activeRainVerts;

        // Optical Ring: 3.0 km NANF HCF + Bidirectional SDN + SRv6 Bypass Chords
        private readonly ConduitPipe[] _optHopPipes;
        private readonly ConduitPipe[] _optBypassPipes;
        private readonly StreamSlot[] _optFwdSlots;
        private readonly StreamSlot[] _optRevSlots;
        private readonly StreamSlot[] _optBypassPktSlots;
        private readonly StreamSlot[] _optBypassRingSlots;
        private readonly GpuInstanceBatch _optPacketBatch;
        private readonly InstanceItem[] _optPacketItems;
        private readonly GpuInstanceBatch _optRingBatch;
        private readonly InstanceItem[] _optRingItems;

        private float _phase1;
        private float _phase2;
        private float _phase3;
        private float _phaseOpt;
        private float _internalElapsed;

        private static readonly Vec3 ColOrangeRed = Vec3.FromHex(0xff521b);
        private static readonly Vec3 ColHotOrange = Vec3.FromHex(0xff7733);
        private static readonly Vec3 ColAmber = Vec3.FromHex(0xf59e0b);
        private static readonly Vec3 ColGold = Vec3.FromHex(0xfbbf24);
        private static readonly Vec3 ColTeal = Vec3.FromHex(0x14b8a6);
        private static readonly Vec3 ColAqua = Vec3.FromHex(0x2dd4bf);
        private static readonly Vec3 ColViolet = Vec3.FromHex(0xa855f7);
        private static readonly Vec3 ColMagenta = Vec3.FromHex(0xe879f9);
        private static readonly Vec3 ColCyan = Vec3.FromHex(0x22d3ee);
        private static readonly Vec3 ColSkyBlue = Vec3.FromHex(0x38bdf8);
        private static readonly Vec3 ColRedFault = Vec3.FromHex(0xef4444);
        private static readonly Vec3 ColBlueIdle = Vec3.FromHex(0x60a5fa);

        public FlowsScene(Renderer r)
        {
            // Elongated directional packet capsule along local +Z (matches Mat4.FromDirection!)
            MeshBuilder mbPkt = new MeshBuilder();
            mbPkt.AddCylinderAxis(new Vec3(0f, 0f, -0.28f), new Vec3(0f, 0f, 0.28f), 0.14f, 8);
            mbPkt.AddSphere(new Vec3(0f, 0f, 0.28f), 0.14f, 5, 8);
            mbPkt.AddSphere(new Vec3(0f, 0f, -0.28f), 0.14f, 5, 8);
            _packetUnitMesh = r.UploadMesh(mbPkt);

            // Collar flow ring in XY plane perpendicular to local +Z (horizontal: false)
            MeshBuilder mbRing = new MeshBuilder();
            mbRing.AddTorus(Vec3.Zero, 0.30f, 0.055f, 16, 6, false);
            _ringUnitMesh = r.UploadMesh(mbRing);

            _packetMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.2f,
                Metallic = 0.15f,
                CastShadow = false
            };
            _ringMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.2f,
                Metallic = 0.25f,
                Alpha = 0.90f,
                CastShadow = false
            };

            VaultPos[] vaults = WorldLayout.VaultPositions;
            float hubY = WorldLayout.VaultY + 0.8f;

            // =========================================================================
            // LOOP 1: 75°C Primary Cooling Supply + 50°C Return Manifold
            // =========================================================================
            _l1SupplyPipes = new ConduitPipe[5];
            _l1ReturnPipes = new ConduitPipe[5];
            _l1SupplySlots = new StreamSlot[5];
            _l1ReturnSlots = new StreamSlot[5];
            _l1RingSlots = new StreamSlot[5];

            int l1PktCursor = 0;
            int l1RingCursor = 0;
            for (int i = 0; i < 5; i++)
            {
                VaultPos vp = vaults[i];
                BakedCurve supCurve = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(vp.X, vp.Y + 0.45f, vp.Z),
                    new Vec3(vp.X * 0.62f, hubY + 1.55f, vp.Z * 0.62f),
                    new Vec3(-4.2f, hubY + 1.95f, -3.5f)
                }, 48, false);
                _l1SupplyPipes[i] = BuildPipe(r, supCurve, 0.25f, ColOrangeRed, 0.26f, 1.15f, false);

                float perpX = -(float)Math.Sin(vp.Angle) * 0.95f;
                float perpZ = (float)Math.Cos(vp.Angle) * 0.95f;
                BakedCurve retCurve = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(-4.2f + perpX * 0.4f, hubY + 0.45f, -3.5f + perpZ * 0.4f),
                    new Vec3(vp.X * 0.62f + perpX, hubY + 0.25f, vp.Z * 0.62f + perpZ),
                    new Vec3(vp.X + perpX * 0.5f, vp.Y - 0.15f, vp.Z + perpZ * 0.5f)
                }, 40, false);
                _l1ReturnPipes[i] = BuildPipe(r, retCurve, 0.17f, ColAmber, 0.20f, 0.78f, false);

                _l1SupplySlots[i] = new StreamSlot
                {
                    Curve = supCurve, StartIdx = l1PktCursor, Count = 10, BaseScale = 1.0f,
                    Reverse = false, Color = ColHotOrange, EmissiveBoost = 1.45f
                };
                l1PktCursor += 10;

                _l1ReturnSlots[i] = new StreamSlot
                {
                    Curve = retCurve, StartIdx = l1PktCursor, Count = 7, BaseScale = 0.72f,
                    Reverse = false, Color = ColGold, EmissiveBoost = 1.15f
                };
                l1PktCursor += 7;

                _l1RingSlots[i] = new StreamSlot
                {
                    Curve = supCurve, StartIdx = l1RingCursor, Count = 4, BaseScale = 1.05f,
                    Reverse = false, Color = Vec3.FromHex(0xff9a3c), EmissiveBoost = 1.35f
                };
                l1RingCursor += 4;
            }
            _l1PacketBatch = r.CreateInstanceBatch(_packetUnitMesh, l1PktCursor);
            _l1PacketItems = AllocateItems(l1PktCursor);
            _l1RingBatch = r.CreateInstanceBatch(_ringUnitMesh, l1RingCursor);
            _l1RingItems = AllocateItems(l1RingCursor);

            // =========================================================================
            // LOOP 2: 5GDHC 30 km Twin-Pipe Ring + Radial Spurs + Exergy Cascade / Deficit
            // =========================================================================
            BakedCurve ringWarm = BakeHorizontalRing(37.2f, -2.4f, 84);
            _l2WarmPipe = BuildPipe(r, ringWarm, 0.26f, ColAmber, 0.24f, 0.95f, true);

            BakedCurve ringCool = BakeHorizontalRing(38.8f, -2.6f, 84);
            _l2CoolPipe = BuildPipe(r, ringCool, 0.24f, ColTeal, 0.24f, 0.90f, true);

            BakedCurve ringInner = BakeHorizontalRing(16.4f, -2.4f, 56);
            _l2InnerPipe = BuildPipe(r, ringInner, 0.18f, ColAqua, 0.20f, 0.85f, true);

            BakedCurve[] spurCurves = new BakedCurve[5];
            for (int i = 0; i < 5; i++)
            {
                float ca = (float)Math.Cos(vaults[i].Angle);
                float sa = (float)Math.Sin(vaults[i].Angle);
                spurCurves[i] = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(ca * 16.4f, -2.4f, sa * 16.4f),
                    new Vec3(ca * 37.2f, -2.4f, sa * 37.2f),
                    new Vec3(ca * 61.0f, -2.2f, sa * 61.0f)
                }, 28, false);
            }
            _l2SpursPipe = BuildMergedPipe(r, spurCurves, 0.15f, ColTeal, 0.20f, 0.80f);

            BakedCurve riserCurve = BakeCatmullRom(new Vec3[]
            {
                new Vec3(-4.2f, hubY + 1.6f, -3.5f),
                new Vec3(-1.2f, -8.8f, 4.5f),
                new Vec3(0f, -2.4f, 37.2f)
            }, 48, false);
            _l2RiserPipe = BuildPipe(r, riserCurve, 0.25f, ColAmber, 0.26f, 1.10f, false);

            Vec3 tsOrig = new Vec3(-4.2f, hubY - 0.6f, -3.5f);
            BakedCurve tcesCurve = BakeCatmullRom(new Vec3[] { tsOrig, new Vec3(-1.5f, hubY - 0.4f, 1.2f), new Vec3(1.2f, hubY - 1.2f, 4.8f) }, 28, false);
            BakedCurve ptesCurve = BakeCatmullRom(new Vec3[] { tsOrig, new Vec3(1.6f, hubY - 0.2f, 0.8f), new Vec3(6.2f, hubY - 1.0f, 4.4f) }, 28, false);
            BakedCurve orcCurve = BakeCatmullRom(new Vec3[] { tsOrig, new Vec3(-1.8f, hubY - 0.3f, -5.6f), new Vec3(0.2f, hubY - 1.4f, -6.4f) }, 28, false);
            BakedCurve mofCurve = BakeCatmullRom(new Vec3[] { tsOrig, new Vec3(-6.8f, hubY - 0.5f, -2.4f), new Vec3(-8.6f, hubY - 1.4f, -0.4f) }, 28, false);
            BakedCurve dryCurve = BakeCatmullRom(new Vec3[] { tsOrig, new Vec3(3.2f, hubY + 0.1f, -1.6f), new Vec3(9.4f, hubY - 1.6f, 0.2f) }, 28, false);

            _l2TcesPipe = BuildPipe(r, tcesCurve, 0.15f, ColGold, 0.22f, 0.90f, false);
            _l2PtesPipe = BuildPipe(r, ptesCurve, 0.15f, Vec3.FromHex(0xf97316), 0.22f, 0.90f, false);
            _l2OrcPipe = BuildPipe(r, orcCurve, 0.15f, Vec3.FromHex(0x10b981), 0.22f, 0.95f, false);
            _l2MofPipe = BuildPipe(r, mofCurve, 0.15f, ColAqua, 0.22f, 0.90f, false);
            _l2DryPipe = BuildPipe(r, dryCurve, 0.15f, Vec3.FromHex(0xf43f5e), 0.22f, 0.90f, false);

            BakedCurve hthpCurve = BakeCatmullRom(new Vec3[]
            {
                new Vec3(-5.6f, hubY - 1.2f, 4.4f),
                new Vec3(-3.6f, -11.5f, 4.2f),
                new Vec3(-1.2f, -8.8f, 4.5f),
                new Vec3(0f, -2.4f, 37.2f)
            }, 48, false);
            _l2HthpPipe = BuildPipe(r, hthpCurve, 0.22f, ColOrangeRed, 0.26f, 1.35f, false);
            _l2HthpPipe.Visible = false;

            // 15 Loop 2 packet slots: 0=warm, 1=cool, 2=inner, 3..7=spurs, 8=riser, 9=tces, 10=ptes, 11=orc, 12=mof, 13=dry, 14=hthp
            _l2PacketSlots = new StreamSlot[15];
            int l2PktCursor = 0;
            l2PktCursor = AddSlot(_l2PacketSlots, 0, ringWarm, l2PktCursor, 26, 1.05f, false, ColGold, 1.35f);
            l2PktCursor = AddSlot(_l2PacketSlots, 1, ringCool, l2PktCursor, 24, 0.92f, true, ColAqua, 1.30f);
            l2PktCursor = AddSlot(_l2PacketSlots, 2, ringInner, l2PktCursor, 14, 0.78f, false, Vec3.FromHex(0x5eead4), 1.25f);
            for (int i = 0; i < 5; i++)
            {
                l2PktCursor = AddSlot(_l2PacketSlots, 3 + i, spurCurves[i], l2PktCursor, 8, 0.72f, false, Vec3.FromHex(0x38e8d2), 1.25f);
            }
            l2PktCursor = AddSlot(_l2PacketSlots, 8, riserCurve, l2PktCursor, 14, 1.0f, false, ColGold, 1.40f);
            l2PktCursor = AddSlot(_l2PacketSlots, 9, tcesCurve, l2PktCursor, 7, 0.75f, false, Vec3.FromHex(0xfde047), 1.38f);
            l2PktCursor = AddSlot(_l2PacketSlots, 10, ptesCurve, l2PktCursor, 7, 0.75f, false, Vec3.FromHex(0xfb923c), 1.38f);
            l2PktCursor = AddSlot(_l2PacketSlots, 11, orcCurve, l2PktCursor, 7, 0.75f, false, Vec3.FromHex(0x34d399), 1.42f);
            l2PktCursor = AddSlot(_l2PacketSlots, 12, mofCurve, l2PktCursor, 7, 0.75f, false, Vec3.FromHex(0x5eead4), 1.38f);
            l2PktCursor = AddSlot(_l2PacketSlots, 13, dryCurve, l2PktCursor, 7, 0.75f, false, Vec3.FromHex(0xfb7185), 1.38f);
            l2PktCursor = AddSlot(_l2PacketSlots, 14, hthpCurve, l2PktCursor, 14, 1.05f, false, Vec3.FromHex(0xff7849), 1.50f);

            _l2PacketBatch = r.CreateInstanceBatch(_packetUnitMesh, l2PktCursor);
            _l2PacketItems = AllocateItems(l2PktCursor);

            _l2RingSlots = new StreamSlot[3];
            int l2RingCursor = 0;
            l2RingCursor = AddSlot(_l2RingSlots, 0, ringWarm, l2RingCursor, 10, 1.10f, false, Vec3.FromHex(0xfde047), 1.30f);
            l2RingCursor = AddSlot(_l2RingSlots, 1, riserCurve, l2RingCursor, 5, 1.05f, false, Vec3.FromHex(0xfef08a), 1.35f);
            l2RingCursor = AddSlot(_l2RingSlots, 2, hthpCurve, l2RingCursor, 5, 1.15f, false, Vec3.FromHex(0xffa94d), 1.45f);
            _l2RingBatch = r.CreateInstanceBatch(_ringUnitMesh, l2RingCursor);
            _l2RingItems = AllocateItems(l2RingCursor);

            // =========================================================================
            // LOOP 3: Roof Catchment Downspouts -> Cistern -> Ultrapure -> Vaults + 3D Rain
            // =========================================================================
            Vec3 cisternTop = new Vec3(4.8f, hubY + 0.9f, -3.2f);
            Vec3[] dsStarts = new Vec3[]
            {
                new Vec3(24f, -0.3f, -18f),
                new Vec3(22f, -0.3f, 14f),
                new Vec3(-14f, -0.3f, -24f),
                new Vec3(10f, -0.3f, -6f)
            };
            BakedCurve[] catchmentCurves = new BakedCurve[4];
            for (int i = 0; i < 4; i++)
            {
                Vec3 s = dsStarts[i];
                catchmentCurves[i] = BakeCatmullRom(new Vec3[]
                {
                    s,
                    new Vec3(s.X * 0.45f + 3.0f, -7.8f - i * 0.4f, s.Z * 0.45f - 1.8f),
                    cisternTop
                }, 32, false);
            }
            _l3CatchmentPipe = BuildMergedPipe(r, catchmentCurves, 0.16f, Vec3.FromHex(0x0ea5e9), 0.24f, 0.95f);

            BakedCurve cisternToUp = BakeCatmullRom(new Vec3[]
            {
                new Vec3(4.8f, hubY - 1.2f, -5.2f),
                new Vec3(4.7f, hubY - 1.6f, -6.5f),
                new Vec3(4.6f, hubY - 1.4f, -7.8f)
            }, 20, false);
            _l3CisternToUpPipe = BuildPipe(r, cisternToUp, 0.18f, Vec3.FromHex(0x6366f1), 0.25f, 1.15f, false);

            _l3VaultPipes = new ConduitPipe[5];
            BakedCurve[] upVaultCurves = new BakedCurve[5];
            for (int i = 0; i < 5; i++)
            {
                VaultPos vp = vaults[i];
                upVaultCurves[i] = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(4.6f, hubY - 1.4f, -7.8f),
                    new Vec3(vp.X * 0.55f, hubY - 1.1f, vp.Z * 0.55f),
                    new Vec3(vp.X, vp.Y - 0.55f, vp.Z)
                }, 32, false);
                _l3VaultPipes[i] = BuildPipe(r, upVaultCurves[i], 0.13f, ColSkyBlue, 0.22f, 0.85f, false);
            }

            _l3PacketSlots = new StreamSlot[10];
            int l3Cursor = 0;
            for (int i = 0; i < 4; i++)
            {
                l3Cursor = AddSlot(_l3PacketSlots, i, catchmentCurves[i], l3Cursor, 9, 0.78f, false, ColSkyBlue, 1.35f);
            }
            l3Cursor = AddSlot(_l3PacketSlots, 4, cisternToUp, l3Cursor, 6, 0.80f, false, Vec3.FromHex(0x818cf8), 1.40f);
            for (int i = 0; i < 5; i++)
            {
                l3Cursor = AddSlot(_l3PacketSlots, 5 + i, upVaultCurves[i], l3Cursor, 8, 0.65f, false, Vec3.FromHex(0x7dd3fc), 1.28f);
            }
            _l3PacketBatch = r.CreateInstanceBatch(_packetUnitMesh, l3Cursor);
            _l3PacketItems = AllocateItems(l3Cursor);

            // Subtle 3D falling rain streaks (tuned to match visual QA polish)
            _rainSeeds = new RainSeed[RainCount];
            for (int i = 0; i < RainCount; i++)
            {
                float a = (float)((i * 2.399963) % (Math.PI * 2.0));
                float rad = (float)Math.Sqrt((i + 0.5) / RainCount) * 71f;
                _rainSeeds[i] = new RainSeed
                {
                    X = (float)Math.Cos(a) * rad,
                    Y = (i * 7.13f) % 56f,
                    Z = (float)Math.Sin(a) * rad,
                    Speed = 28f + (i % 9) * 2.6f,
                    Len = 0.90f + (i % 5) * 0.20f
                };
            }
            _rainLines = r.CreateLineBatch(RainCount * 2);
            _rainVerts = new float[RainCount * 2 * 7];

            // =========================================================================
            // OPTICAL RING: 3.0 km NANF HCF + Bidirectional SDN + SRv6 Bypass Chords
            // =========================================================================
            _optHopPipes = new ConduitPipe[5];
            _optBypassPipes = new ConduitPipe[5];
            _optFwdSlots = new StreamSlot[5];
            _optRevSlots = new StreamSlot[5];
            _optBypassPktSlots = new StreamSlot[5];
            _optBypassRingSlots = new StreamSlot[5];

            int optPktCursor = 0;
            int optRingCursor = 0;
            for (int i = 0; i < 5; i++)
            {
                int nextIdx = (i + 1) % 5;
                VaultPos vp = vaults[i];
                VaultPos nvp = vaults[nextIdx];
                BakedCurve hopCurve = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(vp.X, vp.Y + 0.32f, vp.Z),
                    new Vec3((vp.X + nvp.X) * 0.56f, vp.Y + 0.55f, (vp.Z + nvp.Z) * 0.56f),
                    new Vec3(nvp.X, nvp.Y + 0.32f, nvp.Z)
                }, 36, false);
                _optHopPipes[i] = BuildPipe(r, hopCurve, 0.18f, ColViolet, 0.30f, 1.25f, false);

                _optFwdSlots[i] = new StreamSlot
                {
                    Curve = hopCurve, StartIdx = optPktCursor, Count = 9, BaseScale = 0.85f,
                    Reverse = false, Color = ColCyan, EmissiveBoost = 1.48f
                };
                optPktCursor += 9;

                _optRevSlots[i] = new StreamSlot
                {
                    Curve = hopCurve, StartIdx = optPktCursor, Count = 7, BaseScale = 0.72f,
                    Reverse = true, Color = ColMagenta, EmissiveBoost = 1.42f
                };
                optPktCursor += 7;
            }

            for (int f = 0; f < 5; f++)
            {
                VaultPos prevVp = vaults[(f + 4) % 5];
                VaultPos nextVp = vaults[(f + 1) % 5];
                BakedCurve bpCurve = BakeCatmullRom(new Vec3[]
                {
                    new Vec3(prevVp.X, prevVp.Y + 0.65f, prevVp.Z),
                    new Vec3((prevVp.X + nextVp.X) * 0.32f, prevVp.Y + 1.85f, (prevVp.Z + nextVp.Z) * 0.32f),
                    new Vec3(nextVp.X, nextVp.Y + 0.65f, nextVp.Z)
                }, 36, false);
                _optBypassPipes[f] = BuildPipe(r, bpCurve, 0.16f, ColCyan, 0.32f, 1.50f, false);
                _optBypassPipes[f].Visible = false;

                _optBypassPktSlots[f] = new StreamSlot
                {
                    Curve = bpCurve, StartIdx = optPktCursor, Count = 12, BaseScale = 0.92f,
                    Reverse = false, Color = Vec3.FromHex(0x67e8f9), EmissiveBoost = 1.55f
                };
                optPktCursor += 12;

                _optBypassRingSlots[f] = new StreamSlot
                {
                    Curve = bpCurve, StartIdx = optRingCursor, Count = 4, BaseScale = 0.90f,
                    Reverse = false, Color = ColCyan, EmissiveBoost = 1.45f
                };
                optRingCursor += 4;
            }

            _optPacketBatch = r.CreateInstanceBatch(_packetUnitMesh, optPktCursor);
            _optPacketItems = AllocateItems(optPktCursor);
            _optRingBatch = r.CreateInstanceBatch(_ringUnitMesh, optRingCursor);
            _optRingItems = AllocateItems(optRingCursor);
        }

        private static InstanceItem[] AllocateItems(int count)
        {
            InstanceItem[] arr = new InstanceItem[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new InstanceItem(Mat4.Scale(0f), Vec4.Zero);
            }
            return arr;
        }

        private static int AddSlot(
            StreamSlot[] slots, int idx, BakedCurve curve, int cursor,
            int count, float baseScale, bool reverse, Vec3 color, float boost)
        {
            slots[idx] = new StreamSlot
            {
                Curve = curve,
                StartIdx = cursor,
                Count = count,
                BaseScale = baseScale,
                Reverse = reverse,
                Color = color,
                EmissiveBoost = boost
            };
            return cursor + count;
        }

        private static BakedCurve BakeHorizontalRing(float radius, float y, int segments)
        {
            Vec3[] pts = new Vec3[segments + 1];
            Vec3[] tans = new Vec3[segments + 1];
            float twoPi = (float)(Math.PI * 2.0);
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * twoPi;
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                pts[i] = new Vec3(ca * radius, y, sa * radius);
                tans[i] = new Vec3(-sa, 0f, ca);
            }
            return new BakedCurve { Points = pts, Tangents = tans, Segments = segments };
        }

        private static BakedCurve BakeCatmullRom(Vec3[] ctrl, int segments, bool closed)
        {
            int n = ctrl.Length;
            Vec3[] pts = new Vec3[segments + 1];
            Vec3[] tans = new Vec3[segments + 1];
            int spanCount = closed ? n : (n - 1);

            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments * spanCount;
                int seg = (int)u;
                if (seg >= spanCount) seg = spanCount - 1;
                float t = u - seg;

                Vec3 p0, p1, p2, p3;
                if (closed)
                {
                    p0 = ctrl[(seg - 1 + n) % n];
                    p1 = ctrl[seg % n];
                    p2 = ctrl[(seg + 1) % n];
                    p3 = ctrl[(seg + 2) % n];
                }
                else
                {
                    p0 = ctrl[Math.Max(0, seg - 1)];
                    p1 = ctrl[seg];
                    p2 = ctrl[Math.Min(n - 1, seg + 1)];
                    p3 = ctrl[Math.Min(n - 1, seg + 2)];
                }

                float t2 = t * t;
                float t3 = t2 * t;
                pts[i] = new Vec3(
                    0.5f * ((2f * p1.X) + (-p0.X + p2.X) * t + (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * t2 + (-p0.X + 3f * p1.X - 3f * p2.X + p3.X) * t3),
                    0.5f * ((2f * p1.Y) + (-p0.Y + p2.Y) * t + (2f * p0.Y - 5f * p1.Y + 4f * p2.Y - p3.Y) * t2 + (-p0.Y + 3f * p1.Y - 3f * p2.Y + p3.Y) * t3),
                    0.5f * ((2f * p1.Z) + (-p0.Z + p2.Z) * t + (2f * p0.Z - 5f * p1.Z + 4f * p2.Z - p3.Z) * t2 + (-p0.Z + 3f * p1.Z - 3f * p2.Z + p3.Z) * t3));
            }

            for (int i = 0; i <= segments; i++)
            {
                Vec3 prev = i > 0 ? pts[i - 1] : (closed ? pts[segments - 1] : pts[0]);
                Vec3 next = i < segments ? pts[i + 1] : (closed ? pts[1] : pts[segments]);
                Vec3 d = (next - prev).Normalized();
                if (d.LengthSq() < 1e-6f) d = Vec3.UnitZ;
                tans[i] = d;
            }
            return new BakedCurve { Points = pts, Tangents = tans, Segments = segments };
        }

        private static ConduitPipe BuildPipe(
            Renderer r, BakedCurve curve, float outerR, Vec3 color, float jacketAlpha, float emissiveIntensity, bool closed)
        {
            Vec3[] tubePts = closed ? SliceClosedPoints(curve.Points) : curve.Points;
            MeshBuilder mbJ = new MeshBuilder();
            mbJ.AddTube(tubePts, outerR, 8, closed);
            MeshBuilder mbC = new MeshBuilder();
            mbC.AddTube(tubePts, outerR * 0.46f, 6, closed);
            return new ConduitPipe
            {
                JacketMesh = r.UploadMesh(mbJ),
                CoreMesh = r.UploadMesh(mbC),
                BaseJacketAlpha = jacketAlpha,
                JacketMat = new MaterialDef
                {
                    Color = color,
                    Roughness = 0.14f,
                    Metallic = 0.12f,
                    Alpha = jacketAlpha,
                    CastShadow = false
                },
                CoreMat = new MaterialDef
                {
                    Color = color,
                    Emissive = color,
                    EmissiveIntensity = emissiveIntensity,
                    Roughness = 0.25f,
                    Metallic = 0.20f,
                    Alpha = 0.88f,
                    CastShadow = false
                }
            };
        }

        private static ConduitPipe BuildMergedPipe(
            Renderer r, BakedCurve[] curves, float outerR, Vec3 color, float jacketAlpha, float emissiveIntensity)
        {
            MeshBuilder mbJ = new MeshBuilder();
            MeshBuilder mbC = new MeshBuilder();
            for (int i = 0; i < curves.Length; i++)
            {
                mbJ.AddTube(curves[i].Points, outerR, 8, false);
                mbC.AddTube(curves[i].Points, outerR * 0.46f, 6, false);
            }
            return new ConduitPipe
            {
                JacketMesh = r.UploadMesh(mbJ),
                CoreMesh = r.UploadMesh(mbC),
                BaseJacketAlpha = jacketAlpha,
                JacketMat = new MaterialDef
                {
                    Color = color,
                    Roughness = 0.14f,
                    Metallic = 0.12f,
                    Alpha = jacketAlpha,
                    CastShadow = false
                },
                CoreMat = new MaterialDef
                {
                    Color = color,
                    Emissive = color,
                    EmissiveIntensity = emissiveIntensity,
                    Roughness = 0.25f,
                    Metallic = 0.20f,
                    Alpha = 0.88f,
                    CastShadow = false
                }
            };
        }

        private static Vec3[] SliceClosedPoints(Vec3[] pts)
        {
            int n = pts.Length - 1;
            Vec3[] arr = new Vec3[n];
            for (int i = 0; i < n; i++) arr[i] = pts[i];
            return arr;
        }

        private static void WriteZeroScale(float[] m)
        {
            for (int i = 0; i < 15; i++) m[i] = 0f;
            m[15] = 1f;
        }

        private static void WriteFromDirection(float[] m, Vec3 pos, Vec3 dir, float sx, float sy, float sz)
        {
            float zx = dir.X, zy = dir.Y, zz = dir.Z;
            float upX = Math.Abs(zy) > 0.96f ? 1f : 0f;
            float upY = Math.Abs(zy) > 0.96f ? 0f : 1f;
            float upZ = 0f;

            // xAxis = normalize(cross(up, z))
            float xx = upY * zz - upZ * zy;
            float xy = upZ * zx - upX * zz;
            float xz = upX * zy - upY * zx;
            float xInv = 1f / ((float)Math.Sqrt(xx * xx + xy * xy + xz * xz) + 1e-6f);
            xx *= xInv; xy *= xInv; xz *= xInv;

            // yAxis = cross(z, x)
            float yx = zy * xz - zz * xy;
            float yy = zz * xx - zx * xz;
            float yz = zx * xy - zy * xx;

            m[0] = xx * sx;  m[1] = xy * sx;  m[2] = xz * sx;  m[3] = 0f;
            m[4] = yx * sy;  m[5] = yy * sy;  m[6] = yz * sy;  m[7] = 0f;
            m[8] = zx * sz;  m[9] = zy * sz;  m[10] = zz * sz; m[11] = 0f;
            m[12] = pos.X;   m[13] = pos.Y;   m[14] = pos.Z;   m[15] = 1f;
        }

        private static void UpdatePacketSlot(
            InstanceItem[] items, ref StreamSlot slot, float phase, bool active, float scaleMult, Vec3 colorOverride, bool stretchZ)
        {
            int start = slot.StartIdx;
            int count = slot.Count;
            if (!active)
            {
                for (int i = 0; i < count; i++) WriteZeroScale(items[start + i].Transform.M);
                return;
            }

            float invCount = 1f / count;
            Vec4 tint = new Vec4(colorOverride.X, colorOverride.Y, colorOverride.Z, slot.EmissiveBoost);
            for (int i = 0; i < count; i++)
            {
                float offset = i * invCount;
                float t = (offset + (slot.Reverse ? -phase : phase)) % 1.0f;
                if (t < 0f) t += 1.0f;

                Vec3 pos;
                Vec3 tan;
                slot.Curve.Sample(t, out pos, out tan);
                if (slot.Reverse) tan = new Vec3(-tan.X, -tan.Y, -tan.Z);

                float szFactor = (i % 3 == 0) ? 1.28f : ((i % 3 == 1) ? 0.95f : 0.72f);
                float s = slot.BaseScale * scaleMult * (stretchZ ? szFactor : 1.0f);
                WriteFromDirection(items[start + i].Transform.M, pos, tan, s, s, stretchZ ? s * 1.15f : s);
                items[start + i].ColorTint = tint;
            }
        }

        public void Update(SimResult sim, float dt, float elapsed, LayerVisibility layers)
        {
            if (sim == null) return;
            _internalElapsed = elapsed > 0f ? elapsed : (_internalElapsed + dt);
            float pulse = 0.88f + 0.18f * (float)Math.Sin(_internalElapsed * 3.4f);

            float flowNorm = Math.Min(1.2f, (float)(sim.Thermal.PrimaryGrossFlowLmin / 4590.0));
            _phase1 = (_phase1 + dt * (0.24f + flowNorm * 0.52f)) % 1.0f;

            float demandNorm = Math.Min(1.2f, (float)(sim.Thermal.DistrictDemandMw / 9.52));
            _phase2 = (_phase2 + dt * (0.12f + demandNorm * 0.32f)) % 1.0f;

            float rainMult = (float)sim.Hydrology.RainfallMultiplier;
            _phase3 = (_phase3 + dt * (0.28f + Math.Min(0.45f, rainMult * 0.22f))) % 1.0f;

            bool sdnOn = sim.Optical.SdnEnabled;
            _phaseOpt = (_phaseOpt + dt * (sdnOn ? 0.62f : 0.26f)) % 1.0f;

            bool showL1 = layers == null || layers.Loop1;
            bool showL2 = layers == null || layers.Loop2;
            bool showL3 = layers == null || layers.Loop3;
            bool showOpt = layers == null || layers.Optical;

            // --- LOOP 1 ---
            if (showL1)
            {
                for (int i = 0; i < 5; i++)
                {
                    VaultTelemetry v = sim.Vaults[i];
                    bool active = !v.Faulted && v.ItMw > 0.05;
                    float vLoad = Math.Min(1.0f, (float)(v.ItMw / 2.0));
                    _l1SupplyPipes[i].SetActive(active, (0.75f + vLoad * 0.85f) * pulse, 0.20f + vLoad * 0.14f);
                    _l1ReturnPipes[i].SetActive(active, (0.50f + vLoad * 0.55f) * pulse, 0.16f + vLoad * 0.10f);

                    float pShift = (_phase1 + i * 0.17f) % 1.0f;
                    UpdatePacketSlot(_l1PacketItems, ref _l1SupplySlots[i], pShift, active, 0.78f + vLoad * 0.35f, _l1SupplySlots[i].Color, true);
                    UpdatePacketSlot(_l1RingItems, ref _l1RingSlots[i], pShift, active, 1.0f, _l1RingSlots[i].Color, false);
                    UpdatePacketSlot(_l1PacketItems, ref _l1ReturnSlots[i], (_phase1 * 0.88f + i * 0.21f) % 1.0f, active, 0.72f + vLoad * 0.28f, _l1ReturnSlots[i].Color, true);
                }
                _l1PacketBatch.UpdateInstances(_l1PacketItems, _l1PacketItems.Length);
                _l1RingBatch.UpdateInstances(_l1RingItems, _l1RingItems.Length);
            }

            // --- LOOP 2 ---
            if (showL2)
            {
                _l2WarmPipe.SetActive(true, (0.75f + demandNorm * 0.65f) * pulse, 0.24f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[0], _phase2, true, 0.85f + demandNorm * 0.30f, _l2PacketSlots[0].Color, true);
                UpdatePacketSlot(_l2RingItems, ref _l2RingSlots[0], _phase2, true, 1.0f, _l2RingSlots[0].Color, false);

                _l2CoolPipe.SetActive(true, (0.65f + demandNorm * 0.55f) * pulse, 0.24f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[1], (_phase2 * 0.95f) % 1.0f, true, 0.82f + demandNorm * 0.25f, _l2PacketSlots[1].Color, true);

                _l2InnerPipe.SetActive(true, (0.65f + demandNorm * 0.50f) * pulse, 0.20f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[2], (_phase2 * 1.25f) % 1.0f, true, 0.85f + demandNorm * 0.25f, _l2PacketSlots[2].Color, true);

                _l2SpursPipe.SetActive(true, (0.60f + demandNorm * 0.55f) * pulse, 0.20f);
                for (int i = 0; i < 5; i++)
                {
                    float zw = (float)sim.Vaults[i].ZoneWeight;
                    float zScale = (0.75f + zw * 2.0f) * 0.85f;
                    UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[3 + i], (_phase2 * (0.9f + zw * 1.8f) + i * 0.19f) % 1.0f, true, zScale, _l2PacketSlots[3 + i].Color, true);
                }

                bool isDeficit = sim.Thermal.DeficitMw > 0.08 || sim.Thermal.HthpThermalOutputMw > 0.05 || sim.Thermal.ThermalStoreDischargeMw > 0.05;
                bool riserActive = sim.Thermal.CapturedHeatMw > 0.2 || isDeficit;
                float riserPhase = (_phase2 * 1.35f) % 1.0f;
                _l2RiserPipe.SetState(riserActive, isDeficit ? ColOrangeRed : ColAmber, (isDeficit ? 1.45f : 1.05f) * pulse, 0.26f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[8], riserPhase, riserActive, isDeficit ? 1.2f : 1.0f, isDeficit ? Vec3.FromHex(0xff7849) : ColGold, true);
                UpdatePacketSlot(_l2RingItems, ref _l2RingSlots[1], riserPhase, riserActive, 1.0f, _l2RingSlots[1].Color, false);

                _l2HthpPipe.Visible = isDeficit;
                if (isDeficit) _l2HthpPipe.SetActive(true, 1.55f * pulse, 0.30f);
                float hthpPhase = (_phase2 * 1.65f) % 1.0f;
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[14], hthpPhase, isDeficit, 1.15f, _l2PacketSlots[14].Color, true);
                UpdatePacketSlot(_l2RingItems, ref _l2RingSlots[2], hthpPhase, isDeficit, 1.0f, _l2RingSlots[2].Color, false);

                bool tcesOn = sim.Thermal.TcesChargeMw > 0.05;
                _l2TcesPipe.SetActive(tcesOn, 1.20f * pulse, 0.22f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[9], (_phase2 * 1.45f) % 1.0f, tcesOn, 1.0f, _l2PacketSlots[9].Color, true);

                bool ptesOn = sim.Thermal.TiPtesChargeMw > 0.05;
                _l2PtesPipe.SetActive(ptesOn, 1.20f * pulse, 0.22f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[10], (_phase2 * 1.35f + 0.25f) % 1.0f, ptesOn, 1.0f, _l2PacketSlots[10].Color, true);

                bool orcOn = sim.Thermal.OrcOutputKw > 10.0;
                _l2OrcPipe.SetActive(orcOn, 1.35f * pulse, 0.22f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[11], (_phase2 * 1.60f + 0.50f) % 1.0f, orcOn, 1.0f, _l2PacketSlots[11].Color, true);

                bool mofOn = (sim.Thermal.CommercialHeatSalesMw + sim.Thermal.MofChillerMw) > 0.05;
                _l2MofPipe.SetActive(mofOn, 1.25f * pulse, 0.22f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[12], (_phase2 * 1.50f + 0.70f) % 1.0f, mofOn, 1.0f, _l2PacketSlots[12].Color, true);

                bool dryOn = sim.Thermal.DryCoolerRejectionMw > 0.05;
                _l2DryPipe.SetActive(dryOn, 1.25f * pulse, 0.22f);
                UpdatePacketSlot(_l2PacketItems, ref _l2PacketSlots[13], (_phase2 * 1.40f + 0.10f) % 1.0f, dryOn, 1.0f, _l2PacketSlots[13].Color, true);

                _l2PacketBatch.UpdateInstances(_l2PacketItems, _l2PacketItems.Length);
                _l2RingBatch.UpdateInstances(_l2RingItems, _l2RingItems.Length);
            }

            // --- LOOP 3 (Rainwater + Ultrapure + 3D Falling Rain) ---
            if (showL3)
            {
                bool isRaining = rainMult > 0.02f;
                _l3CatchmentPipe.SetActive(isRaining, (0.75f + Math.Min(0.8f, rainMult * 0.45f)) * pulse, 0.24f);
                float rainScale = 0.80f + Math.Min(0.40f, rainMult * 0.25f);
                for (int i = 0; i < 4; i++)
                {
                    UpdatePacketSlot(_l3PacketItems, ref _l3PacketSlots[i], (_phase3 + i * 0.23f) % 1.0f, isRaining, rainScale, _l3PacketSlots[i].Color, true);
                }

                bool upActive = sim.Electrical.ActiveVaultCount > 0;
                _l3CisternToUpPipe.SetActive(upActive, 1.20f * pulse, 0.25f);
                UpdatePacketSlot(_l3PacketItems, ref _l3PacketSlots[4], (_phase3 * 1.20f) % 1.0f, upActive, 1.0f, _l3PacketSlots[4].Color, true);

                for (int i = 0; i < 5; i++)
                {
                    bool active = !sim.Vaults[i].Faulted;
                    _l3VaultPipes[i].SetActive(active, 0.90f * pulse, 0.22f);
                    UpdatePacketSlot(_l3PacketItems, ref _l3PacketSlots[5 + i], (_phase3 + i * 0.19f) % 1.0f, active, 1.0f, _l3PacketSlots[5 + i].Color, true);
                }
                _l3PacketBatch.UpdateInstances(_l3PacketItems, _l3PacketItems.Length);

                if (rainMult <= 0.01f)
                {
                    _activeRainVerts = 0;
                    _rainLines.UpdateLines(_rainVerts, 0);
                }
                else
                {
                    float alpha = Math.Min(0.40f, 0.06f + rainMult * 0.15f);
                    int activeDrops = Math.Min(RainCount, (int)(RainCount * Math.Min(1.0f, 0.12f + rainMult * 0.46f)));
                    float windX = 0.28f + rainMult * 0.16f;
                    float windZ = 0.14f;
                    float fallMult = dt * (0.75f + rainMult * 0.32f);
                    float cr = 0.49f; // 0x7dd3fc soft sky-blue
                    float cg = 0.83f;
                    float cb = 0.99f;

                    int vIdx = 0;
                    for (int i = 0; i < activeDrops; i++)
                    {
                        float y = _rainSeeds[i].Y - fallMult * _rainSeeds[i].Speed;
                        if (y < 0f) y += 56f;
                        _rainSeeds[i].Y = y;
                        float x = _rainSeeds[i].X;
                        float z = _rainSeeds[i].Z;
                        float len = _rainSeeds[i].Len;

                        int o0 = vIdx * 7;
                        _rainVerts[o0] = x;
                        _rainVerts[o0 + 1] = y;
                        _rainVerts[o0 + 2] = z;
                        _rainVerts[o0 + 3] = cr;
                        _rainVerts[o0 + 4] = cg;
                        _rainVerts[o0 + 5] = cb;
                        _rainVerts[o0 + 6] = alpha * 0.55f;
                        vIdx++;

                        int o1 = vIdx * 7;
                        _rainVerts[o1] = x + windX * len;
                        _rainVerts[o1 + 1] = y - len;
                        _rainVerts[o1 + 2] = z + windZ * len;
                        _rainVerts[o1 + 3] = cr;
                        _rainVerts[o1 + 4] = cg;
                        _rainVerts[o1 + 5] = cb;
                        _rainVerts[o1 + 6] = alpha;
                        vIdx++;
                    }
                    _activeRainVerts = vIdx;
                    _rainLines.UpdateLines(_rainVerts, _activeRainVerts);
                }
            }

            // --- OPTICAL RING: 3.0 km NANF HCF + SDN + SRv6 Bypass ---
            if (showOpt)
            {
                float faultPulse = 0.65f + 0.95f * Math.Abs((float)Math.Sin(_internalElapsed * 6.5f));
                Vec3 fwdCol = sdnOn ? ColCyan : ColBlueIdle;
                Vec3 revCol = sdnOn ? ColMagenta : ColViolet;

                for (int i = 0; i < 5; i++)
                {
                    int nextIdx = (i + 1) % 5;
                    VaultTelemetry vFrom = sim.Vaults[i];
                    VaultTelemetry vTo = sim.Vaults[nextIdx];
                    bool faulted = vFrom.Faulted || vTo.Faulted;

                    if (faulted)
                    {
                        _optHopPipes[i].SetState(true, ColRedFault, faultPulse * 1.45f, 0.34f);
                        UpdatePacketSlot(_optPacketItems, ref _optFwdSlots[i], _phaseOpt, false, 1f, fwdCol, true);
                        UpdatePacketSlot(_optPacketItems, ref _optRevSlots[i], _phaseOpt, false, 1f, revCol, true);
                    }
                    else
                    {
                        float loadDiff = sdnOn ? (float)(vTo.ItMw - vFrom.ItMw) : 0f;
                        float fwdBias = sdnOn ? Math.Max(0.65f, 1.0f + loadDiff * 0.8f) : 0.9f;
                        float revBias = sdnOn ? Math.Max(0.65f, 1.0f - loadDiff * 0.8f) : 0.85f;
                        _optHopPipes[i].SetState(true, sdnOn ? ColViolet : ColSkyBlue, (sdnOn ? 1.32f : 0.88f) * pulse, 0.28f);
                        UpdatePacketSlot(_optPacketItems, ref _optFwdSlots[i], (_phaseOpt * (sdnOn ? 1.15f : 0.85f) + i * 0.13f) % 1.0f, true, fwdBias, fwdCol, true);
                        UpdatePacketSlot(_optPacketItems, ref _optRevSlots[i], (_phaseOpt * (sdnOn ? 1.05f : 0.80f) + nextIdx * 0.17f) % 1.0f, true, revBias, revCol, true);
                    }
                }

                float bpPhase = (_phaseOpt * 1.45f) % 1.0f;
                for (int f = 0; f < 5; f++)
                {
                    int prevIdx = (f + 4) % 5;
                    int nextIdx = (f + 1) % 5;
                    bool showBypass = sim.Vaults[f].Faulted && !sim.Vaults[prevIdx].Faulted && !sim.Vaults[nextIdx].Faulted;
                    _optBypassPipes[f].Visible = showBypass;
                    if (showBypass) _optBypassPipes[f].SetActive(true, 1.60f * pulse, 0.34f);
                    UpdatePacketSlot(_optPacketItems, ref _optBypassPktSlots[f], bpPhase, showBypass, 1.10f, _optBypassPktSlots[f].Color, true);
                    UpdatePacketSlot(_optRingItems, ref _optBypassRingSlots[f], bpPhase, showBypass, 1.0f, _optBypassRingSlots[f].Color, false);
                }

                _optPacketBatch.UpdateInstances(_optPacketItems, _optPacketItems.Length);
                _optRingBatch.UpdateInstances(_optRingItems, _optRingItems.Length);
            }
        }

        private static void DrawPipe(Renderer r, ConduitPipe pipe, Mat4 id)
        {
            if (pipe == null || !pipe.Visible) return;
            r.DrawMesh(pipe.CoreMesh, id, pipe.CoreMat);
            r.DrawMesh(pipe.JacketMesh, id, pipe.JacketMat);
        }

        public void Render(Renderer r, LayerVisibility layers)
        {
            Mat4 id = Mat4.Identity();

            if (layers == null || layers.Loop1)
            {
                for (int i = 0; i < 5; i++)
                {
                    DrawPipe(r, _l1SupplyPipes[i], id);
                    DrawPipe(r, _l1ReturnPipes[i], id);
                }
                r.DrawInstanced(_l1PacketBatch, _packetMat);
                r.DrawInstanced(_l1RingBatch, _ringMat);
            }

            if (layers == null || layers.Loop2)
            {
                DrawPipe(r, _l2WarmPipe, id);
                DrawPipe(r, _l2CoolPipe, id);
                DrawPipe(r, _l2InnerPipe, id);
                DrawPipe(r, _l2SpursPipe, id);
                DrawPipe(r, _l2RiserPipe, id);
                DrawPipe(r, _l2TcesPipe, id);
                DrawPipe(r, _l2PtesPipe, id);
                DrawPipe(r, _l2OrcPipe, id);
                DrawPipe(r, _l2MofPipe, id);
                DrawPipe(r, _l2DryPipe, id);
                DrawPipe(r, _l2HthpPipe, id);
                r.DrawInstanced(_l2PacketBatch, _packetMat);
                r.DrawInstanced(_l2RingBatch, _ringMat);
            }

            if (layers == null || layers.Loop3)
            {
                DrawPipe(r, _l3CatchmentPipe, id);
                DrawPipe(r, _l3CisternToUpPipe, id);
                for (int i = 0; i < 5; i++) DrawPipe(r, _l3VaultPipes[i], id);
                r.DrawInstanced(_l3PacketBatch, _packetMat);
                if (_activeRainVerts > 0)
                {
                    r.DrawLines(_rainLines, false);
                }
            }

            if (layers == null || layers.Optical)
            {
                for (int i = 0; i < 5; i++)
                {
                    DrawPipe(r, _optHopPipes[i], id);
                    DrawPipe(r, _optBypassPipes[i], id);
                }
                r.DrawInstanced(_optPacketBatch, _packetMat);
                r.DrawInstanced(_optRingBatch, _ringMat);
            }
        }

        public void Dispose()
        {
            if (_packetUnitMesh != null) _packetUnitMesh.Dispose();
            if (_ringUnitMesh != null) _ringUnitMesh.Dispose();
            for (int i = 0; i < 5; i++)
            {
                if (_l1SupplyPipes[i] != null) _l1SupplyPipes[i].Dispose();
                if (_l1ReturnPipes[i] != null) _l1ReturnPipes[i].Dispose();
                if (_l3VaultPipes[i] != null) _l3VaultPipes[i].Dispose();
                if (_optHopPipes[i] != null) _optHopPipes[i].Dispose();
                if (_optBypassPipes[i] != null) _optBypassPipes[i].Dispose();
            }
            if (_l1PacketBatch != null) _l1PacketBatch.Dispose();
            if (_l1RingBatch != null) _l1RingBatch.Dispose();
            if (_l2WarmPipe != null) _l2WarmPipe.Dispose();
            if (_l2CoolPipe != null) _l2CoolPipe.Dispose();
            if (_l2InnerPipe != null) _l2InnerPipe.Dispose();
            if (_l2SpursPipe != null) _l2SpursPipe.Dispose();
            if (_l2RiserPipe != null) _l2RiserPipe.Dispose();
            if (_l2TcesPipe != null) _l2TcesPipe.Dispose();
            if (_l2PtesPipe != null) _l2PtesPipe.Dispose();
            if (_l2OrcPipe != null) _l2OrcPipe.Dispose();
            if (_l2MofPipe != null) _l2MofPipe.Dispose();
            if (_l2DryPipe != null) _l2DryPipe.Dispose();
            if (_l2HthpPipe != null) _l2HthpPipe.Dispose();
            if (_l2PacketBatch != null) _l2PacketBatch.Dispose();
            if (_l2RingBatch != null) _l2RingBatch.Dispose();
            if (_l3CatchmentPipe != null) _l3CatchmentPipe.Dispose();
            if (_l3CisternToUpPipe != null) _l3CisternToUpPipe.Dispose();
            if (_l3PacketBatch != null) _l3PacketBatch.Dispose();
            if (_rainLines != null) _rainLines.Dispose();
            if (_optPacketBatch != null) _optPacketBatch.Dispose();
            if (_optRingBatch != null) _optRingBatch.Dispose();
        }
    }
}
