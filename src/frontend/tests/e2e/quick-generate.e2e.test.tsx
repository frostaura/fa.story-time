import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import userEvent from '@testing-library/user-event'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from '../../src/App'
import { runtimeConfig } from '../../src/config/runtime'

const BACKEND_PORT = Number.parseInt(process.env.STORYTIME_E2E_BACKEND_PORT ?? '18080', 10)
const BACKEND_HOST = process.env.STORYTIME_E2E_BACKEND_HOST ?? '127.0.0.1'
const BACKEND_BASE_URL = process.env.STORYTIME_E2E_BACKEND_BASE_URL ?? `http://${BACKEND_HOST}:${BACKEND_PORT}`
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

const installWebAuthnMock = () => {
  const credentialId = 'Y3JlZGVudGlhbC0x'

  class MockPublicKeyCredential {
    constructor(
      public id: string,
      public response: AuthenticatorAttestationResponse | AuthenticatorAssertionResponse,
    ) {}
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
      create: async () => {
        const keyPair = await crypto.subtle.generateKey(
          {
            name: 'ECDSA',
            namedCurve: 'P-256',
          },
          true,
          ['sign', 'verify'],
        )
        const publicKeyBuffer = await crypto.subtle.exportKey('spki', keyPair.publicKey)
        return new MockPublicKeyCredential(credentialId, {
          getPublicKey: () => publicKeyBuffer,
        } as unknown as AuthenticatorAttestationResponse)
      },
      get: async () =>
        new MockPublicKeyCredential(credentialId, {
          clientDataJSON: new TextEncoder().encode('mock-client-data').buffer,
          authenticatorData: new Uint8Array(37).buffer,
          signature: new Uint8Array([1, 2, 3]).buffer,
        } as unknown as AuthenticatorAssertionResponse),
    },
  })
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

describe('quick generate end-to-end flow against live backend', () => {
  beforeAll(async () => {
    backendProcess = spawn('dotnet', ['run', '--no-launch-profile', '--project', '../backend/StoryTime.Api'], {
      cwd: process.cwd(),
      env: {
        ...process.env,
        ASPNETCORE_URLS: BACKEND_BASE_URL,
        StoryTime__Generation__AiOrchestration__Enabled: 'false',
        StoryTime__ParentGate__RequireAssertion: 'false',
        StoryTime__ParentGate__RequireChallengeBoundAssertion: 'false',
        StoryTime__ParentGate__RequireRegisteredCredential: 'false',
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

  it(
    'continues series coherently after webhook entitlement reset',
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

      const softUserId = window.localStorage.getItem(runtimeConfig.storageKeys.softUserId)
      expect(softUserId).toBeTruthy()

      const webhookResponse = await fetch(`${BACKEND_BASE_URL}/api/subscription/webhook`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          softUserId,
          tier: 'Premium',
          resetCooldown: true,
        }),
      })
      expect(webhookResponse.ok).toBe(true)

      await userEvent.click(generateButton)
      await waitFor(() => {
        expect(readStoredStories().length).toBeGreaterThanOrEqual(2)
      })

      const stories = readStoredStories()
      expect(stories[0]?.seriesId).toBeTruthy()
      expect(stories[0]?.seriesId).toBe(stories[1]?.seriesId)
      expect(stories[0]?.recap.toLowerCase()).toContain('previous')
    },
    120_000,
  )

  it(
    'unlocks parent settings through the frontend parent gate flow',
    async () => {
      installWebAuthnMock()
      render(<App />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Parent Controls' })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: 'Verify parent with passkey' }))

      await waitFor(() => {
        expect(screen.getByLabelText('Notifications enabled')).toBeEnabled()
        expect(screen.getByLabelText('Analytics enabled')).toBeEnabled()
      })
    },
    120_000,
  )
})
