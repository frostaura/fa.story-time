import { expect, test } from '@playwright/test'
import {
  fulfillJson,
  installCommonMockRoutes,
  jsonHeaders,
  mockGeneratedStory,
  posterLayers,
  setRangeValue,
} from './support/storyMocks'

test.beforeEach(async ({ page }) => {
  await installCommonMockRoutes(page)
})

test('UC-001 quick generate renders and completes in browser', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, {
      recent: [],
      favorites: [],
      kidModeEnabled: false,
    })
  })
  await mockGeneratedStory(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByText('Ari and the Moonlit Meadow')).toBeVisible()
  await expect(page.getByLabel('Teaser narration for Ari and the Moonlit Meadow')).toBeVisible()
})

test('UC-002 series continuation keeps a stable seriesId and prior-context recap', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        recent: [],
        favorites: [],
        kidModeEnabled: false,
      }),
    })
  })

  let generateCount = 0
  await page.route('**/api/stories/generate', async (route) => {
    generateCount += 1
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        storyId: `series-story-${generateCount}`,
        title: `Series Episode ${generateCount}`,
        mode: 'series',
        seriesId: 'series-42',
        recap:
          generateCount === 1 ? 'The journey starts quietly.' : 'Previously: The journey starts quietly.',
        scenes: ['Scene 1', 'Scene 2'],
        sceneCount: 2,
        posterLayers,
        approvalRequired: true,
        teaserAudio: 'data:audio/wav;base64,AAA=',
        fullAudioReady: false,
        reducedMotion: false,
        generatedAt: new Date().toISOString(),
      }),
    })
  })

  await page.goto('/')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(page.getByText('Series Episode 1')).toBeVisible()
  await expect(page.getByText('Series Episode 2')).toBeVisible()
  await expect(page.getByText('Previously: The journey starts quietly.')).toBeVisible()
})

test('UC-003 parent approval unlocks full narration playback', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, {
      recent: [],
      favorites: [],
      kidModeEnabled: false,
    })
  })
  await mockGeneratedStory(page, {
    storyId: 'approval-story-1',
    title: 'Approval Story',
    recap: 'A teaser-first story.',
    scenes: ['Scene 1'],
    sceneCount: 1,
  })

  await page.route('**/api/stories/*/approve', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        fullAudioReady: true,
        fullAudio: 'data:audio/wav;base64,BBBB',
      }),
    })
  })

  await page.goto('/')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await page.getByRole('button', { name: 'Approve full narration' }).click()
  await expect(page.getByLabel('Full narration for Approval Story')).toBeVisible()
})

test('UC-004 kid shelf toggle sends kidMode=true and renders curated shelves', async ({ page }) => {
  await page.addInitScript(() => {
    const now = new Date().toISOString()
    localStorage.setItem(
      'storyArtifacts',
      JSON.stringify([
        {
          storyId: 'kid-recent-1',
          title: 'Kid Recent Story',
          mode: 'series',
          seriesId: 'kid-series',
          recap: 'Previously: bedtime calm.',
          scenes: ['Scene 1'],
          sceneCount: 1,
          posterLayers: [],
          approvalRequired: true,
          teaserAudio: 'data:audio/wav;base64,AAA=',
          fullAudioReady: false,
          reducedMotion: false,
          generatedAt: now,
          isFavorite: true,
        },
        {
          storyId: 'kid-favorite-1',
          title: 'Kid Favorite Story',
          mode: 'one-shot',
          recap: 'A favorite calm tale.',
          scenes: ['Scene 1'],
          sceneCount: 1,
          posterLayers: [],
          approvalRequired: true,
          teaserAudio: 'data:audio/wav;base64,AAA=',
          fullAudioReady: false,
          reducedMotion: false,
          generatedAt: now,
          isFavorite: true,
        },
      ]),
    )
  })

  let sawKidModeRequest = false
  await page.route('**/api/library/**', async (route) => {
    sawKidModeRequest = sawKidModeRequest || route.request().url().includes('kidMode=true')
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        recent: [
          {
            storyId: 'kid-recent-1',
            title: 'Kid Recent Story',
            mode: 'series',
            seriesId: 'kid-series',
            isFavorite: true,
            fullAudioReady: false,
            createdAt: new Date().toISOString(),
          },
        ],
        favorites: [
          {
            storyId: 'kid-favorite-1',
            title: 'Kid Favorite Story',
            mode: 'one-shot',
            isFavorite: true,
            fullAudioReady: false,
            createdAt: new Date().toISOString(),
          },
        ],
        kidModeEnabled: true,
      }),
    })
  })

  await page.goto('/')
  await page.getByRole('checkbox', { name: 'Kid Shelf' }).check()
  await expect(page.getByText('Kid Recent Story')).toBeVisible()
  await expect(page.getByText('Kid Favorite Story')).toBeVisible()
  expect(sawKidModeRequest).toBeTruthy()
})

test('UC-005 duration paywall shows upgrade metadata on 402 response', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        recent: [],
        favorites: [],
        kidModeEnabled: false,
      }),
    })
  })

  await page.route('**/api/stories/generate', async (route) => {
    await route.fulfill({
      status: 402,
      headers: jsonHeaders,
      body: JSON.stringify({
        error: 'Tier duration limit reached.',
        paywall: {
          currentTier: 'Trial',
          upgradeTier: 'Premium',
          maxDurationMinutes: 10,
          upgradeUrl: '/subscribe',
          message: 'Upgrade to Premium for longer bedtime stories.',
        },
      }),
    })
  })

  await page.goto('/')
  await setRangeValue(page, 'Duration', '15')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByRole('heading', { name: /unlock longer stories/i })).toBeVisible()
  await expect(page.getByText('Upgrade to Premium for longer bedtime stories.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Confirm Premium upgrade' })).toBeVisible()
})
