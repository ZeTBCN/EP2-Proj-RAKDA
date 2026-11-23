using System;
using System.Collections.Generic;

namespace RakdaSim
{
    public sealed class SurfaceScene : IDisposable
    {
        private const int PodCount = 12;

        // Static surface meshes
        private readonly GpuMesh _roadsMesh;
        private readonly GpuMesh _guidewayMesh;
        private readonly GpuMesh _hubDeckMesh;
        private readonly GpuMesh _hubCourtyardLawnMesh;
        private readonly GpuMesh _hubBasinRimMesh;
        private readonly GpuMesh _hubBasinWaterMesh;
        private readonly GpuMesh _hubSpireMesh;
        private readonly GpuMesh _hubBuildingsMesh;
        private readonly GpuMesh _hubSedumMesh;
        private readonly GpuMesh _hubParapetsMesh;
        private readonly GpuMesh _bessPadMesh;
        private readonly GpuMesh _bessRailsMesh;
        private readonly GpuMesh _bessPostsMesh;
        private readonly GpuMesh _bessUnitsMesh;
        private readonly GpuMesh _bessBushingsMesh;
        private readonly GpuMesh _parksLawnMesh;
        private readonly GpuMesh _parksPathMesh;
        private readonly GpuMesh _pondBanksMesh;
        private readonly GpuMesh _pondWatersMesh;

        // Shared unit meshes for instanced batches
        private readonly GpuMesh _plazaStoneMesh;
        private readonly GpuMesh _plazaTrimMesh;
        private readonly GpuMesh _plazaBenchMesh;
        private readonly GpuMesh _plazaOculusGlassMesh;
        private readonly GpuMesh _plazaCanopyGlassMesh;
        private readonly GpuMesh _plazaRibsMesh;
        private readonly GpuMesh _plazaBollardPostMesh;
        private readonly GpuMesh _plazaBollardLampMesh;
        private readonly GpuMesh _plazaStatusRingMesh;

        private readonly GpuMesh _bessLedUnitMesh;
        private readonly GpuMesh _treeTrunkUnitMesh;
        private readonly GpuMesh _decCrownUnitMesh;
        private readonly GpuMesh _conCrownUnitMesh;
        private readonly GpuMesh _unitBoxMesh;
        private readonly GpuMesh _parapetPenthouseUnitMesh;
        private readonly GpuMesh _gabledRoofUnitMesh;
        private readonly GpuMesh _pvFrameUnitMesh;
        private readonly GpuMesh _pvPanelUnitMesh;

        private readonly GpuMesh _podSkirtMesh;
        private readonly GpuMesh _podHullMesh;
        private readonly GpuMesh _podGlassMesh;
        private readonly GpuMesh _podHeadlightMesh;
        private readonly GpuMesh _podTaillightMesh;
        private readonly GpuMesh _podLidarMesh;

        // Instanced batches
        private readonly GpuInstanceBatch _plazaStoneBatch;
        private readonly GpuInstanceBatch _plazaTrimBatch;
        private readonly GpuInstanceBatch _plazaBenchBatch;
        private readonly GpuInstanceBatch _plazaOculusGlassBatch;
        private readonly GpuInstanceBatch _plazaCanopyGlassBatch;
        private readonly GpuInstanceBatch _plazaRibsBatch;
        private readonly GpuInstanceBatch _plazaBollardPostBatch;
        private readonly GpuInstanceBatch _plazaBollardLampBatch;
        private readonly GpuInstanceBatch _plazaStatusRingBatch;
        private readonly InstanceItem[] _plazaStatusItems;

        private readonly GpuInstanceBatch _bessLedBatch;
        private readonly InstanceItem[] _bessLedItems;

        private readonly GpuInstanceBatch _treeTrunkBatch;
        private readonly GpuInstanceBatch _decCrownBatch;
        private readonly GpuInstanceBatch _conCrownBatch;

        private readonly BuildingBatchGroup[] _buildingGroups;
        private readonly GpuInstanceBatch _pvFrameBatch;
        private readonly GpuInstanceBatch _pvPanelBatch;

        private readonly GpuInstanceBatch _podSkirtBatch;
        private readonly GpuInstanceBatch _podHullBatch;
        private readonly GpuInstanceBatch _podGlassBatch;
        private readonly GpuInstanceBatch _podHeadlightBatch;
        private readonly GpuInstanceBatch _podTaillightBatch;
        private readonly GpuInstanceBatch _podLidarBatch;
        private readonly InstanceItem[] _podItems;
        private readonly PodOrbit[] _podOrbits;

        private readonly GpuLineBatch _v2xLines;
        private readonly float[] _v2xVerts; // 12 pods * (2 core + 2 aura + 2 pulse) = 72 verts * 7 floats

        // Materials
        private readonly MaterialDef _asphaltMat;
        private readonly MaterialDef _guidewayMat;
        private readonly MaterialDef _plazaStoneMat;
        private readonly MaterialDef _plazaTrimMat;
        private readonly MaterialDef _benchTimberMat;
        private readonly MaterialDef _oculusGlassMat;
        private readonly MaterialDef _canopyGlassMat;
        private readonly MaterialDef _pavilionRibMat;
        private readonly MaterialDef _bollardBodyMat;
        private readonly MaterialDef _bollardLampMat;
        private readonly MaterialDef _plazaStatusRingMat;
        private readonly MaterialDef _civicFacadeMat;
        private readonly MaterialDef _sedumGreenMat;
        private readonly MaterialDef _darkMetalMat;
        private readonly MaterialDef _waterReflectMat;
        private readonly MaterialDef _hubSpireMat;
        private readonly MaterialDef _bessRailsMat;
        private readonly MaterialDef _bessPostsMat;
        private readonly MaterialDef _bessUnitsMat;
        private readonly MaterialDef _bessBushingMat;
        private readonly MaterialDef _bessLedMat;
        private readonly MaterialDef _parkLawnMat;
        private readonly MaterialDef _parkPathMat;
        private readonly MaterialDef _pondBankMat;
        private readonly MaterialDef _trunkMat;
        private readonly MaterialDef _foliageMat;
        private readonly MaterialDef _midRiseMat;
        private readonly MaterialDef _lowRiseMat;
        private readonly MaterialDef _townhouseMat;
        private readonly MaterialDef _detachedMat;
        private readonly MaterialDef _parapetMat;
        private readonly MaterialDef _gabledRoofMat;
        private readonly MaterialDef _pvFrameMat;
        private readonly MaterialDef _pvPanelMat;
        private readonly MaterialDef _podSkirtMat;
        private readonly MaterialDef _podHullMat;
        private readonly MaterialDef _podGlassMat;
        private readonly MaterialDef _podHeadlightMat;
        private readonly MaterialDef _podTaillightMat;
        private readonly MaterialDef _podLidarMat;

        // Static and dynamic pick targets
        private readonly List<PickTarget> _staticPickTargets = new List<PickTarget>(32);
        private readonly PickTarget[] _podPickTargets;

        // Heatmap dirty cache
        private bool _hasLastHeat;
        private bool _lastHeatmapVisible;
        private int _lastDemandBucket = -1;

        private static readonly float[] ZoneWeights = new float[] { 0.24f, 0.22f, 0.20f, 0.18f, 0.16f };
        private static readonly Vec3 HeatLow = Vec3.FromHex(0x0ea5e9);
        private static readonly Vec3 HeatMid = Vec3.FromHex(0xf59e0b);
        private static readonly Vec3 HeatHigh = Vec3.FromHex(0xff3b14);

        private sealed class BuildingBatchGroup
        {
            public GpuInstanceBatch Batch;
            public MaterialDef Material;
            public InstanceItem[] Items;
            public Vec3[] BaseColors;
            public byte[] ZoneIndices;
            public bool IsWindowed;
        }

        private struct PodOrbit
        {
            public float RingRadius;
            public float BaseAngle;
            public float Speed;
        }

        private struct SamplePt
        {
            public float X;
            public float Z;
            public float Angle;
        }

        private struct PvRecord
        {
            public float X;
            public float Y;
            public float Z;
            public float RotY;
            public float W;
            public float D;
        }

        private struct PondSpec
        {
            public float X;
            public float Z;
            public float R;
        }

