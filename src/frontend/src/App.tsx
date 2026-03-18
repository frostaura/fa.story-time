import { type CSSProperties, useCallback, useEffect, useMemo, useState } from 'react'
import './App.css'
import { FavoritesShelf } from './components/FavoritesShelf'
import { ParentControlsSection } from './components/ParentControlsSection'
import {
  QuickGenerateCard,
  type OneShotCustomization,
  type SeriesOption,
} from './components/QuickGenerateCard'
import { RecentStoriesShelf } from './components/RecentStoriesShelf'
import iconUrl from '/storytime-icon.svg'
import { appMessages } from './config/messages'
import { type Mode, storyModes } from './config/modes'
import { logClientWarning } from './config/clientLogger'
import {
  EMPTY_ONE_SHOT_DEFAULTS,
  resolveApiBaseUrl,
  runtimeConfig,
} from './config/runtime'
import { createStoryTimeApi } from './services/storyTimeApi'
import type {
  ChildProfile,
  GenerateStoryResponse,
  HomeStatusResponse,
  LibraryItem,
  LibraryResponse,
  ParentCredential,
  ParentGateChallengeResponse,
  ParentGateVerifyResponse,
  ParentSettingsResponse,
  ParentSignedAssertion,
  PosterLayer,
  StoryApprovalResponse,
  StoryArtifact,
  SubscriptionCheckoutCompleteResponse,
  SubscriptionCheckoutSessionResponse,
  SubscriptionPaywallResponse,
} from './types/storyTime'

const getSoftUserId = (): string => {
  const existing = localStorage.getItem(runtimeConfig.storageKeys.softUserId)
  if (existing) {
    return existing
  }

  const generated = crypto.randomUUID()
  localStorage.setItem(runtimeConfig.storageKeys.softUserId, generated)
  return generated
}

const detectReducedMotion = (): boolean => {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false
  }

  return window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

const readStoryArtifacts = (): StoryArtifact[] => {
  const raw = localStorage.getItem(runtimeConfig.storageKeys.storyArtifacts)
  if (!raw) {
    return []
  }

  try {
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? (parsed as StoryArtifact[]) : []
  } catch (error) {
    logClientWarning(appMessages.unableToParseStoryArtifacts, error)
    return []
  }
}

const readChildProfile = (defaultChildName: string): ChildProfile => {
  const raw = localStorage.getItem(runtimeConfig.storageKeys.childProfile)
  const fallback: ChildProfile = {
    childName: defaultChildName,
    reducedMotion: detectReducedMotion(),
    notificationsEnabled: runtimeConfig.profileDefaults.notificationsEnabled,
    analyticsEnabled: runtimeConfig.profileDefaults.analyticsEnabled,
  }

  if (!raw) {
    return fallback
  }

  try {
    const parsed = JSON.parse(raw) as Partial<ChildProfile>
    return {
      childName:
        typeof parsed.childName === 'string' && parsed.childName.trim().length > 0
          ? parsed.childName.trim()
          : fallback.childName,
      reducedMotion:
        typeof parsed.reducedMotion === 'boolean' ? parsed.reducedMotion : fallback.reducedMotion,
      notificationsEnabled:
        typeof parsed.notificationsEnabled === 'boolean'
          ? parsed.notificationsEnabled
          : fallback.notificationsEnabled,
      analyticsEnabled:
        typeof parsed.analyticsEnabled === 'boolean'
          ? parsed.analyticsEnabled
          : fallback.analyticsEnabled,
    }
  } catch (error) {
    logClientWarning(appMessages.unableToParseProfile, error)
    return fallback
  }
}

const persistStoryArtifacts = (stories: StoryArtifact[]) => {
  localStorage.setItem(runtimeConfig.storageKeys.storyArtifacts, JSON.stringify(stories))
}

const persistChildProfile = (profile: ChildProfile) => {
  localStorage.setItem(runtimeConfig.storageKeys.childProfile, JSON.stringify(profile))
}

const clampDuration = (value: number, min: number, max: number): number => {
  if (value < min) {
    return min
  }

  if (value > max) {
    return max
  }

  return value
}

const arrayBufferToBase64 = (buffer: ArrayBufferLike): string => {
  let binary = ''
  const bytes = new Uint8Array(buffer)
  bytes.forEach((value) => {
    binary += String.fromCharCode(value)
  })
  return btoa(binary)
}

const base64UrlToArrayBuffer = (value: string): ArrayBuffer => {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/')
  const padding = '='.repeat((4 - (normalized.length % 4)) % 4)
  const binary = atob(`${normalized}${padding}`)
  const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0))
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer
}

const supportsNativeWebAuthn = (): boolean => {
  if (typeof window === 'undefined' || typeof navigator === 'undefined') {
    return false
  }

  const credentialApi = navigator.credentials
  return (
    typeof PublicKeyCredential !== 'undefined' &&
    typeof credentialApi?.create === 'function' &&
    typeof credentialApi?.get === 'function'
  )
}

const ensureParentPromptCanOpen = async (): Promise<void> => {
  if (typeof window === 'undefined' || typeof document === 'undefined') {
    return
  }

  if (document.visibilityState === 'hidden') {
    throw new Error(appMessages.ui.parentVerificationFocusRetry)
  }

  if (document.hasFocus()) {
    return
  }

  window.focus()
  await new Promise<void>((resolve) => {
    window.requestAnimationFrame(() => resolve())
  })

  if (!document.hasFocus()) {
    throw new Error(appMessages.ui.parentVerificationFocusRetry)
  }
}

const readParentCredential = (): ParentCredential | null => {
  const raw = localStorage.getItem(runtimeConfig.storageKeys.parentCredential)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as Partial<ParentCredential>
    if (
      parsed.credentialKind === runtimeConfig.parentGate.credentialKind &&
      typeof parsed.credentialId === 'string' &&
      typeof parsed.publicKey === 'string'
    ) {
      return {
        credentialId: parsed.credentialId,
        publicKey: parsed.publicKey,
        credentialKind: runtimeConfig.parentGate.credentialKind,
      }
    }
  } catch (error) {
    logClientWarning(appMessages.unableToParseParentCredential, error)
  }

  return null
}

const persistParentCredential = (credential: ParentCredential) => {
  localStorage.setItem(runtimeConfig.storageKeys.parentCredential, JSON.stringify(credential))
}

type PendingCheckoutSession = {
  softUserId: string
  gateToken: string
  sessionId: string
  expectedTier: string
}

type InlineFeedback = {
  message: string
  tone: 'error' | 'success' | 'info'
}

