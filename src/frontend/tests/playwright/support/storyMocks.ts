import type { Page, Route } from '@playwright/test'

export const jsonHeaders = Object.freeze({
  'Content-Type': 'application/json',
  'Access-Control-Allow-Origin': '*',
})

export const posterLayers = Object.freeze([
  {
    role: 'BACKGROUND',
    speedMultiplier: 0.2,
    dataUri: 'data:image/svg+xml;base64,PHN2Zy8+',
  },
  {
    role: 'FOREGROUND',
    speedMultiplier: 1.0,
    dataUri: 'data:image/svg+xml;base64,PHN2Zy8+',
  },
  {
    role: 'PARTICLES',
    speedMultiplier: 1.3,
    dataUri: 'data:image/svg+xml;base64,PHN2Zy8+',
  },
])

export const homeStatusPayload = Object.freeze({
  quickGenerateVisible: true,
  durationSliderVisible: true,
  durationMinMinutes: 5,
  durationMaxMinutes: 15,
  durationDefaultMinutes: 6,
  defaultChildName: 'Dreamer',
  parentControlsEnabled: true,
  defaultTier: 'Trial',
  oneShotDefaults: {
    arcName: 'Moonlit Harbor',
    companionName: 'Pip the fox',
    setting: 'Floating lantern docks',
    mood: 'Curious and gentle',
    themeTrackId: 'night-chimes',
    narrationStyle: 'calm-storyteller',
  },
})

export const emptyLibraryPayload = Object.freeze({
  recent: [],
  favorites: [],
  kidModeEnabled: false,
})

export const fulfillJson = async (route: Route, status: number, body: unknown) => {
  await route.fulfill({
    status,
    headers: jsonHeaders,
    body: JSON.stringify(body),
  })
}

export const installCommonMockRoutes = async (page: Page) => {
  await page.route('**/api/home/status', async (route) => {
    await fulfillJson(route, 200, homeStatusPayload)
  })
}

export const mockEmptyLibrary = async (page: Page) => {
  await page.route('**/api/library/**', async (route) => {
    await fulfillJson(route, 200, emptyLibraryPayload)
  })
}

export const buildGeneratedStoryPayload = (overrides: Record<string, unknown> = {}) => ({
  storyId: 'playwright-story-1',
  title: 'Ari and the Moonlit Meadow',
  mode: 'series',
  recap: 'Previously: calm winds.',
  scenes: ['Scene 1', 'Scene 2'],
  sceneCount: 2,
  posterLayers,
  approvalRequired: true,
  teaserAudio: 'data:audio/wav;base64,AAA=',
  fullAudioReady: false,
  reducedMotion: false,
  generatedAt: new Date().toISOString(),
  ...overrides,
})

export const mockGeneratedStory = async (page: Page, overrides: Record<string, unknown> = {}) => {
  await page.route('**/api/stories/generate', async (route) => {
    await fulfillJson(route, 200, buildGeneratedStoryPayload(overrides))
  })
}

export const setRangeValue = async (page: Page, label: string, value: string) => {
  await page.getByLabel(label).evaluate((element, nextValue) => {
    const input = element as HTMLInputElement
    const descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')
    descriptor?.set?.call(input, nextValue)
    input.dispatchEvent(new Event('input', { bubbles: true }))
    input.dispatchEvent(new Event('change', { bubbles: true }))
  }, value)
}
