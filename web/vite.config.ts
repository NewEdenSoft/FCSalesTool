/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import { tanstackRouter } from '@tanstack/router-plugin/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// When run under Aspire, the API's URL is injected via service discovery env vars.
const apiTarget =
  process.env.API_HTTPS ??
  process.env.API_HTTP ??
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  'http://localhost:5000'

// xfwd: send X-Forwarded-Host/Proto so the API builds email links and the EVE SSO
// redirect URI for the browser's origin (localhost:5173), not its own.
const backendProxy = { target: apiTarget, changeOrigin: true, secure: false, xfwd: true }

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    // Must come before the React plugin.
    tanstackRouter({ target: 'react', autoCodeSplitting: true }),
    react(),
    tailwindcss(),
  ],
  server: {
    port: process.env.PORT ? Number(process.env.PORT) : 5173,
    strictPort: !!process.env.PORT,
    proxy: {
      '/api': backendProxy,
      '/auth': backendProxy,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
