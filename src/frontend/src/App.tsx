import { type CSSProperties, useCallback, useEffect, useMemo, useState } from 'react'
import './App.css'
import { FavoritesShelf } from './components/FavoritesShelf'
import { ParentControlsSection } from './components/ParentControlsSection'
import { QuickGenerateCard, type OneShotCustomization } from './components/QuickGenerateCard'
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
      pubKeyCredParams: [{ type: 'public-key', alg: runtimeConfig.parentGate.coseAlgorithm }],
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

function App() {
  const apiBaseUrl = resolveApiBaseUrl()
  const api = useMemo(() => createStoryTimeApi(apiBaseUrl), [apiBaseUrl])
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
  const [libraryRecentIds, setLibraryRecentIds] = useState<string[]>([])
  const [libraryFavoriteIds, setLibraryFavoriteIds] = useState<string[]>([])
  const [kidShelfEnabled, setKidShelfEnabled] = useState(false)
  const [isGenerating, setIsGenerating] = useState(false)
  const [isUnlockingParent, setIsUnlockingParent] = useState(false)
  const [isUpgradingSubscription, setIsUpgradingSubscription] = useState(false)
  const [parentGateToken, setParentGateToken] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [paywall, setPaywall] = useState<SubscriptionPaywallResponse | null>(null)

  const storiesById = useMemo(() => new Map(stories.map((story) => [story.storyId, story])), [stories])
  const favorites = useMemo(() => {
    if (!kidShelfEnabled) {
      return stories.filter((story) => story.isFavorite)
    }

    return libraryFavoriteIds
      .map((storyId) => storiesById.get(storyId))
      .filter((story): story is StoryArtifact => Boolean(story))
  }, [kidShelfEnabled, libraryFavoriteIds, stories, storiesById])

  const refreshLibrary = useCallback(async (softUserId: string, kidMode: boolean) => {
    const response = await api.getLibrary(softUserId, kidMode)
    if (!response.ok) {
      throw new Error(appMessages.loadingLibraryFailed(response.status))
    }

    const library = (await response.json()) as LibraryResponse
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
  }, [api])

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
          throw new Error(appMessages.homeStatusFailed(response.status))
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
    void refreshLibrary(softUserId, kidShelfEnabled).catch((libraryError) => {
      const message = libraryError instanceof Error ? libraryError.message : appMessages.unableToLoadLibrary
      setError(message)
    })
  }, [kidShelfEnabled, refreshLibrary])

  const onGenerate = async () => {
    setIsGenerating(true)
    setError(null)
    setPaywall(null)

    try {
      const softUserId = getSoftUserId()
      const continuationSeriesId =
        mode === storyModes.series
          ? stories.find((story) => story.mode === storyModes.series && typeof story.seriesId === 'string')?.seriesId
          : undefined
      const response = await api.generateStory({
        softUserId,
        childName: profile.childName,
        mode,
        durationMinutes,
        seriesId: continuationSeriesId,
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
      })

      if (!response.ok) {
        if (response.status === 402) {
          const rejected = (await response.json()) as {
            error?: string
            paywall?: SubscriptionPaywallResponse
          }
          setPaywall(rejected.paywall ?? null)
          throw new Error(rejected.error ?? appMessages.upgradeRequiredForLength)
        }

        throw new Error(appMessages.generationFailed(response.status))
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
      await refreshLibrary(softUserId, kidShelfEnabled)
    } catch (generationError) {
      const message = generationError instanceof Error ? generationError.message : appMessages.unknownGenerationError
      setError(message)
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
      await refreshLibrary(getSoftUserId(), kidShelfEnabled)
    } catch (favoriteError) {
      const message = favoriteError instanceof Error ? favoriteError.message : appMessages.unableToUpdateFavorite
      setError(message)
    }
  }

  const approveStory = async (storyId: string) => {
    try {
      const response = await api.approveStory(storyId)

      if (!response.ok) {
        throw new Error(appMessages.approvalFailed(response.status))
      }

      const approval = (await response.json()) as StoryApprovalResponse

      setStories((existing) =>
        existing.map((story) =>
          story.storyId === storyId
            ? { ...story, fullAudioReady: approval.fullAudioReady, fullAudio: approval.fullAudio }
            : story,
        ),
      )
      await refreshLibrary(getSoftUserId(), kidShelfEnabled)
    } catch (approvalError) {
      const message = approvalError instanceof Error ? approvalError.message : appMessages.unableToApproveStory
      setError(message)
    }
  }

  const unlockParentSettings = async () => {
    setIsUnlockingParent(true)
    setError(null)

    try {
      const softUserId = getSoftUserId()
      const challengeResponse = await api.createParentGateChallenge(softUserId)

      if (!challengeResponse.ok) {
        throw new Error(appMessages.parentChallengeFailed(challengeResponse.status))
      }

      const challenge = (await challengeResponse.json()) as ParentGateChallengeResponse
      const assertion = await createParentAssertion(softUserId, challenge.challenge, challenge.rpId)
      const verifyResponse = await api.verifyParentGate(softUserId, {
        challengeId: challenge.challengeId,
        assertion,
      })

      if (!verifyResponse.ok) {
        throw new Error(appMessages.parentVerificationFailed(verifyResponse.status))
      }

      const verification = (await verifyResponse.json()) as ParentGateVerifyResponse
      setParentGateToken(verification.gateToken)

      const settingsResponse = await api.getParentSettingsWithToken(softUserId, verification.gateToken)

      if (!settingsResponse.ok) {
        throw new Error(appMessages.loadingParentSettingsFailed(settingsResponse.status))
      }

      const settings = (await settingsResponse.json()) as ParentSettingsResponse
      setProfile((current) => ({
        ...current,
        notificationsEnabled: settings.notificationsEnabled,
        analyticsEnabled: settings.analyticsEnabled,
      }))
    } catch (parentError) {
      const message =
        parentError instanceof Error ? parentError.message : appMessages.unableToUnlockParentSettings
      setError(message)
    } finally {
      setIsUnlockingParent(false)
    }
  }

  const updateParentSettings = async (
    notificationsEnabled: boolean,
    analyticsEnabled: boolean,
  ) => {
    if (!parentGateToken) {
      setError(appMessages.unlockParentSettingsFirst)
      return
    }

    try {
      const softUserId = getSoftUserId()
      const response = await api.updateParentSettings(softUserId, {
        gateToken: parentGateToken,
        notificationsEnabled,
        analyticsEnabled,
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
    } catch (settingsError) {
      const message =
        settingsError instanceof Error ? settingsError.message : appMessages.unableToUpdateParentSettings
      setError(message)
    }
  }

  const upgradeSubscription = async () => {
    if (!paywall) {
      return
    }

    if (!parentGateToken) {
      setError(appMessages.unlockBeforeUpgrade)
      return
    }

    setIsUpgradingSubscription(true)
    setError(null)

    try {
      const softUserId = getSoftUserId()
      const sessionResponse = await api.createCheckoutSession(softUserId, {
        gateToken: parentGateToken,
        upgradeTier: paywall.upgradeTier,
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

      const completeResponse = await api.completeCheckoutSession(softUserId, {
        gateToken: parentGateToken,
        sessionId: checkoutSession.sessionId,
      })

      if (!completeResponse.ok) {
        throw new Error(appMessages.checkoutCompletionFailed(completeResponse.status))
      }

      const completion = (await completeResponse.json()) as SubscriptionCheckoutCompleteResponse
      if (completion.currentTier !== paywall.upgradeTier) {
        throw new Error(appMessages.unexpectedUpgradeTier)
      }

      setHomeStatus((current) => ({ ...current, defaultTier: completion.currentTier }))
      setPaywall(null)
    } catch (checkoutError) {
      const message =
        checkoutError instanceof Error
          ? checkoutError.message
          : appMessages.unableToCompleteUpgradeCheckout
      setError(message)
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
  const ui = appMessages.ui

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
        </div>
        <label className="toggle">
          <input
            aria-label={ui.kidShelf}
            checked={kidShelfEnabled}
            data-testid="kid-shelf-toggle"
            onChange={(event) => setKidShelfEnabled(event.target.checked)}
            type="checkbox"
          />
          {ui.kidShelf}
        </label>
      </header>

      {!kidShelfEnabled && (
        <QuickGenerateCard
        durationMinutes={durationMinutes}
        homeStatus={homeStatus}
        isGenerating={isGenerating}
        mode={mode}
        onChildNameChange={(childName) => setProfile((current) => ({ ...current, childName }))}
        onDurationChange={setDurationMinutes}
        onGenerate={() => {
          void onGenerate()
        }}
        onModeChange={setMode}
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
        ui={ui}
        visible={homeStatus.quickGenerateVisible}
      />
      )}

      {!kidShelfEnabled && (
      <ParentControlsSection
        hasParentGateToken={Boolean(parentGateToken)}
        isUnlockingParent={isUnlockingParent}
        onAnalyticsChange={(analyticsEnabled) => {
          void updateParentSettings(profile.notificationsEnabled, analyticsEnabled)
        }}
        onNotificationsChange={(notificationsEnabled) => {
          void updateParentSettings(notificationsEnabled, profile.analyticsEnabled)
        }}
        onUnlockParentSettings={() => {
          void unlockParentSettings()
        }}
        profile={profile}
        ui={ui}
        visible={homeStatus.parentControlsEnabled}
      />
      )}

      {!kidShelfEnabled && error ? (
        <div className="error-banner" data-testid="app-error" role="alert">
          <span aria-hidden="true" className="error-banner-icon">⚠️</span>
          <span className="error-banner-text">
            {isNetworkError(error) ? ui.errorFriendly : error}
          </span>
        </div>
      ) : null}

      {!kidShelfEnabled && paywall ? (
        <section aria-label={ui.upgradePaywallAria} className="shelf paywall" data-testid="upgrade-paywall">
          <h3>{ui.unlockLongerStories}</h3>
          <p>{paywall.message}</p>
          <p>
            {ui.currentTier}: <strong>{paywall.currentTier}</strong> (up to {paywall.maxDurationMinutes} minutes)
          </p>
          <p>
            {ui.upgradePath}: <code>{paywall.upgradeUrl}</code>
          </p>
          <button
            data-testid="confirm-upgrade-button"
            disabled={isUpgradingSubscription}
            onClick={upgradeSubscription}
            type="button"
          >
            {isUpgradingSubscription ? ui.upgrading : ui.confirmUpgrade(paywall.upgradeTier)}
          </button>
        </section>
      ) : null}

      <RecentStoriesShelf
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
