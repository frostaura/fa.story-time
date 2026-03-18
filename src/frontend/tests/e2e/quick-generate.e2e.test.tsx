import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import userEvent from '@testing-library/user-event'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from '../../src/App'
import { runtimeConfig } from '../../src/config/runtime'

const BACKEND_PORT = Number.parseInt(process.env.STORYTIME_E2E_BACKEND_PORT ?? '19082', 10)
const BACKEND_HOST = process.env.STORYTIME_E2E_BACKEND_HOST ?? '127.0.0.1'
const BACKEND_BASE_URL = process.env.STORYTIME_E2E_BACKEND_BASE_URL ?? `http://${BACKEND_HOST}:${BACKEND_PORT}`
const WEBHOOK_SHARED_SECRET = process.env.STORYTIME_E2E_WEBHOOK_SHARED_SECRET ?? 'storytime-local-webhook-secret'
let backendProcess: ChildProcessWithoutNullStreams | null = null

type StoredStory = {
  storyId: string
  seriesId?: string
  recap: string
}

const readStoredStories = (): StoredStory[] => {
  const raw = window.localStorage.getItem(runtimeConfig.storageKeys.storyArtifacts)
  if (!raw) {
    return []
  }

  const parsed = JSON.parse(raw)
  return Array.isArray(parsed) ? (parsed as StoredStory[]) : []
}

const waitForBackend = async (): Promise<void> => {
  const maxAttempts = 80
  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    try {
      const response = await fetch(`${BACKEND_BASE_URL}/api/home/status`)
      if (response.ok) {
        return
      }
    } catch {
      // retry until startup completes
    }

    await new Promise((resolve) => setTimeout(resolve, 250))
  }

  throw new Error('Backend did not become ready in time for frontend e2e test.')
}

describe('quick generate fetch-boundary flow against live backend', () => {
  beforeAll(async () => {
    backendProcess = spawn('dotnet', ['run', '--no-launch-profile', '--project', '../backend/StoryTime.Api'], {
      cwd: process.cwd(),
      env: {
        ...process.env,
        ASPNETCORE_URLS: BACKEND_BASE_URL,
        StoryTime__Generation__AiOrchestration__Enabled: 'false',
        StoryTime__Checkout__WebhookSharedSecret: WEBHOOK_SHARED_SECRET,
      },
      stdio: 'pipe',
    })

    await waitForBackend()
  }, 120_000)

  afterAll(() => {
    if (backendProcess && !backendProcess.killed) {
      backendProcess.kill('SIGTERM')
    }
  })

  beforeEach(() => {
    window.localStorage.clear()
    ;(window as Window & { __STORYTIME_API_BASE_URL__?: string }).__STORYTIME_API_BASE_URL__ =
      BACKEND_BASE_URL
  })

  it(
    'generates a story, keeps approval gated, and favorites it through real HTTP calls',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
      })

      expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeDisabled()
      expect(screen.getByTestId(/^recent-story-approval-hint-/)).toHaveTextContent(
        'Verify parent identity before approving full narration.',
      )

      await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

      await waitFor(() => {
        expect(screen.getAllByRole('button', { name: 'Unfavorite' }).length).toBeGreaterThan(0)
      })
    },
    120_000,
  )

  it(
    'shows upgrade paywall when trial user selects premium-only duration',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
      })

      fireEvent.change(screen.getByLabelText('Duration'), { target: { value: '15' } })
      await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Verify parent to continue upgrade' })).toBeInTheDocument()
      })

      expect(screen.getByText('Upgrade to Premium for longer bedtime stories.')).toBeInTheDocument()
      expect(screen.queryByText('Unlock parent settings before confirming an upgrade.')).not.toBeInTheDocument()
    },
    120_000,
  )

  it(
    'enforces trial cooldown on immediate repeated generation for the same child session',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: 'Generate story' })
      await userEvent.click(generateButton)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
      })

      await waitFor(() => {
        expect(generateButton).not.toBeDisabled()
      })
      await userEvent.click(generateButton)

      await waitFor(() => {
        expect(
          screen.getByText(
            'StoryTime needs a short pause before creating another story. Please wait a moment, then try again.',
          ),
        ).toBeInTheDocument()
      })
    },
    120_000,
  )

  it(
    'surfaces live series continuation options after a real generation',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: 'Generate story' })
      await userEvent.click(generateButton)
      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
      })

      const firstSeriesId = readStoredStories()[0]?.seriesId
      expect(firstSeriesId).toBeTruthy()
      fireEvent.change(screen.getByLabelText('Series continuation'), {
        target: { value: firstSeriesId },
      })
      expect(screen.getByRole('button', { name: 'Continue series' })).toBeInTheDocument()
      expect(screen.getByTestId('series-selection-summary')).toHaveTextContent(/^Continuing .+ · Episode 1\.$/)
    },
    120_000,
  )

  it(
    'surfaces the parent verification CTA while strict passkey proof stays in browser coverage',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Parent Controls' })).toBeInTheDocument()
      })

      expect(screen.getByRole('button', { name: 'Verify parent with passkey' })).toBeEnabled()
      expect(screen.getByLabelText('Notifications enabled')).toBeDisabled()
      expect(screen.getByLabelText('Analytics enabled')).toBeDisabled()
      expect(screen.getByTestId('parent-controls-locked-note')).toBeInTheDocument()
    },
    120_000,
  )
})
