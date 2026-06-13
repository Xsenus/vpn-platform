import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: {
    timeout: 7_500
  },
  fullyParallel: false,
  reporter: [
    ['list'],
    ['html', { open: 'never', outputFolder: 'playwright-report/public' }]
  ],
  use: {
    baseURL: 'http://127.0.0.1:5293',
    trace: 'retain-on-failure'
  },
  webServer: {
    command: 'node apps/public-web/node_modules/vite/bin/vite.js apps/public-web --host 127.0.0.1 --port 5293 --strictPort',
    url: 'http://127.0.0.1:5293',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      VITE_API_BASE_URL: 'http://127.0.0.1:19080'
    }
  },
  projects: [
    {
      name: 'public-web',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
})
