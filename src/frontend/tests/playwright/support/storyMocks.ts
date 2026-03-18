import type { Page, Route } from '@playwright/test'

export const jsonHeaders = Object.freeze({
  'Content-Type': 'application/json',
  'Access-Control-Allow-Origin': '*',
})

const createPosterLayerDataUri = (fill: string, opacity: number, accentFill: string): string => {
  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 160 96">
      <rect width="160" height="96" rx="12" fill="${fill}" fill-opacity="${opacity}" />
      <circle cx="120" cy="22" r="10" fill="${accentFill}" fill-opacity="0.72" />
      <path d="M8 86C28 54 46 42 72 42C96 42 118 56 152 82V96H8Z" fill="${accentFill}" fill-opacity="0.34" />
    </svg>
  `.trim()

  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`
}

export const posterLayers = Object.freeze([
  {
    role: 'BACKGROUND',
    speedMultiplier: 0.2,
    dataUri: createPosterLayerDataUri('#172554', 1, '#67e8f9'),
  },
  {
    role: 'MIDGROUND_1',
    speedMultiplier: 0.55,
    dataUri: createPosterLayerDataUri('#1d4ed8', 0.86, '#fef08a'),
  },
  {
    role: 'FOREGROUND',
    speedMultiplier: 1.0,
    dataUri: createPosterLayerDataUri('#7c3aed', 0.72, '#fca5a5'),
  },
  {
    role: 'PARTICLES',
    speedMultiplier: 1.3,
    dataUri: createPosterLayerDataUri('#ec4899', 0.42, '#ffffff'),
  },
])

export const homeStatusPayload = Object.freeze({
  quickGenerateVisible: true,
  durationSliderVisible: true,
  durationMinMinutes: 5,
  durationMaxMinutes: 15,
  durationDefaultMinutes: 6,
  defaultChildName: 'Child',
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
  kidShelfEnabled: false,
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
