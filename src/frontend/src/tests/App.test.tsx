import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'
import { runtimeConfig } from '../config/runtime'

const installWebAuthnMock = (
  overrides?: Partial<{
    create: () => Promise<unknown>
    get: () => Promise<unknown>
  }>,
) => {
  const credentialId = 'Y3JlZGVudGlhbC0x'

  class MockPublicKeyCredential {
    id: string
    response: AuthenticatorAttestationResponse | AuthenticatorAssertionResponse

    constructor(
      id: string,
      response: AuthenticatorAttestationResponse | AuthenticatorAssertionResponse,
    ) {
      this.id = id
      this.response = response
    }
  }

  Object.defineProperty(window, 'PublicKeyCredential', {
    value: MockPublicKeyCredential,
    configurable: true,
    writable: true,
  })
  Object.defineProperty(globalThis, 'PublicKeyCredential', {
    value: MockPublicKeyCredential,
    configurable: true,
    writable: true,
  })

  Object.defineProperty(navigator, 'credentials', {
    configurable: true,
    value: {
      create:
        overrides?.create ??
        (async () =>
        new MockPublicKeyCredential(credentialId, {
          getPublicKey: () => new Uint8Array([1, 2, 3]).buffer,
        } as unknown as AuthenticatorAttestationResponse)),
      get:
        overrides?.get ??
        (async () =>
        new MockPublicKeyCredential(credentialId, {
          clientDataJSON: new TextEncoder().encode('mock-client-data').buffer,
          authenticatorData: new Uint8Array([1, 2, 3, 4]).buffer,
          signature: new Uint8Array([5, 6, 7, 8]).buffer,
        } as unknown as AuthenticatorAssertionResponse)),
    },
  })
}

