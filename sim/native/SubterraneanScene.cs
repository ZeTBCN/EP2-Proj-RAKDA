using System;
using System.Collections.Generic;

namespace RakdaSim
{
    public sealed class SubterraneanScene : IDisposable
    {
        private const float Pi = 3.14159265f;
        private const float TwoPi = 6.28318530f;
        private const float HalfPi = 1.57079633f;

        private sealed class StaticDrawEntry
        {
            public GpuMesh Mesh;
            public Mat4 Model;
            public MaterialDef Material;
        }

        private sealed class RawMeshCombiner
        {
            private readonly List<float> _verts = new List<float>(4096);
            private readonly List<uint> _indices = new List<uint>(2048);

            public int VertexCount
            {
                get { return _verts.Count / 8; }
            }

            public void Append(MeshBuilder mb)
            {
                float[] v;
                uint[] idx;
                mb.BuildData(out v, out idx);
                if (v == null || v.Length == 0 || idx == null || idx.Length == 0) return;
                uint baseVert = (uint)(_verts.Count / 8);
                for (int i = 0; i < v.Length; i++)
                {
                    _verts.Add(v[i]);
                }
                for (int i = 0; i < idx.Length; i++)
                {
                    _indices.Add(idx[i] + baseVert);
                }
            }

            public void AppendTransformed(MeshBuilder src, Mat4 transform)
            {
                float[] v;
                uint[] idx;
                src.BuildData(out v, out idx);
                if (v == null || v.Length == 0 || idx == null || idx.Length == 0) return;

                float[] a = transform.M;
                float c00 = a[5] * a[10] - a[9] * a[6];
                float c10 = -(a[1] * a[10] - a[9] * a[2]);
                float c20 = a[1] * a[6] - a[5] * a[2];

                float c01 = -(a[4] * a[10] - a[8] * a[6]);
                float c11 = a[0] * a[10] - a[8] * a[2];
                float c21 = -(a[0] * a[6] - a[4] * a[2]);

                float c02 = a[4] * a[9] - a[8] * a[5];
                float c12 = -(a[0] * a[9] - a[8] * a[1]);
                float c22 = a[0] * a[5] - a[4] * a[1];

                uint baseVert = (uint)(_verts.Count / 8);
                for (int i = 0; i < v.Length; i += 8)
                {
                    float px = v[i];
                    float py = v[i + 1];
                    float pz = v[i + 2];
                    float nx = v[i + 3];
                    float ny = v[i + 4];
                    float nz = v[i + 5];

                    _verts.Add(a[0] * px + a[4] * py + a[8] * pz + a[12]);
                    _verts.Add(a[1] * px + a[5] * py + a[9] * pz + a[13]);
                    _verts.Add(a[2] * px + a[6] * py + a[10] * pz + a[14]);

                    float tnx = c00 * nx + c01 * ny + c02 * nz;
                    float tny = c10 * nx + c11 * ny + c12 * nz;
                    float tnz = c20 * nx + c21 * ny + c22 * nz;
                    float nLen = (float)Math.Sqrt(tnx * tnx + tny * tny + tnz * tnz);
                    if (nLen > 1e-7f)
                    {
                        float inv = 1f / nLen;
                        tnx *= inv;
                        tny *= inv;
                        tnz *= inv;
                    }
                    _verts.Add(tnx);
                    _verts.Add(tny);
                    _verts.Add(tnz);
                    _verts.Add(v[i + 6]);
                    _verts.Add(v[i + 7]);
                }

                for (int i = 0; i < idx.Length; i++)
                {
                    _indices.Add(idx[i] + baseVert);
                }
            }

            public GpuMesh Upload(Renderer r)
            {
                if (_verts.Count == 0 || _indices.Count == 0) return null;
                return r.UploadMesh(_verts.ToArray(), _indices.ToArray());
            }
        }

        private sealed class VaultStateNode
        {
            public Mat4 BaseModel;
            public MaterialDef RackLedMat;
            public MaterialDef HotHeaderMat;
            public MaterialDef HtsMat;
            public MaterialDef BufferFluidMat;
            public MaterialDef FaultBeaconMat;
            public MaterialDef FaultHaloMat;
            public Mat4 FaultBeaconModel;
            public Mat4 FaultHaloModel;
            public bool Faulted;
            public bool LastFaultedValid;
            public bool LastFaulted;
            public float LastLoadFactor;
        }

        private readonly List<GpuMesh> _ownedMeshes = new List<GpuMesh>(64);
        private readonly List<StaticDrawEntry> _opaqueStaticDraws = new List<StaticDrawEntry>(48);
        private readonly List<StaticDrawEntry> _glassStaticDraws = new List<StaticDrawEntry>(16);
        private readonly List<PickTarget> _pickTargets = new List<PickTarget>(16);

        // Shared Vault dynamic template meshes (drawn 5x with per-vault materials)
        private readonly GpuMesh _vaultRackDoorsMesh;
        private readonly GpuMesh _vaultHotHeadersMesh;
        private readonly GpuMesh _vaultHtsMesh;
        private readonly GpuMesh _vaultBufferFluidMesh;
        private readonly GpuMesh _faultBeaconMesh;
        private readonly GpuMesh _faultHaloMesh;
        private readonly VaultStateNode[] _vaultNodes = new VaultStateNode[5];

        // Thermal Store (2,500 m³) animated state
        private readonly GpuMesh _tsFluidMesh;
        private readonly GpuMesh _tsMeniscusMesh;
        private readonly MaterialDef _tsFluidMat;
        private readonly MaterialDef _tsMeniscusMat;
        private readonly Mat4 _tsFluidModel;
        private readonly Mat4 _tsMeniscusModel;
        private readonly Vec3 _tsOrigin;
        private const float TsFluidMaxH = 5.85f;
        private float _lastTsSoc = -1f;
        private int _lastTsMode = -1;

        // Rainwater Cistern (10,000 m³) animated state
        private readonly GpuMesh _cisternWaterMesh;
        private readonly GpuMesh _cisternSurfaceMesh;
        private readonly MaterialDef _cisternWaterMat;
        private readonly MaterialDef _cisternSurfaceMat;
        private readonly Mat4 _cisternWaterModel;
        private readonly Mat4 _cisternSurfaceModel;
        private readonly Vec3 _cisternOrigin;
        private const float CisternMaxWaterH = 4.25f;
        private float _lastCisternSoc = -1f;
        private int _lastCisternLow = -1;

        // HTHP Bank (8.0 MW_th) animated state
        private readonly GpuMesh _hthpFanMesh;
        private readonly MaterialDef _fanBladeMat;
        private readonly MaterialDef _hthpDischargeMat;
        private readonly Vec3[] _hthpFanCenters = new Vec3[4];
        private readonly Mat4[] _hthpFanModels = new Mat4[4];
        private float _hthpFanAngle = 0f;
        private int _lastHthpActive = -1;

        // Exergy Cascade Bay animated state
        private readonly MaterialDef _tcesMat;
        private int _lastTcesActive = -1;

        private readonly MaterialDef _ptesHotMat;
        private readonly MaterialDef _ptesColdMat;
        private readonly MaterialDef _brassMat;
        private readonly GpuMesh _ptesFlywheelMesh;
        private readonly Vec3 _ptesFlywheelCenter;
        private readonly Mat4 _ptesFlywheelModel;
        private float _ptesFlywheelAngle = 0f;
        private int _lastPtesActive = -1;

        private readonly MaterialDef _orcTurbineMat;
        private readonly MaterialDef _orcBusMat;
        private readonly MaterialDef _brushedAlumMat;
        private readonly GpuMesh _orcShaftMesh;
        private readonly GpuMesh _orcFinsMesh;
        private readonly Vec3 _orcRotorCenter;
        private readonly Mat4 _orcRotorModel;
        private float _orcRotorAngle = 0f;
        private float _lastOrcNorm = -1f;

        private readonly MaterialDef _mofMat;
        private int _lastMofActive = -1;

        private readonly GpuMesh _dcFanMesh;
        private readonly Vec3[] _dcFanCenters = new Vec3[3];
        private readonly Mat4[] _dcFanModels = new Mat4[3];
        private float _dcFanAngle = 0f;

        // Ultrapure UV-C LED shimmer
        private readonly MaterialDef _uvCoreMat;

        private bool _firstUpdate = true;
        private bool _disposed = false;

