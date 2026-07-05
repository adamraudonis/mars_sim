import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// base './' so the built site works from any static-host subpath.
export default defineConfig({
  base: './',
  plugins: [react()],
  server: {
    // Preview harnesses assign a port via env; fall back to vite's default.
    port: Number(process.env.PORT) || 5173,
    strictPort: false,
  },
  build: {
    target: 'es2022',
    chunkSizeWarningLimit: 1200,
  },
});