const readPendingCheckoutSession = (): PendingCheckoutSession | null => {
  const raw = localStorage.getItem(runtimeConfig.storageKeys.pendingCheckout)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as Partial<PendingCheckoutSession>
    if (
      typeof parsed.softUserId === 'string' &&
      typeof parsed.gateToken === 'string' &&
      typeof parsed.sessionId === 'string' &&
      typeof parsed.expectedTier === 'string'
    ) {
      return {
        softUserId: parsed.softUserId,
        gateToken: parsed.gateToken,
        sessionId: parsed.sessionId,
        expectedTier: parsed.expectedTier,
      }
    }
  } catch (error) {
    logClientWarning(appMessages.unableToCompleteUpgradeCheckout, error)
  }

  return null
}

const persistPendingCheckoutSession = (pending: PendingCheckoutSession) => {
  localStorage.setItem(runtimeConfig.storageKeys.pendingCheckout, JSON.stringify(pending))
}

const clearPendingCheckoutSession = () => {
  localStorage.removeItem(runtimeConfig.storageKeys.pendingCheckout)
}

const buildCheckoutReturnUrl = (): string => {
  const currentUrl = new URL(window.location.href)
  currentUrl.searchParams.delete('checkoutStatus')
  currentUrl.searchParams.delete('checkoutSessionId')
  currentUrl.searchParams.delete('checkoutTier')
  currentUrl.hash = ''
  return currentUrl.toString()
}

const readCheckoutCallbackParams = (): URLSearchParams => {
  if (typeof window === 'undefined') {
    return new URLSearchParams()
  }

  const hash = window.location.hash.startsWith('#') ? window.location.hash.slice(1) : window.location.hash
  if (hash.trim().length > 0) {
    return new URLSearchParams(hash)
  }

  return new URLSearchParams(window.location.search)
}

const ensureParentCredential = async (
  softUserId: string,
  challenge: string,
  rpId: string,
): Promise<ParentCredential> => {
  if (typeof crypto.subtle === 'undefined') {
    throw new Error(appMessages.webCryptoUnavailable)
  }

  const apiBaseUrl = resolveApiBaseUrl()
  const api = createStoryTimeApi(apiBaseUrl)
  const existing = readParentCredential()
  if (existing) {
    const registration = await api.registerParentCredential(softUserId, {
      credentialId: existing.credentialId,
      publicKey: existing.publicKey,
    })

    if (!registration.ok) {
      throw new Error(appMessages.parentCredentialRegistrationFailed(registration.status))
    }

    return existing
  }

  if (!supportsNativeWebAuthn()) {
    throw new Error(appMessages.webauthnRequiredForVerification)
  }

  const userId = new TextEncoder().encode(softUserId).slice(0, runtimeConfig.parentGate.userIdMaxLength)
  await ensureParentPromptCanOpen()
  const created = await navigator.credentials.create({
    publicKey: {
      challenge: base64UrlToArrayBuffer(challenge),
      rp: {
        name: runtimeConfig.parentGate.rpDisplayName,
        id: rpId,
      },
      user: {
        id: userId,
        name: `${runtimeConfig.parentGate.userNamePrefix}${softUserId}`,
        displayName: runtimeConfig.parentGate.userDisplayName,
      },
      pubKeyCredParams: runtimeConfig.parentGate.coseAlgorithms.map((alg) => ({
        type: 'public-key',
        alg,
      })),
      timeout: runtimeConfig.parentGate.timeoutMs,
      attestation: runtimeConfig.parentGate.attestation,
      authenticatorSelection: {
        residentKey: runtimeConfig.parentGate.residentKey,
        userVerification: runtimeConfig.parentGate.userVerification,
      },
    },
  })

  if (!(created instanceof PublicKeyCredential)) {
    throw new Error(appMessages.parentCredentialRegistrationCancelled)
  }

  const attestationResponse = created.response as AuthenticatorAttestationResponse & {
    getPublicKey?: () => ArrayBuffer | null
  }
  const publicKeyBuffer =
    typeof attestationResponse.getPublicKey === 'function' ? attestationResponse.getPublicKey() : null
  if (!publicKeyBuffer) {
    throw new Error(appMessages.webauthnPublicKeyUnavailable)
  }

  const credential: ParentCredential = {
    credentialId: created.id,
    publicKey: arrayBufferToBase64(publicKeyBuffer),
    credentialKind: runtimeConfig.parentGate.credentialKind,
  }

  const registerResponse = await api.registerParentCredential(softUserId, {
    credentialId: credential.credentialId,
    publicKey: credential.publicKey,
  })

  if (!registerResponse.ok) {
    throw new Error(appMessages.parentCredentialRegistrationFailed(registerResponse.status))
  }

  persistParentCredential(credential)
  return credential
}

const createParentAssertion = async (
  softUserId: string,
  challenge: string,
  rpId: string,
): Promise<ParentSignedAssertion> => {
  const credential = await ensureParentCredential(softUserId, challenge, rpId)
  if (!supportsNativeWebAuthn()) {
    throw new Error(appMessages.webauthnRequiredForVerification)
  }

  await ensureParentPromptCanOpen()
  const assertion = await navigator.credentials.get({
    publicKey: {
      challenge: base64UrlToArrayBuffer(challenge),
      rpId,
      allowCredentials: [
        {
          type: 'public-key',
          id: base64UrlToArrayBuffer(credential.credentialId),
        },
      ],
      userVerification: runtimeConfig.parentGate.userVerification,
      timeout: runtimeConfig.parentGate.timeoutMs,
    },
  })

  if (!(assertion instanceof PublicKeyCredential)) {
    throw new Error(appMessages.parentVerificationCancelled)
  }

  const response = assertion.response as AuthenticatorAssertionResponse

  return {
    credentialId: assertion.id,
    clientDataJson: arrayBufferToBase64(response.clientDataJSON),
    authenticatorData: arrayBufferToBase64(response.authenticatorData),
    signature: arrayBufferToBase64(response.signature),
    type: runtimeConfig.parentGate.assertionType,
  }
}

const getLayerStyle = (layer: PosterLayer, reducedMotion: boolean): CSSProperties => {
  const speed = Math.max(0, layer.speedMultiplier)
  const parallax = runtimeConfig.posterParallax
  if (reducedMotion || speed === 0) {
    return {
      backgroundImage: `url(${layer.dataUri})`,
      animationDuration: '0s',
      transform: 'translate3d(0, 0, 0)',
    }
  }

  return {
    backgroundImage: `url(${layer.dataUri})`,
    animationDuration: `${Math.max(parallax.minAnimationDurationSeconds, parallax.durationDivisor / speed)}s`,
    animationDelay: `${Math.min(parallax.maxAnimationDelaySeconds, speed / parallax.delayDivisor)}s`,
    transform: `translate3d(0, 0, ${Math.round(speed * parallax.depthScale)}px)`,
  }
}

