import { defineConfig, devices } from '@playwright/test'

const port = Number.parseInt(process.env.PLAYWRIGHT_WEB_PORT ?? '4184', 10)
const webHost = process.env.PLAYWRIGHT_WEB_HOST ?? '127.0.0.1'
const backendHost = process.env.PLAYWRIGHT_API_HOST ?? '127.0.0.1'
const backendPort = Number.parseInt(process.env.PLAYWRIGHT_API_PORT ?? '19082', 10)
const webhookSecret = process.env.PLAYWRIGHT_WEBHOOK_SECRET ?? 'playwright-webhook-secret'
const reuseExistingServer = process.env.PLAYWRIGHT_REUSE_EXISTING_SERVER === 'true'

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  use: {
    baseURL: `http://${webHost}:${port}`,
    trace: 'on-first-retry',
  },
  webServer: [
    {
      command:
        `ASPNETCORE_URLS=http://${backendHost}:${backendPort} ` +
        `ASPNETCORE_ENVIRONMENT=Development ` +
        `StoryTime__Generation__AiOrchestration__Enabled=false ` +
        `StoryTime__Checkout__WebhookSharedSecret=${webhookSecret} ` +
        `StoryTime__Cors__AllowedOrigins__0=http://127.0.0.1:${port} ` +
        `StoryTime__Cors__AllowedOrigins__1=http://localhost:${port} ` +
        `dotnet run --no-launch-profile --project ../backend/StoryTime.Api`,
      port: backendPort,
      reuseExistingServer,
      timeout: 120_000,
    },
    {
      command: `npm run preview -- --host 0.0.0.0 --port ${port}`,
      port,
      reuseExistingServer,
      timeout: 120_000,
    },
  ],
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
