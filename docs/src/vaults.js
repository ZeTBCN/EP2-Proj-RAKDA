import * as THREE from 'three';
import { WORLD_LAYOUT } from './scene.js';

function tagInspectable(obj, key) {
  obj.userData.inspectKey = key;
  return obj;
}

export function createVaults(scene) {
  const group = new THREE.Group();
  group.name = 'subterranean_layer';
  scene.add(group);

  const concreteMat = new THREE.MeshStandardMaterial({
    color: 0x334155,
    roughness: 0.85,
    metalness: 0.12,
  });
  const slabMat = new THREE.MeshStandardMaterial({
    color: 0x1e293b,
    roughness: 0.72,
    metalness: 0.25,
  });
  const trenchMat = new THREE.MeshStandardMaterial({
    color: 0x090f1a,
    roughness: 0.95,
  });
  const hazardMat = new THREE.MeshStandardMaterial({
    color: 0xf59e0b,
    emissive: 0x78350f,
    emissiveIntensity: 0.35,
    roughness: 0.5,
  });
  const rackBodyMat = new THREE.MeshStandardMaterial({
    color: 0x0f172a,
    roughness: 0.45,
    metalness: 0.65,
  });
  const glassTankMat = new THREE.MeshPhysicalMaterial({
    color: 0x94a3b8,
    roughness: 0.15,
    metalness: 0.05,
    transparent: true,
    opacity: 0.32,
  });

  // 1. Five 2.0 MW Subterranean Vaults
  const vaultNodes = WORLD_LAYOUT.VAULT_POSITIONS.map((vp, idx) => {
    const vGroup = tagInspectable(new THREE.Group(), `vault_${idx}`);
    vGroup.position.set(vp.x, vp.y, vp.z);
    vGroup.rotation.y = -vp.angle + Math.PI / 2;
    group.add(vGroup);

    // 1.0 m perimeter vibration-isolation trench
    const trench = new THREE.Mesh(new THREE.BoxGeometry(11.6, 0.35, 9.6), trenchMat);
    trench.position.y = -3.7;
    vGroup.add(trench);

    const hazardTrim = new THREE.Mesh(new THREE.BoxGeometry(11.1, 0.12, 9.1), hazardMat);
    hazardTrim.position.y = -3.52;
    vGroup.add(hazardTrim);

    // Floating acoustic slab
    const slab = new THREE.Mesh(new THREE.BoxGeometry(9.8, 0.45, 7.8), slabMat);
    slab.position.y = -3.2;
    slab.receiveShadow = true;
    vGroup.add(slab);

    // Cutaway RC walls (back + left + right) so interior racks are visible
    const backWall = new THREE.Mesh(new THREE.BoxGeometry(9.8, 3.8, 0.35), concreteMat);
    backWall.position.set(0, -1.1, -3.72);
    vGroup.add(backWall);

    const leftWall = new THREE.Mesh(new THREE.BoxGeometry(0.35, 3.8, 7.8), concreteMat);
    leftWall.position.set(-4.72, -1.1, 0);
    vGroup.add(leftWall);

    const rightWall = new THREE.Mesh(new THREE.BoxGeometry(0.35, 3.8, 7.8), concreteMat);
    rightWall.position.set(4.72, -1.1, 0);
    vGroup.add(rightWall);

    // 8 Liquid-cooled AI server racks (2 rows x 4)
    const rackLedMat = new THREE.MeshStandardMaterial({
      color: 0x22d3ee,
      emissive: 0x22d3ee,
      emissiveIntensity: 1.4,
      roughness: 0.3,
    });

    for (let r = 0; r < 2; r++) {
      for (let c = 0; c < 4; c++) {
        const rx = -2.4 + c * 1.45;
        const rz = r === 0 ? -1.4 : 1.1;
        const rack = new THREE.Mesh(new THREE.BoxGeometry(0.95, 2.35, 1.1), rackBodyMat);
        rack.position.set(rx, -1.8, rz);
        vGroup.add(rack);

        const ledStrip = new THREE.Mesh(new THREE.BoxGeometry(0.75, 1.85, 0.08), rackLedMat);
        ledStrip.position.set(rx, -1.75, rz + 0.56);
        vGroup.add(ledStrip);
      }
    }

    // 800 VDC SST + glowing HTS superconducting busways overhead
    const htsMat = new THREE.MeshStandardMaterial({
      color: 0x38bdf8,
      emissive: 0x0ea5e9,
      emissiveIntensity: 1.35,
      roughness: 0.2,
      metalness: 0.8,
    });
    for (const rz of [-1.4, 1.1]) {
      const busway = new THREE.Mesh(new THREE.CylinderGeometry(0.11, 0.11, 5.4, 12), htsMat);
      busway.rotation.z = Math.PI / 2;
      busway.position.set(-0.2, -0.35, rz);
      vGroup.add(busway);
    }

    // 43.4 m³ 15-min Buffer Tank (right side of vault)
    const bufferJacket = new THREE.Mesh(new THREE.CylinderGeometry(1.05, 1.05, 2.7, 20), glassTankMat);
    bufferJacket.position.set(3.45, -1.65, -0.2);
    vGroup.add(bufferJacket);

    const bufferFluidMat = new THREE.MeshStandardMaterial({
      color: 0xff5a1f,
      emissive: 0xea580c,
      emissiveIntensity: 0.9,
      roughness: 0.25,
    });
    const bufferFluid = new THREE.Mesh(new THREE.CylinderGeometry(0.86, 0.86, 2.2, 18), bufferFluidMat);
    bufferFluid.position.set(3.45, -1.8, -0.2);
    vGroup.add(bufferFluid);

    // Vertical shaft riser connecting vault to surface plaza
    const shaft = new THREE.Mesh(
      new THREE.CylinderGeometry(0.7, 0.85, 15.6, 16),
      new THREE.MeshStandardMaterial({
        color: 0x334155,
        roughness: 0.7,
        metalness: 0.35,
        transparent: true,
        opacity: 0.45,
      })
    );
    shaft.position.set(0, 7.6, 0);
    vGroup.add(shaft);

    // Fault beacon ring
    const beaconMat = new THREE.MeshStandardMaterial({
      color: 0xef4444,
      emissive: 0xef4444,
      emissiveIntensity: 2.2,
      transparent: true,
      opacity: 0.0,
    });
    const faultBeacon = new THREE.Mesh(new THREE.TorusGeometry(5.6, 0.2, 12, 40), beaconMat);
    faultBeacon.rotation.x = Math.PI / 2;
    faultBeacon.position.y = 0.9;
    vGroup.add(faultBeacon);

    return {
      group: vGroup,
      rackLedMat,
      htsMat,
      bufferFluidMat,
      faultBeacon,
      beaconMat,
    };
  });

  // 2. Central Utility Hub (around origin, Level -4)
  const hubGroup = new THREE.Group();
  hubGroup.position.set(0, WORLD_LAYOUT.VAULT_Y, 0);
  group.add(hubGroup);

  const hubPlinth = new THREE.Mesh(new THREE.CylinderGeometry(13.2, 13.8, 0.5, 48), slabMat);
  hubPlinth.position.y = -3.4;
  hubGroup.add(hubPlinth);

  // 2,500 m³ Thermal Store (animated water level)
  const thermalStoreGroup = tagInspectable(new THREE.Group(), 'thermal_store');
  thermalStoreGroup.position.set(-4.2, -3.1, -3.5);
  hubGroup.add(thermalStoreGroup);

  const tsShell = new THREE.Mesh(new THREE.CylinderGeometry(2.6, 2.6, 6.4, 28), glassTankMat);
  tsShell.position.y = 3.2;
  thermalStoreGroup.add(tsShell);

  const tsCap = new THREE.Mesh(new THREE.CylinderGeometry(2.72, 2.72, 0.35, 28), slabMat);
  tsCap.position.y = 6.45;
  thermalStoreGroup.add(tsCap);

  const tsFluidMat = new THREE.MeshStandardMaterial({
    color: 0xff6b2c,
    emissive: 0xf97316,
    emissiveIntensity: 0.95,
    roughness: 0.25,
  });
  const tsFluid = new THREE.Mesh(new THREE.CylinderGeometry(2.35, 2.35, 5.8, 24), tsFluidMat);
  tsFluid.position.y = 2.9;
  thermalStoreGroup.add(tsFluid);

  // 10,000 m³ RC Drought Rainwater Cistern (animated water level)
  const cisternGroup = tagInspectable(new THREE.Group(), 'rainwater_cistern');
  cisternGroup.position.set(4.8, -3.1, -3.2);
  hubGroup.add(cisternGroup);

  const cisternWalls = new THREE.Mesh(
    new THREE.BoxGeometry(7.2, 4.6, 5.4),
    new THREE.MeshPhysicalMaterial({
      color: 0x475569,
      roughness: 0.2,
      transparent: true,
      opacity: 0.28,
    })
  );
  cisternWalls.position.y = 2.3;
  cisternGroup.add(cisternWalls);

  const cisternFrame = new THREE.LineSegments(
    new THREE.EdgesGeometry(new THREE.BoxGeometry(7.24, 4.64, 5.44)),
    new THREE.LineBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.55 })
  );
  cisternFrame.position.y = 2.3;
  cisternGroup.add(cisternFrame);

  const cisternWaterMat = new THREE.MeshStandardMaterial({
    color: 0x0ea5e9,
    emissive: 0x0284c7,
    emissiveIntensity: 0.75,
    roughness: 0.18,
    transparent: true,
    opacity: 0.84,
  });
  const cisternWater = new THREE.Mesh(new THREE.BoxGeometry(6.8, 4.1, 5.0), cisternWaterMat);
  cisternWater.position.y = 2.05;
  cisternGroup.add(cisternWater);

  // 8.0 MW_th Backup HTHP Bank
  const hthpGroup = tagInspectable(new THREE.Group(), 'hthp_bank');
  hthpGroup.position.set(-5.6, -3.1, 4.4);
  hubGroup.add(hthpGroup);

  const hthpMat = new THREE.MeshStandardMaterial({
    color: 0xf59e0b,
    emissive: 0xb45309,
    emissiveIntensity: 0.35,
    roughness: 0.35,
    metalness: 0.6,
  });
  const hthpFans = [];
  for (let i = 0; i < 4; i++) {
    const unit = new THREE.Mesh(new THREE.BoxGeometry(1.4, 2.2, 1.8), concreteMat);
    unit.position.set(-2.4 + i * 1.7, 1.1, 0);
    hthpGroup.add(unit);

    const coil = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.45, 1.5, 14), hthpMat);
    coil.position.set(-2.4 + i * 1.7, 2.35, 0);
    hthpGroup.add(coil);
    hthpFans.push(coil);
  }

  // SrBr2 / K2CO3 TCES Reactor (35 MWh_th)
  const tcesGroup = tagInspectable(new THREE.Group(), 'tces');
  tcesGroup.position.set(1.2, -3.1, 4.8);
  hubGroup.add(tcesGroup);

  const tcesMat = new THREE.MeshStandardMaterial({
    color: 0xfbbf24,
    emissive: 0xd97706,
    emissiveIntensity: 0.4,
    roughness: 0.3,
    metalness: 0.5,
  });
  for (let i = 0; i < 3; i++) {
    const pod = new THREE.Mesh(new THREE.CylinderGeometry(0.68, 0.68, 2.6, 16), tcesMat);
    pod.position.set(-1.5 + i * 1.5, 1.3, 0);
    tcesGroup.add(pod);
  }

  // 2 MW / 16 MWh TI-PTES Carnot Battery
  const ptesGroup = tagInspectable(new THREE.Group(), 'ti_ptes');
  ptesGroup.position.set(6.2, -3.1, 4.4);
  hubGroup.add(ptesGroup);

  const ptesHotMat = new THREE.MeshStandardMaterial({
    color: 0xf97316,
    emissive: 0xc2410c,
    emissiveIntensity: 0.45,
    roughness: 0.3,
    metalness: 0.55,
  });
  const ptesColdMat = new THREE.MeshStandardMaterial({
    color: 0x06b6d4,
    emissive: 0x0e7490,
    emissiveIntensity: 0.45,
    roughness: 0.3,
    metalness: 0.55,
  });
  const ptesHot = new THREE.Mesh(new THREE.CylinderGeometry(0.9, 0.9, 3.2, 18), ptesHotMat);
  ptesHot.position.set(-1.1, 1.6, 0);
  ptesGroup.add(ptesHot);
  const ptesCold = new THREE.Mesh(new THREE.CylinderGeometry(0.9, 0.9, 3.2, 18), ptesColdMat);
  ptesCold.position.set(1.1, 1.6, 0);
  ptesGroup.add(ptesCold);

  // Micro-ORC Skid (<= 440 kW_e)
  const orcGroup = tagInspectable(new THREE.Group(), 'micro_orc');
  orcGroup.position.set(0.2, -3.1, -6.4);
  hubGroup.add(orcGroup);

  const orcSkid = new THREE.Mesh(new THREE.BoxGeometry(3.2, 1.4, 1.8), slabMat);
  orcSkid.position.y = 0.7;
  orcGroup.add(orcSkid);

  const orcTurbineMat = new THREE.MeshStandardMaterial({
    color: 0x10b981,
    emissive: 0x059669,
    emissiveIntensity: 0.45,
    roughness: 0.25,
    metalness: 0.75,
  });
  const orcRotor = new THREE.Mesh(new THREE.CylinderGeometry(0.65, 0.65, 2.2, 16), orcTurbineMat);
  orcRotor.rotation.z = Math.PI / 2;
  orcRotor.position.y = 1.75;
  orcGroup.add(orcRotor);

  // MOF Adsorption Chillers + N+1 Dry Coolers
  const mofGroup = tagInspectable(new THREE.Group(), 'mof_chiller');
  mofGroup.position.set(-8.6, -3.1, -0.4);
  hubGroup.add(mofGroup);

  const mofMat = new THREE.MeshStandardMaterial({
    color: 0x2dd4bf,
    emissive: 0x0f766e,
    emissiveIntensity: 0.35,
    roughness: 0.35,
    metalness: 0.5,
  });
  const mofBlock = new THREE.Mesh(new THREE.BoxGeometry(2.6, 2.4, 2.4), mofMat);
  mofBlock.position.y = 1.2;
  mofGroup.add(mofBlock);

  const dryCoolerGroup = tagInspectable(new THREE.Group(), 'dry_coolers');
  dryCoolerGroup.position.set(9.4, -3.1, 0.2);
  hubGroup.add(dryCoolerGroup);

  const dryFanBlades = [];
  const dcBank = new THREE.Mesh(new THREE.BoxGeometry(2.4, 1.6, 4.6), concreteMat);
  dcBank.position.y = 0.8;
  dryCoolerGroup.add(dcBank);

  const fanMat = new THREE.MeshStandardMaterial({ color: 0x94a3b8, metalness: 0.7, roughness: 0.3 });
  for (let i = 0; i < 3; i++) {
    const fan = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.1, 0.28), fanMat);
    fan.position.set(0, 1.72, -1.4 + i * 1.4);
    dryCoolerGroup.add(fan);
    dryFanBlades.push(fan);
  }

  // Semiconductor Grade C Ultrapure Water Train
  const ultrapureGroup = tagInspectable(new THREE.Group(), 'ultrapure_train');
  ultrapureGroup.position.set(4.6, -3.1, -7.8);
  hubGroup.add(ultrapureGroup);

  const uvMat = new THREE.MeshStandardMaterial({
    color: 0x818cf8,
    emissive: 0x6366f1,
    emissiveIntensity: 1.25,
    roughness: 0.2,
  });
  for (let i = 0; i < 4; i++) {
    const vessel = new THREE.Mesh(
      new THREE.CylinderGeometry(0.42, 0.42, 2.4, 14),
      i === 2 ? uvMat : ptesColdMat
    );
    vessel.position.set(-1.8 + i * 1.15, 1.2, 0);
    ultrapureGroup.add(vessel);
  }

  function update(sim, dt, elapsed) {
    // Update 5 vaults
    sim.vaults.forEach((v, idx) => {
      const node = vaultNodes[idx];
      if (!node) return;
      const loadFactor = v.itMw / 2.0;

      if (v.faulted) {
        node.rackLedMat.color.setHex(0xef4444);
        node.rackLedMat.emissive.setHex(0xef4444);
        node.rackLedMat.emissiveIntensity = 0.4 + 0.5 * Math.abs(Math.sin(elapsed * 5));
        node.htsMat.emissiveIntensity = 0.15;
        node.bufferFluidMat.emissiveIntensity = 0.2;
        node.beaconMat.opacity = 0.55 + 0.4 * Math.sin(elapsed * 6);
        node.faultBeacon.scale.setScalar(1 + 0.06 * Math.sin(elapsed * 6));
      } else {
        node.rackLedMat.color.setHex(0x22d3ee);
        node.rackLedMat.emissive.setHex(0x22d3ee);
        node.rackLedMat.emissiveIntensity = 0.5 + loadFactor * 1.4;
        node.htsMat.emissiveIntensity = 0.45 + loadFactor * 1.1;
        node.bufferFluidMat.emissiveIntensity = 0.4 + loadFactor * 0.9;
        node.beaconMat.opacity = 0;
      }
    });

    // Thermal store animated level
    const tsSoc = Math.max(0.06, sim.thermal.thermalStoreSoc);
    tsFluid.scale.y = tsSoc;
    tsFluid.position.y = tsSoc * 2.9;
    tsFluidMat.emissiveIntensity =
      sim.thermal.thermalStoreDischargeMw > 0.1
        ? 1.45 + 0.35 * Math.sin(elapsed * 4)
        : sim.thermal.thermalStoreChargeMw > 0.1
          ? 1.2
          : 0.65;

    // 10,000 m³ cistern animated level
    const cSoc = Math.max(0.05, sim.hydrology.cisternSoc);
    cisternWater.scale.y = cSoc;
    cisternWater.position.y = cSoc * 2.05;
    cisternWaterMat.color.setHex(cSoc < 0.48 ? 0x38bdf8 : 0x0ea5e9);

    // HTHP Bank activity
    const hthpActive = sim.thermal.hthpThermalOutputMw > 0.05 || sim.thermal.scenarioBActive;
    hthpMat.emissiveIntensity = hthpActive ? 0.9 + 0.35 * Math.sin(elapsed * 4) : 0.22;
    const hthpSpin = hthpActive ? 3.5 : 0.4;
    for (const f of hthpFans) f.rotation.y += dt * hthpSpin;

    // Exergy cascade components
    tcesMat.emissiveIntensity = sim.thermal.tcesChargeMw > 0.05 ? 1.15 : 0.25;
    const ptesIntensity = sim.thermal.tiPtesChargeMw > 0.05 ? 1.1 : 0.25;
    ptesHotMat.emissiveIntensity = ptesIntensity;
    ptesColdMat.emissiveIntensity = ptesIntensity;

    const orcNorm = sim.thermal.orcOutputKw / 440;
    orcTurbineMat.emissiveIntensity = 0.25 + orcNorm * 1.6;
    orcRotor.rotation.x += dt * (0.5 + orcNorm * 8.0);

    mofMat.emissiveIntensity = sim.thermal.mofChillerMw > 0.05 ? 1.15 : 0.22;

    const dcSpin = 0.6 + sim.thermal.dryCoolerRejectionMw * 2.8;
    for (const blade of dryFanBlades) blade.rotation.y += dt * dcSpin;
  }

  return { group, vaultNodes, update };
}
