import * as THREE from 'three';
import { WORLD_LAYOUT } from './scene.js';

function tagInspectable(obj, key) {
  obj.userData.inspectKey = key;
  return obj;
}

function isNearVaultPlaza(x, z, minClear = 8.2) {
  for (const vp of WORLD_LAYOUT.VAULT_POSITIONS) {
    const dx = x - vp.x;
    const dz = z - vp.z;
    if (dx * dx + dz * dz < minClear * minClear) return true;
  }
  return false;
}

function isNearRoadOrPark(r, angle) {
  if (Math.abs(r - 16) < 2.8 || Math.abs(r - 38) < 3.0) return true;
  // 5 radial boulevards aligned with vault angles
  for (const vp of WORLD_LAYOUT.VAULT_POSITIONS) {
    let diff = Math.abs(angle - vp.angle);
    if (diff > Math.PI) diff = Math.PI * 2 - diff;
    if (diff < 0.11) return true;
  }
  // 2 blue-green park wedges
  const normA = ((angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
  if ((normA > 0.42 && normA < 0.72) || (normA > 3.55 && normA < 3.85)) return true;
  return false;
}

export function createDistrict(scene) {
  const group = new THREE.Group();
  group.name = 'surface_layer';
  scene.add(group);

  let heatmapVisible = false;

  // 1. Concentric & Radial Transit Avenues
  const roadMat = new THREE.MeshStandardMaterial({
    color: 0x1e293b,
    roughness: 0.85,
    metalness: 0.1,
  });
  for (const ringR of [16, 38]) {
    const ringRoad = new THREE.Mesh(new THREE.RingGeometry(ringR - 1.6, ringR + 1.6, 84), roadMat);
    ringRoad.rotation.x = -Math.PI / 2;
    ringRoad.position.y = 0.04;
    ringRoad.receiveShadow = true;
    group.add(ringRoad);
  }
  for (const vp of WORLD_LAYOUT.VAULT_POSITIONS) {
    const spoke = new THREE.Mesh(new THREE.PlaneGeometry(2.6, WORLD_LAYOUT.DISTRICT_RADIUS - 14), roadMat);
    spoke.rotation.x = -Math.PI / 2;
    spoke.rotation.z = -vp.angle + Math.PI / 2;
    spoke.position.set(
      Math.cos(vp.angle) * (WORLD_LAYOUT.DISTRICT_RADIUS * 0.5 + 4),
      0.035,
      Math.sin(vp.angle) * (WORLD_LAYOUT.DISTRICT_RADIUS * 0.5 + 4)
    );
    group.add(spoke);
  }

  // 2. Five Public Civic Plazas directly over the 5 subterranean vaults
  const plazaMat = new THREE.MeshStandardMaterial({
    color: 0x475569,
    roughness: 0.75,
    metalness: 0.15,
  });
  const skylightMat = new THREE.MeshPhysicalMaterial({
    color: 0x38bdf8,
    roughness: 0.15,
    transparent: true,
    opacity: 0.38,
  });

  for (const vp of WORLD_LAYOUT.VAULT_POSITIONS) {
    const plaza = tagInspectable(new THREE.Group(), 'civic_plaza');
    plaza.position.set(vp.x, 0.07, vp.z);
    group.add(plaza);

    const disc = new THREE.Mesh(new THREE.CylinderGeometry(6.4, 6.6, 0.16, 32), plazaMat);
    disc.receiveShadow = true;
    plaza.add(disc);

    const skylight = new THREE.Mesh(new THREE.CylinderGeometry(2.2, 2.2, 0.2, 24), skylightMat);
    skylight.position.y = 0.05;
    plaza.add(skylight);

    const rim = new THREE.Mesh(
      new THREE.TorusGeometry(6.2, 0.12, 8, 36),
      new THREE.MeshBasicMaterial({ color: 0x22d3ee, transparent: true, opacity: 0.45 })
    );
    rim.rotation.x = Math.PI / 2;
    rim.position.y = 0.12;
    plaza.add(rim);
  }

  // 3. Central Civic Hub, Schools & Health Centre (inside r = 13)
  const civicHubGroup = tagInspectable(new THREE.Group(), 'civic_hub');
  group.add(civicHubGroup);

  const hubDeck = new THREE.Mesh(
    new THREE.CylinderGeometry(12.5, 12.8, 0.16, 48),
    new THREE.MeshStandardMaterial({ color: 0x334155, roughness: 0.75 })
  );
  hubDeck.position.y = 0.06;
  civicHubGroup.add(hubDeck);

  const civicMat = new THREE.MeshStandardMaterial({ color: 0x64748b, roughness: 0.45, metalness: 0.25 });
  const civicSpecs = [
    { x: -4.8, z: -3.6, w: 4.8, h: 3.2, d: 3.4 },
    { x: 4.5, z: -3.2, w: 5.2, h: 2.8, d: 3.2 },
    { x: -4.2, z: 4.4, w: 4.2, h: 2.5, d: 3.8 },
    { x: 4.2, z: 4.4, w: 4.4, h: 3.6, d: 3.4 },
  ];
  for (const cs of civicSpecs) {
    const b = new THREE.Mesh(new THREE.BoxGeometry(cs.w, cs.h, cs.d), civicMat);
    b.position.set(cs.x, cs.h / 2 + 0.14, cs.z);
    b.castShadow = true;
    b.receiveShadow = true;
    civicHubGroup.add(b);
  }

  // 4. 8 MW / 32 MWh LFP BESS Yard (outer civic utility pad)
  const bessGroup = tagInspectable(new THREE.Group(), 'bess_yard');
  bessGroup.position.set(-48, 0.1, 22);
  group.add(bessGroup);

  const bessPad = new THREE.Mesh(new THREE.BoxGeometry(10.5, 0.2, 8.5), plazaMat);
  bessGroup.add(bessPad);

  const bessLedMat = new THREE.MeshStandardMaterial({
    color: 0x10b981,
    emissive: 0x10b981,
    emissiveIntensity: 0.9,
  });
  for (let r = 0; r < 2; r++) {
    for (let c = 0; c < 4; c++) {
      const container = new THREE.Mesh(
        new THREE.BoxGeometry(1.8, 1.35, 3.0),
        new THREE.MeshStandardMaterial({ color: 0x1e293b, roughness: 0.5, metalness: 0.5 })
      );
      container.position.set(-3.3 + c * 2.2, 0.78, r === 0 ? -2.0 : 2.0);
      container.castShadow = true;
      bessGroup.add(container);

      const led = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.16, 0.1), bessLedMat);
      led.position.set(-3.3 + c * 2.2, 1.25, (r === 0 ? -2.0 : 2.0) + 1.52);
      bessGroup.add(led);
    }
  }

  // 5. Blue-Green Parks & Instanced Trees
  const parkMat = new THREE.MeshStandardMaterial({ color: 0x14532d, roughness: 0.9 });
  for (const parkAngle of [0.57, 3.70]) {
    const wedge = new THREE.Mesh(
      new THREE.RingGeometry(18.5, 68, 20, 1, parkAngle - 0.16, 0.32),
      parkMat
    );
    wedge.rotation.x = -Math.PI / 2;
    wedge.position.y = 0.045;
    group.add(wedge);
  }

  const treeCoords = [];
  for (let i = 0; i < 240; i++) {
    const a = ((i * 2.39996) % (Math.PI * 2));
    const r = 19 + ((i * 17.3) % 48);
    const x = Math.cos(a) * r;
    const z = Math.sin(a) * r;
    if (isNearVaultPlaza(x, z, 7.5)) continue;
    const normA = ((a % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
    const inPark = (normA > 0.41 && normA < 0.73) || (normA > 3.54 && normA < 3.86);
    if (inPark || i % 4 === 0) {
      if (Math.abs(r - 16) > 2.4 && Math.abs(r - 38) > 2.6) {
        treeCoords.push({ x, z, scale: 0.75 + (i % 5) * 0.12 });
      }
    }
  }

  if (treeCoords.length > 0) {
    const trunkMesh = new THREE.InstancedMesh(
      new THREE.CylinderGeometry(0.14, 0.2, 1.1, 6),
      new THREE.MeshStandardMaterial({ color: 0x5c4033, roughness: 0.9 }),
      treeCoords.length
    );
    const canopyMesh = new THREE.InstancedMesh(
      new THREE.ConeGeometry(0.85, 2.2, 7),
      new THREE.MeshStandardMaterial({ color: 0x15803d, roughness: 0.85 }),
      treeCoords.length
    );
    const dummy = new THREE.Object3D();
    treeCoords.forEach((tc, i) => {
      dummy.position.set(tc.x, 0.55 * tc.scale, tc.z);
      dummy.scale.setScalar(tc.scale);
      dummy.updateMatrix();
      trunkMesh.setMatrixAt(i, dummy.matrix);

      dummy.position.set(tc.x, 1.85 * tc.scale, tc.z);
      dummy.updateMatrix();
      canopyMesh.setMatrixAt(i, dummy.matrix);
    });
    group.add(trunkMesh, canopyMesh);
  }

  // 6. Four Residential Typologies (InstancedMeshes) + 12 MW_p Rooftop PV Arrays
  const buildingRecords = [];
  const pvRecords = [];

  function sampleRing(count, rMin, rMax, seedOffset) {
    const pts = [];
    let attempt = 0;
    while (pts.length < count && attempt < count * 8) {
      attempt++;
      const a = ((attempt * 2.39996 + seedOffset) % (Math.PI * 2));
      const frac = ((attempt * 0.6180339) % 1);
      const r = rMin + frac * (rMax - rMin);
      const x = Math.cos(a) * r;
      const z = Math.sin(a) * r;
      if (isNearVaultPlaza(x, z, 8.6)) continue;
      if (isNearRoadOrPark(r, a)) continue;
      if (Math.hypot(x + 48, z - 22) < 9.5) continue; // avoid BESS pad
      pts.push({ x, z, angle: a });
    }
    return pts;
  }

  const midRisePts = sampleRing(55, 19.5, 34.5, 0.1);
  const lowRisePts = sampleRing(40, 31.0, 41.5, 1.2);
  const townhousePts = sampleRing(165, 42.5, 54.5, 2.4);
  const detachedPts = sampleRing(150, 56.0, 67.5, 3.7);

  const baseColor = new THREE.Color(0xcbd5e1);
  const midColor = new THREE.Color(0x94a3b8);
  const townColor = new THREE.Color(0x7c8ea6);
  const detColor = new THREE.Color(0x64748b);

  function buildTypologyMesh(pts, w, hMin, hVar, d, defaultColor, addPv) {
    if (pts.length === 0) return null;
    const mat = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      roughness: 0.58,
      metalness: 0.18,
    });
    const mesh = tagInspectable(
      new THREE.InstancedMesh(new THREE.BoxGeometry(w, 1, d), mat, pts.length),
      'residential_district'
    );
    mesh.castShadow = true;
    mesh.receiveShadow = true;

    const dummy = new THREE.Object3D();
    pts.forEach((pt, idx) => {
      const h = hMin + ((idx * 37) % 100) / 100 * hVar;
      dummy.position.set(pt.x, h / 2 + 0.05, pt.z);
      dummy.rotation.set(0, -pt.angle + Math.PI / 2, 0);
      dummy.scale.set(1, h, 1);
      dummy.updateMatrix();
      mesh.setMatrixAt(idx, dummy.matrix);
      mesh.setColorAt(idx, defaultColor);

      if (addPv) {
        pvRecords.push({ x: pt.x, y: h + 0.22, z: pt.z, rotY: -pt.angle + Math.PI / 2, w: w * 0.78, d: d * 0.65 });
      }
    });
    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
    group.add(mesh);
    buildingRecords.push({ mesh, count: pts.length, defaultColor });
    return mesh;
  }

  buildTypologyMesh(midRisePts, 3.6, 5.2, 2.4, 2.4, baseColor, true);
  buildTypologyMesh(lowRisePts, 3.2, 3.2, 1.2, 2.2, midColor, true);
  buildTypologyMesh(townhousePts, 2.1, 1.9, 0.7, 1.5, townColor, false);
  buildTypologyMesh(detachedPts, 1.7, 1.5, 0.5, 1.5, detColor, false);

  // Add PV arrays onto civic hub roofs as well
  for (const cs of civicSpecs) {
    pvRecords.push({ x: cs.x, y: cs.h + 0.3, z: cs.z, rotY: 0, w: cs.w * 0.8, d: cs.d * 0.7 });
  }

  const pvMat = new THREE.MeshStandardMaterial({
    color: 0x1d4ed8,
    emissive: 0x1e40af,
    emissiveIntensity: 0.35,
    roughness: 0.2,
    metalness: 0.85,
  });
  let pvMesh = null;
  if (pvRecords.length > 0) {
    pvMesh = tagInspectable(
      new THREE.InstancedMesh(new THREE.BoxGeometry(1, 0.12, 1), pvMat, pvRecords.length),
      'rooftop_pv'
    );
    const dummy = new THREE.Object3D();
    pvRecords.forEach((p, idx) => {
      dummy.position.set(p.x, p.y, p.z);
      dummy.rotation.set(0.24, p.rotY, 0);
      dummy.scale.set(p.w, 1, p.d);
      dummy.updateMatrix();
      pvMesh.setMatrixAt(idx, dummy.matrix);
    });
    pvMesh.instanceMatrix.needsUpdate = true;
    group.add(pvMesh);
  }

  // 7. 12 Autonomous 6G V2X Transit Pods + Dynamic Optical V2X Beams
  const podsGroup = tagInspectable(new THREE.Group(), 'autonomous_pods');
  group.add(podsGroup);

  const podCount = 12;
  const podBodyMat = new THREE.MeshStandardMaterial({
    color: 0xf8fafc,
    roughness: 0.25,
    metalness: 0.4,
  });
  const podGlowMat = new THREE.MeshStandardMaterial({
    color: 0x22d3ee,
    emissive: 0x22d3ee,
    emissiveIntensity: 1.5,
  });

  const pods = Array.from({ length: podCount }, (_, i) => {
    const pod = new THREE.Group();
    const hull = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.75, 0.9), podBodyMat);
    hull.position.y = 0.48;
    hull.castShadow = true;
    pod.add(hull);

    const visor = new THREE.Mesh(new THREE.BoxGeometry(1.54, 0.22, 0.94), podGlowMat);
    visor.position.y = 0.62;
    pod.add(visor);

    podsGroup.add(pod);
    return {
      mesh: pod,
      ringRadius: i % 2 === 0 ? 16 : 38,
      baseAngle: (i / podCount) * Math.PI * 2,
      speed: i % 2 === 0 ? 0.22 : -0.13,
    };
  });

  const v2xPositions = new Float32Array(podCount * 2 * 3);
  const v2xGeom = new THREE.BufferGeometry();
  v2xGeom.setAttribute('position', new THREE.BufferAttribute(v2xPositions, 3));
  const v2xLines = new THREE.LineSegments(
    v2xGeom,
    new THREE.LineBasicMaterial({
      color: 0xa855f7,
      transparent: true,
      opacity: 0.68,
    })
  );
  podsGroup.add(v2xLines);

  const heatLow = new THREE.Color(0x0ea5e9);
  const heatMid = new THREE.Color(0xf59e0b);
  const heatHigh = new THREE.Color(0xff4518);
  const tempColor = new THREE.Color();

  function setHeatmapVisible(visible) {
    heatmapVisible = Boolean(visible);
  }

  function update(sim, dt, elapsed) {
    // Update thermal heat-map tinting on buildings
    const demandNorm = Math.min(1, Math.max(0, (sim.thermal.districtDemandMw - 1.45) / (9.52 - 1.45)));
    if (heatmapVisible) {
      if (demandNorm < 0.5) {
        tempColor.lerpColors(heatLow, heatMid, demandNorm * 2);
      } else {
        tempColor.lerpColors(heatMid, heatHigh, (demandNorm - 0.5) * 2);
      }
    }

    for (const rec of buildingRecords) {
      const targetColor = heatmapVisible ? tempColor : rec.defaultColor;
      for (let i = 0; i < rec.count; i++) {
        rec.mesh.setColorAt(i, targetColor);
      }
      if (rec.mesh.instanceColor) rec.mesh.instanceColor.needsUpdate = true;
    }

    // Update rooftop PV shimmer from live solar output
    pvMat.emissiveIntensity = 0.15 + (sim.electrical.pvOutputMw / 12.0) * 0.95;

    // Update BESS status LED
    if (sim.electrical.bessDischargeMw > 0.05) {
      bessLedMat.color.setHex(0x10b981);
      bessLedMat.emissive.setHex(0x10b981);
      bessLedMat.emissiveIntensity = 1.4;
    } else if (sim.electrical.bessChargeMw > 0.05) {
      bessLedMat.color.setHex(0x22d3ee);
      bessLedMat.emissive.setHex(0x22d3ee);
      bessLedMat.emissiveIntensity = 1.4;
    } else {
      bessLedMat.color.setHex(0x64748b);
      bessLedMat.emissive.setHex(0x334155);
      bessLedMat.emissiveIntensity = 0.4;
    }

    // Animate Autonomous Pods & V2X beams to nearest non-faulted vault
    const healthyVaults = WORLD_LAYOUT.VAULT_POSITIONS.filter((_, i) => !sim.vaults[i]?.faulted);
    const targetPool = healthyVaults.length > 0 ? healthyVaults : WORLD_LAYOUT.VAULT_POSITIONS;

    pods.forEach((p, idx) => {
      const ang = p.baseAngle + elapsed * p.speed;
      const px = Math.cos(ang) * p.ringRadius;
      const pz = Math.sin(ang) * p.ringRadius;
      p.mesh.position.set(px, 0.06, pz);
      p.mesh.rotation.y = -ang + (p.speed > 0 ? Math.PI / 2 : -Math.PI / 2);

      let nearest = targetPool[0];
      let bestDistSq = Infinity;
      for (const vp of targetPool) {
        const dSq = (px - vp.x) ** 2 + (pz - vp.z) ** 2;
        if (dSq < bestDistSq) {
          bestDistSq = dSq;
          nearest = vp;
        }
      }

      const offset = idx * 6;
      v2xPositions[offset] = px;
      v2xPositions[offset + 1] = 0.85;
      v2xPositions[offset + 2] = pz;
      v2xPositions[offset + 3] = nearest.x;
      v2xPositions[offset + 4] = nearest.y + 0.5;
      v2xPositions[offset + 5] = nearest.z;
    });
    v2xGeom.attributes.position.needsUpdate = true;
  }

  return { group, setHeatmapVisible, update };
}
