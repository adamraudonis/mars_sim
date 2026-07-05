import * as THREE from 'three';

/** Zero-asset structure builders. Each returns a single group positioned at origin. */

const steel = new THREE.MeshStandardMaterial({ color: 0xb8babf, metalness: 0.85, roughness: 0.35 });
const tile = new THREE.MeshStandardMaterial({ color: 0x1f1f23, metalness: 0.2, roughness: 0.7 });
const habShell = new THREE.MeshStandardMaterial({ color: 0xd9d1c2, metalness: 0.1, roughness: 0.6 });
const trim = new THREE.MeshStandardMaterial({ color: 0x596068, metalness: 0.4, roughness: 0.6 });
const rigBody = new THREE.MeshStandardMaterial({ color: 0xcc9926, metalness: 0.3, roughness: 0.6 });
const track = new THREE.MeshStandardMaterial({ color: 0x232326, metalness: 0.2, roughness: 0.8 });
const tankMat = new THREE.MeshStandardMaterial({ color: 0xe6e8ee, metalness: 0.7, roughness: 0.3 });
const isruBox = new THREE.MeshStandardMaterial({ color: 0xbfb8a6, metalness: 0.4, roughness: 0.6 });
const crateMat = new THREE.MeshStandardMaterial({ color: 0xa69980, metalness: 0.1, roughness: 0.8 });
const reactorCore = new THREE.MeshStandardMaterial({ color: 0x4c5158, metalness: 0.7, roughness: 0.45 });
const radiatorMat = new THREE.MeshStandardMaterial({ color: 0xe0e2e8, metalness: 0.35, roughness: 0.4 });
const bermMat = new THREE.MeshStandardMaterial({ color: 0x804d30, roughness: 1 });
const pitMat = new THREE.MeshStandardMaterial({ color: 0x52321f, roughness: 1 });

function box(w: number, h: number, d: number, mat: THREE.Material, x = 0, y = 0, z = 0, ry = 0, rx = 0): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
  m.position.set(x, y, z);
  m.rotation.set(rx, ry, 0);
  return m;
}

function cyl(rTop: number, rBot: number, h: number, mat: THREE.Material, x = 0, y = 0, z = 0, rz = 0, rx = 0): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(rTop, rBot, h, 20), mat);
  m.position.set(x, y, z);
  m.rotation.set(rx, 0, rz);
  return m;
}

export function makeStarship(): THREE.Group {
  const g = new THREE.Group();
  g.add(cyl(4.5, 4.5, 46, steel, 0, 23, 0));
  g.add(cyl(2.2, 4.4, 8, steel, 0, 50, 0));
  const nose = new THREE.Mesh(new THREE.SphereGeometry(2.2, 16, 12), steel);
  nose.position.set(0, 54, 0);
  nose.scale.y = 1.4;
  g.add(nose);
  g.add(box(3.4, 9, 0.5, tile, -5.2, 41, 0));
  g.add(box(3.4, 9, 0.5, tile, 5.2, 41, 0));
  g.add(box(4.2, 11, 0.6, tile, -5.6, 6, 0));
  g.add(box(4.2, 11, 0.6, tile, 5.6, 6, 0));
  return g;
}

export function makeHabModule(): THREE.Group {
  const g = new THREE.Group();
  g.add(cyl(3.3, 3.3, 14, habShell, 0, 3.4, 0, 0, Math.PI / 2));
  const domeA = new THREE.Mesh(new THREE.SphereGeometry(3.3, 16, 12), habShell);
  domeA.position.set(0, 3.4, 7);
  g.add(domeA);
  const domeB = domeA.clone();
  domeB.position.z = -7;
  g.add(domeB);
  g.add(box(7.4, 1.4, 12, trim, 0, 0.7, 0));
  g.add(cyl(1.2, 1.2, 2.6, trim, 4.6, 1.8, 0, Math.PI / 2));
  return g;
}

