import { expect, test, type Page } from '@playwright/test'
import { setRangeValue } from './support/storyMocks'
import { browserStorageKeys } from './support/runtimeStorage'

const webPort = Number.parseInt(process.env.PLAYWRIGHT_WEB_PORT ?? '4184', 10)
const backendHost = process.env.PLAYWRIGHT_API_HOST ?? '127.0.0.1'
const backendPort = Number.parseInt(process.env.PLAYWRIGHT_API_PORT ?? '19082', 10)
const backendBaseUrl = process.env.PLAYWRIGHT_API_BASE_URL ?? `http://${backendHost}:${backendPort}`
const localhostBaseUrl = process.env.PLAYWRIGHT_LOCALHOST_BASE_URL ?? `http://localhost:${webPort}`
const webhookSecret = process.env.PLAYWRIGHT_WEBHOOK_SECRET ?? 'playwright-webhook-secret'
const liveBackendExpectationTimeout = 15_000
const liveBackendBootstrapMarker = '__storytime_live_backend_configured__'

const configureLiveBackend = async (page: Page, softUserId: string) => {
  await page.addInitScript(
    ({ apiBaseUrl, userId, liveBackendBootstrapMarker, softUserIdStorageKey }) => {
      if (!sessionStorage.getItem(liveBackendBootstrapMarker)) {
        localStorage.clear()
        sessionStorage.setItem(liveBackendBootstrapMarker, 'true')
      }
      const storyTimeWindow = window as Window & { __STORYTIME_API_BASE_URL__?: string }
      storyTimeWindow.__STORYTIME_API_BASE_URL__ = apiBaseUrl
      localStorage.setItem(softUserIdStorageKey, userId)
    },
    {
      apiBaseUrl: backendBaseUrl,
      userId: softUserId,
      liveBackendBootstrapMarker,
      softUserIdStorageKey: browserStorageKeys.softUserId,
    },
  )
}

test.describe.configure({ mode: 'serial' })

const waitForLiveAppReady = async (page: Page) => {
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
  await expect(page.getByTestId('tier-badge')).toHaveText(/trial/i, {
    timeout: liveBackendExpectationTimeout,
  })
}

const resetEntitlement = async (page: Page, softUserId: string, tier: 'Plus' | 'Premium' = 'Premium') => {
  const response = await page.request.post(`${backendBaseUrl}/api/subscription/webhook`, {
    headers: {
      'Content-Type': 'application/json',
      'X-StoryTime-Webhook-Secret': webhookSecret,
    },
    data: {
      softUserId,
      tier,
      resetCooldown: true,
    },
  })

  expect(response.ok()).toBeTruthy()
}

const installVirtualAuthenticator = async (page: Page) => {
  const session = await page.context().newCDPSession(page)
  await session.send('WebAuthn.enable')
  const { authenticatorId } = await session.send('WebAuthn.addVirtualAuthenticator', {
    options: {
      protocol: 'ctap2',
      transport: 'internal',
      hasResidentKey: false,
      hasUserVerification: true,
      isUserVerified: true,
      automaticPresenceSimulation: true,
    },
  })

  return { authenticatorId, session }
}

test('UC-001 live browser flow generates, approves, and favorites through the real backend', async ({
  page,
}, testInfo) => {
  await configureLiveBackend(page, `playwright-live-generate-${testInfo.project.name}-${Date.now()}`)
  const authenticator = await installVirtualAuthenticator(page)

  await page.goto(localhostBaseUrl)
  await waitForLiveAppReady(page)
  await page.getByRole('button', { name: 'Verify parent with passkey' }).click()
  await expect(page.getByTestId('notifications-toggle')).toBeEnabled({
    timeout: liveBackendExpectationTimeout,
  })
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(page.getByRole('button', { name: 'Approve full narration' })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await page.getByRole('button', { name: 'Approve full narration' }).click()
  await expect(page.getByLabel(/^Full narration for /)).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })

  await page.getByRole('button', { name: 'Favorite' }).click()
  await expect(page.getByRole('button', { name: 'Unfavorite' }).first()).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })

  await authenticator.session.send('WebAuthn.removeVirtualAuthenticator', {
    authenticatorId: authenticator.authenticatorId,
  })
})

test('UC-003 live parent verification unlocks parent-managed settings on localhost', async ({ page }, testInfo) => {
  await configureLiveBackend(page, `playwright-live-parent-${testInfo.project.name}-${Date.now()}`)
  const authenticator = await installVirtualAuthenticator(page)

  await page.goto(localhostBaseUrl)
  await waitForLiveAppReady(page)
  await expect(page.getByRole('button', { name: 'Verify parent with passkey' })).toBeEnabled()
  await expect(
    page.getByText(/Use the same browser profile that created your parent passkey\./),
  ).toBeVisible()

  await page.getByRole('button', { name: 'Verify parent with passkey' }).click()
  await expect(page.getByTestId('notifications-toggle')).toBeEnabled({
    timeout: liveBackendExpectationTimeout,
  })

  await page.getByTestId('notifications-toggle').check()
  await expect(page.getByTestId('notifications-toggle')).toBeChecked()
  await page.getByTestId('analytics-toggle').check()
  await expect(page.getByTestId('analytics-toggle')).toBeChecked()
  await page.getByTestId('kid-shelf-parent-toggle').click()

  await expect(page.getByTestId('kid-shelf-indicator')).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toHaveCount(0)

  await authenticator.session.send('WebAuthn.removeVirtualAuthenticator', {
    authenticatorId: authenticator.authenticatorId,
  })
})

