import { expect, test } from '@playwright/test'
import {
  emptyLibraryPayload,
  fulfillJson,
  installCommonMockRoutes,
  jsonHeaders,
  mockGeneratedStory,
  posterLayers,
  setRangeValue,
} from './support/storyMocks'
import { browserStorageKeys } from './support/runtimeStorage'

const webPort = Number.parseInt(process.env.PLAYWRIGHT_WEB_PORT ?? '4184', 10)
const localhostBaseUrl = `http://localhost:${webPort}/`

test.beforeEach(async ({ page }) => {
  await installCommonMockRoutes(page)
})

test('UC-001 quick generate renders and completes in browser', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
      await fulfillJson(route, 200, {
        recent: [],
        favorites: [],
        kidShelfEnabled: false,
      })
  })
  await mockGeneratedStory(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByTestId('recent-story-playwright-story-1')).toContainText('Ari and the Moonlit Meadow')
  await expect(page.getByText('Preview clip').first()).toBeVisible()
  await expect(page.getByLabel('Teaser narration for Ari and the Moonlit Meadow')).toBeVisible()
})

test('QA teardown home load stays calm when library sync fails', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await route.fulfill({
      status: 400,
      headers: jsonHeaders,
      body: JSON.stringify({ error: 'softUserId is required.' }),
    })
  })

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
  await expect(page.getByTestId('library-sync-banner')).toBeVisible()
  await expect(page.getByText('Saved stories are taking a moment to load.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
  await expect(page.getByText(/Loading library failed with status/)).toHaveCount(0)
})

test('UC-002 series continuation keeps a stable seriesId and prior-context recap', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        recent: [],
        favorites: [],
        kidShelfEnabled: false,
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

  await expect(page.getByTestId('recent-story-series-story-1')).toContainText('Series Episode 1')
  await expect(page.getByTestId('recent-story-series-story-2')).toContainText('Series Episode 2')
  await expect(page.getByText('Previously: The journey starts quietly.')).toBeVisible()
})

test('UC-003 parent approval stays locked until parent verification completes', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
      await fulfillJson(route, 200, {
        recent: [],
        favorites: [],
        kidShelfEnabled: false,
      })
  })
  await mockGeneratedStory(page, {
    storyId: 'approval-story-1',
    title: 'Approval Story',
    recap: 'A teaser-first story.',
    scenes: ['Scene 1'],
    sceneCount: 1,
  })

  await page.goto('/')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await expect(page.getByRole('button', { name: 'Approve full narration' })).toBeDisabled()
  await expect(page.getByTestId('recent-story-approval-hint-approval-story-1')).toHaveText(
    'Passkeys only work in the localhost version of StoryTime on this device.',
  )
})

test('QA teardown generation cooldown uses plain-language recovery copy', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, emptyLibraryPayload)
  })

  await page.route('**/api/stories/generate', async (route) => {
    await route.fulfill({
      status: 429,
      headers: jsonHeaders,
      body: JSON.stringify({ error: 'Cooldown active.' }),
    })
  })

  await page.goto('/')
  await page.getByRole('button', { name: 'Generate story' }).click()

  await expect(
    page.getByText('StoryTime needs a short pause before creating another story. Please wait a moment, then try again.'),
  ).toBeVisible()
})

test('QA teardown one-shot mode keeps optional details hidden until expanded', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, emptyLibraryPayload)
  })

  await page.goto('/')
  await page.getByLabel('Mode').selectOption('one-shot')

  await expect(page.getByRole('button', { name: 'Generate one-shot' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Add optional details' })).toBeVisible()
  await expect(page.getByLabel('Story arc')).toHaveCount(0)

  await page.getByRole('button', { name: 'Add optional details' }).click()
  await expect(page.getByLabel('Story arc')).toBeVisible()
  await expect(page.getByLabel('Narration style')).toBeVisible()
})

test('QA teardown parent verification shows localhost guidance on unsupported hosts', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, emptyLibraryPayload)
  })

  await page.goto('/')

  await expect(page.getByRole('button', { name: 'Verify parent with passkey' })).toBeDisabled()
  await expect(page.getByText('Passkeys only work in the localhost version of StoryTime on this device.')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Open localhost version' })).toHaveAttribute('href', localhostBaseUrl)
})

test('UC-004 kid shelf renders curated shelves from parent-managed backend state', async ({ page }) => {
  await page.addInitScript(({ storyArtifactsStorageKey, seededPosterLayers }) => {
    const now = new Date().toISOString()
    localStorage.setItem(
      storyArtifactsStorageKey,
      JSON.stringify([
        {
          storyId: 'kid-recent-1',
          title: 'Kid Recent Story',
          mode: 'series',
          seriesId: 'kid-series',
          recap: 'Previously: bedtime calm.',
          scenes: ['Scene 1'],
          sceneCount: 1,
          posterLayers: seededPosterLayers,
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
          posterLayers: seededPosterLayers,
          approvalRequired: true,
          teaserAudio: 'data:audio/wav;base64,AAA=',
          fullAudioReady: false,
          reducedMotion: false,
          generatedAt: now,
          isFavorite: true,
        },
      ]),
    )
  }, {
    storyArtifactsStorageKey: browserStorageKeys.storyArtifacts,
    seededPosterLayers: posterLayers,
  })

  await page.route('**/api/library/**', async (route) => {
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
        kidShelfEnabled: true,
      }),
    })
  })

  await page.goto('/')
  await expect(page.getByText('Kid Shelf')).toBeVisible()
  await expect(page.getByTestId('recent-story-kid-recent-1').getByText('Kid Recent Story')).toBeVisible()
  await expect(page.getByTestId('favorite-story-kid-favorite-1').getByText('Kid Favorite Story')).toBeVisible()
})

test('UC-005 duration paywall shows upgrade metadata on 402 response', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        recent: [],
        favorites: [],
        kidShelfEnabled: false,
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
  await expect(page.getByRole('button', { name: 'Verify parent to continue upgrade' })).toBeVisible()
  await page.getByRole('button', { name: 'Verify parent to continue upgrade' }).click()
  await expect(page.getByTestId('paywall-feedback')).toContainText(
    'Passkeys only work in the localhost version of StoryTime on this device.',
  )
  await expect(page.getByTestId('inline-error')).toHaveCount(0)
})

test('QA teardown favorite failures stay on the story card that triggered them', async ({ page }) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, emptyLibraryPayload)
  })
  await mockGeneratedStory(page, {
    storyId: 'favorite-failure-story',
    title: 'Favorite Failure Story',
  })
  await page.route('**/api/stories/*/favorite', async (route) => {
    await route.fulfill({
      status: 500,
      headers: jsonHeaders,
      body: JSON.stringify({ error: 'Favorite update failed.' }),
    })
  })

  await page.goto('/')
  await page.getByRole('button', { name: 'Generate story' }).click()
  await page.getByRole('button', { name: 'Favorite' }).click()

  await expect(page.getByTestId('recent-story-feedback-favorite-failure-story')).toContainText(
    'Favorite update failed with status 500',
  )
  await expect(page.getByTestId('inline-error')).toHaveCount(0)
})