const NETWORK_ERROR_PATTERNS = ['failed to fetch', 'networkerror', 'load failed', 'network request failed']

const isNetworkError = (message: string): boolean =>
  NETWORK_ERROR_PATTERNS.some((pattern) => message.toLowerCase().includes(pattern))

const triggerHaptic = () => {
  if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
    navigator.vibrate(50)
  }
}

const withTimeout = async <T,>(promise: Promise<T>, timeoutMs: number, timeoutMessage: string): Promise<T> => {
  let timeoutHandle = 0

  try {
    return await Promise.race([
      promise,
      new Promise<T>((_, reject) => {
        timeoutHandle = window.setTimeout(() => {
          reject(new Error(timeoutMessage))
        }, timeoutMs)
      }),
    ])
  } finally {
    window.clearTimeout(timeoutHandle)
  }
}

type ParentVerificationReadiness = {
  isSupported: boolean
  hint: string
  preferredUrl: string
}

const buildPreferredParentUrl = (requiredHost: string): string => {
  if (typeof window === 'undefined') {
    return `http://${requiredHost}`
  }

  const { protocol, port, pathname, search, hash } = window.location
  const portSegment = port.length > 0 ? `:${port}` : ''
  return `${protocol}//${requiredHost}${portSegment}${pathname}${search}${hash}`
}

const getParentVerificationReadiness = (): ParentVerificationReadiness => {
  const requiredHost = runtimeConfig.parentGate.rpId.toLowerCase()
  const preferredUrl = buildPreferredParentUrl(requiredHost)
  if (typeof window === 'undefined') {
    return {
      isSupported: true,
      hint: appMessages.ui.parentVerificationReadyHint,
      preferredUrl,
    }
  }

  const currentHost = window.location.hostname.toLowerCase()
  if (currentHost === requiredHost) {
    return {
      isSupported: true,
      hint: appMessages.ui.parentVerificationReadyHint,
      preferredUrl,
    }
  }

  if (requiredHost === 'localhost' && currentHost === '127.0.0.1') {
    return {
      isSupported: false,
      hint: appMessages.ui.parentVerificationUnsupportedLocalHost(preferredUrl),
      preferredUrl,
    }
  }

  return {
    isSupported: false,
    hint: appMessages.ui.parentVerificationUnsupportedHost(window.location.hostname, preferredUrl),
    preferredUrl,
  }
}

const PARENT_FOCUS_ERROR_PATTERNS = [
  'does not have focus',
  'document is not focused',
  'focus retry',
]
const PARENT_RETRYABLE_ERROR_PATTERNS = [
  'invalid domain',
  'webauthn',
  'parent verification failed',
  'loading parent settings failed',
]

const formatParentVerificationError = (
  message: string,
  readiness: ParentVerificationReadiness,
): string => {
  if (!readiness.isSupported) {
    return readiness.hint
  }

  const normalized = message.toLowerCase()
  if (PARENT_FOCUS_ERROR_PATTERNS.some((pattern) => normalized.includes(pattern))) {
    return appMessages.ui.parentVerificationFocusRetry
  }

  if (PARENT_RETRYABLE_ERROR_PATTERNS.some((pattern) => normalized.includes(pattern))) {
    return appMessages.ui.parentVerificationRetryCurrentTab
  }

  return message
}

const GENERATION_REQUEST_TIMEOUT_MS = 12_000
const PARENT_REQUEST_TIMEOUT_MS = 6_000
const STORY_HIGHLIGHT_DURATION_MS = 4_500

type LibrarySyncState = 'idle' | 'loading' | 'ready' | 'error'

