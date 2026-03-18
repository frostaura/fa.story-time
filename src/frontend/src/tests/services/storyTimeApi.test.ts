import { createStoryTimeApi } from '../../services/storyTimeApi'

describe('storyTimeApi', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 200 })))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('builds GET paths for home status and encoded library requests', async () => {
    const api = createStoryTimeApi('https://api.storytime.test/')

    await api.getHomeStatus()
    await api.getLibrary('user with/slash')

    const fetchMock = vi.mocked(fetch)
    expect(fetchMock).toHaveBeenNthCalledWith(1, 'https://api.storytime.test/api/home/status')
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      'https://api.storytime.test/api/library/user%20with%2Fslash',
    )
  })

  it('sends headers and JSON payloads for parent settings flows', async () => {
    const api = createStoryTimeApi('https://api.storytime.test')

    await api.generateStory({ softUserId: 'user-1', mode: 'series' })
    await api.getParentSettings('user-1', 'gate-1')
    await api.updateParentSettings('user-1', {
      gateToken: 'gate-1',
      notificationsEnabled: true,
      analyticsEnabled: false,
    })

    const fetchMock = vi.mocked(fetch)
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      'https://api.storytime.test/api/stories/generate',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ softUserId: 'user-1', mode: 'series' }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      'https://api.storytime.test/api/parent/user-1/settings',
      expect.objectContaining({
        headers: { 'X-StoryTime-Gate-Token': 'gate-1' },
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      'https://api.storytime.test/api/parent/user-1/settings',
      expect.objectContaining({
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          gateToken: 'gate-1',
          notificationsEnabled: true,
          analyticsEnabled: false,
        }),
      }),
    )
  })

  it('uses method-specific routes for favorite, approval, and checkout flows', async () => {
    const api = createStoryTimeApi('')

    await api.setStoryFavorite('story/123', true)
    await api.approveStory('story/123', { softUserId: 'user-123', gateToken: 'gate-1' })
    await api.createCheckoutSession('user/123', { gateToken: 'gate-1', upgradeTier: 'Plus' })
    await api.completeCheckoutSession('user/123', { gateToken: 'gate-1', sessionId: 'sess-1' })

    const fetchMock = vi.mocked(fetch)
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/stories/story%2F123/favorite',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ isFavorite: true }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/stories/story%2F123/approve',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ softUserId: 'user-123', gateToken: 'gate-1' }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      '/api/subscription/user%2F123/checkout/session',
      expect.objectContaining({ method: 'POST' }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      4,
      '/api/subscription/user%2F123/checkout/complete',
      expect.objectContaining({ method: 'POST' }),
    )
  })
})
