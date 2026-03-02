import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import userEvent from '@testing-library/user-event'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from '../../src/App'

const BACKEND_PORT = Number.parseInt(process.env.STORYTIME_E2E_BACKEND_PORT ?? '18080', 10)
const BACKEND_HOST = process.env.STORYTIME_E2E_BACKEND_HOST ?? '127.0.0.1'
const BACKEND_BASE_URL = process.env.STORYTIME_E2E_BACKEND_BASE_URL ?? `http://${BACKEND_HOST}:${BACKEND_PORT}`
let backendProcess: ChildProcessWithoutNullStreams | null = null

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

describe('quick generate end-to-end flow against live backend', () => {
  beforeAll(async () => {
    backendProcess = spawn('dotnet', ['run', '--no-launch-profile', '--project', '../backend/StoryTime.Api'], {
      cwd: process.cwd(),
      env: {
        ...process.env,
        ASPNETCORE_URLS: BACKEND_BASE_URL,
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
    'generates a story, approves narration, and favorites it through real HTTP calls',
    async () => {
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Quick Generate' })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: 'Approve full narration' }))

      await waitFor(() => {
        expect(screen.getByLabelText(/^Full narration for /)).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Unfavorite' })).toBeInTheDocument()
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
        expect(screen.getByRole('button', { name: 'Confirm Premium upgrade' })).toBeInTheDocument()
      })

      expect(screen.getByText('Upgrade to Premium for longer bedtime stories.')).toBeInTheDocument()
      await userEvent.click(screen.getByRole('button', { name: 'Confirm Premium upgrade' }))
      expect(screen.getByText('Unlock parent settings before confirming an upgrade.')).toBeInTheDocument()
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
        expect(screen.getByText('Generation failed with status 429')).toBeInTheDocument()
      })
    },
    120_000,
  )
})