test('UC-005 live browser paywall shows upgrade metadata for trial users', async ({ page }, testInfo) => {
  await configureLiveBackend(page, `playwright-live-paywall-${testInfo.project.name}-${Date.now()}`)

  await page.goto('/')
  await waitForLiveAppReady(page)

  await setRangeValue(page, 'Duration', '15')
  await expect(page.getByText('Duration (15 min)')).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(page.getByRole('heading', { name: /unlock longer stories/i })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await expect(page.getByText('Upgrade to Premium for longer bedtime stories.')).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await expect(page.getByRole('button', { name: 'Verify parent to continue upgrade' })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
})

test('UC-002 live browser continuation keeps a stable seriesId after a reset-safe entitlement refresh', async ({
  page,
}, testInfo) => {
  const softUserId = `playwright-live-series-${testInfo.project.name}-${Date.now()}`
  await configureLiveBackend(page, softUserId)

  await page.goto('/')
  await waitForLiveAppReady(page)
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByRole('button', { name: 'Approve full narration' })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })

  await page.waitForFunction((storyArtifactsStorageKey) => {
    const raw = localStorage.getItem(storyArtifactsStorageKey)
    if (!raw) {
      return false
    }

    const stories = JSON.parse(raw) as Array<{ seriesId?: string }>
    return typeof stories[0]?.seriesId === 'string' && stories[0].seriesId.length > 0
  }, browserStorageKeys.storyArtifacts)
  const firstSeriesId = await page.evaluate((storyArtifactsStorageKey) => {
    const raw = localStorage.getItem(storyArtifactsStorageKey)
    const stories = raw ? (JSON.parse(raw) as Array<{ seriesId?: string }>) : []
    return stories[0]?.seriesId ?? ''
  }, browserStorageKeys.storyArtifacts)
  expect(firstSeriesId).not.toEqual('')

  await resetEntitlement(page, softUserId)
  await page.getByLabel('Series continuation').selectOption(firstSeriesId)
  await page.getByTestId('generate-story-button').click()

  await page.waitForFunction((storyArtifactsStorageKey) => {
    const raw = localStorage.getItem(storyArtifactsStorageKey)
    if (!raw) {
      return false
    }

    const stories = JSON.parse(raw) as Array<{ recap?: string; seriesId?: string }>
    return stories.length >= 2 && typeof stories[0]?.recap === 'string' && stories[0].recap.includes('Previously:')
  }, browserStorageKeys.storyArtifacts)

  const latestStories = await page.evaluate((storyArtifactsStorageKey) => {
    const raw = localStorage.getItem(storyArtifactsStorageKey)
    return raw ? (JSON.parse(raw) as Array<{ recap?: string; seriesId?: string }>) : []
  }, browserStorageKeys.storyArtifacts)
  expect(latestStories[0]?.seriesId).toEqual(firstSeriesId)
  expect(latestStories[1]?.seriesId).toEqual(firstSeriesId)
  expect(latestStories[0]?.recap ?? '').toContain('Previously:')
})

test('UC-005 live browser completes a gated Premium checkout and unlocks long-form generation', async ({
  page,
}, testInfo) => {
  await configureLiveBackend(page, `playwright-live-checkout-${testInfo.project.name}-${Date.now()}`)
  const authenticator = await installVirtualAuthenticator(page)

  await page.goto(localhostBaseUrl)
  await waitForLiveAppReady(page)
  await page.getByRole('button', { name: 'Verify parent with passkey' }).click()
  await expect(page.getByTestId('notifications-toggle')).toBeEnabled({
    timeout: liveBackendExpectationTimeout,
  })

  await setRangeValue(page, 'Duration', '15')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByRole('button', { name: 'Confirm Premium upgrade' })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })
  await page.getByRole('button', { name: 'Confirm Premium upgrade' }).click()

  await expect(page.getByTestId('tier-badge')).toHaveText(/premium/i, {
    timeout: liveBackendExpectationTimeout,
  })
  await expect(page.getByTestId('upgrade-paywall')).toHaveCount(0)

  await setRangeValue(page, 'Duration', '15')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByRole('button', { name: 'Approve full narration' })).toBeVisible({
    timeout: liveBackendExpectationTimeout,
  })

  await authenticator.session.send('WebAuthn.removeVirtualAuthenticator', {
    authenticatorId: authenticator.authenticatorId,
  })
})
