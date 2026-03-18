import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../../App'

const generatedAt = '2026-03-02T00:00:00.000Z'

describe('App visual regression', () => {
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
              ? (JSON.parse(init.body) as { reducedMotion?: boolean })
              : null
          const reducedMotion = payload?.reducedMotion ?? false
          return new Response(
            JSON.stringify({
              storyId: 'story-visual-1',
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
              reducedMotion,
              generatedAt,
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

        if (url.includes('/favorite')) {
          return new Response('{}', {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          })
        }

        return new Response('{}', { status: 404 })
      }),
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('matches quick generate baseline', async () => {
    const view = render(<App />)

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
    })

    expect(view.asFragment()).toMatchSnapshot()
  })

  it('matches generated story baseline', async () => {
    const view = render(<App />)

    await userEvent.click(await screen.findByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    expect(view.asFragment()).toMatchSnapshot()
  })

  it('matches reduced-motion generated baseline', async () => {
    const view = render(<App />)

    await userEvent.click(await screen.findByLabelText('Reduced motion'))
    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))
    await waitFor(() => {
      expect(screen.getByText('Ari and the Moonlit Meadow')).toBeInTheDocument()
    })

    expect(view.asFragment()).toMatchSnapshot()
  })
})
