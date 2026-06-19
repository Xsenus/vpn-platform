import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  timeout: 120_000,
  expect: {
    timeout: 15_000
  },
  fullyParallel: false,
  reporter: [['list']],
  use: {
    ...devices['Desktop Chrome'],
    trace: 'off',
    screenshot: 'off',
    video: 'off'
  },
  projects: [
    {
      name: 'admin-vps-smoke',
      testMatch: /admin-vps-smoke\.spec\.ts/
    }
  ]
})
