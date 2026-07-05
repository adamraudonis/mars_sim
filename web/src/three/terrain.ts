import * as THREE from 'three';
import { valueNoise2 } from './noise';

export const TERRAIN_EXTENT = 3200;
const RES = 300;
const FLAT_RADIUS = 260;

function crater(x: number, z: number, cx: number, cz: number, radius: number, depth: number): number {
  const d = Math.hypot(x - cx, z - cz) / radius;
  if (d > 1.6) return 0;
  const bowl = -depth * Math.exp(-d * d * 3.2);
  const rim = depth * 0.35 * Math.exp(-(d - 1.05) * (d - 1.05) * 14);
  return bowl + rim;
}

/** Height field: fBm relief, crater dressing, graded flat settlement zone in the middle. */
export function terrainHeight(x: number, z: number): number {
  let h = 0;
  h += valueNoise2(x * 0.0008 + 31.7, z * 0.0008 + 11.3) * 26;
  h += valueNoise2(x * 0.003 + 7.1, z * 0.003 + 3.9) * 7;
  h += valueNoise2(x * 0.012 + 1.7, z * 0.012 + 9.2) * 1.6;
  h -= 18;

  h += crater(x, z, 620, 540, 130, 14);
  h += crater(x, z, -780, 300, 170, 18);
  h += crater(x, z, 240, -900, 200, 22);
  h += crater(x, z, -420, -680, 90, 10);
  h += crater(x, z, 980, -260, 110, 12);

  const d = Math.hypot(x, z);
  const t = Math.max(0, Math.min(1, (d - FLAT_RADIUS) / 220));
  const flat = t * t * (3 - 2 * t);
  return h * flat;
}

/** Build the terrain mesh with per-vertex Mars-tone coloring (no textures needed). */
export function buildTerrain(): THREE.Mesh {
  const geo = new THREE.PlaneGeometry(TERRAIN_EXTENT, TERRAIN_EXTENT, RES - 1, RES - 1);
  geo.rotateX(-Math.PI / 2);

  const pos = geo.attributes.position as THREE.BufferAttribute;
  const colors = new Float32Array(pos.count * 3);
  const c1 = new THREE.Color(0.62, 0.36, 0.22); // ochre
  const c2 = new THREE.Color(0.48, 0.26, 0.16); // darker basalt-dust
  const c3 = new THREE.Color(0.72, 0.46, 0.3); // bright dust
  const tmp = new THREE.Color();

  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i);
    const z = pos.getZ(i);
    pos.setY(i, terrainHeight(x, z));

    const n = valueNoise2(x * 0.004, z * 0.004);
    const n2 = valueNoise2(x * 0.02 + 40, z * 0.02 + 17);
    const n3 = valueNoise2(x * 0.09 + 80, z * 0.09 + 51);
    tmp.copy(c2).lerp(c1, n).lerp(c3, n2 * 0.45);
    const shade = 0.92 + 0.16 * n3;
    colors[i * 3] = tmp.r * shade;
    colors[i * 3 + 1] = tmp.g * shade;
    colors[i * 3 + 2] = tmp.b * shade;
  }
  geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));
  geo.computeVertexNormals();

  const mat = new THREE.MeshLambertMaterial({ vertexColors: true });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.receiveShadow = true;
  return mesh;
}
