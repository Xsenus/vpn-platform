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
    ['html', { open: 'never', outputFolder: 'playwright-report/e2e' }]
  ],
  use: {
    baseURL: 'http://127.0.0.1:5293',
    trace: 'retain-on-failure'
  },
  webServer: [
    {
      command: 'node scripts/playwright-webservers.mjs public-web',
      url: 'http://127.0.0.1:5293',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000
    },
    {
      command: 'node scripts/playwright-webservers.mjs cabinet',
      url: 'http://127.0.0.1:5294',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000
    },
    {
      command: 'node scripts/playwright-webservers.mjs admin-panel',
      url: 'http://127.0.0.1:5295',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000
    }
  ],
  projects: [
    {
      name: 'public-web',
      testMatch: /public\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'cabinet',
      testMatch: /cabinet\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://127.0.0.1:5294'
      }
    },
    {
      name: 'admin-panel',
      testMatch: /admin\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://127.0.0.1:5295'
      }
    },
    {
      name: 'all-screens',
      testMatch: /all-screens\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://127.0.0.1:5293'
      }
    },
    {
      name: 'mobile-public',
      testMatch: /public\.spec\.ts/,
      use: {
        ...devices['Pixel 5'],
        baseURL: 'http://127.0.0.1:5293'
      }
    },
    {
      name: 'mobile-cabinet',
      testMatch: /cabinet\.spec\.ts/,
      use: {
        ...devices['Pixel 5'],
        baseURL: 'http://127.0.0.1:5294'
      }
    },
    {
      name: 'mobile-admin',
      testMatch: /admin\.spec\.ts/,
      use: {
        ...devices['Pixel 5'],
        baseURL: 'http://127.0.0.1:5295'
      }
    }
  ]
})
