import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { adminManualChunk } from './src/bundle-chunks'

export default defineConfig({
  plugins: [react()],
  build: {
    rolldownOptions: {
      output: {
        manualChunks: adminManualChunk
      }
    }
  }
})
