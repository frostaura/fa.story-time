import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'

describe('App', () => {
  beforeEach(() => {
    window.localStorage.clear()
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
                defaultChildName: 'Dreamer',
                parentControlsEnabled: true,
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
              kidModeEnabled: false,
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
            JSON.stringify({ notificationsEnabled: false, analyticsEnabled: false }),
            { status: 200, headers: { 'Content-Type': 'application/json' } },
          )
        }

        if (url.includes('/settings') && init?.method === 'PUT') {
          return new Response(
            JSON.stringify({ notificationsEnabled: true, analyticsEnabled: false }),
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
  })

  it('generates a story and allows favoriting', async () => {
    render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))

    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

    expect(screen.getByRole('button', { name: 'Unfavorite' })).toBeInTheDocument()
  })

  it('shows extended one-shot customization controls', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    await userEvent.selectOptions(screen.getByLabelText('Mode'), 'one-shot')

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
  })

  it('shows parent-gated checkout controls when generation hits subscription limit', async () => {
    render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    fireEvent.change(screen.getByLabelText('Duration'), { target: { value: '15' } })
    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Confirm Premium upgrade' })).toBeInTheDocument()
    })

    await userEvent.click(screen.getByRole('button', { name: 'Confirm Premium upgrade' }))
    expect(screen.getByText('Unlock parent settings before confirming an upgrade.')).toBeInTheDocument()
  })
})