        public SubterraneanScene(Renderer r)
        {
            if (r == null) throw new ArgumentNullException("r");

            // -----------------------------------------------------------------
            // Shared PBR Materials
            // -----------------------------------------------------------------
            MaterialDef concreteMat = MakeMat(0x8ea1b9, 0x000000, 0f, 0.84f, 0.08f, 1f, MaterialPattern.Concrete, 1.4f, false, true);
            MaterialDef slabMat = MakeMat(0x2d3b4e, 0x000000, 0f, 0.72f, 0.22f, 1f, MaterialPattern.Concrete, 1.8f, false, true);
            MaterialDef darkSteelMat = MakeMat(0x1e293b, 0x000000, 0f, 0.40f, 0.78f, 1f, MaterialPattern.Solid, 1f, false, true);
            MaterialDef brushedSteelMat = MakeMat(0xcbd5e1, 0x000000, 0f, 0.28f, 0.85f, 1f, MaterialPattern.Solid, 1f, false, true);
            _brushedAlumMat = MakeMat(0xe2e8f0, 0x000000, 0f, 0.24f, 0.88f, 1f, MaterialPattern.Solid, 1f, false, true);
            MaterialDef finCoilMat = MakeMat(0x94a3b8, 0x000000, 0f, 0.42f, 0.74f, 1f, MaterialPattern.Concrete, 3.6f, false, true);
            MaterialDef trenchMat = MakeMat(0x070c16, 0x000000, 0f, 0.96f, 0.05f, 1f, MaterialPattern.Solid, 1f, false, false);
            MaterialDef hazardMat = MakeMat(0xf59e0b, 0x000000, 0.08f, 0.48f, 0.15f, 1f, MaterialPattern.Hazard, 1.2f, false, false);
            MaterialDef elastomerMat = MakeMat(0xf97316, 0x000000, 0f, 0.65f, 0.10f, 1f, MaterialPattern.Solid, 1f, false, false);
            MaterialDef rackBodyMat = MakeMat(0x111827, 0x000000, 0f, 0.38f, 0.72f, 1f, MaterialPattern.Solid, 1f, false, true);
            MaterialDef coolPipeMat = MakeMat(0x38bdf8, 0x0369a1, 0.38f, 0.24f, 0.78f, 1f, MaterialPattern.Solid, 1f, false, false);
            _brassMat = MakeMat(0xd97706, 0x000000, 0f, 0.30f, 0.84f, 1f, MaterialPattern.Solid, 1f, false, false);
            MaterialDef coveLightMat = MakeMat(0xe0f2fe, 0x7dd3fc, 1.15f, 0.20f, 0.05f, 1f, MaterialPattern.Solid, 1f, false, false);
            MaterialDef insulatorMat = MakeMat(0x7dd3fc, 0x0284c7, 0.25f, 0.14f, 0.15f, 1f, MaterialPattern.Solid, 1f, false, false);
            MaterialDef glassTankMat = MakeMat(0xcbe4ff, 0x1e3a5f, 0.08f, 0.08f, 0.05f, 0.26f, MaterialPattern.Solid, 1f, true, false);
            MaterialDef shaftGlassMat = MakeMat(0x64748b, 0x0f172a, 0.06f, 0.20f, 0.20f, 0.24f, MaterialPattern.Solid, 1f, true, false);
            _fanBladeMat = MakeMat(0xcbd5e1, 0x000000, 0f, 0.26f, 0.84f, 1f, MaterialPattern.Solid, 1f, false, false);

            // =================================================================
            // 1. FIVE 2.0 MW SUBTERRANEAN VAULTS (vault_0..vault_4)
            // =================================================================
            Mat4[] vaultMats = new Mat4[5];
            for (int i = 0; i < 5; i++)
            {
                VaultPos vp = WorldLayout.VaultPositions[i];
                float rotY = -vp.Angle + HalfPi;
                vaultMats[i] = Mat4.TRS(new Vec3(vp.X, vp.Y, vp.Z), new Vec3(0f, rotY, 0f), Vec3.One);
                RegisterVaultPickTarget(vp.Id, vaultMats[i]);
            }

            // Build local vault template MeshBuilders once per material
            MeshBuilder vTrench = new MeshBuilder();
            MeshBuilder vConcrete = new MeshBuilder();
            MeshBuilder vHazard = new MeshBuilder();
            MeshBuilder vDarkSteel = new MeshBuilder();
            MeshBuilder vBrushedSteel = new MeshBuilder();
            MeshBuilder vElastomer = new MeshBuilder();
            MeshBuilder vSlab = new MeshBuilder();
            MeshBuilder vCove = new MeshBuilder();
            MeshBuilder vRackBody = new MeshBuilder();
            MeshBuilder vCoolPipe = new MeshBuilder();
            MeshBuilder vInsulator = new MeshBuilder();
            MeshBuilder vFinCoil = new MeshBuilder();
            MeshBuilder vBrass = new MeshBuilder();
            MeshBuilder vGlass = new MeshBuilder();
            MeshBuilder vShaftGlass = new MeshBuilder();

            // Dynamic per-vault template builders (uploaded ONCE, drawn 5x with per-vault materials)
            MeshBuilder vRackDoors = new MeshBuilder();
            MeshBuilder vHotHeaders = new MeshBuilder();
            MeshBuilder vHtsBus = new MeshBuilder();
            MeshBuilder vBufferFluid = new MeshBuilder();

            // 1.0 m Perimeter Vibration-Isolation Trench
            vTrench.AddBox(new Vec3(0f, -3.86f, 0f), new Vec3(11.9f, 0.26f, 9.9f));

            // 4 Outer concrete retaining curbs framing the 1.0 m moat
            vConcrete.AddBox(new Vec3(0f, -3.56f, 4.76f), new Vec3(11.9f, 0.42f, 0.38f));
            vConcrete.AddBox(new Vec3(0f, -3.56f, -4.76f), new Vec3(11.9f, 0.42f, 0.38f));
            vConcrete.AddBox(new Vec3(-5.76f, -3.56f, 0f), new Vec3(0.38f, 0.42f, 9.14f));
            vConcrete.AddBox(new Vec3(5.76f, -3.56f, 0f), new Vec3(0.38f, 0.42f, 9.14f));

            // Yellow/black industrial safety striping along trench rim
            vHazard.AddBox(new Vec3(0f, -3.33f, 4.76f), new Vec3(11.92f, 0.05f, 0.34f));
            vHazard.AddBox(new Vec3(0f, -3.33f, -4.76f), new Vec3(11.92f, 0.05f, 0.34f));
            vHazard.AddBox(new Vec3(-5.76f, -3.33f, 0f), new Vec3(0.34f, 0.05f, 9.12f));
            vHazard.AddBox(new Vec3(5.76f, -3.33f, 0f), new Vec3(0.34f, 0.05f, 9.12f));

            // 10 Visible Spring / Elastomer Acoustic Isolators under floating slab
            float[] isoX = new float[] { -4.2f, -1.4f, 1.4f, 4.2f, -4.2f, -1.4f, 1.4f, 4.2f, -4.2f, 4.2f };
            float[] isoZ = new float[] { 3.3f, 3.3f, 3.3f, 3.3f, -3.3f, -3.3f, -3.3f, -3.3f, 0f, 0f };
            for (int i = 0; i < isoX.Length; i++)
            {
                float ix = isoX[i];
                float iz = isoZ[i];
                vDarkSteel.AddCylinder(new Vec3(ix, -3.73f, iz), 0.25f, 0.22f, 0.08f, 10, true);
                vBrushedSteel.AddCylinder(new Vec3(ix, -3.65f, iz), 0.14f, 0.14f, 0.21f, 10, true);
                vElastomer.AddCylinder(new Vec3(ix, -3.44f, iz), 0.19f, 0.19f, 0.08f, 10, true);
            }

            // Floating Acoustic RC Slab + steel front edge beam
            vSlab.AddBox(new Vec3(0f, -3.18f, 0f), new Vec3(9.8f, 0.38f, 7.8f));
            vDarkSteel.AddBox(new Vec3(0f, -3.12f, 3.91f), new Vec3(9.86f, 0.14f, 0.08f));

            // Structural Ribbed Cutaway Concrete Walls (Back + Left + Right)
            vConcrete.AddBox(new Vec3(0f, -1.09f, -3.73f), new Vec3(9.8f, 3.8f, 0.34f));
            vConcrete.AddBox(new Vec3(-4.73f, -1.09f, 0f), new Vec3(0.34f, 3.8f, 7.8f));
            vConcrete.AddBox(new Vec3(4.73f, -1.09f, 0f), new Vec3(0.34f, 3.8f, 7.8f));

            // Buttress Pilasters
            float[] backPilX = new float[] { -3.6f, -1.8f, 0f, 1.8f, 3.6f };
            for (int i = 0; i < backPilX.Length; i++)
            {
                vConcrete.AddBox(new Vec3(backPilX[i], -1.09f, -3.50f), new Vec3(0.32f, 3.82f, 0.28f));
            }
            float[] sidePilZ = new float[] { -2.4f, 0f, 2.4f };
            for (int i = 0; i < sidePilZ.Length; i++)
            {
                vConcrete.AddBox(new Vec3(-4.50f, -1.09f, sidePilZ[i]), new Vec3(0.28f, 3.82f, 0.32f));
                vConcrete.AddBox(new Vec3(4.50f, -1.09f, sidePilZ[i]), new Vec3(0.28f, 3.82f, 0.32f));
            }

            // Steel Cap Beams & Overhead Gantry Tie Beams
            vDarkSteel.AddBox(new Vec3(0f, 0.85f, -3.73f), new Vec3(9.92f, 0.16f, 0.46f));
            vDarkSteel.AddBox(new Vec3(-4.73f, 0.85f, 0f), new Vec3(0.46f, 0.16f, 7.92f));
            vDarkSteel.AddBox(new Vec3(4.73f, 0.85f, 0f), new Vec3(0.46f, 0.16f, 7.92f));
            vDarkSteel.AddBox(new Vec3(0f, 0.82f, -1.45f), new Vec3(9.2f, 0.12f, 0.16f));
            vDarkSteel.AddBox(new Vec3(0f, 0.82f, 1.15f), new Vec3(9.2f, 0.12f, 0.16f));

            // Recessed Architectural Wall LED Coves
            vCove.AddBox(new Vec3(0f, 0.62f, -3.48f), new Vec3(8.8f, 0.06f, 0.06f));
            vCove.AddBox(new Vec3(-4.48f, 0.62f, 0f), new Vec3(0.06f, 0.06f, 6.8f));
            vCove.AddBox(new Vec3(4.48f, 0.62f, 0f), new Vec3(0.06f, 0.06f, 6.8f));

            // 8 Liquid-Cooled AI Server Racks (2 rows x 4) + Dual Cooling Manifolds + HTS Busways
            float[] rowZs = new float[] { -1.45f, 1.15f };
            float[] colXs = new float[] { -2.55f, -1.15f, 0.25f, 1.65f };
            float[] hangerXs = new float[] { -2.9f, -0.5f, 1.8f };

            for (int rIdx = 0; rIdx < rowZs.Length; rIdx++)
            {
                float rz = rowZs[rIdx];

                // Overhead 75°C Hot Return Header & 50°C Cool Supply Header
                vHotHeaders.AddCylinderAxis(new Vec3(-2.9f, -0.36f, rz - 0.18f), new Vec3(2.5f, -0.36f, rz - 0.18f), 0.075f, 10);
                vCoolPipe.AddCylinderAxis(new Vec3(-2.9f, -0.36f, rz + 0.18f), new Vec3(2.5f, -0.36f, rz + 0.18f), 0.075f, 10);

                for (int cIdx = 0; cIdx < colXs.Length; cIdx++)
                {
                    float rx = colXs[cIdx];
                    vRackBody.AddBox(new Vec3(rx, -1.81f, rz), new Vec3(0.96f, 2.36f, 1.08f));
                    vDarkSteel.AddBox(new Vec3(rx, -0.60f, rz), new Vec3(0.98f, 0.06f, 1.12f));

                    // Perforated front door with status LED rows (thin panel flush in front of rack)
                    vRackDoors.AddBox(new Vec3(rx, -1.81f, rz + 0.56f), new Vec3(0.88f, 2.22f, 0.03f));

                    // Vertical drops + quick-disconnect couplings
                    vHotHeaders.AddCylinder(new Vec3(rx - 0.12f, -0.70f, rz - 0.18f), 0.032f, 0.032f, 0.36f, 8, false);
                    vCoolPipe.AddCylinder(new Vec3(rx + 0.12f, -0.70f, rz + 0.18f), 0.032f, 0.032f, 0.36f, 8, false);
                    vBrushedSteel.AddCylinder(new Vec3(rx - 0.12f, -0.64f, rz - 0.18f), 0.052f, 0.052f, 0.06f, 8, true);
                    vBrushedSteel.AddCylinder(new Vec3(rx + 0.12f, -0.64f, rz + 0.18f), 0.052f, 0.052f, 0.06f, 8, true);
                }

                // Gleaming Suspended Cyan HTS Superconducting Busway
                vHtsBus.AddCylinderAxis(new Vec3(-3.70f, -0.10f, rz), new Vec3(2.50f, -0.10f, rz), 0.095f, 12);

                // Cryogenic clamp rings + Insulator Hangers up to overhead tie beams
                for (int hIdx = 0; hIdx < hangerXs.Length; hIdx++)
                {
                    float hx = hangerXs[hIdx];
                    vBrushedSteel.AddCylinderAxis(new Vec3(hx - 0.06f, -0.10f, rz), new Vec3(hx + 0.06f, -0.10f, rz), 0.145f, 10);
                    vBrushedSteel.AddCylinder(new Vec3(hx, -0.10f, rz), 0.022f, 0.022f, 0.92f, 6, false);
                    vInsulator.AddCylinder(new Vec3(hx, 0.19f, rz), 0.085f, 0.085f, 0.06f, 10, true);
                    vInsulator.AddCylinder(new Vec3(hx, 0.31f, rz), 0.085f, 0.085f, 0.06f, 10, true);
                }
            }

            // Cross-manifold connecting both rows' hot headers into the 43.4 m³ buffer tank
            vHotHeaders.AddCylinderAxis(new Vec3(2.45f, -0.36f, -1.63f), new Vec3(2.45f, -0.36f, 1.00f), 0.085f, 10);
            vHotHeaders.AddCylinderAxis(new Vec3(2.45f, -0.36f, -0.20f), new Vec3(3.50f, -0.36f, -0.20f), 0.085f, 10);

            // Twin 800 VDC SST Power Cabinets (Left Wall)
            for (int sIdx = 0; sIdx < rowZs.Length; sIdx++)
            {
                float sz = rowZs[sIdx];
                vBrushedSteel.AddBox(new Vec3(-3.98f, -1.85f, sz), new Vec3(0.72f, 2.28f, 1.45f));
                vFinCoil.AddBox(new Vec3(-3.61f, -1.92f, sz), new Vec3(0.03f, 1.65f, 1.18f));
                vHtsBus.AddBox(new Vec3(-3.60f, -0.92f, sz), new Vec3(0.06f, 0.14f, 1.15f));
                vHtsBus.AddCylinder(new Vec3(-3.75f, -0.72f, sz), 0.075f, 0.075f, 0.64f, 10, true);
            }

            // Detailed 43.4 m³ Cylindrical Thermal Buffer Tank (Right Side of Vault)
            float tankX = 3.55f;
            float tankZ = -0.20f;
            vDarkSteel.AddCylinder(new Vec3(tankX, -2.99f, tankZ), 1.10f, 1.02f, 0.28f, 18, true);
            AddDomeY(vBrushedSteel, new Vec3(tankX, -2.62f, tankZ), 0.98f, 0.41f, false, 18);
            AddDomeY(vBrushedSteel, new Vec3(tankX, -0.60f, tankZ), 0.98f, 0.41f, true, 18);
            vBrushedSteel.AddCylinder(new Vec3(tankX, -0.43f, tankZ), 0.22f, 0.18f, 0.34f, 12, true);
            vDarkSteel.AddTorus(new Vec3(tankX, -2.60f, tankZ), 1.02f, 0.055f, 22, 8, true);
            vDarkSteel.AddTorus(new Vec3(tankX, -1.61f, tankZ), 1.02f, 0.055f, 22, 8, true);
            vDarkSteel.AddTorus(new Vec3(tankX, -0.62f, tankZ), 1.02f, 0.055f, 22, 8, true);

            vBufferFluid.AddCylinder(new Vec3(tankX, -2.58f, tankZ), 0.84f, 0.84f, 1.92f, 18, true);
            vGlass.AddCylinder(new Vec3(tankX, -2.62f, tankZ), 0.99f, 0.99f, 2.02f, 20, false);

            // External Sight-Glass Column with Brass Valves
            float sgZ = tankZ + 1.07f;
            vGlass.AddCylinder(new Vec3(tankX, -2.47f, sgZ), 0.065f, 0.065f, 1.72f, 10, false);
            vBufferFluid.AddCylinder(new Vec3(tankX, -2.42f, sgZ), 0.038f, 0.038f, 1.56f, 8, true);
            vBrass.AddCylinderAxis(new Vec3(tankX, -2.44f, sgZ - 0.13f), new Vec3(tankX, -2.44f, sgZ + 0.01f), 0.075f, 8);
            vBrass.AddCylinderAxis(new Vec3(tankX, -0.78f, sgZ - 0.13f), new Vec3(tankX, -0.78f, sgZ + 0.01f), 0.075f, 8);

            // Low-Noise <110 dB Water-Mist Fire-Suppression Ring + 8 Nozzles
            vBrushedSteel.AddTorus(new Vec3(-0.2f, 0.66f, -0.15f), 2.65f, 0.042f, 28, 8, true);
            for (int m = 0; m < 8; m++)
            {
                float ma = (m / 8f) * TwoPi;
                float nx = -0.2f + (float)Math.Cos(ma) * 2.65f;
                float nz = -0.15f + (float)Math.Sin(ma) * 2.65f;
                vBrass.AddCylinder(new Vec3(nx, 0.54f, nz), 0.045f, 0.025f, 0.11f, 8, true);
            }

            // Vertical Riser Shaft to Surface Plaza + Internal Pipes + 5 Service Ring Collars
            vShaftGlass.AddCylinder(new Vec3(0f, 0.85f, 0f), 0.84f, 0.76f, 14.8f, 18, false);
            vHotHeaders.AddCylinder(new Vec3(-0.22f, 0.95f, -0.12f), 0.11f, 0.11f, 14.6f, 8, true);
            vCoolPipe.AddCylinder(new Vec3(0.22f, 0.95f, -0.12f), 0.11f, 0.11f, 14.6f, 8, true);
            vHtsBus.AddCylinder(new Vec3(0f, 0.95f, 0.24f), 0.065f, 0.065f, 14.6f, 8, true);
            float[] collarYs = new float[] { 1.2f, 4.4f, 7.6f, 10.8f, 14.0f };
            for (int i = 0; i < collarYs.Length; i++)
            {
                vDarkSteel.AddTorus(new Vec3(0f, collarYs[i], 0f), 0.84f, 0.075f, 20, 8, true);
            }

            // Combine all 5 vaults' static geometry into 1 world-space GpuMesh per material (15 draw calls total for ALL 5 vaults!)
            AddCombined5VaultStatic(r, vTrench, vaultMats, trenchMat, false);
            AddCombined5VaultStatic(r, vConcrete, vaultMats, concreteMat, false);
            AddCombined5VaultStatic(r, vHazard, vaultMats, hazardMat, false);
            AddCombined5VaultStatic(r, vDarkSteel, vaultMats, darkSteelMat, false);
            AddCombined5VaultStatic(r, vBrushedSteel, vaultMats, brushedSteelMat, false);
            AddCombined5VaultStatic(r, vElastomer, vaultMats, elastomerMat, false);
            AddCombined5VaultStatic(r, vSlab, vaultMats, slabMat, false);
            AddCombined5VaultStatic(r, vCove, vaultMats, coveLightMat, false);
            AddCombined5VaultStatic(r, vRackBody, vaultMats, rackBodyMat, false);
            AddCombined5VaultStatic(r, vCoolPipe, vaultMats, coolPipeMat, false);
            AddCombined5VaultStatic(r, vInsulator, vaultMats, insulatorMat, false);
            AddCombined5VaultStatic(r, vFinCoil, vaultMats, finCoilMat, false);
            AddCombined5VaultStatic(r, vBrass, vaultMats, _brassMat, false);
            AddCombined5VaultStatic(r, vGlass, vaultMats, glassTankMat, true);
            AddCombined5VaultStatic(r, vShaftGlass, vaultMats, shaftGlassMat, true);

            // Upload 4 shared dynamic vault template meshes + 2 fault ring templates
            _vaultRackDoorsMesh = OwnMesh(r.UploadMesh(vRackDoors));
            _vaultHotHeadersMesh = OwnMesh(r.UploadMesh(vHotHeaders));
            _vaultHtsMesh = OwnMesh(r.UploadMesh(vHtsBus));
            _vaultBufferFluidMesh = OwnMesh(r.UploadMesh(vBufferFluid));

            MeshBuilder beaconMb = new MeshBuilder();
            beaconMb.AddTorus(Vec3.Zero, 5.6f, 0.16f, 40, 10, true);
            _faultBeaconMesh = OwnMesh(r.UploadMesh(beaconMb));

            MeshBuilder haloMb = new MeshBuilder();
            haloMb.AddRing(Vec3.Zero, 5.8f, 6.5f, 40, 0f, TwoPi);
            _faultHaloMesh = OwnMesh(r.UploadMesh(haloMb));

            for (int i = 0; i < 5; i++)
            {
                VaultStateNode node = new VaultStateNode();
                node.BaseModel = vaultMats[i];
                node.RackLedMat = MakeMat(0x162235, 0x22d3ee, 1.45f, 0.34f, 0.58f, 1f, MaterialPattern.RackLed, 1f, false, false);
                node.HotHeaderMat = MakeMat(0xff5a1f, 0xea580c, 0.85f, 0.28f, 0.72f, 1f, MaterialPattern.Solid, 1f, false, false);
                node.HtsMat = MakeMat(0x38bdf8, 0x0ea5e9, 1.45f, 0.14f, 0.84f, 1f, MaterialPattern.Solid, 1f, false, false);
                node.BufferFluidMat = MakeMat(0xff5a1f, 0xea580c, 0.95f, 0.22f, 0.10f, 1f, MaterialPattern.Solid, 1f, false, false);
                node.FaultBeaconMat = MakeMat(0xef4444, 0xff2222, 2.35f, 0.25f, 0.10f, 0.88f, MaterialPattern.Solid, 1f, false, false);
                node.FaultHaloMat = MakeMat(0xef4444, 0xff3333, 1.75f, 0.40f, 0.0f, 0.45f, MaterialPattern.Solid, 1f, true, false);
                node.FaultBeaconModel = Mat4.Identity();
                node.FaultHaloModel = Mat4.Identity();
                node.Faulted = false;
                node.LastFaultedValid = false;
                node.LastLoadFactor = -1f;
                _vaultNodes[i] = node;
            }

            // =================================================================
            // 2. CENTRAL UTILITY HUB (x = 0, z = 0, y = -16)
            // =================================================================
            float hubY = WorldLayout.VaultY;
            float deckY = hubY - 3.1f; // -19.1 (base level for all hub subsystems)

            // Hub Foundation Plinth + Curb + Glowing Cyan Perimeter Accent Ring
            MeshBuilder hubSlabMb = new MeshBuilder();
            hubSlabMb.AddCylinder(new Vec3(0f, hubY - 3.64f, 0f), 14.0f, 13.4f, 0.52f, 48, true);
            AddStaticMesh(r, hubSlabMb, slabMat, false);

            MaterialDef hubFloorMat = MakeMat(0x1f2d42, 0x000000, 0f, 0.70f, 0.26f, 1f, MaterialPattern.PlazaTile, 2.4f, false, false);
            MeshBuilder hubTopMb = new MeshBuilder();
            hubTopMb.AddDisc(new Vec3(0f, hubY - 3.112f, 0f), 13.38f, 48);
            AddStaticMesh(r, hubTopMb, hubFloorMat, false);

            MeshBuilder hubCurbMb = new MeshBuilder();
            hubCurbMb.AddTorus(new Vec3(0f, hubY - 3.10f, 0f), 13.42f, 0.12f, 56, 8, true);
            AddStaticMesh(r, hubCurbMb, darkSteelMat, false);

            MaterialDef hubAccentMat = MakeMat(0x22d3ee, 0x0ea5e9, 1.05f, 0.20f, 0.20f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder hubAccentMb = new MeshBuilder();
            hubAccentMb.AddTorus(new Vec3(0f, hubY - 3.38f, 0f), 13.78f, 0.045f, 56, 8, true);
            AddStaticMesh(r, hubAccentMb, hubAccentMat, false);

            // Shared hub static builders (batching repeated hub materials across all hub subsystems!)
            MeshBuilder hubConcrete = new MeshBuilder();
            MeshBuilder hubDarkSteel = new MeshBuilder();
            MeshBuilder hubBrushedSteel = new MeshBuilder();
            MeshBuilder hubBrushedAlum = new MeshBuilder();
            MeshBuilder hubFinCoil = new MeshBuilder();
            MeshBuilder hubHazard = new MeshBuilder();
            MeshBuilder hubGlass = new MeshBuilder();

            // -----------------------------------------------------------------
            // 2A. 2,500 m³ Stratified Thermal Store (thermal_store)
            // -----------------------------------------------------------------
            _tsOrigin = new Vec3(-4.2f, deckY, -3.5f);
            AddPickTarget("thermal_store",
                new Vec3(_tsOrigin.X - 3.4f, _tsOrigin.Y, _tsOrigin.Z - 3.4f),
                new Vec3(_tsOrigin.X + 3.4f, _tsOrigin.Y + 7.8f, _tsOrigin.Z + 3.4f));

            hubConcrete.AddCylinder(_tsOrigin, 3.05f, 2.95f, 0.24f, 30, true);

            // Insulated ribbed steel jacket covering back 240° (120° cutaway window facing +X/+Z)
            float cutawayCenter = Pi * 0.25f;
            float windowArc = Pi * 0.68f;
            float jacketStart = cutawayCenter + windowArc * 0.5f;
            float jacketLen = TwoPi - windowArc;

            int staveCount = 16;
            for (int i = 0; i < staveCount; i++)
            {
                float a = jacketStart + ((i + 0.5f) / staveCount) * jacketLen;
                Vec3 p = new Vec3(_tsOrigin.X + (float)Math.Sin(a) * 2.66f, _tsOrigin.Y + 3.34f, _tsOrigin.Z + (float)Math.Cos(a) * 2.66f);
                hubBrushedSteel.AddBox(p, new Vec3(0.76f, 6.20f, 0.12f), a);
            }
            for (int i = 0; i <= 10; i++)
            {
                float a = jacketStart + (i / 10f) * jacketLen;
                Vec3 p = new Vec3(_tsOrigin.X + (float)Math.Sin(a) * 2.74f, _tsOrigin.Y + 3.34f, _tsOrigin.Z + (float)Math.Cos(a) * 2.74f);
                hubDarkSteel.AddBox(p, new Vec3(0.10f, 6.22f, 0.16f), a);
            }
            float[] tsHoops = new float[] { 0.35f, 2.25f, 4.35f, 6.38f };
            for (int i = 0; i < tsHoops.Length; i++)
            {
                hubDarkSteel.AddTorus(new Vec3(_tsOrigin.X, _tsOrigin.Y + tsHoops[i], _tsOrigin.Z), 2.74f, 0.06f, 32, 8, true);
            }

            // Cutaway curved glass window (6 faceted clear panels across front 120° arc)
            int glassSegs = 6;
            float winStart = cutawayCenter - windowArc * 0.5f;
            for (int i = 0; i < glassSegs; i++)
            {
                float a = winStart + ((i + 0.5f) / glassSegs) * windowArc;
                Vec3 p = new Vec3(_tsOrigin.X + (float)Math.Sin(a) * 2.66f, _tsOrigin.Y + 3.34f, _tsOrigin.Z + (float)Math.Cos(a) * 2.66f);
                hubGlass.AddBox(p, new Vec3(0.98f, 6.08f, 0.04f), a);
            }

            // Domed top manifold cap + vertical side pipe + elevated steel catwalk & safety handrails
            AddDomeY(hubBrushedSteel, new Vec3(_tsOrigin.X, _tsOrigin.Y + 6.42f, _tsOrigin.Z), 2.72f, 0.96f, true, 24);
            hubDarkSteel.AddCylinder(new Vec3(_tsOrigin.X, _tsOrigin.Y + 7.18f, _tsOrigin.Z), 0.56f, 0.48f, 0.55f, 16, true);
            hubBrushedSteel.AddCylinder(new Vec3(_tsOrigin.X - 2.98f, _tsOrigin.Y + 0.20f, _tsOrigin.Z + 0.40f), 0.14f, 0.14f, 6.8f, 10, true);
            hubDarkSteel.AddRing(new Vec3(_tsOrigin.X, _tsOrigin.Y + 5.55f, _tsOrigin.Z), 2.86f, 3.38f, 32, 0f, TwoPi);
            hubBrushedAlum.AddTorus(new Vec3(_tsOrigin.X, _tsOrigin.Y + 6.28f, _tsOrigin.Z), 3.36f, 0.032f, 32, 6, true);
            hubBrushedAlum.AddTorus(new Vec3(_tsOrigin.X, _tsOrigin.Y + 5.92f, _tsOrigin.Z), 3.36f, 0.024f, 32, 6, true);
            for (int i = 0; i < 12; i++)
            {
                float a = (i / 12f) * TwoPi;
                hubBrushedSteel.AddCylinder(
                    new Vec3(_tsOrigin.X + (float)Math.Cos(a) * 3.36f, _tsOrigin.Y + 5.53f, _tsOrigin.Z + (float)Math.Sin(a) * 3.36f),
                    0.025f, 0.025f, 0.78f, 6, true);
            }

            // Animated Thermal Store Fluid Core (unit height cylinder scaled dynamically to ThermalStoreSoc)
            // Emissive = 0 so Renderer uses the stratified ThermalStrat baseCol as the emissive hue!
            _tsFluidMat = MakeMat(0xffffff, 0x000000, 0.42f, 0.36f, 0.04f, 1f, MaterialPattern.ThermalStrat, 1f, false, false);
            MeshBuilder tsFluidMb = new MeshBuilder();
            tsFluidMb.AddCylinder(Vec3.Zero, 2.40f, 2.40f, 1.0f, 26, true);
            _tsFluidMesh = OwnMesh(r.UploadMesh(tsFluidMb));
            _tsFluidModel = Mat4.Identity();

            _tsMeniscusMat = MakeMat(0xff6b2c, 0xff4518, 0.85f, 0.28f, 0.05f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder tsMenMb = new MeshBuilder();
            tsMenMb.AddCylinder(new Vec3(0f, -0.07f, 0f), 2.42f, 2.42f, 0.14f, 26, true);
            _tsMeniscusMesh = OwnMesh(r.UploadMesh(tsMenMb));
            _tsMeniscusModel = Mat4.Identity();

            // -----------------------------------------------------------------
            // 2B. 10,000 m³ RC Drought Rainwater Cistern (rainwater_cistern)
            // -----------------------------------------------------------------
            _cisternOrigin = new Vec3(4.8f, deckY, -3.2f);
            float cW = 7.4f;
            float cH = 4.7f;
            float cD = 5.6f;
            float wallT = 0.28f;

            AddPickTarget("rainwater_cistern",
                new Vec3(_cisternOrigin.X - cW * 0.5f, _cisternOrigin.Y, _cisternOrigin.Z - cD * 0.5f),
                new Vec3(_cisternOrigin.X + cW * 0.5f, _cisternOrigin.Y + cH + 0.3f, _cisternOrigin.Z + cD * 0.5f));

            hubConcrete.AddBox(new Vec3(_cisternOrigin.X, _cisternOrigin.Y + 0.12f, _cisternOrigin.Z), new Vec3(cW, 0.24f, cD));
            hubConcrete.AddBox(new Vec3(_cisternOrigin.X, _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z - cD * 0.5f + wallT * 0.5f), new Vec3(cW, cH, wallT));
            hubConcrete.AddBox(new Vec3(_cisternOrigin.X - cW * 0.5f + wallT * 0.5f, _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z + wallT * 0.5f), new Vec3(wallT, cH, cD - wallT));
            hubConcrete.AddBox(new Vec3(_cisternOrigin.X + cW * 0.5f - wallT * 0.5f, _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z + wallT * 0.5f), new Vec3(wallT, cH, cD - wallT));

            float[] cRibXs = new float[] { -2.4f, 0f, 2.4f };
            for (int i = 0; i < cRibXs.Length; i++)
            {
                hubConcrete.AddBox(new Vec3(_cisternOrigin.X + cRibXs[i], _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z - cD * 0.5f - 0.10f), new Vec3(0.26f, cH, 0.26f));
            }
            float[] cBeamXs = new float[] { -2.3f, 0f, 2.3f };
            for (int i = 0; i < cBeamXs.Length; i++)
            {
                hubConcrete.AddBox(new Vec3(_cisternOrigin.X + cBeamXs[i], _cisternOrigin.Y + cH + 0.10f, _cisternOrigin.Z), new Vec3(0.28f, 0.26f, cD));
                hubDarkSteel.AddBox(new Vec3(_cisternOrigin.X + cBeamXs[i], _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z + cD * 0.5f - 0.06f), new Vec3(0.10f, cH, 0.12f));
            }
            hubDarkSteel.AddBox(new Vec3(_cisternOrigin.X, _cisternOrigin.Y + cH + 0.10f, _cisternOrigin.Z + cD * 0.5f - 0.14f), new Vec3(cW, 0.28f, 0.28f));

            // 6 Interior Structural RC Support Pillars (visible through cutaway glass & translucent water)
            float[] cColXs = new float[] { -2.1f, 0f, 2.1f };
            float[] cColZs = new float[] { -1.1f, 1.1f };
            for (int ix = 0; ix < cColXs.Length; ix++)
            {
                for (int iz = 0; iz < cColZs.Length; iz++)
                {
                    float px = _cisternOrigin.X + cColXs[ix];
                    float pz = _cisternOrigin.Z + cColZs[iz];
                    hubConcrete.AddCylinder(new Vec3(px, _cisternOrigin.Y + 0.24f, pz), 0.26f, 0.22f, cH - 0.22f, 12, true);
                    hubConcrete.AddBox(new Vec3(px, _cisternOrigin.Y + cH - 0.08f, pz), new Vec3(0.68f, 0.16f, 0.68f));
                }
            }

            // Cutaway front glass wall
            hubGlass.AddBox(new Vec3(_cisternOrigin.X, _cisternOrigin.Y + cH * 0.5f + 0.12f, _cisternOrigin.Z + cD * 0.5f - 0.05f), new Vec3(cW - 0.56f, cH - 0.20f, 0.04f));

            // Animated Cistern Water Volume + WaterCaustic Top Rippling Surface
            _cisternWaterMat = MakeMat(0x0284c7, 0x0369a1, 0.42f, 0.15f, 0.04f, 0.60f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder cWaterMb = new MeshBuilder();
            cWaterMb.AddBox(new Vec3(0f, 0.5f, 0f), new Vec3(cW - wallT * 2f - 0.08f, 1.0f, cD - wallT * 2f - 0.08f));
            _cisternWaterMesh = OwnMesh(r.UploadMesh(cWaterMb));
            _cisternWaterModel = Mat4.Identity();

            _cisternSurfaceMat = MakeMat(0x38bdf8, 0x000000, 0.45f, 0.12f, 0.12f, 0.86f, MaterialPattern.WaterCaustic, 1.4f, false, false);
            MeshBuilder cSurfMb = new MeshBuilder();
            cSurfMb.AddBox(new Vec3(0f, 0.02f, 0f), new Vec3(cW - wallT * 2f - 0.10f, 0.04f, cD - wallT * 2f - 0.10f));
            _cisternSurfaceMesh = OwnMesh(r.UploadMesh(cSurfMb));
            _cisternSurfaceModel = Mat4.Identity();

            // -----------------------------------------------------------------
            // 2C. 8.0 MW_th Backup HTHP Bank (hthp_bank)
            // -----------------------------------------------------------------
            Vec3 hthpOrigin = new Vec3(-5.6f, deckY, 4.4f);
            AddPickTarget("hthp_bank",
                new Vec3(hthpOrigin.X - 3.5f, hthpOrigin.Y, hthpOrigin.Z - 1.3f),
                new Vec3(hthpOrigin.X + 3.5f, hthpOrigin.Y + 2.7f, hthpOrigin.Z + 1.3f));

            hubDarkSteel.AddBox(new Vec3(hthpOrigin.X, hthpOrigin.Y + 0.11f, hthpOrigin.Z), new Vec3(6.8f, 0.22f, 2.4f));
            hubHazard.AddBox(new Vec3(hthpOrigin.X, hthpOrigin.Y + 0.14f, hthpOrigin.Z + 1.21f), new Vec3(6.82f, 0.08f, 0.08f));

            _hthpDischargeMat = MakeMat(0xf59e0b, 0xd97706, 0.30f, 0.28f, 0.72f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder hthpDisMb = new MeshBuilder();

            for (int i = 0; i < 4; i++)
            {
                float ux = hthpOrigin.X - 2.45f + i * 1.63f;
                hubDarkSteel.AddBox(new Vec3(ux, hthpOrigin.Y + 1.16f, hthpOrigin.Z), new Vec3(1.36f, 1.88f, 1.72f));
                hubFinCoil.AddBox(new Vec3(ux, hthpOrigin.Y + 1.56f, hthpOrigin.Z + 0.87f), new Vec3(1.18f, 0.82f, 0.04f));
                hubFinCoil.AddBox(new Vec3(ux, hthpOrigin.Y + 1.16f, hthpOrigin.Z - 0.87f), new Vec3(1.18f, 1.48f, 0.04f));

                // Twin vertical compressor drums in lower front bay
                float[] dOffsets = new float[] { -0.30f, 0.30f };
                for (int d = 0; d < 2; d++)
                {
                    float dx = ux + dOffsets[d];
                    hubBrushedSteel.AddCylinder(new Vec3(dx, hthpOrigin.Y + 0.22f, hthpOrigin.Z + 0.98f), 0.24f, 0.24f, 1.04f, 12, true);
                    AddDomeY(hubDarkSteel, new Vec3(dx, hthpOrigin.Y + 1.26f, hthpOrigin.Z + 0.98f), 0.24f, 0.11f, true, 12);
                }

                // Fan shroud + rim
                hubBrushedSteel.AddCylinder(new Vec3(ux, hthpOrigin.Y + 2.10f, hthpOrigin.Z), 0.52f, 0.56f, 0.36f, 16, false);
                hubDarkSteel.AddTorus(new Vec3(ux, hthpOrigin.Y + 2.46f, hthpOrigin.Z), 0.56f, 0.04f, 18, 6, true);

                // Discharge riser into shared glowing header
                hthpDisMb.AddCylinder(new Vec3(ux, hthpOrigin.Y + 1.58f, hthpOrigin.Z - 0.98f), 0.085f, 0.085f, 0.74f, 10, true);

                _hthpFanCenters[i] = new Vec3(ux, hthpOrigin.Y + 2.28f, hthpOrigin.Z);
                _hthpFanModels[i] = Mat4.Translation(_hthpFanCenters[i]);
            }
            hthpDisMb.AddCylinderAxis(
                new Vec3(hthpOrigin.X - 2.8f, hthpOrigin.Y + 2.30f, hthpOrigin.Z - 0.98f),
                new Vec3(hthpOrigin.X + 2.8f, hthpOrigin.Y + 2.30f, hthpOrigin.Z - 0.98f),
                0.13f, 12);
            AddStaticMesh(r, hthpDisMb, _hthpDischargeMat, false);

            _hthpFanMesh = OwnMesh(BuildMergedFanMesh(r, 4, 0.48f));

            // -----------------------------------------------------------------
            // 2D. SrBr₂ / K₂CO₃ TCES Reactor Vessels (tces)
            // -----------------------------------------------------------------
            Vec3 tcesOrigin = new Vec3(1.2f, deckY, 4.8f);
            AddPickTarget("tces",
                new Vec3(tcesOrigin.X - 2.4f, tcesOrigin.Y, tcesOrigin.Z - 1.1f),
                new Vec3(tcesOrigin.X + 2.4f, tcesOrigin.Y + 3.6f, tcesOrigin.Z + 1.1f));

            hubDarkSteel.AddBox(new Vec3(tcesOrigin.X, tcesOrigin.Y + 0.10f, tcesOrigin.Z), new Vec3(4.6f, 0.20f, 1.9f));

            _tcesMat = MakeMat(0xfbbf24, 0xd97706, 0.35f, 0.28f, 0.55f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder tcesGlowMb = new MeshBuilder();

            for (int i = 0; i < 3; i++)
            {
                float vx = tcesOrigin.X - 1.48f + i * 1.48f;
                hubDarkSteel.AddCylinder(new Vec3(vx, tcesOrigin.Y + 0.19f, tcesOrigin.Z), 0.68f, 0.58f, 0.35f, 16, true);
                hubBrushedSteel.AddCylinder(new Vec3(vx, tcesOrigin.Y + 0.49f, tcesOrigin.Z), 0.64f, 0.64f, 2.15f, 18, true);
                AddDomeY(hubBrushedSteel, new Vec3(vx, tcesOrigin.Y + 2.63f, tcesOrigin.Z), 0.64f, 0.36f, true, 18);
                tcesGlowMb.AddCylinder(new Vec3(vx, tcesOrigin.Y + 1.35f, tcesOrigin.Z), 0.665f, 0.665f, 0.42f, 18, true);
                hubDarkSteel.AddTorus(new Vec3(vx, tcesOrigin.Y + 0.60f, tcesOrigin.Z), 0.66f, 0.045f, 20, 6, true);
                hubDarkSteel.AddTorus(new Vec3(vx, tcesOrigin.Y + 2.52f, tcesOrigin.Z), 0.66f, 0.045f, 20, 6, true);
                hubDarkSteel.AddCylinder(new Vec3(vx, tcesOrigin.Y + 2.90f, tcesOrigin.Z), 0.13f, 0.11f, 0.52f, 10, true);
            }
            tcesGlowMb.AddCylinderAxis(
                new Vec3(tcesOrigin.X - 1.8f, tcesOrigin.Y + 3.42f, tcesOrigin.Z),
                new Vec3(tcesOrigin.X + 1.8f, tcesOrigin.Y + 3.42f, tcesOrigin.Z),
                0.10f, 12);
            hubBrushedSteel.AddCylinderAxis(
                new Vec3(tcesOrigin.X - 1.8f, tcesOrigin.Y + 3.18f, tcesOrigin.Z - 0.45f),
                new Vec3(tcesOrigin.X + 1.8f, tcesOrigin.Y + 3.18f, tcesOrigin.Z - 0.45f),
                0.075f, 10);
            AddStaticMesh(r, tcesGlowMb, _tcesMat, false);

            // -----------------------------------------------------------------
            // 2E. 2 MW / 16 MWh TI-PTES Carnot Battery (ti_ptes)
            // -----------------------------------------------------------------
            Vec3 ptesOrigin = new Vec3(6.2f, deckY, 4.4f);
            AddPickTarget("ti_ptes",
                new Vec3(ptesOrigin.X - 2.3f, ptesOrigin.Y, ptesOrigin.Z - 1.2f),
                new Vec3(ptesOrigin.X + 2.3f, ptesOrigin.Y + 3.6f, ptesOrigin.Z + 1.2f));

            hubDarkSteel.AddBox(new Vec3(ptesOrigin.X, ptesOrigin.Y + 0.11f, ptesOrigin.Z), new Vec3(4.2f, 0.22f, 2.3f));

            _ptesHotMat = MakeMat(0xf97316, 0xea580c, 0.45f, 0.26f, 0.55f, 1f, MaterialPattern.Solid, 1f, false, false);
            _ptesColdMat = MakeMat(0x06b6d4, 0x0891b2, 0.45f, 0.26f, 0.55f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder ptesHotMb = new MeshBuilder();
            MeshBuilder ptesColdMb = new MeshBuilder();

            float[] ptesXs = new float[] { -1.32f, 1.32f };
            float[] ptesFlangeYs = new float[] { 0.45f, 1.25f, 2.20f, 2.95f };
            for (int side = 0; side < 2; side++)
            {
                float px = ptesOrigin.X + ptesXs[side];
                hubBrushedSteel.AddCylinder(new Vec3(px, ptesOrigin.Y + 0.22f, ptesOrigin.Z), 0.84f, 0.84f, 2.85f, 18, true);
                AddDomeY(hubBrushedSteel, new Vec3(px, ptesOrigin.Y + 3.07f, ptesOrigin.Z), 0.84f, 0.42f, true, 18);
                MeshBuilder bandTarget = (side == 0) ? ptesHotMb : ptesColdMb;
                bandTarget.AddCylinder(new Vec3(px, ptesOrigin.Y + 1.40f, ptesOrigin.Z), 0.875f, 0.875f, 0.65f, 18, true);
                for (int f = 0; f < ptesFlangeYs.Length; f++)
                {
                    hubDarkSteel.AddTorus(new Vec3(px, ptesOrigin.Y + ptesFlangeYs[f], ptesOrigin.Z), 0.86f, 0.045f, 20, 6, true);
                }
            }

            // Bridging turbo-machinery skid + cycle pipes + spinning flywheel
            hubDarkSteel.AddBox(new Vec3(ptesOrigin.X, ptesOrigin.Y + 0.68f, ptesOrigin.Z), new Vec3(1.15f, 0.92f, 1.25f));
            hubBrushedSteel.AddCylinderAxis(
                new Vec3(ptesOrigin.X - 0.48f, ptesOrigin.Y + 1.48f, ptesOrigin.Z),
                new Vec3(ptesOrigin.X + 0.48f, ptesOrigin.Y + 1.48f, ptesOrigin.Z),
                0.38f, 14);
            ptesHotMb.AddCylinderAxis(
                new Vec3(ptesOrigin.X - 1.28f, ptesOrigin.Y + 2.65f, ptesOrigin.Z + 0.25f),
                new Vec3(ptesOrigin.X + 1.28f, ptesOrigin.Y + 2.65f, ptesOrigin.Z + 0.25f),
                0.10f, 10);
            ptesColdMb.AddCylinderAxis(
                new Vec3(ptesOrigin.X - 1.28f, ptesOrigin.Y + 2.25f, ptesOrigin.Z - 0.25f),
                new Vec3(ptesOrigin.X + 1.28f, ptesOrigin.Y + 2.25f, ptesOrigin.Z - 0.25f),
                0.10f, 10);

            AddStaticMesh(r, ptesHotMb, _ptesHotMat, false);

            // Flywheel (rotated in YZ plane around X axis)
            MeshBuilder fwMb = new MeshBuilder();
            fwMb.AddTorus(Vec3.Zero, 0.42f, 0.065f, 18, 8, false);
            fwMb.AddBox(Vec3.Zero, new Vec3(0.06f, 0.80f, 0.08f));
            fwMb.TransformAll(Mat4.RotationY(HalfPi));
            _ptesFlywheelMesh = OwnMesh(r.UploadMesh(fwMb));
            _ptesFlywheelCenter = new Vec3(ptesOrigin.X, ptesOrigin.Y + 1.48f, ptesOrigin.Z);
            _ptesFlywheelModel = Mat4.Translation(_ptesFlywheelCenter);

            // -----------------------------------------------------------------
            // 2F. Micro-ORC Turbine-Expander + Condenser + Generator Skid (micro_orc)
            // -----------------------------------------------------------------
            Vec3 orcOrigin = new Vec3(0.2f, deckY, -6.4f);
            AddPickTarget("micro_orc",
                new Vec3(orcOrigin.X - 2.0f, orcOrigin.Y, orcOrigin.Z - 1.2f),
                new Vec3(orcOrigin.X + 2.0f, orcOrigin.Y + 2.3f, orcOrigin.Z + 1.2f));

            hubDarkSteel.AddBox(new Vec3(orcOrigin.X, orcOrigin.Y + 0.17f, orcOrigin.Z), new Vec3(3.7f, 0.34f, 2.1f));
            hubBrushedSteel.AddCylinderAxis(
                new Vec3(orcOrigin.X - 1.32f, orcOrigin.Y + 0.92f, orcOrigin.Z - 0.52f),
                new Vec3(orcOrigin.X + 1.32f, orcOrigin.Y + 0.92f, orcOrigin.Z - 0.52f),
                0.48f, 14);
            hubDarkSteel.AddSphere(new Vec3(orcOrigin.X - 1.32f, orcOrigin.Y + 0.92f, orcOrigin.Z - 0.52f), 0.475f, 8, 14);
            hubDarkSteel.AddSphere(new Vec3(orcOrigin.X + 1.32f, orcOrigin.Y + 0.92f, orcOrigin.Z - 0.52f), 0.475f, 8, 14);

            hubBrushedAlum.AddCylinderAxis(
                new Vec3(orcOrigin.X - 1.39f, orcOrigin.Y + 1.48f, orcOrigin.Z + 0.32f),
                new Vec3(orcOrigin.X - 0.57f, orcOrigin.Y + 1.48f, orcOrigin.Z + 0.32f),
                0.56f, 16);
            hubDarkSteel.AddCylinderAxis(
                new Vec3(orcOrigin.X + 0.38f, orcOrigin.Y + 1.48f, orcOrigin.Z + 0.32f),
                new Vec3(orcOrigin.X + 1.52f, orcOrigin.Y + 1.48f, orcOrigin.Z + 0.32f),
                0.52f, 16);

            _orcTurbineMat = MakeMat(0x10b981, 0x059669, 0.45f, 0.24f, 0.78f, 1f, MaterialPattern.Solid, 1f, false, false);
            _orcBusMat = MakeMat(0x34d399, 0x10b981, 0.55f, 0.20f, 0.65f, 1f, MaterialPattern.Solid, 1f, false, false);

            MeshBuilder orcBusMb = new MeshBuilder();
            orcBusMb.AddBox(new Vec3(orcOrigin.X, orcOrigin.Y + 0.52f, orcOrigin.Z + 0.96f), new Vec3(3.2f, 0.12f, 0.14f));
            orcBusMb.AddBox(new Vec3(orcOrigin.X + 0.95f, orcOrigin.Y + 0.96f, orcOrigin.Z + 0.90f), new Vec3(0.14f, 0.85f, 0.14f));
            AddStaticMesh(r, orcBusMb, _orcBusMat, false);

            // Exposed Spinning Drive Shaft + 3-Fin Rotor Coupling
            MeshBuilder orcShaftMb = new MeshBuilder();
            orcShaftMb.AddCylinderAxis(new Vec3(-0.62f, 0f, 0f), new Vec3(0.62f, 0f, 0f), 0.16f, 12);
            _orcShaftMesh = OwnMesh(r.UploadMesh(orcShaftMb));

            RawMeshCombiner finComb = new RawMeshCombiner();
            for (int f = 0; f < 3; f++)
            {
                MeshBuilder fm = new MeshBuilder();
                fm.AddBox(Vec3.Zero, new Vec3(0.68f, 0.52f, 0.06f));
                finComb.AppendTransformed(fm, Mat4.RotationX((f / 3f) * Pi));
            }
            _orcFinsMesh = OwnMesh(finComb.Upload(r));
            _orcRotorCenter = new Vec3(orcOrigin.X, orcOrigin.Y + 1.48f, orcOrigin.Z + 0.32f);
            _orcRotorModel = Mat4.Translation(_orcRotorCenter);

            // -----------------------------------------------------------------
            // 2G. Finned MOF Adsorption Chiller Bank (mof_chiller)
            // -----------------------------------------------------------------
            Vec3 mofOrigin = new Vec3(-8.6f, deckY, -0.4f);
            AddPickTarget("mof_chiller",
                new Vec3(mofOrigin.X - 1.5f, mofOrigin.Y, mofOrigin.Z - 1.6f),
                new Vec3(mofOrigin.X + 1.5f, mofOrigin.Y + 2.8f, mofOrigin.Z + 1.6f));

            hubDarkSteel.AddBox(new Vec3(mofOrigin.X, mofOrigin.Y + 0.12f, mofOrigin.Z), new Vec3(2.8f, 0.24f, 3.0f));
            _mofMat = MakeMat(0x2dd4bf, 0x0d9488, 0.35f, 0.28f, 0.55f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder mofGlowMb = new MeshBuilder();

            float[] mofZs = new float[] { -0.72f, 0.72f };
            for (int i = 0; i < 2; i++)
            {
                float mz = mofOrigin.Z + mofZs[i];
                hubBrushedSteel.AddCylinderAxis(
                    new Vec3(mofOrigin.X - 1.12f, mofOrigin.Y + 1.05f, mz),
                    new Vec3(mofOrigin.X + 1.12f, mofOrigin.Y + 1.05f, mz),
                    0.56f, 16);
                mofGlowMb.AddCylinderAxis(
                    new Vec3(mofOrigin.X - 0.22f, mofOrigin.Y + 1.05f, mz),
                    new Vec3(mofOrigin.X + 0.22f, mofOrigin.Y + 1.05f, mz),
                    0.595f, 16);
            }
            hubFinCoil.AddBox(new Vec3(mofOrigin.X, mofOrigin.Y + 2.12f, mofOrigin.Z), new Vec3(2.35f, 0.92f, 2.55f));
            hubDarkSteel.AddBox(new Vec3(mofOrigin.X, mofOrigin.Y + 2.62f, mofOrigin.Z), new Vec3(2.48f, 0.14f, 2.68f));
            AddStaticMesh(r, mofGlowMb, _mofMat, false);

            // -----------------------------------------------------------------
            // 2H. Louvered N+1 Low-Noise Dry Coolers (dry_coolers)
            // -----------------------------------------------------------------
            Vec3 dcOrigin = new Vec3(9.4f, deckY, 0.2f);
            AddPickTarget("dry_coolers",
                new Vec3(dcOrigin.X - 1.4f, dcOrigin.Y, dcOrigin.Z - 2.6f),
                new Vec3(dcOrigin.X + 1.4f, dcOrigin.Y + 2.6f, dcOrigin.Z + 2.6f));

            float[] dcLegXs = new float[] { -1.05f, 1.05f };
            float[] dcLegZs = new float[] { -2.1f, 0f, 2.1f };
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 3; iz++)
                {
                    hubDarkSteel.AddBox(new Vec3(dcOrigin.X + dcLegXs[ix], dcOrigin.Y + 0.26f, dcOrigin.Z + dcLegZs[iz]), new Vec3(0.18f, 0.52f, 0.18f));
                }
            }
            hubDarkSteel.AddBox(new Vec3(dcOrigin.X, dcOrigin.Y + 1.24f, dcOrigin.Z), new Vec3(2.46f, 1.48f, 4.80f));
            hubFinCoil.AddBox(new Vec3(dcOrigin.X + 1.24f, dcOrigin.Y + 1.24f, dcOrigin.Z), new Vec3(0.04f, 1.42f, 4.76f));
            hubFinCoil.AddBox(new Vec3(dcOrigin.X - 1.24f, dcOrigin.Y + 1.24f, dcOrigin.Z), new Vec3(0.04f, 1.42f, 4.76f));
            hubFinCoil.AddBox(new Vec3(dcOrigin.X, dcOrigin.Y + 1.24f, dcOrigin.Z + 2.41f), new Vec3(2.42f, 1.42f, 0.04f));
            hubFinCoil.AddBox(new Vec3(dcOrigin.X, dcOrigin.Y + 1.24f, dcOrigin.Z - 2.41f), new Vec3(2.42f, 1.42f, 0.04f));
            hubDarkSteel.AddBox(new Vec3(dcOrigin.X, dcOrigin.Y + 2.02f, dcOrigin.Z), new Vec3(2.58f, 0.14f, 4.92f));

            for (int i = 0; i < 3; i++)
            {
                float fz = dcOrigin.Z - 1.52f + i * 1.52f;
                hubBrushedSteel.AddCylinder(new Vec3(dcOrigin.X, dcOrigin.Y + 2.05f, fz), 0.68f, 0.72f, 0.34f, 18, false);
                hubDarkSteel.AddTorus(new Vec3(dcOrigin.X, dcOrigin.Y + 2.39f, fz), 0.72f, 0.045f, 20, 6, true);
                _dcFanCenters[i] = new Vec3(dcOrigin.X, dcOrigin.Y + 2.22f, fz);
                _dcFanModels[i] = Mat4.Translation(_dcFanCenters[i]);
            }
            _dcFanMesh = OwnMesh(BuildMergedFanMesh(r, 5, 0.64f));

            // -----------------------------------------------------------------
            // 2I. Semiconductor Grade A Ultrapure Water Train (ultrapure_train)
            // -----------------------------------------------------------------
            Vec3 upOrigin = new Vec3(4.6f, deckY, -7.8f);
            AddPickTarget("ultrapure_train",
                new Vec3(upOrigin.X - 2.7f, upOrigin.Y, upOrigin.Z - 1.0f),
                new Vec3(upOrigin.X + 2.7f, upOrigin.Y + 2.7f, upOrigin.Z + 1.0f));

            hubDarkSteel.AddBox(new Vec3(upOrigin.X, upOrigin.Y + 0.11f, upOrigin.Z), new Vec3(5.1f, 0.22f, 1.75f));

            // Stage 1: Twin vertical Ultrafiltration (UF) pressure vessels
            float[] ufXs = new float[] { -2.05f, -1.35f };
            for (int i = 0; i < 2; i++)
            {
                float ux = upOrigin.X + ufXs[i];
                hubBrushedAlum.AddCylinder(new Vec3(ux, upOrigin.Y + 0.22f, upOrigin.Z), 0.28f, 0.28f, 2.15f, 14, true);
                AddDomeY(hubDarkSteel, new Vec3(ux, upOrigin.Y + 2.37f, upOrigin.Z), 0.28f, 0.14f, true, 14);
            }

            // Stage 2: Stacked horizontal MCDI/2D membrane housings + vertical DI vessel
            float[] mcdiYs = new float[] { 0.85f, 1.52f, 2.18f };
            for (int i = 0; i < 3; i++)
            {
                hubBrushedSteel.AddCylinderAxis(
                    new Vec3(upOrigin.X - 0.88f, upOrigin.Y + mcdiYs[i], upOrigin.Z),
                    new Vec3(upOrigin.X + 0.44f, upOrigin.Y + mcdiYs[i], upOrigin.Z),
                    0.20f, 12);
            }
            ptesColdMb.AddCylinder(new Vec3(upOrigin.X + 0.85f, upOrigin.Y + 0.22f, upOrigin.Z), 0.32f, 0.32f, 2.15f, 14, true);
            AddStaticMesh(r, ptesColdMb, _ptesColdMat, false);

            // Stage 3: Glowing Violet Quartz-Sleeved UV-C LED Chamber
            _uvCoreMat = MakeMat(0xa5b4fc, 0x6366f1, 1.75f, 0.15f, 0.10f, 1f, MaterialPattern.Solid, 1f, false, false);
            MeshBuilder uvMb = new MeshBuilder();
            uvMb.AddCylinder(new Vec3(upOrigin.X + 1.58f, upOrigin.Y + 0.36f, upOrigin.Z), 0.16f, 0.16f, 1.92f, 14, true);
            uvMb.AddBox(new Vec3(upOrigin.X + 2.57f, upOrigin.Y + 1.15f, upOrigin.Z), new Vec3(0.06f, 1.15f, 0.62f));
            AddStaticMesh(r, uvMb, _uvCoreMat, false);

            hubGlass.AddCylinder(new Vec3(upOrigin.X + 1.58f, upOrigin.Y + 0.31f, upOrigin.Z), 0.32f, 0.32f, 2.02f, 16, false);
            hubBrushedSteel.AddCylinder(new Vec3(upOrigin.X + 1.58f, upOrigin.Y + 0.26f, upOrigin.Z), 0.36f, 0.36f, 0.12f, 14, true);
            hubBrushedSteel.AddCylinder(new Vec3(upOrigin.X + 1.58f, upOrigin.Y + 2.26f, upOrigin.Z), 0.36f, 0.36f, 0.12f, 14, true);

            // Stage 4: BDD / Ti4O7 PFAS Mineralisation Unit + Overhead Stainless Header Piping
            hubDarkSteel.AddBox(new Vec3(upOrigin.X + 2.22f, upOrigin.Y + 1.10f, upOrigin.Z), new Vec3(0.68f, 1.75f, 0.92f));
            hubBrushedAlum.AddCylinderAxis(
                new Vec3(upOrigin.X - 2.05f, upOrigin.Y + 2.55f, upOrigin.Z),
                new Vec3(upOrigin.X + 2.22f, upOrigin.Y + 2.55f, upOrigin.Z),
                0.055f, 8);
            float[] upDropXs = new float[] { -2.05f, -1.35f, 0.85f, 1.58f, 2.22f };
            for (int i = 0; i < upDropXs.Length; i++)
            {
                hubBrushedAlum.AddCylinder(new Vec3(upOrigin.X + upDropXs[i], upOrigin.Y + 2.19f, upOrigin.Z), 0.045f, 0.045f, 0.38f, 8, true);
            }

            // Upload all batched static Central Utility Hub meshes (only 7 draw calls for all shared hub steel/concrete/glass!)
            AddStaticMesh(r, hubConcrete, concreteMat, false);
            AddStaticMesh(r, hubDarkSteel, darkSteelMat, false);
            AddStaticMesh(r, hubBrushedSteel, brushedSteelMat, false);
            AddStaticMesh(r, hubBrushedAlum, _brushedAlumMat, false);
            AddStaticMesh(r, hubFinCoil, finCoilMat, false);
            AddStaticMesh(r, hubHazard, hazardMat, false);
            AddStaticMesh(r, hubGlass, glassTankMat, true);
        }

        public void Update(SimResult sim, float dt, float elapsed)
        {
            if (_disposed || sim == null) return;
            bool animate = dt > 0f || _firstUpdate;

            // 1. Update 5 Subterranean Vaults (zero per-frame heap allocations)
            VaultTelemetry[] vaults = sim.Vaults;
            if (vaults != null)
            {
                int count = vaults.Length < 5 ? vaults.Length : 5;
                for (int i = 0; i < count; i++)
                {
                    VaultTelemetry v = vaults[i];
                    VaultStateNode node = _vaultNodes[i];
                    if (v == null || node == null) continue;

                    bool faulted = v.Faulted;
                    float loadFactor = Clamp01((float)(v.ItMw * 0.5));
                    bool stateChanged = !node.LastFaultedValid || (faulted != node.LastFaulted);
                    node.Faulted = faulted;

                    if (faulted)
                    {
                        if (stateChanged)
                        {
                            node.RackLedMat.Emissive = Vec3.FromHex(0xef4444);
                            node.HotHeaderMat.EmissiveIntensity = 0.12f;
                            node.HtsMat.EmissiveIntensity = 0.18f;
                            node.BufferFluidMat.EmissiveIntensity = 0.20f;
                            node.LastFaultedValid = true;
                            node.LastFaulted = true;
                            node.LastLoadFactor = -1f;
                        }
                        if (animate || stateChanged)
                        {
                            float pulse = (float)Math.Abs(Math.Sin(elapsed * 5.2f));
                            node.RackLedMat.EmissiveIntensity = 0.55f + 1.35f * pulse;

                            float sin6 = (float)Math.Sin(elapsed * 6.0f);
                            node.FaultBeaconMat.Alpha = 0.60f + 0.35f * sin6;
                            float bScale = 1.0f + 0.05f * sin6;
                            WriteVaultLocalOffsetScale(node.BaseModel, 0f, 1.05f, 0f, bScale, node.FaultBeaconModel.M);

                            float haloPhase = (elapsed * 0.9f) % 1.0f;
                            if (haloPhase < 0f) haloPhase += 1.0f;
                            float hScale = 0.95f + haloPhase * 0.38f;
                            node.FaultHaloMat.Alpha = (1.0f - haloPhase) * 0.55f;
                            WriteVaultLocalOffsetScale(node.BaseModel, 0f, 1.05f, 0f, hScale, node.FaultHaloModel.M);
                        }
                    }
                    else
                    {
                        if (stateChanged)
                        {
                            node.RackLedMat.Emissive = Vec3.FromHex(0x22d3ee);
                            node.LastFaultedValid = true;
                            node.LastFaulted = false;
                        }
                        if (stateChanged || Math.Abs(loadFactor - node.LastLoadFactor) > 1e-4f)
                        {
                            node.RackLedMat.EmissiveIntensity = 0.55f + loadFactor * 1.25f;
                            node.HotHeaderMat.EmissiveIntensity = 0.35f + loadFactor * 0.95f;
                            node.HtsMat.EmissiveIntensity = 0.70f + loadFactor * 1.05f;
                            node.BufferFluidMat.EmissiveIntensity = 0.45f + loadFactor * 0.95f;
                            node.LastLoadFactor = loadFactor;
                        }
                    }
                }
            }

            // 2. 2,500 m³ Stratified Thermal Store live SOC level & calibrated glow
            ThermalTelemetry thermal = sim.Thermal;
            if (thermal != null)
            {
                float tsSoc = Clamp((float)thermal.ThermalStoreSoc, 0.06f, 1.0f);
                if (Math.Abs(tsSoc - _lastTsSoc) > 1e-4f)
                {
                    float fluidH = TsFluidMaxH * tsSoc;
                    WriteTranslateScale(_tsOrigin.X, _tsOrigin.Y + 0.26f, _tsOrigin.Z, 1f, fluidH, 1f, _tsFluidModel.M);
                    WriteTranslateScale(_tsOrigin.X, _tsOrigin.Y + 0.26f + fluidH, _tsOrigin.Z, 1f, 1f, 1f, _tsMeniscusModel.M);
                    _lastTsSoc = tsSoc;
                }

                int tsMode = thermal.ThermalStoreDischargeMw > 0.1 ? 2 : (thermal.ThermalStoreChargeMw > 0.1 ? 1 : 0);
                bool tsModeChanged = tsMode != _lastTsMode;
                if (tsModeChanged)
                {
                    _tsMeniscusMat.EmissiveIntensity = tsMode == 2 ? 1.05f : (tsMode == 1 ? 0.88f : 0.75f);
                    if (tsMode == 0) _tsFluidMat.EmissiveIntensity = 0.34f;
                    _lastTsMode = tsMode;
                }
                if (tsMode == 2 && (animate || tsModeChanged))
                {
                    _tsFluidMat.EmissiveIntensity = 0.48f + 0.10f * (float)Math.Sin(elapsed * 4.2f);
                }
                else if (tsMode == 1 && (animate || tsModeChanged))
                {
                    _tsFluidMat.EmissiveIntensity = 0.42f + 0.06f * (float)Math.Sin(elapsed * 2.5f);
                }

                // 4. 8.0 MW_th Backup HTHP Bank fans & glowing discharge header
                bool hthpActive = thermal.HthpThermalOutputMw > 0.05 || thermal.ScenarioBActive;
                int hthpFlag = hthpActive ? 1 : 0;
                if (hthpActive)
                {
                    if (animate || _lastHthpActive != 1)
                    {
                        _hthpDischargeMat.EmissiveIntensity = 1.15f + 0.40f * (float)Math.Sin(elapsed * 4.5f);
                    }
                }
                else if (_lastHthpActive != 0)
                {
                    _hthpDischargeMat.EmissiveIntensity = 0.20f;
                }
                _lastHthpActive = hthpFlag;

                if (dt > 0f || _firstUpdate)
                {
                    _hthpFanAngle += dt * (hthpActive ? 5.2f : 0.55f);
                    if (_hthpFanAngle > TwoPi) _hthpFanAngle -= TwoPi;
                    for (int i = 0; i < 4; i++)
                    {
                        WriteTranslateRotY(_hthpFanCenters[i], _hthpFanAngle + i * 0.65f, _hthpFanModels[i].M);
                    }
                }

                // 5. Exergy Cascade Bay (TCES, TI-PTES, Micro-ORC, MOF, Dry Coolers)
                bool tcesActive = thermal.TcesChargeMw > 0.05;
                if (tcesActive)
                {
                    if (animate || _lastTcesActive != 1)
                    {
                        _tcesMat.EmissiveIntensity = 1.25f + 0.25f * (float)Math.Sin(elapsed * 3.0f);
                    }
                }
                else if (_lastTcesActive != 0)
                {
                    _tcesMat.EmissiveIntensity = 0.22f;
                }
                _lastTcesActive = tcesActive ? 1 : 0;

                bool ptesActive = thermal.TiPtesChargeMw > 0.05;
                if (ptesActive)
                {
                    if (animate || _lastPtesActive != 1)
                    {
                        float pInt = 1.20f + 0.22f * (float)Math.Sin(elapsed * 3.4f);
                        _ptesHotMat.EmissiveIntensity = pInt;
                        _ptesColdMat.EmissiveIntensity = pInt;
                    }
                }
                else if (_lastPtesActive != 0)
                {
                    _ptesHotMat.EmissiveIntensity = 0.25f;
                    _ptesColdMat.EmissiveIntensity = 0.25f;
                }
                _lastPtesActive = ptesActive ? 1 : 0;

                if (dt > 0f || _firstUpdate)
                {
                    _ptesFlywheelAngle += dt * (ptesActive ? 6.5f : 0.45f);
                    if (_ptesFlywheelAngle > TwoPi) _ptesFlywheelAngle -= TwoPi;
                    WriteTranslateRotX(_ptesFlywheelCenter, _ptesFlywheelAngle, _ptesFlywheelModel.M);
                }

                float orcNorm = Clamp01((float)(thermal.OrcOutputKw / 420.0));
                if (Math.Abs(orcNorm - _lastOrcNorm) > 1e-4f)
                {
                    _orcTurbineMat.EmissiveIntensity = 0.22f + orcNorm * 1.45f;
                    _orcBusMat.EmissiveIntensity = 0.20f + orcNorm * 1.75f;
                    _lastOrcNorm = orcNorm;
                }
                if (dt > 0f || _firstUpdate)
                {
                    _orcRotorAngle += dt * (0.6f + orcNorm * 11.0f);
                    if (_orcRotorAngle > TwoPi) _orcRotorAngle -= TwoPi;
                    WriteTranslateRotX(_orcRotorCenter, _orcRotorAngle, _orcRotorModel.M);
                }

                bool mofActive = (thermal.CommercialHeatSalesMw + thermal.MofChillerMw) > 0.05;
                if (mofActive)
                {
                    if (animate || _lastMofActive != 1)
                    {
                        _mofMat.EmissiveIntensity = 1.25f + 0.20f * (float)Math.Sin(elapsed * 3.2f);
                    }
                }
                else if (_lastMofActive != 0)
                {
                    _mofMat.EmissiveIntensity = 0.20f;
                }
                _lastMofActive = mofActive ? 1 : 0;

                if (dt > 0f || _firstUpdate)
                {
                    _dcFanAngle += dt * (0.8f + (float)thermal.DryCoolerRejectionMw * 3.4f);
                    if (_dcFanAngle > TwoPi) _dcFanAngle -= TwoPi;
                    for (int i = 0; i < 3; i++)
                    {
                        WriteTranslateRotY(_dcFanCenters[i], _dcFanAngle + i * 0.9f, _dcFanModels[i].M);
                    }
                }
            }

            // 3. 10,000 m³ Drought Rainwater Cistern live level + subtle wave shimmer
            HydrologyTelemetry hydro = sim.Hydrology;
            if (hydro != null)
            {
                float cSoc = Clamp((float)hydro.CisternSoc, 0.05f, 1.0f);
                bool cisternMoved = Math.Abs(cSoc - _lastCisternSoc) > 1e-4f;
                if (cisternMoved)
                {
                    float wH = CisternMaxWaterH * cSoc;
                    WriteTranslateScale(_cisternOrigin.X, _cisternOrigin.Y + 0.25f, _cisternOrigin.Z, 1f, wH, 1f, _cisternWaterModel.M);
                    _lastCisternSoc = cSoc;
                }
                int isLow = cSoc < 0.48f ? 1 : 0;
                if (isLow != _lastCisternLow)
                {
                    _cisternWaterMat.Color = Vec3.FromHex(isLow == 1 ? 0x38bdf8 : 0x0284c7);
                    _lastCisternLow = isLow;
                }
                if (animate || cisternMoved)
                {
                    float surfY = _cisternOrigin.Y + 0.27f + CisternMaxWaterH * cSoc + 0.015f * (float)Math.Sin(elapsed * 2.2f);
                    WriteTranslateScale(_cisternOrigin.X, surfY, _cisternOrigin.Z, 1f, 1f, 1f, _cisternSurfaceModel.M);
                }
            }

            // 6. Ultrapure UV-C Quartz Chamber subtle high-energy violet shimmer
            if (animate)
            {
                _uvCoreMat.EmissiveIntensity = 1.60f + 0.22f * (float)Math.Sin(elapsed * 5.0f);
            }

            _firstUpdate = false;
        }

        public void Render(Renderer r, LayerVisibility layers)
        {
            if (_disposed || r == null) return;
            if (layers != null && !layers.Vaults) return;

            // Pass 1: Opaque Static Geometry (5 Vaults combined + Central Utility Hub batched)
            for (int i = 0; i < _opaqueStaticDraws.Count; i++)
            {
                StaticDrawEntry e = _opaqueStaticDraws[i];
                r.DrawMesh(e.Mesh, e.Model, e.Material);
            }

            // Pass 1b: Opaque Dynamic Per-Vault Assemblies (racks, hot headers, HTS busways, buffer fluid)
            for (int i = 0; i < 5; i++)
            {
                VaultStateNode node = _vaultNodes[i];
                r.DrawMesh(_vaultRackDoorsMesh, node.BaseModel, node.RackLedMat);
                r.DrawMesh(_vaultHotHeadersMesh, node.BaseModel, node.HotHeaderMat);
                r.DrawMesh(_vaultHtsMesh, node.BaseModel, node.HtsMat);
                r.DrawMesh(_vaultBufferFluidMesh, node.BaseModel, node.BufferFluidMat);
            }

            // Pass 1c: Opaque Animated Hub Machinery (Thermal Store fluid, spinning fans, flywheel, ORC rotor)
            r.DrawMesh(_tsFluidMesh, _tsFluidModel, _tsFluidMat);
            r.DrawMesh(_tsMeniscusMesh, _tsMeniscusModel, _tsMeniscusMat);

            for (int i = 0; i < 4; i++)
            {
                r.DrawMesh(_hthpFanMesh, _hthpFanModels[i], _fanBladeMat);
            }
            r.DrawMesh(_ptesFlywheelMesh, _ptesFlywheelModel, _brassMat);
            r.DrawMesh(_orcShaftMesh, _orcRotorModel, _brushedAlumMat);
            r.DrawMesh(_orcFinsMesh, _orcRotorModel, _orcTurbineMat);
            for (int i = 0; i < 3; i++)
            {
                r.DrawMesh(_dcFanMesh, _dcFanModels[i], _fanBladeMat);
            }

            // Pass 2: Inner Translucent Liquids (Cistern Water + Caustic Surface)
            r.DrawMesh(_cisternWaterMesh, _cisternWaterModel, _cisternWaterMat);
            r.DrawMesh(_cisternSurfaceMesh, _cisternSurfaceModel, _cisternSurfaceMat);

            // Pass 3: Outer Translucent Cutaway Glass Jackets & Active Fault Beacons
            for (int i = 0; i < _glassStaticDraws.Count; i++)
            {
                StaticDrawEntry e = _glassStaticDraws[i];
                r.DrawMesh(e.Mesh, e.Model, e.Material);
            }

            for (int i = 0; i < 5; i++)
            {
                VaultStateNode node = _vaultNodes[i];
                if (node.Faulted)
                {
                    r.DrawMesh(_faultBeaconMesh, node.FaultBeaconModel, node.FaultBeaconMat);
                    r.DrawMesh(_faultHaloMesh, node.FaultHaloModel, node.FaultHaloMat);
                }
            }
        }

        public void CollectPickTargets(List<PickTarget> dst)
        {
            if (dst == null) return;
            for (int i = 0; i < _pickTargets.Count; i++)
            {
                dst.Add(_pickTargets[i]);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = 0; i < _ownedMeshes.Count; i++)
            {
                if (_ownedMeshes[i] != null)
                {
                    _ownedMeshes[i].Dispose();
                }
            }
            _ownedMeshes.Clear();
            _opaqueStaticDraws.Clear();
            _glassStaticDraws.Clear();
        }

        // ---------------------------------------------------------------------
        // Internal Geometry, Material & Zero-Alloc Matrix Helpers
        // ---------------------------------------------------------------------

        private GpuMesh OwnMesh(GpuMesh mesh)
        {
            if (mesh != null) _ownedMeshes.Add(mesh);
            return mesh;
        }

        private void AddStaticMesh(Renderer r, MeshBuilder mb, MaterialDef mat, bool isGlass)
        {
            if (mb == null || mb.VertexCount == 0) return;
            GpuMesh mesh = OwnMesh(r.UploadMesh(mb));
            if (mesh == null) return;
            StaticDrawEntry entry = new StaticDrawEntry();
            entry.Mesh = mesh;
            entry.Model = Mat4.Identity();
            entry.Material = mat;
            if (isGlass) _glassStaticDraws.Add(entry);
            else _opaqueStaticDraws.Add(entry);
        }

        private void AddCombined5VaultStatic(Renderer r, MeshBuilder localTemplate, Mat4[] vaultMats, MaterialDef mat, bool isGlass)
        {
            if (localTemplate == null || localTemplate.VertexCount == 0) return;
            RawMeshCombiner comb = new RawMeshCombiner();
            for (int i = 0; i < vaultMats.Length; i++)
            {
                comb.AppendTransformed(localTemplate, vaultMats[i]);
            }
            GpuMesh mesh = OwnMesh(comb.Upload(r));
            if (mesh == null) return;
            StaticDrawEntry entry = new StaticDrawEntry();
            entry.Mesh = mesh;
            entry.Model = Mat4.Identity();
            entry.Material = mat;
            if (isGlass) _glassStaticDraws.Add(entry);
            else _opaqueStaticDraws.Add(entry);
        }

        private static GpuMesh BuildMergedFanMesh(Renderer r, int bladeCount, float radius)
        {
            RawMeshCombiner comb = new RawMeshCombiner();

            MeshBuilder hubMb = new MeshBuilder();
            hubMb.AddCylinder(new Vec3(0f, -0.07f, 0f), radius * 0.24f, radius * 0.20f, 0.14f, 12, true);
            comb.Append(hubMb);

            MeshBuilder bladeMb = new MeshBuilder();
            bladeMb.AddBox(Vec3.Zero, new Vec3(radius * 0.82f, 0.035f, radius * 0.24f));
            Mat4 localPitch = Mat4.TRS(new Vec3(radius * 0.48f, 0f, 0f), new Vec3(0.28f, 0f, 0f), Vec3.One);

            for (int i = 0; i < bladeCount; i++)
            {
                float a = (i / (float)bladeCount) * TwoPi;
                Mat4 m = Mat4.Multiply(Mat4.RotationY(a), localPitch);
                comb.AppendTransformed(bladeMb, m);
            }
            return comb.Upload(r);
        }

        private static void AddDomeY(MeshBuilder dst, Vec3 baseCenter, float radius, float height, bool upward, int segs)
        {
            const int bands = 4;
            for (int b = 0; b < bands; b++)
            {
                float a0;
                float a1;
                if (upward)
                {
                    a0 = (b / (float)bands) * HalfPi;
                    a1 = ((b + 1) / (float)bands) * HalfPi;
                }
                else
                {
                    a0 = -HalfPi + (b / (float)bands) * HalfPi;
                    a1 = -HalfPi + ((b + 1) / (float)bands) * HalfPi;
                }
                float y0 = baseCenter.Y + height * (float)Math.Sin(a0);
                float y1 = baseCenter.Y + height * (float)Math.Sin(a1);
                float r0 = radius * (float)Math.Cos(a0);
                float r1 = radius * (float)Math.Cos(a1);
                bool cap = upward ? (b == bands - 1) : (b == 0);
                dst.AddCylinder(new Vec3(baseCenter.X, y0, baseCenter.Z), r0, r1, y1 - y0, segs, cap);
            }
        }

        private void RegisterVaultPickTarget(int vaultId, Mat4 vaultMat)
        {
            Vec3 lMin = new Vec3(-6.0f, -4.0f, -5.0f);
            Vec3 lMax = new Vec3(6.0f, 1.5f, 5.0f);
            Vec3 wMin = new Vec3(1e9f, 1e9f, 1e9f);
            Vec3 wMax = new Vec3(-1e9f, -1e9f, -1e9f);

            for (int cx = 0; cx < 2; cx++)
            {
                float x = cx == 0 ? lMin.X : lMax.X;
                for (int cy = 0; cy < 2; cy++)
                {
                    float y = cy == 0 ? lMin.Y : lMax.Y;
                    for (int cz = 0; cz < 2; cz++)
                    {
                        float z = cz == 0 ? lMin.Z : lMax.Z;
                        Vec3 wp = Mat4.TransformPoint(vaultMat, new Vec3(x, y, z));
                        wMin = Vec3.Min(wMin, wp);
                        wMax = Vec3.Max(wMax, wp);
                    }
                }
            }
            AddPickTarget("vault_" + vaultId.ToString(), wMin, wMax);
        }

        private void AddPickTarget(string inspectKey, Vec3 min, Vec3 max)
        {
            PickTarget pt = new PickTarget();
            pt.InspectKey = inspectKey;
            pt.Bounds = new Aabb(min, max);
            pt.Center = (min + max) * 0.5f;
            pt.Radius = (max - min).Length() * 0.5f;
            _pickTargets.Add(pt);
        }

        private static MaterialDef MakeMat(
            int colorHex,
            int emissiveHex,
            float emissiveIntensity,
            float roughness,
            float metallic,
            float alpha,
            MaterialPattern pattern,
            float patternScale,
            bool doubleSided,
            bool castShadow)
        {
            MaterialDef m = new MaterialDef();
            m.Color = Vec3.FromHex(colorHex);
            m.Emissive = Vec3.FromHex(emissiveHex);
            m.EmissiveIntensity = emissiveIntensity;
            m.Roughness = roughness;
            m.Metallic = metallic;
            m.Alpha = alpha;
            m.Pattern = pattern;
            m.PatternScale = patternScale;
            m.DoubleSided = doubleSided;
            m.CastShadow = castShadow;
            return m;
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private static void WriteTranslateScale(float tx, float ty, float tz, float sx, float sy, float sz, float[] m)
        {
            m[0] = sx; m[1] = 0f; m[2] = 0f; m[3] = 0f;
            m[4] = 0f; m[5] = sy; m[6] = 0f; m[7] = 0f;
            m[8] = 0f; m[9] = 0f; m[10] = sz; m[11] = 0f;
            m[12] = tx; m[13] = ty; m[14] = tz; m[15] = 1f;
        }

        private static void WriteTranslateRotY(Vec3 pos, float rad, float[] m)
        {
            float c = (float)Math.Cos(rad);
            float s = (float)Math.Sin(rad);
            m[0] = c;  m[1] = 0f; m[2] = -s; m[3] = 0f;
            m[4] = 0f; m[5] = 1f; m[6] = 0f; m[7] = 0f;
            m[8] = s;  m[9] = 0f; m[10] = c; m[11] = 0f;
            m[12] = pos.X; m[13] = pos.Y; m[14] = pos.Z; m[15] = 1f;
        }

        private static void WriteTranslateRotX(Vec3 pos, float rad, float[] m)
        {
            float c = (float)Math.Cos(rad);
            float s = (float)Math.Sin(rad);
            m[0] = 1f; m[1] = 0f; m[2] = 0f; m[3] = 0f;
            m[4] = 0f; m[5] = c;  m[6] = s;  m[7] = 0f;
            m[8] = 0f; m[9] = -s; m[10] = c; m[11] = 0f;
            m[12] = pos.X; m[13] = pos.Y; m[14] = pos.Z; m[15] = 1f;
        }

        private static void WriteVaultLocalOffsetScale(Mat4 vaultBase, float lx, float ly, float lz, float scaleXZ, float[] dst)
        {
            // dst = vaultBase * Translation(lx, ly, lz) * Scale(scaleXZ, 1, scaleXZ)
            float[] a = vaultBase.M;
            dst[0] = a[0] * scaleXZ;
            dst[1] = a[1] * scaleXZ;
            dst[2] = a[2] * scaleXZ;
            dst[3] = 0f;

            dst[4] = a[4];
            dst[5] = a[5];
            dst[6] = a[6];
            dst[7] = 0f;

            dst[8] = a[8] * scaleXZ;
            dst[9] = a[9] * scaleXZ;
            dst[10] = a[10] * scaleXZ;
            dst[11] = 0f;

            dst[12] = a[0] * lx + a[4] * ly + a[8] * lz + a[12];
            dst[13] = a[1] * lx + a[5] * ly + a[9] * lz + a[13];
            dst[14] = a[2] * lx + a[6] * ly + a[10] * lz + a[14];
            dst[15] = 1f;
        }
    }
}