/** Greenhouse tube; material is per-instance so it can glow at night. */
export function makeGreenhouse(): { group: THREE.Group; glass: THREE.MeshStandardMaterial } {
  const glass = new THREE.MeshStandardMaterial({
    color: 0xbfd4cf,
    metalness: 0.1,
    roughness: 0.25,
    transparent: true,
    opacity: 0.75,
    emissive: 0x000000,
  });
  const g = new THREE.Group();
  g.add(cyl(2.5, 2.5, 22, glass, 0, 2.2, 0, 0, Math.PI / 2));
  g.add(box(5.6, 0.8, 23, trim, 0, 0.4, 0));
  return { group: g, glass };
}

export function makeReactor(): THREE.Group {
  const g = new THREE.Group();
  g.add(cyl(1.3, 1.3, 5.2, reactorCore, 0, 2.6, 0));
  g.add(box(10, 4.2, 0.12, radiatorMat, -6.5, 3.3, 0));
  g.add(box(10, 4.2, 0.12, radiatorMat, 6.5, 3.3, 0));
  g.add(box(9, 1.6, 1.8, bermMat, 0, 0.8, 5));
  return g;
}

export function makeIsruPlant(): THREE.Group {
  const g = new THREE.Group();
  g.add(box(10, 4, 6, isruBox, 0, 2, 0));
  g.add(box(10, 4, 6, isruBox, 0, 2, 8));
  g.add(cyl(0.4, 0.4, 4.8, trim, 4.2, 5.4, 0));
  g.add(cyl(0.25, 0.25, 8, trim, 0, 1, 4, 0, Math.PI / 2));
  return g;
}

export function makeDepotTank(): THREE.Group {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.CapsuleGeometry(2.3, 6, 6, 14), tankMat);
  body.position.set(0, 2.6, 0);
  body.rotation.z = Math.PI / 2;
  g.add(body);
  g.add(box(1, 2, 5, trim, -3, 1, 0));
  g.add(box(1, 2, 5, trim, 3, 1, 0));
  return g;
}

export function makeMiningRig(): THREE.Group {
  const g = new THREE.Group();
  g.add(box(3.4, 1.4, 5.4, rigBody, 0, 1.5, 0));
  g.add(box(0.8, 1.4, 6, track, -1.9, 0.7, 0));
  g.add(box(0.8, 1.4, 6, track, 1.9, 0.7, 0));
  g.add(cyl(0.8, 0.8, 3.2, track, 0, 1.1, 3.4, Math.PI / 2));
  return g;
}

export function makeCrate(): THREE.Mesh {
  return box(3.2, 2.4, 3.2, crateMat, 0, 1.2, 0);
}

export function makeMinePit(): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(30, 30, 1.5, 28), pitMat);
  m.position.y = -0.6;
  return m;
}

/** Instanced solar table (panel slab, tilted 18°). */
export function makeSolarInstances(max: number): { mesh: THREE.InstancedMesh; material: THREE.MeshStandardMaterial } {
  const material = new THREE.MeshStandardMaterial({ color: 0x14172a, metalness: 0.6, roughness: 0.25 });
  const geo = new THREE.BoxGeometry(19.4, 0.12, 9.4);
  const mesh = new THREE.InstancedMesh(geo, material, max);
  mesh.count = 0;
  mesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
  return { mesh, material };
}

/** Instanced movers (crew / robots). */
export function makeMoverInstances(max: number, robot: boolean): THREE.InstancedMesh {
  const mat = robot
    ? new THREE.MeshStandardMaterial({ color: 0x33383f, metalness: 0.5, roughness: 0.5, emissive: 0x0a3a55 })
    : new THREE.MeshStandardMaterial({ color: 0xecece8, metalness: 0.05, roughness: 0.5 });
  const geo = new THREE.CapsuleGeometry(0.5, 1.1, 3, 8);
  const mesh = new THREE.InstancedMesh(geo, mat, max);
  mesh.count = 0;
  mesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
  return mesh;
}