function App() {
  const apiBaseUrl = resolveApiBaseUrl()
  const api = useMemo(() => createStoryTimeApi(apiBaseUrl), [apiBaseUrl])
  const ui = appMessages.ui
  const parentVerificationReadiness = useMemo(() => getParentVerificationReadiness(), [])
  const configuredLibraryRecentLimit =
    runtimeConfig.libraryRecentLimit && runtimeConfig.libraryRecentLimit > 0
      ? Math.floor(runtimeConfig.libraryRecentLimit)
      : null
  const initialHomeStatus: HomeStatusResponse = {
    quickGenerateVisible: runtimeConfig.homeStatusFallback.quickGenerateVisible,
    durationSliderVisible: runtimeConfig.homeStatusFallback.durationSliderVisible,
    durationMinMinutes: runtimeConfig.durationMinMinutes ?? 1,
    durationMaxMinutes: runtimeConfig.durationMaxMinutes ?? 1,
    durationDefaultMinutes: runtimeConfig.durationSelection ?? 1,
    defaultChildName: runtimeConfig.defaultChildName,
    parentControlsEnabled: runtimeConfig.homeStatusFallback.parentControlsEnabled,
    defaultTier: '',
    oneShotDefaults: { ...EMPTY_ONE_SHOT_DEFAULTS },
  }
  const [homeStatus, setHomeStatus] = useState<HomeStatusResponse>({
    ...initialHomeStatus,
  })
  const [durationMinutes, setDurationMinutes] = useState<number>(initialHomeStatus.durationDefaultMinutes)
  const [mode, setMode] = useState<Mode>(storyModes.series)
  const [stories, setStories] = useState<StoryArtifact[]>(() => readStoryArtifacts())
  const [profile, setProfile] = useState<ChildProfile>(() => readChildProfile(initialHomeStatus.defaultChildName))
  const [oneShotCustomization, setOneShotCustomization] = useState<OneShotCustomization>({
    arcName: '',
    companionName: '',
    setting: '',
    mood: '',
    themeTrackId: '',
    narrationStyle: '',
  })
  const [isOneShotDetailsExpanded, setIsOneShotDetailsExpanded] = useState(false)
  const [libraryRecentIds, setLibraryRecentIds] = useState<string[]>([])
  const [libraryFavoriteIds, setLibraryFavoriteIds] = useState<string[]>([])
  const [kidShelfEnabled, setKidShelfEnabled] = useState(false)
  const [isGenerating, setIsGenerating] = useState(false)
  const [approvingStoryId, setApprovingStoryId] = useState<string | null>(null)
  const [favoritingStoryId, setFavoritingStoryId] = useState<string | null>(null)
  const [isUnlockingParent, setIsUnlockingParent] = useState(false)
  const [isUpgradingSubscription, setIsUpgradingSubscription] = useState(false)
  const [parentGateToken, setParentGateToken] = useState<string | null>(null)
  const [librarySyncState, setLibrarySyncState] = useState<LibrarySyncState>('idle')
  const [storyActionFeedback, setStoryActionFeedback] = useState<{ storyId: string; message: string } | null>(null)
  const [generationFeedback, setGenerationFeedback] = useState<InlineFeedback | null>(null)
  const [highlightedStoryId, setHighlightedStoryId] = useState<string | null>(null)
  const [parentFeedback, setParentFeedback] = useState<InlineFeedback | null>(null)
  const [paywallFeedback, setPaywallFeedback] = useState<InlineFeedback | null>(null)
  const [checkoutFeedback, setCheckoutFeedback] = useState<InlineFeedback | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [paywall, setPaywall] = useState<SubscriptionPaywallResponse | null>(null)

  const storiesById = useMemo(() => new Map(stories.map((story) => [story.storyId, story])), [stories])
  const seriesOptions = useMemo<SeriesOption[]>(() => {
    const seen = new Set<string>()
    const options: SeriesOption[] = []
    for (const story of stories) {
      if (story.mode !== storyModes.series || !story.seriesId || !story.storyBible || seen.has(story.seriesId)) {
        continue
      }

      seen.add(story.seriesId)
      options.push({
        seriesId: story.seriesId,
        label: ui.seriesProgress(story.storyBible.arcName, story.storyBible.arcEpisodeNumber),
      })
    }

    return options
  }, [stories, ui])
  const [selectedSeriesId, setSelectedSeriesId] = useState<string>('new')
  const favorites = useMemo(() => {
    if (!kidShelfEnabled) {
      return stories.filter((story) => story.isFavorite)
    }

    return libraryFavoriteIds
      .map((storyId) => storiesById.get(storyId))
      .filter((story): story is StoryArtifact => Boolean(story))
  }, [kidShelfEnabled, libraryFavoriteIds, stories, storiesById])
  const approvalHint = parentGateToken
    ? null
    : parentVerificationReadiness.isSupported
      ? ui.verifyParentToApprove
      : parentVerificationReadiness.hint

  const refreshLibrary = useCallback(async (softUserId: string) => {
    setLibrarySyncState('loading')

    try {
      const response = await api.getLibrary(softUserId)
      if (!response.ok) {
        setLibrarySyncState('error')
        return false
      }

      const library = (await response.json()) as LibraryResponse
      setKidShelfEnabled(library.kidShelfEnabled)
      const byId = new Map<string, LibraryItem>()
      for (const item of library.recent) {
        byId.set(item.storyId, item)
      }
      for (const item of library.favorites) {
        byId.set(item.storyId, item)
      }
      setLibraryRecentIds(library.recent.map((item) => item.storyId))
      setLibraryFavoriteIds(library.favorites.map((item) => item.storyId))

      setStories((existing) =>
        existing.map((story) => {
          const libraryItem = byId.get(story.storyId)
          if (!libraryItem) {
            return story
          }

          return {
            ...story,
            isFavorite: libraryItem.isFavorite,
            fullAudioReady: libraryItem.fullAudioReady,
            fullAudio: libraryItem.fullAudio ?? story.fullAudio,
          }
        }),
      )
      setLibrarySyncState('ready')
      return true
    } catch {
      setLibrarySyncState('error')
      return false
    }
  }, [api])

  useEffect(() => {
    if (librarySyncState === 'error') {
      if (selectedSeriesId !== 'new') {
        setSelectedSeriesId('new')
      }
      return
    }

    if (selectedSeriesId === 'new') {
      return
    }

    if (!seriesOptions.some((option) => option.seriesId === selectedSeriesId)) {
      setSelectedSeriesId('new')
    }
  }, [librarySyncState, selectedSeriesId, seriesOptions])

  const selectedSeriesOption = useMemo(
    () =>
      selectedSeriesId === 'new'
        ? null
        : seriesOptions.find((option) => option.seriesId === selectedSeriesId) ?? null,
    [selectedSeriesId, seriesOptions],
  )

  useEffect(() => {
    if (mode !== storyModes.oneShot && isOneShotDetailsExpanded) {
      setIsOneShotDetailsExpanded(false)
    }
  }, [isOneShotDetailsExpanded, mode])

  useEffect(() => {
    if (!highlightedStoryId || typeof window === 'undefined') {
      return
    }

    const scrollHandle = window.setTimeout(() => {
      const highlightedCard = document.getElementById(`recent-story-${highlightedStoryId}`)
      if (typeof highlightedCard?.scrollIntoView === 'function') {
        highlightedCard.scrollIntoView({
          behavior: 'smooth',
          block: 'center',
        })
      }
    }, 50)

    const clearHandle = window.setTimeout(() => {
      setHighlightedStoryId((current) => (current === highlightedStoryId ? null : current))
    }, STORY_HIGHLIGHT_DURATION_MS)

    return () => {
      window.clearTimeout(scrollHandle)
      window.clearTimeout(clearHandle)
    }
  }, [highlightedStoryId])

  useEffect(() => {
    persistStoryArtifacts(stories)
  }, [stories])

  useEffect(() => {
    persistChildProfile(profile)
  }, [profile])

  useEffect(() => {
    const loadHomeStatus = async () => {
      try {
        const response = await api.getHomeStatus()
        if (!response.ok) {
          throw new Error(appMessages.unableToLoadHomeStatus)
        }

        const status = (await response.json()) as HomeStatusResponse
        const mergedStatus: HomeStatusResponse = {
          ...status,
          oneShotDefaults: {
            ...EMPTY_ONE_SHOT_DEFAULTS,
            ...status.oneShotDefaults,
          },
        }
        setHomeStatus(mergedStatus)
        setDurationMinutes(() =>
          clampDuration(
            mergedStatus.durationDefaultMinutes,
            mergedStatus.durationMinMinutes,
            mergedStatus.durationMaxMinutes,
          ),
        )
        setProfile((current) => ({
          ...current,
          childName: current.childName.trim() === '' ? mergedStatus.defaultChildName : current.childName,
        }))
      } catch (statusError) {
        const message =
          statusError instanceof Error ? statusError.message : appMessages.unableToLoadHomeStatus
        setError(message)
      }
    }

    void loadHomeStatus()
  }, [api])

  useEffect(() => {
    const softUserId = getSoftUserId()
    void refreshLibrary(softUserId)
  }, [refreshLibrary])

  useEffect(() => {
    const pendingCheckout = readPendingCheckoutSession()
    const params = readCheckoutCallbackParams()
    if (!pendingCheckout) {
      return
    }

    if (
      params.get('checkoutStatus') !== 'success' ||
      params.get('checkoutSessionId') !== pendingCheckout.sessionId
    ) {
      return
    }

    let cancelled = false
    const finalizeCheckout = async () => {
      setIsUpgradingSubscription(true)
      setError(null)

      try {
        const completeResponse = await api.completeCheckoutSession(pendingCheckout.softUserId, {
          gateToken: pendingCheckout.gateToken,
          sessionId: pendingCheckout.sessionId,
        })

        if (!completeResponse.ok) {
          throw new Error(appMessages.checkoutCompletionFailed(completeResponse.status))
        }

        const completion = (await completeResponse.json()) as SubscriptionCheckoutCompleteResponse
        if (completion.currentTier !== pendingCheckout.expectedTier) {
          throw new Error(appMessages.unexpectedUpgradeTier)
        }

        setHomeStatus((current) => ({ ...current, defaultTier: completion.currentTier }))
        setPaywall(null)
        setPaywallFeedback(null)
        setCheckoutFeedback({
          tone: 'success',
          message: appMessages.upgradeCheckoutCompleted,
        })
        await refreshLibrary(pendingCheckout.softUserId)
      } catch (checkoutError) {
        const message =
          checkoutError instanceof Error
            ? checkoutError.message
            : appMessages.unableToCompleteUpgradeCheckout
        setCheckoutFeedback({
          tone: 'error',
          message,
        })
      } finally {
        clearPendingCheckoutSession()
        const sanitizedUrl = new URL(window.location.href)
        sanitizedUrl.searchParams.delete('checkoutStatus')
        sanitizedUrl.searchParams.delete('checkoutSessionId')
        sanitizedUrl.searchParams.delete('checkoutTier')
        sanitizedUrl.hash = ''
        window.history.replaceState({}, document.title, `${sanitizedUrl.pathname}${sanitizedUrl.search}`)
        if (!cancelled) {
          setIsUpgradingSubscription(false)
        }
      }
    }

    void finalizeCheckout()
    return () => {
      cancelled = true
    }
  }, [api, kidShelfEnabled, refreshLibrary])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key === 'Enter' && !isGenerating && !kidShelfEnabled) {
        event.preventDefault()
        void onGenerate()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  })

    const onGenerate = async () => {
      triggerHaptic()
      setIsGenerating(true)
      setError(null)
      setStoryActionFeedback(null)
      setGenerationFeedback(null)
      setPaywall(null)
      setPaywallFeedback(null)
      setCheckoutFeedback(null)

    try {
      const softUserId = getSoftUserId()
      const selectedSeriesStory =
        mode === storyModes.series && selectedSeriesId !== 'new'
          ? stories.find((story) => story.seriesId === selectedSeriesId && Boolean(story.storyBible))
          : undefined
      const continuationSeriesId =
        mode === storyModes.series && selectedSeriesId !== 'new' ? selectedSeriesId : undefined
      const generationStatusMessage = mode === storyModes.oneShot
        ? ui.generatingOneShotStatus(profile.childName)
        : continuationSeriesId && selectedSeriesOption
          ? ui.generatingContinuationStatus(selectedSeriesOption.label, profile.childName)
          : ui.generatingStoryStatus(profile.childName)

      setGenerationFeedback({
        tone: 'info',
        message: generationStatusMessage,
      })

      const response = await withTimeout(api.generateStory({
        softUserId,
        childName: profile.childName,
        mode,
        durationMinutes,
        seriesId: continuationSeriesId,
        storyBible: selectedSeriesStory?.storyBible,
        favorite: false,
        reducedMotion: profile.reducedMotion,
        customization:
          mode === storyModes.oneShot
            ? {
                arcName: oneShotCustomization.arcName,
                companionName: oneShotCustomization.companionName,
                setting: oneShotCustomization.setting,
                mood: oneShotCustomization.mood,
                themeTrackId: oneShotCustomization.themeTrackId,
                narrationStyle: oneShotCustomization.narrationStyle,
              }
            : undefined,
      }), GENERATION_REQUEST_TIMEOUT_MS, appMessages.generationTemporarilyUnavailable)

      if (!response.ok) {
        if (response.status === 402) {
          const rejected = (await response.json()) as {
            error?: string
            paywall?: SubscriptionPaywallResponse
          }
          setPaywall(rejected.paywall ?? null)
          setGenerationFeedback(null)
          setPaywallFeedback(null)
          return
        }

        throw new Error(
          response.status === 429
            ? appMessages.generationRateLimited
            : appMessages.generationTemporarilyUnavailable,
        )
      }

      const generated = (await response.json()) as GenerateStoryResponse
      const nextStories: StoryArtifact[] = [
        {
          storyId: generated.storyId,
          title: generated.title,
          mode: generated.mode,
          seriesId: generated.seriesId,
          recap: generated.recap,
          scenes: generated.scenes,
          sceneCount: generated.sceneCount,
          posterLayers: generated.posterLayers,
          approvalRequired: generated.approvalRequired,
          teaserAudio: generated.teaserAudio,
          fullAudio: generated.fullAudio,
          fullAudioReady: generated.fullAudioReady,
          storyBible: generated.storyBible,
          reducedMotion: generated.reducedMotion,
          generatedAt: generated.generatedAt,
          isFavorite: false,
        },
        ...stories,
      ]

      setStories(nextStories)
      setHighlightedStoryId(generated.storyId)
      if (mode === storyModes.oneShot) {
        setIsOneShotDetailsExpanded(false)
      }
      setGenerationFeedback({
        tone: 'success',
        message: mode === storyModes.oneShot
          ? ui.oneShotGenerated(generated.title)
          : continuationSeriesId
            ? ui.continuationGenerated(generated.title)
            : ui.storyGenerated(generated.title),
      })
      await refreshLibrary(softUserId)
    } catch (generationError) {
      const message = generationError instanceof Error ? generationError.message : appMessages.unknownGenerationError
      setGenerationFeedback({
        tone: 'error',
        message,
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const toggleFavorite = async (storyId: string) => {
    const current = stories.find((story) => story.storyId === storyId)
    if (!current) {
      return
    }

    const nextFavoriteValue = !current.isFavorite
    setFavoritingStoryId(storyId)
    setStoryActionFeedback((currentFeedback) =>
      currentFeedback?.storyId === storyId ? null : currentFeedback,
    )

    try {
      const response = await api.setStoryFavorite(storyId, nextFavoriteValue)

      if (!response.ok) {
        throw new Error(appMessages.favoriteUpdateFailed(response.status))
      }

      setStories((existing) =>
        existing.map((story) =>
          story.storyId === storyId ? { ...story, isFavorite: nextFavoriteValue } : story,
        ),
      )
      setStoryActionFeedback((currentFeedback) =>
        currentFeedback?.storyId === storyId ? null : currentFeedback,
      )
      await refreshLibrary(getSoftUserId())
    } catch (favoriteError) {
      const message = favoriteError instanceof Error ? favoriteError.message : appMessages.unableToUpdateFavorite
      setStoryActionFeedback({ storyId, message })
    } finally {
      setFavoritingStoryId(null)
    }
  }

  const approveStory = async (storyId: string) => {
    const currentStory = stories.find((story) => story.storyId === storyId)
    if (!currentStory) {
      return
    }

    if (!parentGateToken) {
      setStoryActionFeedback({
        storyId,
        message: approvalHint ?? appMessages.unlockParentSettingsFirst,
      })
      return
    }

    setApprovingStoryId(storyId)
    setStoryActionFeedback((currentFeedback) =>
      currentFeedback?.storyId === storyId ? null : currentFeedback,
    )
    try {
      const softUserId = getSoftUserId()
      const response = await api.approveStory(storyId, {
        softUserId,
        gateToken: parentGateToken,
      })

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error(approvalHint ?? appMessages.unlockParentSettingsFirst)
        }

        if (currentStory.fullAudio) {
          setStories((existing) =>
            existing.map((story) =>
              story.storyId === storyId
                ? { ...story, fullAudioReady: true, fullAudio: currentStory.fullAudio }
                : story,
            ),
          )
          await refreshLibrary(softUserId)
          return
        }

        throw new Error(appMessages.approvalTemporarilyUnavailable)
      }

      const approval = (await response.json()) as StoryApprovalResponse

      setStories((existing) =>
        existing.map((story) =>
          story.storyId === storyId
            ? { ...story, fullAudioReady: approval.fullAudioReady, fullAudio: approval.fullAudio }
            : story,
        ),
      )
      setStoryActionFeedback((currentFeedback) =>
        currentFeedback?.storyId === storyId ? null : currentFeedback,
      )
      await refreshLibrary(softUserId)
    } catch (approvalError) {
      const message =
        approvalError instanceof Error ? approvalError.message : appMessages.approvalTemporarilyUnavailable
      setStoryActionFeedback({ storyId, message })
    } finally {
      setApprovingStoryId(null)
    }
  }

  const unlockParentSettings = async (source: 'controls' | 'upgrade' = 'controls') => {
    if (parentGateToken) {
      return parentGateToken
    }

    if (!parentVerificationReadiness.isSupported) {
      const feedback: InlineFeedback = {
        tone: 'error',
        message: parentVerificationReadiness.hint,
      }
      setParentFeedback(feedback)
      if (source === 'upgrade') {
        setPaywallFeedback(feedback)
      }
      return null
    }

    setIsUnlockingParent(true)
    setParentFeedback({
      tone: 'info',
      message: ui.parentVerificationProgress,
    })
    if (source === 'upgrade') {
      setPaywallFeedback({
        tone: 'info',
        message: ui.upgradeVerificationProgress,
      })
    }

    try {
      const softUserId = getSoftUserId()
      const challengeResponse = await withTimeout(
        api.createParentGateChallenge(softUserId),
        PARENT_REQUEST_TIMEOUT_MS,
        appMessages.ui.parentVerificationRetryCurrentTab,
      )

      if (!challengeResponse.ok) {
        throw new Error(appMessages.parentChallengeFailed(challengeResponse.status))
      }

      const challenge = (await challengeResponse.json()) as ParentGateChallengeResponse
      const assertion = await withTimeout(
        createParentAssertion(softUserId, challenge.challenge, challenge.rpId),
        runtimeConfig.parentGate.timeoutMs + 2_000,
        appMessages.ui.parentVerificationRetryCurrentTab,
      )
      const verifyResponse = await withTimeout(
        api.verifyParentGate(softUserId, {
          challengeId: challenge.challengeId,
          assertion,
        }),
        PARENT_REQUEST_TIMEOUT_MS,
        appMessages.ui.parentVerificationRetryCurrentTab,
      )

      if (!verifyResponse.ok) {
        throw new Error(appMessages.parentVerificationFailed(verifyResponse.status))
      }

      const verification = (await verifyResponse.json()) as ParentGateVerifyResponse
      setParentGateToken(verification.gateToken)
      setParentFeedback({
        tone: 'success',
        message: ui.parentControlsUnlocked,
      })
      if (source === 'upgrade') {
        setPaywallFeedback({
          tone: 'info',
          message: ui.upgradeVerificationProgress,
        })
      } else {
        setPaywallFeedback(null)
      }

      const settingsResponse = await withTimeout(
        api.getParentSettings(softUserId, verification.gateToken),
        PARENT_REQUEST_TIMEOUT_MS,
        appMessages.ui.parentVerificationRetryCurrentTab,
      )

      if (!settingsResponse.ok) {
        throw new Error(appMessages.loadingParentSettingsFailed(settingsResponse.status))
      }

      const settings = (await settingsResponse.json()) as ParentSettingsResponse
      setProfile((current) => ({
        ...current,
        notificationsEnabled: settings.notificationsEnabled,
        analyticsEnabled: settings.analyticsEnabled,
      }))
      setKidShelfEnabled(settings.kidShelfEnabled)
      return verification.gateToken
    } catch (parentError) {
      const message =
        parentError instanceof Error ? parentError.message : appMessages.unableToUnlockParentSettings
      const feedback: InlineFeedback = {
        tone: 'error',
        message: formatParentVerificationError(message, parentVerificationReadiness),
      }
      setParentFeedback(feedback)
      if (source === 'upgrade') {
        setPaywallFeedback(feedback)
      }
      return null
    } finally {
      setIsUnlockingParent(false)
    }
  }

  const updateParentSettings = async (
    notificationsEnabled: boolean,
    analyticsEnabled: boolean,
    nextKidShelfEnabled: boolean,
  ) => {
    if (!parentGateToken) {
      setParentFeedback({
        tone: 'error',
        message: ui.verifyToChange,
      })
      return
    }

    const previousProfile = profile
    const previousKidShelfEnabled = kidShelfEnabled

    setParentFeedback(null)
    setProfile((current) => ({
      ...current,
      notificationsEnabled,
      analyticsEnabled,
    }))
    setKidShelfEnabled(nextKidShelfEnabled)

    try {
      const softUserId = getSoftUserId()
      const response = await api.updateParentSettings(softUserId, {
        gateToken: parentGateToken,
        notificationsEnabled,
        analyticsEnabled,
        kidShelfEnabled: nextKidShelfEnabled,
      })

      if (!response.ok) {
        throw new Error(appMessages.updatingParentSettingsFailed(response.status))
      }

      const updated = (await response.json()) as ParentSettingsResponse
      setProfile((current) => ({
        ...current,
        notificationsEnabled: updated.notificationsEnabled,
        analyticsEnabled: updated.analyticsEnabled,
      }))
      setKidShelfEnabled(updated.kidShelfEnabled)
      setParentFeedback(null)
      await refreshLibrary(softUserId)
    } catch (settingsError) {
      setProfile((current) => ({
        ...current,
        notificationsEnabled: previousProfile.notificationsEnabled,
        analyticsEnabled: previousProfile.analyticsEnabled,
      }))
      setKidShelfEnabled(previousKidShelfEnabled)
      const message =
        settingsError instanceof Error ? settingsError.message : appMessages.unableToUpdateParentSettings
      setParentFeedback({
        tone: 'error',
        message,
      })
    }
  }

  const upgradeSubscription = async () => {
    if (!paywall) {
      return
    }

    const verifiedGateToken = parentGateToken ?? (await unlockParentSettings('upgrade'))
    if (!verifiedGateToken) {
      return
    }

    setIsUpgradingSubscription(true)
    setPaywallFeedback(null)
    setCheckoutFeedback(null)

    try {
      const softUserId = getSoftUserId()
      const sessionResponse = await api.createCheckoutSession(softUserId, {
        gateToken: verifiedGateToken,
        upgradeTier: paywall.upgradeTier,
        returnUrl: buildCheckoutReturnUrl(),
      })

      if (!sessionResponse.ok) {
        throw new Error(appMessages.checkoutSessionFailed(sessionResponse.status))
      }

      const checkoutSession = (await sessionResponse.json()) as SubscriptionCheckoutSessionResponse
      const checkoutUrlRaw = checkoutSession.checkoutUrl.trim()
      if (checkoutUrlRaw.length === 0) {
        throw new Error(appMessages.unableToCompleteUpgradeCheckout)
      }

      let checkoutUrl: URL
      try {
        checkoutUrl = new URL(checkoutUrlRaw, window.location.origin)
      } catch {
        throw new Error(appMessages.unableToCompleteUpgradeCheckout)
      }

      if (checkoutUrl.protocol !== 'https:' && checkoutUrl.protocol !== 'http:') {
        throw new Error(appMessages.unableToCompleteUpgradeCheckout)
      }
      if (checkoutUrl.username || checkoutUrl.password) {
        throw new Error(appMessages.unableToCompleteUpgradeCheckout)
      }

      persistPendingCheckoutSession({
        softUserId,
        gateToken: verifiedGateToken,
        sessionId: checkoutSession.sessionId,
        expectedTier: paywall.upgradeTier,
      })
      window.location.assign(checkoutUrl.toString())
    } catch (checkoutError) {
      const message =
        checkoutError instanceof Error
          ? checkoutError.message
          : appMessages.unableToCompleteUpgradeCheckout
      setPaywallFeedback({
        tone: 'error',
        message,
      })
    } finally {
      setIsUpgradingSubscription(false)
    }
  }

  const displayedRecent = kidShelfEnabled
    ? libraryRecentIds
        .map((storyId) => storiesById.get(storyId))
        .filter((story): story is StoryArtifact => Boolean(story))
    : configuredLibraryRecentLimit
      ? stories.slice(0, configuredLibraryRecentLimit)
      : stories
  const seriesSupportMessage =
    librarySyncState === 'error' && seriesOptions.length > 0 ? ui.seriesSyncHint : null
  const visibleSeriesOptions = librarySyncState === 'error' ? [] : seriesOptions
  const generationButtonLabel = mode === storyModes.oneShot
    ? ui.generateOneShot
    : selectedSeriesId !== 'new'
      ? ui.continueSeries
      : ui.generateStory
  const upgradeButtonLabel = !paywall || parentGateToken
    ? paywall
      ? ui.confirmUpgrade(paywall.upgradeTier)
      : ''
    : ui.verifyParentToContinueUpgrade
  const paywallFeedbackIcon =
    paywallFeedback?.tone === 'success'
      ? '✨'
      : paywallFeedback?.tone === 'info'
        ? '⏳'
        : '⚠️'
  const isUpgradeActionBusy = isUnlockingParent || isUpgradingSubscription
  return (
    <main className="app-shell" data-testid="app-shell">
      <header className="header" data-testid="app-header">
        <div className="header-brand">
          <img alt="" aria-hidden="true" className="header-brand-icon" src={iconUrl} />
          <h1>{ui.appTitle}</h1>
          {homeStatus.defaultTier ? (
            <span className="tier-badge" data-testid="tier-badge">
              {ui.tierBadge(homeStatus.defaultTier)}
            </span>
          ) : null}
          {kidShelfEnabled ? (
            <span className="tier-badge" data-testid="kid-shelf-indicator">
              {ui.kidShelf}
            </span>
          ) : null}
        </div>
      </header>

      {!kidShelfEnabled && (
        <div className="controls-row">
          <QuickGenerateCard
            durationMinutes={durationMinutes}
            error={error ? (isNetworkError(error) ? ui.errorFriendly : error) : null}
            feedback={generationFeedback}
            generateButtonLabel={generationButtonLabel}
            homeStatus={homeStatus}
            isGenerating={isGenerating}
            isOneShotDetailsExpanded={isOneShotDetailsExpanded}
            mode={mode}
            onChildNameChange={(childName) => setProfile((current) => ({ ...current, childName }))}
            onDurationChange={setDurationMinutes}
            onGenerate={() => {
              void onGenerate()
            }}
            onModeChange={setMode}
            onOneShotDetailsExpandedChange={setIsOneShotDetailsExpanded}
            onSelectedSeriesIdChange={setSelectedSeriesId}
            onOneShotChange={(key, value) =>
              setOneShotCustomization((current) => ({
                ...current,
                [key]: value,
              }))
            }
            onReducedMotionChange={(reducedMotion) =>
              setProfile((current) => ({ ...current, reducedMotion }))
            }
            oneShotCustomization={oneShotCustomization}
            profile={profile}
            selectedSeriesId={selectedSeriesId}
            selectedSeriesLabel={selectedSeriesOption?.label ?? null}
            seriesOptions={visibleSeriesOptions}
            seriesSupportMessage={seriesSupportMessage}
            ui={ui}
            visible={homeStatus.quickGenerateVisible}
          />

          <ParentControlsSection
            kidShelfEnabled={kidShelfEnabled}
            hasParentGateToken={Boolean(parentGateToken)}
            errorMessage={parentFeedback?.tone === 'error' ? parentFeedback.message : null}
            isParentVerificationSupported={parentVerificationReadiness.isSupported}
            isUnlockingParent={isUnlockingParent}
            onKidShelfChange={(nextKidShelfEnabled) => {
              void updateParentSettings(profile.notificationsEnabled, profile.analyticsEnabled, nextKidShelfEnabled)
            }}
            onAnalyticsChange={(analyticsEnabled) => {
              void updateParentSettings(profile.notificationsEnabled, analyticsEnabled, kidShelfEnabled)
            }}
            onNotificationsChange={(notificationsEnabled) => {
              void updateParentSettings(notificationsEnabled, profile.analyticsEnabled, kidShelfEnabled)
            }}
            onUnlockParentSettings={() => {
              void unlockParentSettings()
            }}
            parentVerificationHint={parentVerificationReadiness.hint}
            statusMessage={parentFeedback?.tone !== 'error' ? parentFeedback?.message ?? null : null}
            profile={profile}
            supportActionHref={
              parentVerificationReadiness.isSupported ? null : parentVerificationReadiness.preferredUrl
            }
            supportActionLabel={
              parentVerificationReadiness.isSupported ? null : ui.openLocalhostVersion
            }
            ui={ui}
            visible={homeStatus.parentControlsEnabled}
          />
        </div>
      )}

      {kidShelfEnabled && error ? (
        <div className="error-banner error-banner-standalone" data-testid="app-error" role="alert">
          <span aria-hidden="true" className="error-banner-icon">⚠️</span>
          <span className="error-banner-text">
            {isNetworkError(error) ? ui.errorFriendly : error}
          </span>
        </div>
      ) : null}

      {!kidShelfEnabled && paywall ? (
        <section aria-label={ui.upgradePaywallAria} className="shelf paywall" data-testid="upgrade-paywall">
          <div className="paywall-header">
            <div className="paywall-copy">
              <h3>{ui.unlockLongerStories}</h3>
              <p className="paywall-lead">{paywall.message}</p>
            </div>
            <span className="tier-badge paywall-badge">{ui.tierBadge(paywall.upgradeTier)}</span>
          </div>
          <div className="paywall-meta">
            <div className="paywall-stat">
              <span className="paywall-stat-label">{ui.currentTier}</span>
              <strong>{paywall.currentTier}</strong>
              <span className="paywall-stat-caption">{ui.paywallDurationLimit(paywall.maxDurationMinutes)}</span>
            </div>
            <div className="paywall-stat">
              <span className="paywall-stat-label">{ui.upgradePath}</span>
              <code className="paywall-route">
                {paywall.upgradeUrl.trim().length > 0 ? paywall.upgradeUrl : ui.upgradeRouteManaged}
              </code>
              <span className="paywall-stat-caption">{ui.upgradeFlowHint}</span>
            </div>
          </div>
          <div className="paywall-actions">
            <button
              data-testid="confirm-upgrade-button"
              disabled={isUpgradeActionBusy}
              onClick={upgradeSubscription}
              type="button"
            >
              {isUpgradeActionBusy ? (
                <span className="btn-spinner">
                  <span aria-hidden="true" className="spinner-icon" />
                  {isUnlockingParent ? ui.verifyingParent : ui.upgrading}
                </span>
              ) : (
                upgradeButtonLabel
              )}
            </button>
            {paywallFeedback ? (
              <div
                className={`feedback-banner feedback-banner-${paywallFeedback.tone}`}
                data-testid="paywall-feedback"
                role={paywallFeedback.tone === 'error' ? 'alert' : 'status'}
              >
                <span aria-hidden="true" className="feedback-banner-icon">{paywallFeedbackIcon}</span>
                <span className="feedback-banner-text">{paywallFeedback.message}</span>
              </div>
            ) : null}
          </div>
        </section>
      ) : null}

      {librarySyncState === 'error' ? (
        <section aria-live="polite" className="sync-banner" data-testid="library-sync-banner">
          <div className="sync-banner-copy">
            <strong>{ui.librarySyncTitle}</strong>
            <p>{ui.librarySyncBody}</p>
          </div>
          <button
            className="btn-secondary sync-banner-action"
            onClick={() => {
              void refreshLibrary(getSoftUserId())
            }}
            type="button"
          >
            {ui.retryLoadingStories}
          </button>
        </section>
      ) : null}

      {checkoutFeedback ? (
        <section
          aria-live="polite"
          className={`feedback-banner feedback-banner-${checkoutFeedback.tone} feedback-banner-standalone`}
          data-testid="checkout-feedback"
          role={checkoutFeedback.tone === 'error' ? 'alert' : 'status'}
        >
          <span aria-hidden="true" className="feedback-banner-icon">
            {checkoutFeedback.tone === 'success' ? '✨' : '⚠️'}
          </span>
          <span className="feedback-banner-text">{checkoutFeedback.message}</span>
        </section>
      ) : null}

      <RecentStoriesShelf
        approvalHint={approvalHint}
        approvalLocked={!parentGateToken}
        approvingStoryId={approvingStoryId}
        favoritingStoryId={favoritingStoryId}
        highlightedStoryId={highlightedStoryId}
        storyFeedbackMessage={storyActionFeedback?.message ?? null}
        storyFeedbackStoryId={storyActionFeedback?.storyId ?? null}
        getLayerStyle={getLayerStyle}
        onApproveStory={(storyId) => {
          void approveStory(storyId)
        }}
        onToggleFavorite={(storyId) => {
          void toggleFavorite(storyId)
        }}
        stories={displayedRecent}
        ui={ui}
      />

      <FavoritesShelf
        approvalHint={approvalHint}
        approvalLocked={!parentGateToken}
        approvingStoryId={approvingStoryId}
        favoritingStoryId={favoritingStoryId}
        highlightedStoryId={highlightedStoryId}
        storyFeedbackMessage={storyActionFeedback?.message ?? null}
        storyFeedbackStoryId={storyActionFeedback?.storyId ?? null}
        getLayerStyle={getLayerStyle}
        onApproveStory={(storyId) => {
          void approveStory(storyId)
        }}
        onToggleFavorite={(storyId) => {
          void toggleFavorite(storyId)
        }}
        stories={favorites}
        ui={ui}
      />
    </main>
  )
}

export default App
