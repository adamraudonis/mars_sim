/**
 * Generates the site favicon (brand mark: Mars-orange diamond on a dark rounded square)
 * as SVG plus real PNGs (64px favicon, 180px apple-touch-icon) via a minimal PNG encoder —
 * no image libraries needed. Usage: bun run scripts/make-favicon.ts
 */
import { deflateSync } from 'node:zlib';
import { writeFileSync } from 'node:fs';
import { join } from 'node:path';

const OUT = join(import.meta.dir, '..', 'public');

// ---------- SVG ----------
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
  <rect width="64" height="64" rx="12" fill="#0b0e14"/>
  <rect x="19.5" y="19.5" width="25" height="25" rx="3" fill="#ff7a3d" transform="rotate(45 32 32)"/>
</svg>
`;
writeFileSync(join(OUT, 'favicon.svg'), svg);

// ---------- PNG encoder ----------
const crcTable = new Uint32Array(256);
for (let n = 0; n < 256; n++) {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  crcTable[n] = c >>> 0;
}
function crc32(buf: Uint8Array): number {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}
function chunk(type: string, data: Uint8Array): Uint8Array {
  const out = new Uint8Array(12 + data.length);
  const dv = new DataView(out.buffer);
  dv.setUint32(0, data.length);
  for (let i = 0; i < 4; i++) out[4 + i] = type.charCodeAt(i);
  out.set(data, 8);
  dv.setUint32(8 + data.length, crc32(out.subarray(4, 8 + data.length)));
  return out;
}
function encodePng(size: number, rgba: Uint8Array): Uint8Array {
  const sig = new Uint8Array([137, 80, 78, 71, 13, 10, 26, 10]);
  const ihdr = new Uint8Array(13);
  const dv = new DataView(ihdr.buffer);
  dv.setUint32(0, size);
  dv.setUint32(4, size);
  ihdr[8] = 8; // bit depth
  ihdr[9] = 6; // RGBA
  // scanlines with filter byte 0
  const raw = new Uint8Array(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) {
    raw[y * (size * 4 + 1)] = 0;
    raw.set(rgba.subarray(y * size * 4, (y + 1) * size * 4), y * (size * 4 + 1) + 1);
  }
  const idat = new Uint8Array(deflateSync(raw));
  const parts = [sig, chunk('IHDR', ihdr), chunk('IDAT', idat), chunk('IEND', new Uint8Array(0))];
  const total = parts.reduce((n, p) => n + p.length, 0);
  const out = new Uint8Array(total);
  let off = 0;
  for (const p of parts) {
    out.set(p, off);
    off += p.length;
  }
  return out;
}

// ---------- Rasterize the mark (3x3 supersampled) ----------
function render(size: number): Uint8Array {
  const bg = [11, 14, 20];
  const orange = [255, 122, 61];
  const radius = size * (12 / 64);
  const half = size * (17.7 / 64); // diamond half-diagonal
  const rgba = new Uint8Array(size * size * 4);

  const inRoundedRect = (x: number, y: number): boolean => {
    const rx = Math.max(0, Math.max(radius - x, x - (size - radius)));
    const ry = Math.max(0, Math.max(radius - y, y - (size - radius)));
    return rx * rx + ry * ry <= radius * radius;
  };

  for (let y = 0; y < size; y++)
    for (let x = 0; x < size; x++) {
      let cover = 0;
      let diamond = 0;
      for (let sy = 0; sy < 3; sy++)
        for (let sx = 0; sx < 3; sx++) {
          const px = x + (sx + 0.5) / 3;
          const py = y + (sy + 0.5) / 3;
          if (!inRoundedRect(px, py)) continue;
          cover++;
          if (Math.abs(px - size / 2) + Math.abs(py - size / 2) <= half) diamond++;
        }
      const i = (y * size + x) * 4;
      const t = diamond / 9;
      rgba[i] = bg[0] + (orange[0] - bg[0]) * t;
      rgba[i + 1] = bg[1] + (orange[1] - bg[1]) * t;
      rgba[i + 2] = bg[2] + (orange[2] - bg[2]) * t;
      rgba[i + 3] = Math.round((cover / 9) * 255);
    }
  return rgba;
}

for (const size of [64, 180]) {
  writeFileSync(join(OUT, `favicon-${size}.png`), encodePng(size, render(size)));
}
console.log('wrote favicon.svg, favicon-64.png, favicon-180.png');