describe('App', () => {
  beforeEach(() => {
    window.localStorage.clear()
    Element.prototype.scrollIntoView = vi.fn()
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
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
              }),
              { status: 200, headers: { 'Content-Type': 'application/json' } },
            )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [],
              favorites: [],
              kidShelfEnabled: false,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate')) {
          const payload =
            typeof init?.body === 'string'
              ? (JSON.parse(init.body) as { durationMinutes?: number; reducedMotion?: boolean })
              : null
          if ((payload?.durationMinutes ?? 0) > 10) {
            return new Response(
              JSON.stringify({
                error: 'Upgrade is required for this story length.',
                paywall: {
                  currentTier: 'Trial',
                  upgradeTier: 'Premium',
                  maxDurationMinutes: 10,
                  upgradeUrl: '/subscribe',
                  message: 'Upgrade to Premium for longer bedtime stories.',
                },
              }),
              { status: 402, headers: { 'Content-Type': 'application/json' } },
            )
          }

          return new Response(
            JSON.stringify({
              storyId: 'story-123',
              title: 'Ari and the Moonlit Meadow',
              mode: 'series',
              recap: 'Previously: calm winds.',
              scenes: ['Scene 1', 'Scene 2'],
              sceneCount: 2,
              posterLayers: [
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
              ],
              approvalRequired: true,
              teaserAudio: 'data:audio/wav;base64,AAA=',
              fullAudio: 'data:audio/wav;base64,BBB=',
              fullAudioReady: false,
              reducedMotion: payload?.reducedMotion ?? false,
              generatedAt: new Date().toISOString(),
              storyBible: {
                arcName: 'Moonlit Meadow',
                arcEpisodeNumber: 1,
                arcObjective: 'Find calm',
                previousEpisodeSummary: '',
                audioAnchorMetadata: {
                  themeTrackId: 'soft-piano',
                  narrationStyle: 'warm-whisper',
                },
              },
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/approve')) {
          return new Response(
            JSON.stringify({
              fullAudioReady: true,
              fullAudio: 'data:audio/wav;base64,BBB=',
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url.includes('/favorite') || url.includes('/gate/register') || url.includes('/gate/')) {
          return new Response('{}', {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          })
        }

        if (url.includes('/settings') && !init?.method) {
          return new Response(
            JSON.stringify({ notificationsEnabled: false, analyticsEnabled: false, kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/settings') && init?.method === 'PUT') {
          return new Response(
            JSON.stringify({ notificationsEnabled: true, analyticsEnabled: false, kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders quick generate with duration slider from API config', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    expect(screen.getByLabelText('Duration')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Generate story' })).toBeInTheDocument()
  })

  it('displays subscription tier badge in the header', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByTestId('tier-badge')).toBeInTheDocument()
    })

    expect(screen.getByTestId('tier-badge')).toHaveTextContent('Trial')
  })

  it('displays series progress badge on generated series stories', async () => {
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    const progressBadge = screen.getByText('Moonlit Meadow · Episode 1')
    expect(progressBadge).toBeInTheDocument()
  })

  it('matches the quick generate visual baseline', async () => {
    const view = render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    expect(view.asFragment()).toMatchSnapshot()
  })

  it('matches generated story visual baseline with poster layers', async () => {
    const view = render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    expect(view.asFragment()).toMatchSnapshot()
  })

  it('enforces poster layer motion budget thresholds when reduced motion is off', async () => {
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    const layers = Array.from(document.querySelectorAll<HTMLDivElement>('.poster-layer'))
    expect(layers).toHaveLength(3)

    const durations = layers.map((layer) => Number.parseFloat(layer.style.animationDuration))
    expect(durations.some((value) => value > 0)).toBe(true)
    expect(durations.every((value) => Number.isFinite(value) && value >= 7 && value <= 120)).toBe(true)
  })

  it('disables poster parallax animation when reduced motion is on', async () => {
    render(<App />)

    await userEvent.click(await screen.findByLabelText('Reduced motion'))
    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    const layers = Array.from(document.querySelectorAll<HTMLDivElement>('.poster-layer'))
    expect(layers).toHaveLength(3)
    expect(layers.every((layer) => layer.style.animationDuration === '0s')).toBe(true)
  })

  it('shows parent controls and keeps toggles locked until unlocked', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Parent Controls' })).toBeInTheDocument()
    })

    expect(screen.getByLabelText('Notifications enabled')).toBeDisabled()
    expect(screen.getByLabelText('Analytics enabled')).toBeDisabled()
    expect(screen.getByText('Use the same browser profile that created your parent passkey. StoryTime will unlock the rest of this card as soon as verification finishes.')).toBeInTheDocument()
    expect(screen.getByText('Saves a bedtime reminder preference only. Push delivery is not enabled in this build.')).toBeInTheDocument()
    expect(screen.getByText('Stores consent for future diagnostics only. No external analytics provider is enabled by default.')).toBeInTheDocument()
  })

  it('generates a story and allows favoriting', async () => {
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))

    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

    expect(screen.getAllByRole('button', { name: 'Unfavorite' }).length).toBeGreaterThan(0)
  })

  it('keeps one-shot optional details progressive until expanded', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    await userEvent.selectOptions(screen.getByLabelText('Mode'), 'one-shot')

    expect(screen.getByRole('button', { name: 'Generate one-shot' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add optional details' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Story arc')).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Add optional details' }))

    expect(screen.getByLabelText('Story arc')).toBeInTheDocument()
    expect(screen.getByLabelText('Companion')).toBeInTheDocument()
    expect(screen.getByLabelText('Setting')).toBeInTheDocument()
    expect(screen.getByLabelText('Mood')).toBeInTheDocument()
    expect(screen.getByLabelText('Theme track')).toBeInTheDocument()
    expect(screen.getByLabelText('Narration style')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Moonlit Harbor')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Pip the fox')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Floating lantern docks')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Curious and gentle')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('night-chimes')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('calm-storyteller')).toBeInTheDocument()
    expect(screen.getByText('Optional details are open below.')).toBeInTheDocument()
  })

  it('starts parent verification from the paywall before requesting secure checkout', async () => {
    installWebAuthnMock()
    let checkoutRequestBody: Record<string, unknown> | null = null

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({ recent: [], favorites: [], kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({
              error: 'Upgrade is required for this story length.',
              paywall: {
                currentTier: 'Trial',
                upgradeTier: 'Premium',
                maxDurationMinutes: 10,
                upgradeUrl: '/subscribe',
                message: 'Upgrade to Premium for longer bedtime stories.',
              },
            }),
            { status: 402, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/register') && init?.method === 'POST') {
          return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
        }

        if (url.includes('/gate/challenge') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({
              challengeId: 'challenge-1',
              challenge: 'Y2hhbGxlbmdlLTE',
              rpId: 'localhost',
              expiresAt: new Date().toISOString(),
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/verify') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({ gateToken: 'gate-verified' }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/settings') && !init?.method) {
          return new Response(
            JSON.stringify({ notificationsEnabled: false, analyticsEnabled: false, kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/checkout/session') && init?.method === 'POST') {
          checkoutRequestBody =
            typeof init.body === 'string' ? (JSON.parse(init.body) as Record<string, unknown>) : null
          return new Response(
            JSON.stringify({
              sessionId: 'sess-1',
              checkoutUrl: 'mailto:storybook@example.com',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    fireEvent.change(screen.getByLabelText('Duration'), { target: { value: '15' } })
    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Verify parent to continue upgrade' })).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Verify parent to continue upgrade' }))
    await waitFor(() => {
      expect(checkoutRequestBody).toMatchObject({
        gateToken: 'gate-verified',
        upgradeTier: 'Premium',
      })
    })
    expect(screen.getByTestId('paywall-feedback')).toHaveTextContent(
      'Unable to complete upgrade checkout',
    )
    expect(screen.getByText('Parent controls are unlocked on this device.')).toBeInTheDocument()
    expect(screen.queryByTestId('inline-error')).not.toBeInTheDocument()
  })

  it('keeps home calm when the library bootstrap fails and offers a retry path', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response('{}', { status: 400, headers: { 'Content-Type': 'application/json' } })
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    expect(screen.getByTestId('library-sync-banner')).toBeInTheDocument()
    expect(screen.getByText('Generate your first bedtime adventure!')).toBeInTheDocument()
    expect(screen.queryByText(/Loading library failed with status/i)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('shows plain-language cooldown guidance when generation is rate limited', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [],
              favorites: [],
              kidShelfEnabled: false,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({ error: 'Cooldown active.' }),
            { status: 429, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))

    expect(
      await screen.findByText(
        'StoryTime needs a short pause before creating another story. Please wait a moment, then try again.',
      ),
    ).toBeInTheDocument()
  })

  it('keeps parent verification failures inside the parent controls card', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [],
              favorites: [],
              kidShelfEnabled: false,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/challenge') && init?.method === 'POST') {
          return new Response('{}', { status: 400, headers: { 'Content-Type': 'application/json' } })
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Verify parent with passkey' }))

    expect(await screen.findByText('Parent challenge failed with status 400')).toBeInTheDocument()
    expect(screen.queryByTestId('inline-error')).not.toBeInTheDocument()
  })

  it('shows focus-specific recovery guidance for passkey failures on localhost', async () => {
    installWebAuthnMock({
      get: async () => {
        throw new Error('The operation is not allowed at this time because the page does not have focus.')
      },
    })

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({ recent: [], favorites: [], kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/register') && init?.method === 'POST') {
          return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
        }

        if (url.includes('/gate/challenge') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({
              challengeId: 'challenge-1',
              challenge: 'Y2hhbGxlbmdlLTE',
              rpId: 'localhost',
              expiresAt: new Date().toISOString(),
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Verify parent with passkey' }))

    expect(
      await screen.findByText('Keep StoryTime in the foreground while the passkey prompt appears, then try again.'),
    ).toBeInTheDocument()
    expect(screen.queryByText(/Reopen the localhost version of StoryTime and try again\./)).not.toBeInTheDocument()
  })

  it('routes favorite failures back to the story card that triggered them', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [],
              favorites: [],
              kidShelfEnabled: false,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate')) {
          return new Response(
            JSON.stringify({
              storyId: 'story-favorite-failure',
              title: 'Ari and the Moonlit Meadow',
              mode: 'series',
              recap: 'Previously: calm winds.',
              scenes: ['Scene 1', 'Scene 2'],
              sceneCount: 2,
              posterLayers: [
                {
                  role: 'BACKGROUND',
                  speedMultiplier: 0.2,
                  dataUri: 'data:image/svg+xml;base64,PHN2Zy8+',
                },
              ],
              approvalRequired: true,
              teaserAudio: 'data:audio/wav;base64,AAA=',
              fullAudioReady: false,
              reducedMotion: false,
              generatedAt: new Date().toISOString(),
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/favorite') && init?.method === 'PUT') {
          return new Response('{}', { status: 500, headers: { 'Content-Type': 'application/json' } })
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Favorite' }))

    expect(await screen.findByTestId('recent-story-feedback-story-favorite-failure')).toHaveTextContent(
      'Favorite update failed with status 500',
    )
    expect(screen.queryByTestId('inline-error')).not.toBeInTheDocument()
  })

  it('captures child and one-shot customization in the generation request body', async () => {
    let generateBody: Record<string, unknown> | null = null

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
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
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({ recent: [], favorites: [], kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate')) {
          generateBody = typeof init?.body === 'string' ? (JSON.parse(init.body) as Record<string, unknown>) : null
          return new Response(
            JSON.stringify({
              storyId: 'one-shot-customized',
              title: 'Luna and the Starlit Cove',
              mode: 'one-shot',
              recap: 'A gentle one-shot recap.',
              scenes: ['Scene 1'],
              sceneCount: 1,
              posterLayers: [],
              approvalRequired: false,
              teaserAudio: 'data:audio/wav;base64,AAA=',
              fullAudio: 'data:audio/wav;base64,BBB=',
              fullAudioReady: true,
              reducedMotion: false,
              generatedAt: new Date().toISOString(),
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    await userEvent.clear(screen.getByLabelText('Child name'))
    await userEvent.type(screen.getByLabelText('Child name'), 'Luna')
    await userEvent.selectOptions(screen.getByLabelText('Mode'), 'one-shot')
    await userEvent.click(screen.getByRole('button', { name: 'Add optional details' }))
    await userEvent.type(screen.getByLabelText('Story arc'), 'Starlit Cove')
    await userEvent.type(screen.getByLabelText('Companion'), 'Pip')
    await userEvent.type(screen.getByLabelText('Setting'), 'Moon bridge')
    await userEvent.type(screen.getByLabelText('Mood'), 'Sleepy wonder')
    await userEvent.type(screen.getByLabelText('Theme track'), 'quiet-bells')
    await userEvent.type(screen.getByLabelText('Narration style'), 'softly spoken')
    await userEvent.click(screen.getByRole('button', { name: 'Generate one-shot' }))

    await waitFor(() => {
      expect(screen.getByText('Luna and the Starlit Cove')).toBeInTheDocument()
    })

    expect(generateBody).toMatchObject({
      childName: 'Luna',
      mode: 'one-shot',
      customization: {
        arcName: 'Starlit Cove',
        companionName: 'Pip',
        setting: 'Moon bridge',
        mood: 'Sleepy wonder',
        themeTrackId: 'quiet-bells',
        narrationStyle: 'softly spoken',
      },
    })
  })

  it('renders kid shelf ordering from the curated library payload', async () => {
    window.localStorage.setItem(
      runtimeConfig.storageKeys.storyArtifacts,
      JSON.stringify([
        {
          storyId: 'story-b',
          title: 'Second Story',
          mode: 'series',
          recap: 'Previously: bedtime calm.',
          scenes: ['Scene 1'],
          sceneCount: 1,
          posterLayers: [],
          approvalRequired: true,
          teaserAudio: 'data:audio/wav;base64,AAA=',
          fullAudioReady: false,
          reducedMotion: false,
          generatedAt: '2026-03-01T00:00:00.000Z',
          isFavorite: true,
        },
        {
          storyId: 'story-a',
          title: 'First Story',
          mode: 'series',
          recap: 'Previously: bedtime calm.',
          scenes: ['Scene 1'],
          sceneCount: 1,
          posterLayers: [],
          approvalRequired: true,
          teaserAudio: 'data:audio/wav;base64,AAA=',
          fullAudioReady: false,
          reducedMotion: false,
          generatedAt: '2026-03-02T00:00:00.000Z',
          isFavorite: false,
        },
      ]),
    )

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [
                { storyId: 'story-a', title: 'First Story', mode: 'series', isFavorite: false, fullAudioReady: false, createdAt: '2026-03-02T00:00:00.000Z' },
                { storyId: 'story-b', title: 'Second Story', mode: 'series', isFavorite: true, fullAudioReady: false, createdAt: '2026-03-01T00:00:00.000Z' },
              ],
              favorites: [
                { storyId: 'story-b', title: 'Second Story', mode: 'series', isFavorite: true, fullAudioReady: false, createdAt: '2026-03-01T00:00:00.000Z' },
              ],
              kidShelfEnabled: true,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByTestId('kid-shelf-indicator')).toHaveTextContent('Kid Shelf')
    })

    expect(screen.queryByRole('heading', { name: 'Quick Generate' })).not.toBeInTheDocument()
    expect(screen.getAllByText('First Story')[0]).toBeInTheDocument()
    expect(screen.getAllByText('Second Story').length).toBeGreaterThan(0)
  })

  it('retries library loading from the sync banner', async () => {
    let libraryAttempts = 0

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          libraryAttempts += 1
          return libraryAttempts === 1
            ? new Response('{}', { status: 500, headers: { 'Content-Type': 'application/json' } })
            : new Response(
                JSON.stringify({ recent: [], favorites: [], kidShelfEnabled: false }),
                { status: 200, headers: { 'Content-Type': 'application/json' } },
              )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByTestId('library-sync-banner')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => {
      expect(screen.queryByTestId('library-sync-banner')).not.toBeInTheDocument()
    })
    expect(libraryAttempts).toBeGreaterThanOrEqual(2)
  })

  it('finalizes pending checkout callbacks and clears the callback params', async () => {
    window.localStorage.setItem(
      runtimeConfig.storageKeys.pendingCheckout,
      JSON.stringify({
        softUserId: 'checkout-user',
        gateToken: 'gate-1',
        sessionId: 'sess-1',
        expectedTier: 'Premium',
      }),
    )
    window.history.replaceState({}, '', '/?checkoutStatus=success&checkoutSessionId=sess-1&checkoutTier=Premium')

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
              quickGenerateVisible: true,
              durationSliderVisible: true,
              durationMinMinutes: 5,
              durationMaxMinutes: 15,
              durationDefaultMinutes: 6,
              defaultChildName: 'Child',
              parentControlsEnabled: true,
              defaultTier: 'Trial',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({ recent: [], favorites: [], kidShelfEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/checkout/complete') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({ currentTier: 'Premium', upgradeTier: 'Premium' }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByTestId('checkout-feedback')).toHaveTextContent(
        'Premium unlocked. Longer bedtime stories are ready.',
      )
    })

    expect(screen.getByTestId('tier-badge')).toHaveTextContent('Premium')
    expect(window.localStorage.getItem(runtimeConfig.storageKeys.pendingCheckout)).toBeNull()
    expect(window.location.search).toBe('')
  })

  it('unlocks parent settings, updates toggles, and uses the verified gate for approval from favorites', async () => {
    installWebAuthnMock()

    let parentSettings = {
      notificationsEnabled: false,
      analyticsEnabled: false,
      kidShelfEnabled: false,
    }
    let libraryApprovalState = {
      fullAudioReady: false,
      fullAudio: null as string | null,
    }
    let approveRequestBody: Record<string, unknown> | null = null

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.toString()

        if (url.endsWith('/api/home/status')) {
          return new Response(
            JSON.stringify({
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
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/library/')) {
          return new Response(
            JSON.stringify({
              recent: [
                {
                  storyId: 'approval-story',
                  title: 'Approval Story',
                  mode: 'series',
                  isFavorite: true,
                  fullAudioReady: libraryApprovalState.fullAudioReady,
                  fullAudio: libraryApprovalState.fullAudio,
                  createdAt: '2026-03-02T00:00:00.000Z',
                },
              ],
              favorites: [
                {
                  storyId: 'approval-story',
                  title: 'Approval Story',
                  mode: 'series',
                  isFavorite: true,
                  fullAudioReady: libraryApprovalState.fullAudioReady,
                  fullAudio: libraryApprovalState.fullAudio,
                  createdAt: '2026-03-02T00:00:00.000Z',
                },
              ],
              kidShelfEnabled: parentSettings.kidShelfEnabled,
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/register') && init?.method === 'POST') {
          return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
        }

        if (url.includes('/gate/challenge') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({
              challengeId: 'challenge-1',
              challenge: 'Y2hhbGxlbmdlLTE',
              rpId: 'localhost',
              expiresAt: new Date().toISOString(),
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/gate/verify') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({ gateToken: 'gate-verified' }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/settings') && !init?.method) {
          return new Response(
            JSON.stringify(parentSettings),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/settings') && init?.method === 'PUT') {
          const updates =
            typeof init.body === 'string'
              ? (JSON.parse(init.body) as Partial<typeof parentSettings>)
              : {}
          parentSettings =
            {
              notificationsEnabled: parentSettings.notificationsEnabled,
              analyticsEnabled: parentSettings.analyticsEnabled,
              kidShelfEnabled: parentSettings.kidShelfEnabled,
              ...updates,
            }
          return new Response(
            JSON.stringify(parentSettings),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/api/stories/generate') && init?.method === 'POST') {
          return new Response(
            JSON.stringify({
              storyId: 'approval-story',
              title: 'Approval Story',
              mode: 'series',
              recap: 'A teaser-first story.',
              scenes: ['Scene 1'],
              sceneCount: 1,
              posterLayers: [],
              approvalRequired: true,
              teaserAudio: 'data:audio/wav;base64,AAA=',
              fullAudioReady: false,
              reducedMotion: false,
              generatedAt: new Date().toISOString(),
              storyBible: {
                arcName: 'Moonlit Meadow',
                arcEpisodeNumber: 1,
                arcObjective: 'Find calm',
                previousEpisodeSummary: '',
                audioAnchorMetadata: {
                  themeTrackId: 'soft-piano',
                  narrationStyle: 'warm-whisper',
                },
              },
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/favorite') && init?.method === 'PUT') {
          return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
        }

        if (url.includes('/approve') && init?.method === 'POST') {
          approveRequestBody =
            typeof init.body === 'string' ? (JSON.parse(init.body) as Record<string, unknown>) : null
          libraryApprovalState = {
            fullAudioReady: true,
            fullAudio: 'data:audio/wav;base64,FULL',
          }
          return new Response(
            JSON.stringify({
              fullAudioReady: true,
              fullAudio: 'data:audio/wav;base64,FULL',
            }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        return new Response('{}', { status: 404 })
      }),
    )

    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Verify parent with passkey' })).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getAllByText('Approval Story').length).toBeGreaterThan(0)
    })

    await waitFor(() => {
      expect(screen.getByTestId('favorite-story-approve-approval-story')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Verify parent with passkey' }))
    await waitFor(() => {
      expect(screen.getByLabelText('Notifications enabled')).toBeEnabled()
    })

    await userEvent.click(screen.getByLabelText('Notifications enabled'))
    await userEvent.click(screen.getByLabelText('Kid Shelf'))

    await waitFor(() => {
      expect(screen.getByTestId('kid-shelf-indicator')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByTestId('favorite-story-approve-approval-story'))
    await waitFor(() => {
      expect(screen.getByTestId('favorite-story-audio-summary-approval-story')).toHaveTextContent(
        'Full narration unlocked',
      )
    })
    await userEvent.click(screen.getByTestId('favorite-story-toggle-favorite-approval-story'))

    expect(approveRequestBody).toMatchObject({
      softUserId: expect.any(String),
      gateToken: 'gate-verified',
    })
  })
})
