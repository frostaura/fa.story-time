import { expect, test, type Page } from '@playwright/test'
import { setRangeValue } from './support/storyMocks'

const backendHost = process.env.PLAYWRIGHT_API_HOST ?? '127.0.0.1'
const backendPort = Number.parseInt(process.env.PLAYWRIGHT_API_PORT ?? '18080', 10)
const backendBaseUrl = process.env.PLAYWRIGHT_API_BASE_URL ?? `http://${backendHost}:${backendPort}`

const configureLiveBackend = async (page: Page, softUserId: string) => {
  await page.addInitScript(
    ({ apiBaseUrl, userId }) => {
      localStorage.clear()
      const storyTimeWindow = window as Window & { __STORYTIME_API_BASE_URL__?: string }
      storyTimeWindow.__STORYTIME_API_BASE_URL__ = apiBaseUrl
      localStorage.setItem('softUserId', userId)
    },
    { apiBaseUrl: backendBaseUrl, userId: softUserId },
  )
}

test('UC-001 live browser flow generates, approves, and favorites through the real backend', async ({
  page,
}, testInfo) => {
  await configureLiveBackend(page, `playwright-live-generate-${testInfo.project.name}-${Date.now()}`)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(page.getByRole('button', { name: 'Approve full narration' })).toBeVisible()
  await page.getByRole('button', { name: 'Approve full narration' }).click()
  await expect(page.getByLabel(/^Full narration for /)).toBeVisible()

  await page.getByRole('button', { name: 'Favorite' }).click()
  await expect(page.getByRole('button', { name: 'Unfavorite' }).first()).toBeVisible()
})

test('UC-005 live browser paywall shows upgrade metadata for trial users', async ({ page }, testInfo) => {
  await configureLiveBackend(page, `playwright-live-paywall-${testInfo.project.name}-${Date.now()}`)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()

  await setRangeValue(page, 'Duration', '15')
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(page.getByRole('heading', { name: /unlock longer stories/i })).toBeVisible()
  await expect(page.getByText('Upgrade to Premium for longer bedtime stories.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Confirm Premium upgrade' })).toBeVisible()
})