        public SurfaceScene(Renderer r)
        {
            VaultPos[] vaults = WorldLayout.VaultPositions;
            int vaultCount = vaults.Length;

            // --- MATERIALS ---
            _asphaltMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x283548),
                Roughness = 0.82f,
                Metallic = 0.08f,
                Pattern = MaterialPattern.Asphalt,
                PatternScale = 1.0f,
                CastShadow = false
            };
            _guidewayMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x22d3ee),
                Emissive = Vec3.FromHex(0x06b6d4),
                EmissiveIntensity = 0.85f,
                Roughness = 0.25f,
                Metallic = 0.35f,
                Alpha = 0.85f,
                CastShadow = false
            };
            _plazaStoneMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xb8c4d4),
                Roughness = 0.72f,
                Metallic = 0.12f,
                Pattern = MaterialPattern.PlazaTile,
                PatternScale = 0.45f,
                CastShadow = false
            };
            _plazaTrimMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x334155),
                Roughness = 0.55f,
                Metallic = 0.35f
            };
            _benchTimberMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x9a6b43),
                Roughness = 0.72f,
                Metallic = 0.06f
            };
            _oculusGlassMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x38bdf8),
                Emissive = Vec3.FromHex(0x0284c7),
                EmissiveIntensity = 0.22f,
                Roughness = 0.08f,
                Metallic = 0.12f,
                Alpha = 0.36f,
                CastShadow = false
            };
            _canopyGlassMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x7dd3fc),
                Emissive = Vec3.FromHex(0x0ea5e9),
                EmissiveIntensity = 0.16f,
                Roughness = 0.10f,
                Metallic = 0.15f,
                Alpha = 0.28f,
                DoubleSided = true,
                CastShadow = false
            };
            _pavilionRibMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xd6d3d1),
                Roughness = 0.36f,
                Metallic = 0.68f
            };
            _bollardBodyMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x1e293b),
                Roughness = 0.45f,
                Metallic = 0.65f
            };
            _bollardLampMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xffedd5),
                Emissive = Vec3.FromHex(0xffb347),
                EmissiveIntensity = 0.85f,
                Roughness = 0.2f,
                CastShadow = false
            };
            _plazaStatusRingMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.22f,
                CastShadow = false
            };
            _civicFacadeMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xcbd5e1),
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.48f,
                Metallic = 0.20f,
                Pattern = MaterialPattern.WindowGrid,
                PatternScale = 1.25f
            };
            _sedumGreenMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x36844b),
                Roughness = 0.88f,
                Metallic = 0.02f
            };
            _darkMetalMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x1e293b),
                Roughness = 0.45f,
                Metallic = 0.55f
            };
            _waterReflectMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x0e7490),
                Emissive = Vec3.FromHex(0x083344),
                EmissiveIntensity = 0.18f,
                Roughness = 0.07f,
                Metallic = 0.20f,
                Alpha = 0.90f,
                Pattern = MaterialPattern.WaterCaustic,
                PatternScale = 0.75f,
                CastShadow = false
            };
            _hubSpireMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x38bdf8),
                Emissive = Vec3.FromHex(0x0ea5e9),
                EmissiveIntensity = 1.25f,
                Roughness = 0.2f,
                CastShadow = false
            };
            _bessRailsMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x64748b),
                Roughness = 0.40f,
                Metallic = 0.72f
            };
            _bessPostsMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xf59e0b),
                Roughness = 0.45f,
                Metallic = 0.55f
            };
            _bessUnitsMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x2a3b52),
                Roughness = 0.46f,
                Metallic = 0.58f,
                Pattern = MaterialPattern.Concrete,
                PatternScale = 0.6f
            };
            _bessBushingMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xe2e8f0),
                Roughness = 0.22f,
                Metallic = 0.15f
            };
            _bessLedMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.2f,
                CastShadow = false
            };
            _parkLawnMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x1c6436),
                Roughness = 0.92f,
                Metallic = 0.02f,
                CastShadow = false
            };
            _parkPathMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x94a3b8),
                Roughness = 0.82f,
                Metallic = 0.05f,
                CastShadow = false
            };
            _pondBankMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x334155),
                Roughness = 0.85f,
                Metallic = 0.08f,
                CastShadow = false
            };
            _trunkMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x614434),
                Roughness = 0.92f,
                Metallic = 0.02f
            };
            _foliageMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xffffff),
                Roughness = 0.84f,
                Metallic = 0.02f
            };
            _midRiseMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.52f,
                Metallic = 0.16f,
                Pattern = MaterialPattern.WindowGrid,
                PatternScale = 1.35f
            };
            _lowRiseMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.54f,
                Metallic = 0.15f,
                Pattern = MaterialPattern.WindowGrid,
                PatternScale = 1.15f
            };
            _townhouseMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.58f,
                Metallic = 0.12f,
                Pattern = MaterialPattern.WindowGrid,
                PatternScale = 0.95f
            };
            _detachedMat = new MaterialDef
            {
                Color = Vec3.One,
                Emissive = Vec3.Zero,
                EmissiveIntensity = 0f,
                Roughness = 0.60f,
                Metallic = 0.10f,
                Pattern = MaterialPattern.WindowGrid,
                PatternScale = 0.80f
            };
            _parapetMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xffffff),
                Roughness = 0.64f,
                Metallic = 0.22f
            };
            _gabledRoofMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xffffff),
                Roughness = 0.62f,
                Metallic = 0.18f
            };
            _pvFrameMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x475569),
                Roughness = 0.45f,
                Metallic = 0.78f
            };
            _pvPanelMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xdbeafe),
                Emissive = Vec3.FromHex(0x1d4ed8),
                EmissiveIntensity = 0.18f,
                Roughness = 0.16f,
                Metallic = 0.78f,
                Pattern = MaterialPattern.SolarPv,
                PatternScale = 1.4f
            };
            _podSkirtMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x0f172a),
                Roughness = 0.55f,
                Metallic = 0.50f
            };
            _podHullMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xf1f5f9),
                Roughness = 0.22f,
                Metallic = 0.42f
            };
            _podGlassMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x111c2e),
                Roughness = 0.08f,
                Metallic = 0.25f
            };
            _podHeadlightMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xffffff),
                Emissive = Vec3.FromHex(0xf8fafc),
                EmissiveIntensity = 1.85f,
                Roughness = 0.15f,
                CastShadow = false
            };
            _podTaillightMat = new MaterialDef
            {
                Color = Vec3.FromHex(0xff2244),
                Emissive = Vec3.FromHex(0xff1e3c),
                EmissiveIntensity = 1.80f,
                Roughness = 0.2f,
                CastShadow = false
            };
            _podLidarMat = new MaterialDef
            {
                Color = Vec3.FromHex(0x22d3ee),
                Emissive = Vec3.FromHex(0x22d3ee),
                EmissiveIntensity = 1.65f,
                Roughness = 0.15f,
                CastShadow = false
            };

            // --- 1. CONCENTRIC & RADIAL TRANSIT AVENUES + AUTONOMOUS GUIDEWAYS ---
            MeshBuilder mbRoads = new MeshBuilder();
            mbRoads.AddRing(new Vec3(0f, 0.052f, 0f), 14.3f, 17.7f, 84);
            mbRoads.AddRing(new Vec3(0f, 0.052f, 0f), 36.2f, 39.8f, 112);
            float spokeLen = WorldLayout.DistrictRadius - 12.6f;
            float midR = 12.6f + spokeLen * 0.5f;
            for (int i = 0; i < vaultCount; i++)
            {
                VaultPos vp = vaults[i];
                mbRoads.AddBox(
                    new Vec3((float)Math.Cos(vp.Angle) * midR, 0.036f, (float)Math.Sin(vp.Angle) * midR),
                    new Vec3(spokeLen, 0.02f, 2.9f),
                    -vp.Angle);
            }
            _roadsMesh = r.UploadMesh(mbRoads);

            MeshBuilder mbGuide = new MeshBuilder();
            float[] ringRadii = new float[] { 16f, 38f };
            for (int ri = 0; ri < ringRadii.Length; ri++)
            {
                float ringR = ringRadii[ri];
                float off = ri == 0 ? 0.85f : 0.95f;
                mbGuide.AddRing(new Vec3(0f, 0.064f, 0f), ringR - off - 0.06f, ringR - off + 0.06f, 96);
                mbGuide.AddRing(new Vec3(0f, 0.064f, 0f), ringR + off - 0.06f, ringR + off + 0.06f, 96);
            }
            _guidewayMesh = r.UploadMesh(mbGuide);

            // --- 2. FIVE PUBLIC CIVIC PLAZAS (INSTANCED ACROSS ALL 5 VAULTS) ---
            MeshBuilder mbPlazaStone = new MeshBuilder();
            mbPlazaStone.AddCylinder(new Vec3(0f, 0.06f, 0f), 6.95f, 6.65f, 0.16f, 36);
            mbPlazaStone.AddCylinder(new Vec3(0f, 0.22f, 0f), 5.85f, 5.60f, 0.12f, 32);
            _plazaStoneMesh = r.UploadMesh(mbPlazaStone);

            MeshBuilder mbPlazaTrim = new MeshBuilder();
            mbPlazaTrim.AddTorus(new Vec3(0f, 0.36f, 0f), 2.48f, 0.16f, 28, 8, true);
            for (int b = 0; b < 4; b++)
            {
                float ba = b * (float)(Math.PI * 0.5) + (float)(Math.PI * 0.25);
                for (int s = -1; s <= 1; s++)
                {
                    float a = ba + s * 0.22f;
                    mbPlazaTrim.AddBox(
                        new Vec3((float)Math.Cos(a) * 4.35f, 0.39f, (float)Math.Sin(a) * 4.35f),
                        new Vec3(0.46f, 0.20f, 0.98f),
                        -a);
                }
            }
            _plazaTrimMesh = r.UploadMesh(mbPlazaTrim);

            MeshBuilder mbPlazaBench = new MeshBuilder();
            for (int b = 0; b < 4; b++)
            {
                float ba = b * (float)(Math.PI * 0.5) + (float)(Math.PI * 0.25);
                for (int s = -1; s <= 1; s++)
                {
                    float a = ba + s * 0.22f;
                    mbPlazaBench.AddBox(
                        new Vec3((float)Math.Cos(a) * 4.35f, 0.53f, (float)Math.Sin(a) * 4.35f),
                        new Vec3(0.50f, 0.09f, 1.02f),
                        -a);
                }
            }
            _plazaBenchMesh = r.UploadMesh(mbPlazaBench);

            MeshBuilder mbOculusGlass = new MeshBuilder();
            mbOculusGlass.AddCylinder(new Vec3(0f, 0.25f, 0f), 2.38f, 2.38f, 0.10f, 24);
            _plazaOculusGlassMesh = r.UploadMesh(mbOculusGlass);

            MeshBuilder mbCanopyGlass = new MeshBuilder();
            mbCanopyGlass.AddCylinder(new Vec3(0f, 1.52f, 0f), 2.75f, 1.15f, 0.42f, 24, false);
            mbCanopyGlass.AddDisc(new Vec3(0f, 1.94f, 0f), 1.15f, 20);
            _plazaCanopyGlassMesh = r.UploadMesh(mbCanopyGlass);

            MeshBuilder mbPlazaRibs = new MeshBuilder();
            mbPlazaRibs.AddTorus(new Vec3(0f, 1.54f, 0f), 2.75f, 0.09f, 28, 8, true);
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * (float)(Math.PI * 2.0);
                float ca = (float)Math.Cos(a);
                float sa = (float)Math.Sin(a);
                mbPlazaRibs.AddCylinderAxis(
                    new Vec3(ca * 2.52f, 0.32f, sa * 2.52f),
                    new Vec3(ca * 2.75f, 1.54f, sa * 2.75f),
                    0.09f,
                    8);
            }
            _plazaRibsMesh = r.UploadMesh(mbPlazaRibs);

            MeshBuilder mbBollardPosts = new MeshBuilder();
            MeshBuilder mbBollardLamps = new MeshBuilder();
            for (int l = 0; l < 8; l++)
            {
                float la = l / 8f * (float)(Math.PI * 2.0) + (float)(Math.PI * 0.125);
                float bx = (float)Math.Cos(la) * 5.85f;
                float bz = (float)Math.Sin(la) * 5.85f;
                mbBollardPosts.AddCylinder(new Vec3(bx, 0.26f, bz), 0.10f, 0.08f, 0.72f, 8);
                mbBollardLamps.AddCylinder(new Vec3(bx, 0.98f, bz), 0.085f, 0.085f, 0.18f, 8);
            }
            _plazaBollardPostMesh = r.UploadMesh(mbBollardPosts);
            _plazaBollardLampMesh = r.UploadMesh(mbBollardLamps);

            MeshBuilder mbStatusRing = new MeshBuilder();
            mbStatusRing.AddTorus(new Vec3(0f, 0.24f, 0f), 6.35f, 0.09f, 36, 8, true);
            _plazaStatusRingMesh = r.UploadMesh(mbStatusRing);

            _plazaStoneBatch = r.CreateInstanceBatch(_plazaStoneMesh, vaultCount);
            _plazaTrimBatch = r.CreateInstanceBatch(_plazaTrimMesh, vaultCount);
            _plazaBenchBatch = r.CreateInstanceBatch(_plazaBenchMesh, vaultCount);
            _plazaOculusGlassBatch = r.CreateInstanceBatch(_plazaOculusGlassMesh, vaultCount);
            _plazaCanopyGlassBatch = r.CreateInstanceBatch(_plazaCanopyGlassMesh, vaultCount);
            _plazaRibsBatch = r.CreateInstanceBatch(_plazaRibsMesh, vaultCount);
            _plazaBollardPostBatch = r.CreateInstanceBatch(_plazaBollardPostMesh, vaultCount);
            _plazaBollardLampBatch = r.CreateInstanceBatch(_plazaBollardLampMesh, vaultCount);
            _plazaStatusRingBatch = r.CreateInstanceBatch(_plazaStatusRingMesh, vaultCount);

            InstanceItem[] plazaStaticItems = new InstanceItem[vaultCount];
            _plazaStatusItems = new InstanceItem[vaultCount];
            for (int i = 0; i < vaultCount; i++)
            {
                VaultPos vp = vaults[i];
                Mat4 tr = Mat4.Translation(vp.X, 0f, vp.Z);
                plazaStaticItems[i] = new InstanceItem(tr, new Vec4(1f, 1f, 1f, 0f));
                _plazaStatusItems[i] = new InstanceItem(Mat4.Translation(vp.X, 0f, vp.Z), new Vec4(0.13f, 0.83f, 0.93f, 0.85f));

                _staticPickTargets.Add(new PickTarget
                {
                    InspectKey = "civic_plaza",
                    Bounds = new Aabb(new Vec3(vp.X - 6.8f, 0f, vp.Z - 6.8f), new Vec3(vp.X + 6.8f, 2.2f, vp.Z + 6.8f)),
                    Center = new Vec3(vp.X, 0.35f, vp.Z),
                    Radius = 6.8f
                });
            }
            _plazaStoneBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaTrimBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaBenchBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaOculusGlassBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaCanopyGlassBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaRibsBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaBollardPostBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaBollardLampBatch.UpdateInstances(plazaStaticItems, vaultCount);
            _plazaStatusRingBatch.UpdateInstances(_plazaStatusItems, vaultCount);

            // --- 3. CENTRAL CIVIC HUB, SCHOOLS & HEALTH CENTRE ---
            MeshBuilder mbHubDeck = new MeshBuilder();
            mbHubDeck.AddCylinder(new Vec3(0f, 0.04f, 0f), 13.3f, 13.0f, 0.16f, 48);
            mbHubDeck.AddCylinder(new Vec3(0f, 0.20f, 0f), 11.9f, 11.6f, 0.12f, 44);
            _hubDeckMesh = r.UploadMesh(mbHubDeck);

            MeshBuilder mbHubLawn = new MeshBuilder();
            mbHubLawn.AddCylinder(new Vec3(0f, 0.28f, 0f), 3.4f, 3.3f, 0.10f, 28);
            _hubCourtyardLawnMesh = r.UploadMesh(mbHubLawn);

            MeshBuilder mbHubRim = new MeshBuilder();
            mbHubRim.AddTorus(new Vec3(0f, 0.40f, 0f), 1.95f, 0.14f, 28, 8, true);
            _hubBasinRimMesh = r.UploadMesh(mbHubRim);

            MeshBuilder mbHubWater = new MeshBuilder();
            mbHubWater.AddCylinder(new Vec3(0f, 0.34f, 0f), 1.88f, 1.88f, 0.08f, 24);
            _hubBasinWaterMesh = r.UploadMesh(mbHubWater);

            MeshBuilder mbHubSpire = new MeshBuilder();
            mbHubSpire.AddTorus(new Vec3(0f, 0.54f, 0f), 0.95f, 0.07f, 24, 8, true);
            _hubSpireMesh = r.UploadMesh(mbHubSpire);

            List<PvRecord> pvRecords = new List<PvRecord>(180);
            float[,] civicSpecs = new float[,]
            {
                { -5.4f, -4.2f, 5.4f, 3.4f, 3.6f,  0.18f, 0.72f },
                {  5.3f, -4.0f, 5.6f, 3.0f, 3.5f, -0.22f, 0.76f },
                { -5.0f,  4.8f, 4.8f, 2.8f, 4.0f, -0.15f, 0.70f },
                {  5.0f,  4.9f, 5.0f, 3.9f, 3.6f,  0.24f, 0.68f }
            };

            MeshBuilder mbCivicBldgs = new MeshBuilder();
            MeshBuilder mbCivicSedum = new MeshBuilder();
            MeshBuilder mbCivicParapets = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float cx = civicSpecs[i, 0];
                float cz = civicSpecs[i, 1];
                float w = civicSpecs[i, 2];
                float h = civicSpecs[i, 3];
                float d = civicSpecs[i, 4];
                float rotY = civicSpecs[i, 5];
                float upScale = civicSpecs[i, 6];
                float podH = h * 0.56f;

                mbCivicBldgs.AddBox(new Vec3(cx, 0.30f + podH * 0.5f, cz), new Vec3(w, podH, d), rotY);
                mbCivicSedum.AddBox(new Vec3(cx, 0.30f + podH + 0.06f, cz), new Vec3(w * 0.94f, 0.12f, d * 0.94f), rotY);

                float upW = w * upScale;
                float upD = d * 0.78f;
                float upH = h - podH;
                float cosR = (float)Math.Cos(rotY);
                float sinR = (float)Math.Sin(rotY);
                float lx = w * 0.08f;
                float lz = -d * 0.06f;
                float ux = cx + lx * cosR + lz * sinR;
                float uz = cz - lx * sinR + lz * cosR;

                mbCivicBldgs.AddBox(new Vec3(ux, 0.30f + podH + upH * 0.5f, uz), new Vec3(upW, upH, upD), rotY);
                mbCivicParapets.AddBox(new Vec3(ux, 0.30f + h + 0.08f, uz), new Vec3(upW + 0.18f, 0.18f, upD + 0.18f), rotY);

                pvRecords.Add(new PvRecord
                {
                    X = ux,
                    Y = 0.30f + h + 0.34f,
                    Z = uz,
                    RotY = rotY,
                    W = upW * 0.82f,
                    D = upD * 0.72f
                });
            }
            _hubBuildingsMesh = r.UploadMesh(mbCivicBldgs);
            _hubSedumMesh = r.UploadMesh(mbCivicSedum);
            _hubParapetsMesh = r.UploadMesh(mbCivicParapets);

            _staticPickTargets.Add(new PickTarget
            {
                InspectKey = "civic_hub",
                Bounds = new Aabb(new Vec3(-12.5f, 0f, -12.5f), new Vec3(12.5f, 4.6f, 12.5f)),
                Center = new Vec3(0f, 0.35f, 0f),
                Radius = 12.5f
            });

            // --- 4. 8 MW / 32 MWh LFP BESS YARD ---
            float bx0 = -48f;
            float bz0 = 22f;
            MeshBuilder mbBessPad = new MeshBuilder();
            mbBessPad.AddBox(new Vec3(bx0, 0.19f, bz0), new Vec3(12.6f, 0.22f, 9.6f));
            _bessPadMesh = r.UploadMesh(mbBessPad);

            MeshBuilder mbBessRails = new MeshBuilder();
            MeshBuilder mbBessPosts = new MeshBuilder();
            float halfX = 6.1f;
            float halfZ = 4.6f;
            float railBaseY = 0.30f;
            float[,] segs = new float[,]
            {
                {  0f,    -halfZ, halfX * 2f, 0f },
                {  0f,     halfZ, halfX * 2f, 0f },
                { -halfX,  0f,    halfZ * 2f, (float)(Math.PI * 0.5) },
                {  halfX,  0f,    halfZ * 2f, (float)(Math.PI * 0.5) }
            };
            for (int si = 0; si < 4; si++)
            {
                float sx = segs[si, 0];
                float sz = segs[si, 1];
                float slen = segs[si, 2];
                float srot = segs[si, 3];
                mbBessRails.AddBox(new Vec3(bx0 + sx, railBaseY + 0.42f, bz0 + sz), new Vec3(slen, 0.055f, 0.055f), srot);
                mbBessRails.AddBox(new Vec3(bx0 + sx, railBaseY + 0.78f, bz0 + sz), new Vec3(slen, 0.055f, 0.055f), srot);
                int pc = Math.Max(3, (int)(slen / 2.2f));
                for (int p = 0; p <= pc; p++)
                {
                    float frac = -0.5f + (float)p / pc;
                    float px = srot == 0f ? bx0 + sx + frac * slen : bx0 + sx;
                    float pz = srot == 0f ? bz0 + sz : bz0 + sz + frac * slen;
                    mbBessPosts.AddBox(new Vec3(px, railBaseY + 0.42f, pz), new Vec3(0.08f, 0.85f, 0.08f));
                }
            }
            for (int f = 0; f < 5; f++)
            {
                mbBessRails.AddBox(new Vec3(bx0 + 3.85f + f * 0.35f, railBaseY + 0.82f, bz0 - 1.1f), new Vec3(0.08f, 1.28f, 2.55f));
            }
            _bessRailsMesh = r.UploadMesh(mbBessRails);
            _bessPostsMesh = r.UploadMesh(mbBessPosts);

            MeshBuilder mbBessUnits = new MeshBuilder();
            MeshBuilder mbBessBush = new MeshBuilder();
            mbBessUnits.AddBox(new Vec3(bx0 + 4.55f, railBaseY + 0.825f, bz0 - 1.1f), new Vec3(1.75f, 1.65f, 2.35f));
            mbBessUnits.AddBox(new Vec3(bx0 + 4.55f, railBaseY + 0.71f, bz0 + 1.8f), new Vec3(1.60f, 1.42f, 2.10f));
            for (int i = 0; i < 3; i++)
            {
                mbBessBush.AddCylinder(new Vec3(bx0 + 4.55f, railBaseY + 1.65f, bz0 - 1.7f + i * 0.6f), 0.14f, 0.09f, 0.55f, 10);
            }

            MeshBuilder mbLedUnit = new MeshBuilder();
            mbLedUnit.AddBox(Vec3.Zero, new Vec3(1.18f, 0.11f, 0.06f));
            _bessLedUnitMesh = r.UploadMesh(mbLedUnit);
            _bessLedBatch = r.CreateInstanceBatch(_bessLedUnitMesh, 40);
            _bessLedItems = new InstanceItem[40];

            int ledIdx = 0;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float cx = bx0 - 4.2f + col * 2.2f;
                    float cz = bz0 + (row == 0 ? -2.05f : 2.05f);
                    mbBessUnits.AddBox(new Vec3(cx, railBaseY + 0.74f, cz), new Vec3(1.85f, 1.48f, 3.10f));
                    mbBessUnits.AddBox(new Vec3(cx, railBaseY + 1.48f + 0.12f, cz - 0.35f), new Vec3(1.35f, 0.24f, 1.60f));

                    float faceZ = row == 0 ? cz - 1.57f : cz + 1.57f;
                    for (int tier = 0; tier < 5; tier++)
                    {
                        _bessLedItems[ledIdx] = new InstanceItem(
                            Mat4.Translation(cx, 0.60f + tier * 0.21f, faceZ),
                            new Vec4(0.06f, 0.72f, 0.51f, 1.0f));
                        ledIdx++;
                    }
                }
            }
            _bessUnitsMesh = r.UploadMesh(mbBessUnits);
            _bessBushingsMesh = r.UploadMesh(mbBessBush);
            _bessLedBatch.UpdateInstances(_bessLedItems, 40);

            _staticPickTargets.Add(new PickTarget
            {
                InspectKey = "bess_yard",
                Bounds = new Aabb(new Vec3(bx0 - 6.5f, 0f, bz0 - 5.0f), new Vec3(bx0 + 6.5f, 2.8f, bz0 + 5.0f)),
                Center = new Vec3(bx0, 0.4f, bz0),
                Radius = 7.4f
            });

            // --- 5. 25 ha PARKS, RETENTION PONDS & MULTI-TIER INSTANCED TREES ---
            MeshBuilder mbParkLawn = new MeshBuilder();
            MeshBuilder mbParkPath = new MeshBuilder();
            List<MeshBuilder> pondBankBuilders = new List<MeshBuilder>(6);
            List<MeshBuilder> pondWaterBuilders = new List<MeshBuilder>(6);
            List<PondSpec> ponds = new List<PondSpec>(6);

            float[] parkAngles = new float[] { 0.57f, 3.70f };
            float[,] pondOffsets = new float[,]
            {
                { 28.5f, -0.045f, 3.6f, 2.2f },
                { 48.5f,  0.040f, 5.2f, 2.8f },
                { 60.5f, -0.030f, 3.8f, 2.1f }
            };
            for (int pi = 0; pi < parkAngles.Length; pi++)
            {
                float pa0 = parkAngles[pi];
                mbParkLawn.AddRing(new Vec3(0f, 0.032f, 0f), 18.4f, 68.5f, 24, pa0 - 0.165f, 0.33f);
                mbParkPath.AddBox(
                    new Vec3((float)Math.Cos(pa0) * 43f, 0.038f, (float)Math.Sin(pa0) * 43f),
                    new Vec3(46f, 0.014f, 1.15f),
                    -pa0);

                for (int k = 0; k < 3; k++)
                {
                    float rP = pondOffsets[k, 0];
                    float aP = pa0 + pondOffsets[k, 1];
                    float rx = pondOffsets[k, 2];
                    float rz = pondOffsets[k, 3];
                    float px = (float)Math.Cos(aP) * rP;
                    float pz = (float)Math.Sin(aP) * rP;
                    ponds.Add(new PondSpec { X = px, Z = pz, R = Math.Max(rx, rz) + 1.2f });

                    MeshBuilder tmpBank = new MeshBuilder();
                    tmpBank.AddCylinder(Vec3.Zero, 1.06f, 1.0f, 0.06f, 18);
                    tmpBank.TransformAll(Mat4.TRS(new Vec3(px, 0.025f, pz), new Vec3(0f, -aP, 0f), new Vec3(rx + 0.45f, 1f, rz + 0.45f)));
                    pondBankBuilders.Add(tmpBank);

                    MeshBuilder tmpWater = new MeshBuilder();
                    tmpWater.AddCylinder(Vec3.Zero, 1.0f, 1.0f, 0.06f, 18);
                    tmpWater.TransformAll(Mat4.TRS(new Vec3(px, 0.042f, pz), new Vec3(0f, -aP, 0f), new Vec3(rx, 1f, rz)));
                    pondWaterBuilders.Add(tmpWater);
                }
            }
            _parksLawnMesh = r.UploadMesh(mbParkLawn);
            _parksPathMesh = r.UploadMesh(mbParkPath);
            _pondBanksMesh = UploadMerged(r, pondBankBuilders);
            _pondWatersMesh = UploadMerged(r, pondWaterBuilders);

            // Trees (multi-tier instanced deciduous + conifer)
            List<InstanceItem> trunkItems = new List<InstanceItem>(320);
            List<InstanceItem> decItems = new List<InstanceItem>(220);
            List<InstanceItem> conItems = new List<InstanceItem>(120);
            int[] decHexes = new int[] { 0x1f7a3e, 0x2d8a4e, 0x3f8f45, 0x256d42, 0x4b9452 };
            int[] conHexes = new int[] { 0x14532d, 0x166534, 0x1b5e3a, 0x11492b };
            uint treeSeed = 808u;

            // 6 civic courtyard trees
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * (float)(Math.PI * 2.0) + 0.35f;
                float tx = (float)Math.Cos(a) * 2.85f;
                float tz = (float)Math.Sin(a) * 2.85f;
                float sc = 0.65f + NextRand(ref treeSeed) * 0.20f;
                float rotY = NextRand(ref treeSeed) * 6.283185f;
                trunkItems.Add(new InstanceItem(
                    Mat4.TRS(new Vec3(tx, 0.32f, tz), new Vec3(0f, rotY, 0f), new Vec3(sc, sc, sc)),
                    new Vec4(Vec3.FromHex(0x6b4c3b), 0f)));
                decItems.Add(new InstanceItem(
                    Mat4.TRS(new Vec3(tx, 0.32f, tz), new Vec3(0f, rotY, 0f), new Vec3(sc, sc, sc)),
                    new Vec4(Vec3.FromHex(decHexes[i % decHexes.Length]), 0f)));
            }

            for (int i = 0; i < 420; i++)
            {
                float a = (float)((i * 2.399963) % (Math.PI * 2.0));
                float rad = 18.8f + (float)((i * 19.73) % 49.0);
                float x = (float)Math.Cos(a) * rad;
                float z = (float)Math.Sin(a) * rad;
                if (IsNearVaultPlaza(x, z, 7.6f)) continue;
                float dBessX = x + 48f;
                float dBessZ = z - 22f;
                if (dBessX * dBessX + dBessZ * dBessZ < 9.2f * 9.2f) continue;
                if (Math.Abs(rad - 16f) < 2.5f || Math.Abs(rad - 38f) < 2.75f) continue;
                if (IsInPond(x, z, ponds)) continue;

                bool nearSpoke = false;
                for (int v = 0; v < vaultCount; v++)
                {
                    float diff = Math.Abs(a - vaults[v].Angle);
                    if (diff > (float)Math.PI) diff = (float)(Math.PI * 2.0) - diff;
                    if (diff < 0.068f) { nearSpoke = true; break; }
                }
                if (nearSpoke) continue;

                float normA = NormalizeAngle(a);
                bool inPark = (normA > 0.405f && normA < 0.735f) || (normA > 3.535f && normA < 3.865f);
                if (!inPark && (i % 3 != 0)) continue;

                float sc = (inPark ? 0.85f : 0.70f) + NextRand(ref treeSeed) * 0.45f;
                float rotY = NextRand(ref treeSeed) * 6.283185f;
                Mat4 tr = Mat4.TRS(new Vec3(x, 0.04f, z), new Vec3(0f, rotY, 0f), new Vec3(sc, sc, sc));

                if (i % 3 == 0)
                {
                    trunkItems.Add(new InstanceItem(tr, new Vec4(Vec3.FromHex(0x523a2c), 0f)));
                    conItems.Add(new InstanceItem(tr, new Vec4(Vec3.FromHex(conHexes[conItems.Count % conHexes.Length]), 0f)));
                }
                else
                {
                    trunkItems.Add(new InstanceItem(tr, new Vec4(Vec3.FromHex(0x6b4c3b), 0f)));
                    decItems.Add(new InstanceItem(tr, new Vec4(Vec3.FromHex(decHexes[decItems.Count % decHexes.Length]), 0f)));
                }
            }

            MeshBuilder mbTrunk = new MeshBuilder();
            mbTrunk.AddCylinder(Vec3.Zero, 0.18f, 0.11f, 1.15f, 6);
            _treeTrunkUnitMesh = r.UploadMesh(mbTrunk);

            MeshBuilder mbDecCrown = new MeshBuilder();
            mbDecCrown.AddSphere(new Vec3(0f, 1.55f, 0f), 0.92f, 6, 8);
            mbDecCrown.AddSphere(new Vec3(0.22f, 2.20f, 0.12f), 0.64f, 5, 7);
            _decCrownUnitMesh = r.UploadMesh(mbDecCrown);

            MeshBuilder mbConCrown = new MeshBuilder();
            mbConCrown.AddCone(new Vec3(0f, 0.62f, 0f), 0.88f, 1.75f, 7);
            mbConCrown.AddCone(new Vec3(0f, 1.62f, 0f), 0.62f, 1.55f, 7);
            _conCrownUnitMesh = r.UploadMesh(mbConCrown);

            _treeTrunkBatch = r.CreateInstanceBatch(_treeTrunkUnitMesh, Math.Max(1, trunkItems.Count));
            _treeTrunkBatch.UpdateInstances(trunkItems.ToArray(), trunkItems.Count);
            _decCrownBatch = r.CreateInstanceBatch(_decCrownUnitMesh, Math.Max(1, decItems.Count));
            _decCrownBatch.UpdateInstances(decItems.ToArray(), decItems.Count);
            _conCrownBatch = r.CreateInstanceBatch(_conCrownUnitMesh, Math.Max(1, conItems.Count));
            _conCrownBatch.UpdateInstances(conItems.ToArray(), conItems.Count);

            // --- 6. FOUR RESIDENTIAL TYPOLOGIES + PARAPETS/GABLED ROOFS + 12 MW_p PV ---
            MeshBuilder mbUnitBox = new MeshBuilder();
            mbUnitBox.AddBox(Vec3.Zero, Vec3.One);
            _unitBoxMesh = r.UploadMesh(mbUnitBox);

            MeshBuilder mbParapetUnit = new MeshBuilder();
            mbParapetUnit.AddBox(new Vec3(0f, 0.11f, 0f), new Vec3(1.05f, 0.22f, 1.06f));
            mbParapetUnit.AddBox(new Vec3(0.24f, 0.42f, 0f), new Vec3(0.34f, 0.62f, 0.42f));
            _parapetPenthouseUnitMesh = r.UploadMesh(mbParapetUnit);

            MeshBuilder mbGabledUnit = new MeshBuilder();
            mbGabledUnit.AddGabledRoof(Vec3.Zero, 1.12f, 0.72f, 1.12f, 0f);
            _gabledRoofUnitMesh = r.UploadMesh(mbGabledUnit);

            SamplePt[] midRisePts = SampleRing(55, 19.6f, 34.4f, 0.1f, 3.6f);
            SamplePt[] lowRisePts = SampleRing(40, 30.8f, 41.6f, 1.2f, 3.2f);
            SamplePt[] townhousePts = SampleRing(165, 42.6f, 54.6f, 2.4f, 2.35f);
            SamplePt[] detachedPts = SampleRing(150, 56.2f, 68.0f, 3.7f, 2.25f);

            List<InstanceItem> parapetItems = new List<InstanceItem>(270);
            List<Vec3> parapetBaseColors = new List<Vec3>(270);
            List<byte> parapetZones = new List<byte>(270);

            List<InstanceItem> gabledItems = new List<InstanceItem>(160);
            List<Vec3> gabledBaseColors = new List<Vec3>(160);
            List<byte> gabledZones = new List<byte>(160);

            BuildingBatchGroup midGroup = BuildTypology(
                r, midRisePts, 3.7f, 5.4f, 2.5f, 2.5f, _midRiseMat,
                new int[] { 0xf5f0e6, 0xebe6dd, 0xe5ebf2, 0xf2ece1 },
                true, 1, true, parapetItems, parapetBaseColors, parapetZones, null, null, null, pvRecords);

            BuildingBatchGroup lowGroup = BuildTypology(
                r, lowRisePts, 3.25f, 3.3f, 1.3f, 2.25f, _lowRiseMat,
                new int[] { 0xeae5dc, 0xe2e8f0, 0xdfd8cc, 0xdce5ee },
                true, 1, true, parapetItems, parapetBaseColors, parapetZones, null, null, null, pvRecords);

            BuildingBatchGroup townGroup = BuildTypology(
                r, townhousePts, 2.15f, 2.0f, 0.72f, 1.55f, _townhouseMat,
                new int[] { 0xe6d8c9, 0xdccbb8, 0xd8dee6, 0xe5d6c5 },
                true, 3, true, parapetItems, parapetBaseColors, parapetZones, null, null, null, pvRecords);

            BuildingBatchGroup detGroup = BuildTypology(
                r, detachedPts, 1.75f, 1.55f, 0.52f, 1.55f, _detachedMat,
                new int[] { 0xf0ece3, 0xe8e2d8, 0xe2e8ee, 0xeee6da },
                false, 1, false, null, null, null, gabledItems, gabledBaseColors, gabledZones, pvRecords);

            GpuInstanceBatch parapetBatch = r.CreateInstanceBatch(_parapetPenthouseUnitMesh, Math.Max(1, parapetItems.Count));
            InstanceItem[] parapetArr = parapetItems.ToArray();
            parapetBatch.UpdateInstances(parapetArr, parapetArr.Length);

            GpuInstanceBatch gabledBatch = r.CreateInstanceBatch(_gabledRoofUnitMesh, Math.Max(1, gabledItems.Count));
            InstanceItem[] gabledArr = gabledItems.ToArray();
            gabledBatch.UpdateInstances(gabledArr, gabledArr.Length);

            _buildingGroups = new BuildingBatchGroup[]
            {
                midGroup,
                lowGroup,
                townGroup,
                detGroup,
                new BuildingBatchGroup
                {
                    Batch = parapetBatch,
                    Material = _parapetMat,
                    Items = parapetArr,
                    BaseColors = parapetBaseColors.ToArray(),
                    ZoneIndices = parapetZones.ToArray(),
                    IsWindowed = false
                },
                new BuildingBatchGroup
                {
                    Batch = gabledBatch,
                    Material = _gabledRoofMat,
                    Items = gabledArr,
                    BaseColors = gabledBaseColors.ToArray(),
                    ZoneIndices = gabledZones.ToArray(),
                    IsWindowed = false
                }
            };

            // Add residential sector pick targets (10 radial sectors covering mid/low/town/detached bands)
            for (int s = 0; s < 5; s++)
            {
                float a = vaults[s].Angle + (float)(Math.PI / 5.0);
                float sx1 = (float)Math.Cos(a) * 28f;
                float sz1 = (float)Math.Sin(a) * 28f;
                _staticPickTargets.Add(new PickTarget
                {
                    InspectKey = "residential_district",
                    Bounds = new Aabb(new Vec3(sx1 - 9f, 0f, sz1 - 9f), new Vec3(sx1 + 9f, 8.2f, sz1 + 9f)),
                    Center = new Vec3(sx1, 3.0f, sz1),
                    Radius = 11f
                });

                float sx2 = (float)Math.Cos(a) * 54f;
                float sz2 = (float)Math.Sin(a) * 54f;
                _staticPickTargets.Add(new PickTarget
                {
                    InspectKey = "residential_district",
                    Bounds = new Aabb(new Vec3(sx2 - 12f, 0f, sz2 - 12f), new Vec3(sx2 + 12f, 3.2f, sz2 + 12f)),
                    Center = new Vec3(sx2, 1.6f, sz2),
                    Radius = 13f
                });
            }

            // 12 MW_p Rooftop PV arrays + mounting frames
            MeshBuilder mbPvFrame = new MeshBuilder();
            mbPvFrame.AddBox(Vec3.Zero, new Vec3(0.92f, 0.16f, 0.65f));
            _pvFrameUnitMesh = r.UploadMesh(mbPvFrame);

            MeshBuilder mbPvPanel = new MeshBuilder();
            mbPvPanel.AddBox(Vec3.Zero, new Vec3(1.0f, 0.08f, 1.0f));
            _pvPanelUnitMesh = r.UploadMesh(mbPvPanel);

            int pvCount = Math.Max(1, pvRecords.Count);
            _pvFrameBatch = r.CreateInstanceBatch(_pvFrameUnitMesh, pvCount);
            _pvPanelBatch = r.CreateInstanceBatch(_pvPanelUnitMesh, pvCount);
            InstanceItem[] pvFrameItems = new InstanceItem[pvRecords.Count];
            InstanceItem[] pvPanelItems = new InstanceItem[pvRecords.Count];
            for (int i = 0; i < pvRecords.Count; i++)
            {
                PvRecord p = pvRecords[i];
                pvFrameItems[i] = new InstanceItem(
                    Mat4.TRS(new Vec3(p.X, p.Y - 0.10f, p.Z), new Vec3(0f, p.RotY, 0f), new Vec3(p.W, 1f, p.D)),
                    new Vec4(1f, 1f, 1f, 0f));
                pvPanelItems[i] = new InstanceItem(
                    Mat4.TRS(new Vec3(p.X, p.Y, p.Z), new Vec3(0.24f, p.RotY, 0f), new Vec3(p.W, 1f, p.D)),
                    new Vec4(1f, 1f, 1f, 0f));
            }
            if (pvRecords.Count > 0)
            {
                _pvFrameBatch.UpdateInstances(pvFrameItems, pvRecords.Count);
                _pvPanelBatch.UpdateInstances(pvPanelItems, pvRecords.Count);

                // Representative rooftop PV pick targets on civic hub & inner mid-rise roofs
                for (int i = 0; i < Math.Min(4, pvRecords.Count); i++)
                {
                    PvRecord p = pvRecords[i];
                    _staticPickTargets.Add(new PickTarget
                    {
                        InspectKey = "rooftop_pv",
                        Bounds = new Aabb(
                            new Vec3(p.X - p.W * 0.6f, p.Y - 0.3f, p.Z - p.D * 0.6f),
                            new Vec3(p.X + p.W * 0.6f, p.Y + 0.6f, p.Z + p.D * 0.6f)),
                        Center = new Vec3(p.X, p.Y, p.Z),
                        Radius = Math.Max(p.W, p.D) * 0.7f
                    });
                }
            }

            // --- 7. 12 STREAMLINED AUTONOMOUS 6G V2X PODS + DUAL-GLOW V2X BEAMS ---
            MeshBuilder mbSkirt = new MeshBuilder();
            mbSkirt.AddBox(new Vec3(0f, 0.21f, 0f), new Vec3(0.94f, 0.22f, 1.72f));
            mbSkirt.AddCylinder(new Vec3(0f, 0.91f, 0.12f), 0.20f, 0.16f, 0.10f, 12);
            _podSkirtMesh = r.UploadMesh(mbSkirt);

            MeshBuilder mbHullCore = new MeshBuilder();
            mbHullCore.AddBox(new Vec3(0f, 0.44f, 0f), new Vec3(0.98f, 0.34f, 1.62f));
            mbHullCore.AddBox(new Vec3(0f, 0.89f, 0f), new Vec3(0.76f, 0.08f, 1.18f));
            MeshBuilder tmpNose = new MeshBuilder();
            tmpNose.AddCylinder(new Vec3(0f, -0.17f, 0f), 0.49f, 0.46f, 0.34f, 12);
            tmpNose.TransformAll(Mat4.TRS(new Vec3(0f, 0.44f, 0.80f), Vec3.Zero, new Vec3(1f, 1f, 0.58f)));
            MeshBuilder tmpTail = new MeshBuilder();
            tmpTail.AddCylinder(new Vec3(0f, -0.17f, 0f), 0.49f, 0.46f, 0.34f, 12);
            tmpTail.TransformAll(Mat4.TRS(new Vec3(0f, 0.44f, -0.80f), Vec3.Zero, new Vec3(1f, 1f, 0.58f)));
            _podHullMesh = UploadMerged(r, new List<MeshBuilder> { mbHullCore, tmpNose, tmpTail });

            MeshBuilder mbGlassCore = new MeshBuilder();
            mbGlassCore.AddBox(new Vec3(0f, 0.72f, 0f), new Vec3(0.88f, 0.30f, 1.24f));
            MeshBuilder tmpCabF = new MeshBuilder();
            tmpCabF.AddCylinder(new Vec3(0f, -0.145f, 0f), 0.44f, 0.40f, 0.29f, 12);
            tmpCabF.TransformAll(Mat4.TRS(new Vec3(0f, 0.72f, 0.60f), Vec3.Zero, new Vec3(1f, 1f, 0.52f)));
            MeshBuilder tmpCabR = new MeshBuilder();
            tmpCabR.AddCylinder(new Vec3(0f, -0.145f, 0f), 0.44f, 0.40f, 0.29f, 12);
            tmpCabR.TransformAll(Mat4.TRS(new Vec3(0f, 0.72f, -0.60f), Vec3.Zero, new Vec3(1f, 1f, 0.52f)));
            _podGlassMesh = UploadMerged(r, new List<MeshBuilder> { mbGlassCore, tmpCabF, tmpCabR });

            MeshBuilder mbHead = new MeshBuilder();
            mbHead.AddBox(new Vec3(-0.28f, 0.45f, 1.05f), new Vec3(0.24f, 0.08f, 0.08f));
            mbHead.AddBox(new Vec3(0.28f, 0.45f, 1.05f), new Vec3(0.24f, 0.08f, 0.08f));
            _podHeadlightMesh = r.UploadMesh(mbHead);

            MeshBuilder mbTail = new MeshBuilder();
            mbTail.AddBox(new Vec3(0f, 0.46f, -1.05f), new Vec3(0.74f, 0.075f, 0.08f));
            _podTaillightMesh = r.UploadMesh(mbTail);

            MeshBuilder mbLidar = new MeshBuilder();
            mbLidar.AddCylinder(new Vec3(0f, 0.99f, 0.12f), 0.15f, 0.15f, 0.07f, 12);
            mbLidar.AddBox(new Vec3(-0.495f, 0.55f, 0f), new Vec3(0.03f, 0.045f, 1.15f));
            mbLidar.AddBox(new Vec3(0.495f, 0.55f, 0f), new Vec3(0.03f, 0.045f, 1.15f));
            _podLidarMesh = r.UploadMesh(mbLidar);

            _podSkirtBatch = r.CreateInstanceBatch(_podSkirtMesh, PodCount);
            _podHullBatch = r.CreateInstanceBatch(_podHullMesh, PodCount);
            _podGlassBatch = r.CreateInstanceBatch(_podGlassMesh, PodCount);
            _podHeadlightBatch = r.CreateInstanceBatch(_podHeadlightMesh, PodCount);
            _podTaillightBatch = r.CreateInstanceBatch(_podTaillightMesh, PodCount);
            _podLidarBatch = r.CreateInstanceBatch(_podLidarMesh, PodCount);

            _podItems = new InstanceItem[PodCount];
            _podOrbits = new PodOrbit[PodCount];
            _podPickTargets = new PickTarget[PodCount];
            for (int i = 0; i < PodCount; i++)
            {
                bool isInner = (i % 2 == 0);
                float laneOff = isInner ? ((i % 4 == 0) ? 0.65f : -0.65f) : ((i % 4 == 1) ? 0.72f : -0.72f);
                _podOrbits[i] = new PodOrbit
                {
                    RingRadius = (isInner ? 16.0f : 38.0f) + laneOff,
                    BaseAngle = i / (float)PodCount * (float)(Math.PI * 2.0),
                    Speed = isInner ? 0.23f : -0.14f
                };
                _podItems[i] = new InstanceItem(Mat4.Identity(), new Vec4(1f, 1f, 1f, 0f));
                _podPickTargets[i] = new PickTarget
                {
                    InspectKey = "autonomous_pods",
                    Bounds = new Aabb(Vec3.Zero, Vec3.Zero),
                    Center = Vec3.Zero,
                    Radius = 1.6f
                };
            }

            // 12 pods * 6 vertices per pod (2 cyan core + 2 violet aura + 2 traveling pulse) = 72 vertices
            _v2xLines = r.CreateLineBatch(PodCount * 6);
            _v2xVerts = new float[PodCount * 6 * 7];
        }

        private static GpuMesh UploadMerged(Renderer r, List<MeshBuilder> builders)
        {
            int totalVertFloats = 0;
            int totalIndices = 0;
            float[][] vertsArr = new float[builders.Count][];
            uint[][] idxArr = new uint[builders.Count][];
            for (int i = 0; i < builders.Count; i++)
            {
                float[] v;
                uint[] idx;
                builders[i].BuildData(out v, out idx);
                vertsArr[i] = v;
                idxArr[i] = idx;
                totalVertFloats += v.Length;
                totalIndices += idx.Length;
            }

            float[] allVerts = new float[totalVertFloats];
            uint[] allIndices = new uint[totalIndices];
            int vOff = 0;
            int iOff = 0;
            uint vertBase = 0;
            for (int i = 0; i < builders.Count; i++)
            {
                float[] v = vertsArr[i];
                uint[] idx = idxArr[i];
                Array.Copy(v, 0, allVerts, vOff, v.Length);
                for (int k = 0; k < idx.Length; k++)
                {
                    allIndices[iOff + k] = idx[k] + vertBase;
                }
                vOff += v.Length;
                iOff += idx.Length;
                vertBase += (uint)(v.Length / 8);
            }
            return r.UploadMesh(allVerts, allIndices);
        }

        private static float NextRand(ref uint seed)
        {
            seed = unchecked(seed * 1664525u + 1013904223u);
            return seed / 4294967296f;
        }

        private static float NormalizeAngle(float a)
        {
            float twoPi = (float)(Math.PI * 2.0);
            float r = a % twoPi;
            if (r < 0f) r += twoPi;
            return r;
        }

        private static bool IsNearVaultPlaza(float x, float z, float minClear)
        {
            VaultPos[] vaults = WorldLayout.VaultPositions;
            float minSq = minClear * minClear;
            for (int i = 0; i < vaults.Length; i++)
            {
                float dx = x - vaults[i].X;
                float dz = z - vaults[i].Z;
                if (dx * dx + dz * dz < minSq) return true;
            }
            return false;
        }

        private static bool IsNearRoadOrPark(float rad, float angle)
        {
            if (Math.Abs(rad - 16f) < 2.95f || Math.Abs(rad - 38f) < 3.15f) return true;
            VaultPos[] vaults = WorldLayout.VaultPositions;
            for (int i = 0; i < vaults.Length; i++)
            {
                float diff = Math.Abs(angle - vaults[i].Angle);
                if (diff > (float)Math.PI) diff = (float)(Math.PI * 2.0) - diff;
                if (diff < 0.115f) return true;
            }
            float normA = NormalizeAngle(angle);
            if ((normA > 0.40f && normA < 0.74f) || (normA > 3.53f && normA < 3.87f)) return true;
            return false;
        }

        private static bool IsInPond(float x, float z, List<PondSpec> ponds)
        {
            for (int i = 0; i < ponds.Count; i++)
            {
                float dx = x - ponds[i].X;
                float dz = z - ponds[i].Z;
                float rr = ponds[i].R;
                if (dx * dx + dz * dz < rr * rr) return true;
            }
            return false;
        }

        private static SamplePt[] SampleRing(int count, float rMin, float rMax, float seedOffset, float minSpacing)
        {
            List<SamplePt> pts = new List<SamplePt>(count);
            float minSq = minSpacing * minSpacing;
            int attempt = 0;
            int maxAttempts = count * 14;
            while (pts.Count < count && attempt < maxAttempts)
            {
                attempt++;
                float a = (float)((attempt * 2.399963 + seedOffset) % (Math.PI * 2.0));
                float frac = (float)((attempt * 0.6180339) % 1.0);
                float rad = rMin + frac * (rMax - rMin);
                float x = (float)Math.Cos(a) * rad;
                float z = (float)Math.Sin(a) * rad;
                if (IsNearVaultPlaza(x, z, 8.7f)) continue;
                if (IsNearRoadOrPark(rad, a)) continue;
                float dbx = x + 48f;
                float dbz = z - 22f;
                if (dbx * dbx + dbz * dbz < 10.2f * 10.2f) continue;

                bool overlap = false;
                for (int j = 0; j < pts.Count; j++)
                {
                    float dx = x - pts[j].X;
                    float dz = z - pts[j].Z;
                    if (dx * dx + dz * dz < minSq) { overlap = true; break; }
                }
                if (overlap) continue;
                pts.Add(new SamplePt { X = x, Z = z, Angle = a });
            }
            return pts.ToArray();
        }

        private BuildingBatchGroup BuildTypology(
            Renderer r,
            SamplePt[] pts,
            float w,
            float hMin,
            float hVar,
            float d,
            MaterialDef mat,
            int[] paletteHexes,
            bool addPv,
            int pvEvery,
            bool hasParapet,
            List<InstanceItem> parapetItems,
            List<Vec3> parapetBaseColors,
            List<byte> parapetZones,
            List<InstanceItem> gabledItems,
            List<Vec3> gabledBaseColors,
            List<byte> gabledZones,
            List<PvRecord> pvRecords)
        {
            VaultPos[] vaults = WorldLayout.VaultPositions;
            int n = pts.Length;
            GpuInstanceBatch batch = r.CreateInstanceBatch(_unitBoxMesh, Math.Max(1, n));
            InstanceItem[] items = new InstanceItem[n];
            Vec3[] baseColors = new Vec3[n];
            byte[] zoneIndices = new byte[n];
            int[] roofPalette = new int[] { 0x64748b, 0x526277, 0x717d8c, 0x4b5563 };
            Vec3 parapetCol = Vec3.FromHex(0x94a3b8);

            for (int idx = 0; idx < n; idx++)
            {
                SamplePt pt = pts[idx];
                float h = hMin + (((idx * 37 + 13) % 100) / 100f) * hVar;
                float rotY = -pt.Angle + (float)(Math.PI * 0.5);

                Vec3 col = Vec3.FromHex(paletteHexes[idx % paletteHexes.Length]);
                baseColors[idx] = col;
                items[idx] = new InstanceItem(
                    Mat4.TRS(new Vec3(pt.X, h * 0.5f + 0.04f, pt.Z), new Vec3(0f, rotY, 0f), new Vec3(w, h, d)),
                    new Vec4(col, 0f));

                byte bestZone = 0;
                float bestDistSq = float.MaxValue;
                for (int v = 0; v < vaults.Length; v++)
                {
                    float dx = pt.X - vaults[v].X;
                    float dz = pt.Z - vaults[v].Z;
                    float dSq = dx * dx + dz * dz;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestZone = (byte)v;
                    }
                }
                zoneIndices[idx] = bestZone;

                if (hasParapet && parapetItems != null)
                {
                    float flip = (idx % 2 == 0) ? 0f : (float)Math.PI;
                    parapetItems.Add(new InstanceItem(
                        Mat4.TRS(new Vec3(pt.X, h + 0.04f, pt.Z), new Vec3(0f, rotY + flip, 0f), new Vec3(w, 1f, d)),
                        new Vec4(parapetCol, 0f)));
                    parapetBaseColors.Add(parapetCol);
                    parapetZones.Add(bestZone);
                }
                else if (!hasParapet && gabledItems != null)
                {
                    Vec3 rCol = Vec3.FromHex(roofPalette[idx % roofPalette.Length]);
                    gabledItems.Add(new InstanceItem(
                        Mat4.TRS(new Vec3(pt.X, h + 0.04f, pt.Z), new Vec3(0f, rotY, 0f), new Vec3(w, 1f, d)),
                        new Vec4(rCol, 0f)));
                    gabledBaseColors.Add(rCol);
                    gabledZones.Add(bestZone);
                }

                if (addPv && (idx % pvEvery == 0))
                {
                    float cosR = (float)Math.Cos(rotY);
                    float sinR = (float)Math.Sin(rotY);
                    float pvOff = hasParapet ? -w * 0.14f * ((idx % 2 == 0) ? 1f : -1f) : 0f;
                    pvRecords.Add(new PvRecord
                    {
                        X = pt.X + pvOff * cosR,
                        Y = h + (hasParapet ? 0.34f : 0.46f),
                        Z = pt.Z - pvOff * sinR,
                        RotY = rotY,
                        W = w * (hasParapet ? 0.56f : 0.62f),
                        D = d * 0.64f
                    });
                }
            }

            if (n > 0) batch.UpdateInstances(items, n);
            return new BuildingBatchGroup
            {
                Batch = batch,
                Material = mat,
                Items = items,
                BaseColors = baseColors,
                ZoneIndices = zoneIndices,
                IsWindowed = true
            };
        }

        private static void WriteTranslationRotY(float[] m, float x, float y, float z, float rotY)
        {
            float c = (float)Math.Cos(rotY);
            float s = (float)Math.Sin(rotY);
            m[0] = c;    m[1] = 0f; m[2] = -s;  m[3] = 0f;
            m[4] = 0f;   m[5] = 1f; m[6] = 0f;  m[7] = 0f;
            m[8] = s;    m[9] = 0f; m[10] = c;  m[11] = 0f;
            m[12] = x;   m[13] = y; m[14] = z;  m[15] = 1f;
        }

        private static void WriteLineVertex(float[] arr, int vertIndex, float x, float y, float z, float r, float g, float b, float a)
        {
            int o = vertIndex * 7;
            arr[o] = x;
            arr[o + 1] = y;
            arr[o + 2] = z;
            arr[o + 3] = r;
            arr[o + 4] = g;
            arr[o + 5] = b;
            arr[o + 6] = a;
        }

        public void Update(SimResult sim, float dt, float elapsed, bool heatmapVisible)
        {
            if (sim == null) return;

            float tod = sim.Inputs != null ? (float)sim.Inputs.TimeOfDay : 13.5f;
            float dayPhase = (tod - 6f) / 12f;
            if (dayPhase < 0f) dayPhase = 0f;
            if (dayPhase > 1f) dayPhase = 1f;
            float daylight = Math.Max(0f, (float)Math.Sin(dayPhase * Math.PI));
            float nightFactor = Math.Min(1f, Math.Max(0f, 1f - daylight * 1.35f));

            // Switch building pattern cleanly: WindowGrid (with automatic dusk/night glow) vs Solid thermal heat-map
            MaterialPattern resPattern = heatmapVisible ? MaterialPattern.Solid : MaterialPattern.WindowGrid;
            _midRiseMat.Pattern = resPattern;
            _lowRiseMat.Pattern = resPattern;
            _townhouseMat.Pattern = resPattern;
            _detachedMat.Pattern = resPattern;

            _bollardLampMat.EmissiveIntensity = 0.55f + nightFactor * 1.35f;
            _guidewayMat.EmissiveIntensity = 0.65f + nightFactor * 0.70f + 0.12f * (float)Math.Sin(elapsed * 2.5f);

            // Dirty-checked residential heatmap color updates (zero redundant GPU uploads!)
            float demandNorm = (float)((sim.Thermal.DistrictDemandMw - 1.45) / (9.52 - 1.45));
            if (demandNorm < 0f) demandNorm = 0f;
            if (demandNorm > 1f) demandNorm = 1f;
            int demandBucket = heatmapVisible ? (int)Math.Round(demandNorm * 100f) : -1;

            if (!_hasLastHeat || heatmapVisible != _lastHeatmapVisible || demandBucket != _lastDemandBucket)
            {
                _hasLastHeat = true;
                _lastHeatmapVisible = heatmapVisible;
                _lastDemandBucket = demandBucket;

                Vec3 baseHeat = demandNorm < 0.5f
                    ? Vec3.Lerp(HeatLow, HeatMid, demandNorm * 2f)
                    : Vec3.Lerp(HeatMid, HeatHigh, (demandNorm - 0.5f) * 2f);

                for (int g = 0; g < _buildingGroups.Length; g++)
                {
                    BuildingBatchGroup bg = _buildingGroups[g];
                    InstanceItem[] items = bg.Items;
                    int count = items.Length;
                    if (count == 0) continue;

                    if (heatmapVisible)
                    {
                        byte[] zones = bg.ZoneIndices;
                        for (int i = 0; i < count; i++)
                        {
                            float zw = ZoneWeights[zones[i]];
                            float shift = (zw - 0.20f) * 1.6f;
                            float cr = Math.Min(1f, Math.Max(0f, baseHeat.X + shift * 0.25f));
                            float cg = Math.Min(1f, Math.Max(0f, baseHeat.Y - shift * 0.12f));
                            float cb = Math.Min(1f, Math.Max(0f, baseHeat.Z - shift * 0.20f));
                            items[i].ColorTint = new Vec4(cr, cg, cb, 0.22f);
                        }
                    }
                    else
                    {
                        Vec3[] baseCols = bg.BaseColors;
                        for (int i = 0; i < count; i++)
                        {
                            Vec3 c = baseCols[i];
                            items[i].ColorTint = new Vec4(c.X, c.Y, c.Z, 0f);
                        }
                    }
                    bg.Batch.UpdateInstances(items, count);
                }
            }

            // Civic Plaza fault-reactive status rings
            float faultPulse = 0.75f + 0.90f * Math.Abs((float)Math.Sin(elapsed * 5.0f));
            VaultPos[] vaults = WorldLayout.VaultPositions;
            int vaultCount = vaults.Length;
            for (int i = 0; i < vaultCount; i++)
            {
                bool faulted = sim.Vaults != null && i < sim.Vaults.Length && sim.Vaults[i].Faulted;
                if (faulted)
                {
                    _plazaStatusItems[i].ColorTint = new Vec4(0.96f, 0.24f, 0.24f, faultPulse);
                }
                else
                {
                    _plazaStatusItems[i].ColorTint = new Vec4(0.13f, 0.83f, 0.93f, 0.70f + nightFactor * 0.50f);
                }
            }
            _plazaStatusRingBatch.UpdateInstances(_plazaStatusItems, vaultCount);

            // Rooftop PV live solar shimmer
            float pvFrac = Math.Min(1f, Math.Max(0f, (float)(sim.Electrical.PvOutputMw / 12.0)));
            _pvPanelMat.EmissiveIntensity = 0.05f + pvFrac * 0.34f;

            // BESS Yard multi-bar SOC LEDs
            bool isDischarging = sim.Electrical.BessDischargeMw > 0.05;
            bool isCharging = sim.Electrical.BessChargeMw > 0.05;
            int activeBars = isDischarging ? 4 : (isCharging ? 5 : 3);
            int pulseBar = ((int)(elapsed * 2.5f)) % activeBars;

            for (int unit = 0; unit < 8; unit++)
            {
                int baseIdx = unit * 5;
                for (int tier = 0; tier < 5; tier++)
                {
                    Vec4 tint;
                    if (tier >= activeBars)
                    {
                        tint = new Vec4(0.20f, 0.26f, 0.34f, 0.15f);
                    }
                    else if (isDischarging)
                    {
                        float em = (tier == activeBars - 1 - (pulseBar % activeBars)) ? 1.55f : 0.95f;
                        tint = new Vec4(0.06f, 0.78f, 0.52f, em);
                    }
                    else if (isCharging)
                    {
                        float em = (tier == pulseBar) ? 1.60f : 0.95f;
                        tint = new Vec4(0.13f, 0.83f, 0.93f, em);
                    }
                    else
                    {
                        tint = new Vec4(0.38f, 0.48f, 0.60f, 0.30f);
                    }
                    _bessLedItems[baseIdx + tier].ColorTint = tint;
                }
            }
            _bessLedBatch.UpdateInstances(_bessLedItems, 40);

            // Check if any healthy vault exists (zero allocations)
            bool hasHealthy = false;
            for (int v = 0; v < vaultCount; v++)
            {
                if (sim.Vaults == null || v >= sim.Vaults.Length || !sim.Vaults[v].Faulted)
                {
                    hasHealthy = true;
                    break;
                }
            }

            // Animate 12 Autonomous 6G V2X Pods + Dual-Glow V2X Optical Beams
            int vVert = 0;
            for (int i = 0; i < PodCount; i++)
            {
                PodOrbit orb = _podOrbits[i];
                float ang = orb.BaseAngle + elapsed * orb.Speed;
                float cosA = (float)Math.Cos(ang);
                float sinA = (float)Math.Sin(ang);
                float px = cosA * orb.RingRadius;
                float pz = sinA * orb.RingRadius;
                float dx = -sinA * orb.Speed;
                float dz = cosA * orb.Speed;
                float rotY = (float)Math.Atan2(dx, dz);

                WriteTranslationRotY(_podItems[i].Transform.M, px, 0.06f, pz, rotY);

                // Update dynamic pick target in place (zero allocations)
                PickTarget pt = _podPickTargets[i];
                pt.Center = new Vec3(px, 0.55f, pz);
                pt.Bounds = new Aabb(new Vec3(px - 1.2f, 0.05f, pz - 1.2f), new Vec3(px + 1.2f, 1.3f, pz + 1.2f));

                // Find nearest healthy subterranean vault
                VaultPos nearest = vaults[0];
                float bestDistSq = float.MaxValue;
                for (int v = 0; v < vaultCount; v++)
                {
                    if (hasHealthy && sim.Vaults != null && v < sim.Vaults.Length && sim.Vaults[v].Faulted) continue;
                    VaultPos vp = vaults[v];
                    float ddx = px - vp.X;
                    float ddz = pz - vp.Z;
                    float dSq = ddx * ddx + ddz * ddz;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        nearest = vp;
                    }
                }

                float startY = 1.06f;
                float endY = nearest.Y + 0.6f;

                // Inner electric-cyan V2X core beam
                WriteLineVertex(_v2xVerts, vVert++, px, startY, pz, 0.14f, 0.88f, 0.98f, 0.85f);
                WriteLineVertex(_v2xVerts, vVert++, nearest.X, endY, nearest.Z, 0.14f, 0.88f, 0.98f, 0.70f);

                // Outer electric-violet aura beam
                WriteLineVertex(_v2xVerts, vVert++, px, startY + 0.14f, pz, 0.68f, 0.34f, 0.98f, 0.55f);
                WriteLineVertex(_v2xVerts, vVert++, nearest.X, endY + 0.22f, nearest.Z, 0.68f, 0.34f, 0.98f, 0.45f);

                // Traveling bright optical packet streak along beam
                float t0 = (elapsed * 1.15f + i * 0.19f) % 1.0f;
                if (t0 < 0f) t0 += 1.0f;
                float t1 = Math.Min(1.0f, t0 + 0.14f);
                float sx = nearest.X - px;
                float sy = endY - startY;
                float sz = nearest.Z - pz;
                WriteLineVertex(_v2xVerts, vVert++, px + sx * t0, startY + sy * t0, pz + sz * t0, 0.65f, 0.98f, 1.0f, 1.0f);
                WriteLineVertex(_v2xVerts, vVert++, px + sx * t1, startY + sy * t1, pz + sz * t1, 0.90f, 0.60f, 1.0f, 1.0f);
            }

            _podSkirtBatch.UpdateInstances(_podItems, PodCount);
            _podHullBatch.UpdateInstances(_podItems, PodCount);
            _podGlassBatch.UpdateInstances(_podItems, PodCount);
            _podHeadlightBatch.UpdateInstances(_podItems, PodCount);
            _podTaillightBatch.UpdateInstances(_podItems, PodCount);
            _podLidarBatch.UpdateInstances(_podItems, PodCount);
            _v2xLines.UpdateLines(_v2xVerts, vVert);
        }

        public void Render(Renderer r, LayerVisibility layers)
        {
            if (layers != null && !layers.Surface) return;

            Mat4 id = Mat4.Identity();

            // Roads, parks, ponds
            r.DrawMesh(_parksLawnMesh, id, _parkLawnMat);
            r.DrawMesh(_parksPathMesh, id, _parkPathMat);
            r.DrawMesh(_pondBanksMesh, id, _pondBankMat);
            r.DrawMesh(_pondWatersMesh, id, _waterReflectMat);
            r.DrawMesh(_roadsMesh, id, _asphaltMat);
            r.DrawMesh(_guidewayMesh, id, _guidewayMat);

            // Civic Hub
            r.DrawMesh(_hubDeckMesh, id, _plazaStoneMat);
            r.DrawMesh(_hubCourtyardLawnMesh, id, _sedumGreenMat);
            r.DrawMesh(_hubBasinRimMesh, id, _plazaTrimMat);
            r.DrawMesh(_hubBasinWaterMesh, id, _waterReflectMat);
            r.DrawMesh(_hubSpireMesh, id, _hubSpireMat);
            r.DrawMesh(_hubBuildingsMesh, id, _civicFacadeMat);
            r.DrawMesh(_hubSedumMesh, id, _sedumGreenMat);
            r.DrawMesh(_hubParapetsMesh, id, _darkMetalMat);

            // 5 Civic Plazas (opaque parts first, translucent glazing after)
            r.DrawInstanced(_plazaStoneBatch, _plazaStoneMat);
            r.DrawInstanced(_plazaTrimBatch, _plazaTrimMat);
            r.DrawInstanced(_plazaBenchBatch, _benchTimberMat);
            r.DrawInstanced(_plazaRibsBatch, _pavilionRibMat);
            r.DrawInstanced(_plazaBollardPostBatch, _bollardBodyMat);
            r.DrawInstanced(_plazaBollardLampBatch, _bollardLampMat);
            r.DrawInstanced(_plazaStatusRingBatch, _plazaStatusRingMat);

            // BESS Yard
            r.DrawMesh(_bessPadMesh, id, _plazaStoneMat);
            r.DrawMesh(_bessRailsMesh, id, _bessRailsMat);
            r.DrawMesh(_bessPostsMesh, id, _bessPostsMat);
            r.DrawMesh(_bessUnitsMesh, id, _bessUnitsMat);
            r.DrawMesh(_bessBushingsMesh, id, _bessBushingMat);
            r.DrawInstanced(_bessLedBatch, _bessLedMat);

            // Trees
            r.DrawInstanced(_treeTrunkBatch, _trunkMat);
            r.DrawInstanced(_decCrownBatch, _foliageMat);
            r.DrawInstanced(_conCrownBatch, _foliageMat);

            // 4 Residential Typologies + Parapets + Gabled Roofs + Rooftop PV
            for (int i = 0; i < _buildingGroups.Length; i++)
            {
                r.DrawInstanced(_buildingGroups[i].Batch, _buildingGroups[i].Material);
            }
            r.DrawInstanced(_pvFrameBatch, _pvFrameMat);
            r.DrawInstanced(_pvPanelBatch, _pvPanelMat);

            // 12 Autonomous 6G V2X Pods
            r.DrawInstanced(_podSkirtBatch, _podSkirtMat);
            r.DrawInstanced(_podHullBatch, _podHullMat);
            r.DrawInstanced(_podGlassBatch, _podGlassMat);
            r.DrawInstanced(_podHeadlightBatch, _podHeadlightMat);
            r.DrawInstanced(_podTaillightBatch, _podTaillightMat);
            r.DrawInstanced(_podLidarBatch, _podLidarMat);

            // Translucent plaza skylight pavilions & additive V2X optical beams
            r.DrawInstanced(_plazaOculusGlassBatch, _oculusGlassMat);
            r.DrawInstanced(_plazaCanopyGlassBatch, _canopyGlassMat);
            r.DrawLines(_v2xLines, true);
        }

        public void CollectPickTargets(List<PickTarget> dst)
        {
            if (dst == null) return;
            for (int i = 0; i < _staticPickTargets.Count; i++) dst.Add(_staticPickTargets[i]);
            for (int i = 0; i < _podPickTargets.Length; i++) dst.Add(_podPickTargets[i]);
        }

        public void Dispose()
        {
            DisposeObj(_roadsMesh);
            DisposeObj(_guidewayMesh);
            DisposeObj(_hubDeckMesh);
            DisposeObj(_hubCourtyardLawnMesh);
            DisposeObj(_hubBasinRimMesh);
            DisposeObj(_hubBasinWaterMesh);
            DisposeObj(_hubSpireMesh);
            DisposeObj(_hubBuildingsMesh);
            DisposeObj(_hubSedumMesh);
            DisposeObj(_hubParapetsMesh);
            DisposeObj(_bessPadMesh);
            DisposeObj(_bessRailsMesh);
            DisposeObj(_bessPostsMesh);
            DisposeObj(_bessUnitsMesh);
            DisposeObj(_bessBushingsMesh);
            DisposeObj(_parksLawnMesh);
            DisposeObj(_parksPathMesh);
            DisposeObj(_pondBanksMesh);
            DisposeObj(_pondWatersMesh);
            DisposeObj(_plazaStoneMesh);
            DisposeObj(_plazaTrimMesh);
            DisposeObj(_plazaBenchMesh);
            DisposeObj(_plazaOculusGlassMesh);
            DisposeObj(_plazaCanopyGlassMesh);
            DisposeObj(_plazaRibsMesh);
            DisposeObj(_plazaBollardPostMesh);
            DisposeObj(_plazaBollardLampMesh);
            DisposeObj(_plazaStatusRingMesh);
            DisposeObj(_bessLedUnitMesh);
            DisposeObj(_treeTrunkUnitMesh);
            DisposeObj(_decCrownUnitMesh);
            DisposeObj(_conCrownUnitMesh);
            DisposeObj(_unitBoxMesh);
            DisposeObj(_parapetPenthouseUnitMesh);
            DisposeObj(_gabledRoofUnitMesh);
            DisposeObj(_pvFrameUnitMesh);
            DisposeObj(_pvPanelUnitMesh);
            DisposeObj(_podSkirtMesh);
            DisposeObj(_podHullMesh);
            DisposeObj(_podGlassMesh);
            DisposeObj(_podHeadlightMesh);
            DisposeObj(_podTaillightMesh);
            DisposeObj(_podLidarMesh);
            DisposeObj(_plazaStoneBatch);
            DisposeObj(_plazaTrimBatch);
            DisposeObj(_plazaBenchBatch);
            DisposeObj(_plazaOculusGlassBatch);
            DisposeObj(_plazaCanopyGlassBatch);
            DisposeObj(_plazaRibsBatch);
            DisposeObj(_plazaBollardPostBatch);
            DisposeObj(_plazaBollardLampBatch);
            DisposeObj(_plazaStatusRingBatch);
            DisposeObj(_bessLedBatch);
            DisposeObj(_treeTrunkBatch);
            DisposeObj(_decCrownBatch);
            DisposeObj(_conCrownBatch);
            if (_buildingGroups != null)
            {
                for (int i = 0; i < _buildingGroups.Length; i++) DisposeObj(_buildingGroups[i].Batch);
            }
            DisposeObj(_pvFrameBatch);
            DisposeObj(_pvPanelBatch);
            DisposeObj(_podSkirtBatch);
            DisposeObj(_podHullBatch);
            DisposeObj(_podGlassBatch);
            DisposeObj(_podHeadlightBatch);
            DisposeObj(_podTaillightBatch);
            DisposeObj(_podLidarBatch);
            DisposeObj(_v2xLines);
        }

        private static void DisposeObj(IDisposable d)
        {
            if (d != null) d.Dispose();
        }
    }
}
