import { expect, test } from '@playwright/test'
import { installCommonMockRoutes, mockEmptyLibrary, mockGeneratedStory } from './support/storyMocks'

const viewports = [
  { name: 'mobile', width: 390, height: 844 },
  { name: 'tablet', width: 820, height: 1180 },
  { name: 'desktop', width: 1280, height: 960 },
] as const

for (const viewport of viewports) {
  test(`responsive quick-generate flow stays usable at ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await installCommonMockRoutes(page)
    await mockEmptyLibrary(page)
    await mockGeneratedStory(page)

    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Quick Generate' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Parent Controls' })).toBeVisible()

    const quickGenerateCard = page.getByTestId('quick-generate-card')
    await expect(quickGenerateCard).toBeVisible()

    const preGenerateOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)
    expect(preGenerateOverflow).toBeTruthy()

    const quickGenerateBounds = await quickGenerateCard.boundingBox()
    expect(quickGenerateBounds).not.toBeNull()
    expect((quickGenerateBounds?.x ?? 0) + (quickGenerateBounds?.width ?? 0)).toBeLessThanOrEqual(
      viewport.width + 1,
    )

    await page.getByRole('button', { name: 'Generate story' }).click()
    await expect(page.getByTestId('recent-story-playwright-story-1')).toContainText('Ari and the Moonlit Meadow')

    const postGenerateOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)
    expect(postGenerateOverflow).toBeTruthy()
  })
}
