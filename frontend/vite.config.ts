// ABOUTME: Vite configuration for Vue.js frontend with development proxy
// ABOUTME: Proxies API calls to local Azure Functions during development
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:7071',
        changeOrigin: true,
      }
    }
  }
})