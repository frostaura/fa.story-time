export type WebAuthnUserVerificationRequirement = 'required' | 'preferred' | 'discouraged'
export type WebAuthnAttestationConveyancePreference = 'none' | 'indirect' | 'direct' | 'enterprise'
export type WebAuthnResidentKeyRequirement = 'required' | 'preferred' | 'discouraged'

type StoryTimeWindow = Window & {
  __STORYTIME_API_BASE_URL__?: string
}

export type RuntimeMessageOverrides = {
  ui: Record<string, string>
} & Record<string, string | Record<string, string>>

const toOptionalNumber = (value: unknown): number | null => {
  if (typeof value !== 'string' || value.trim().length === 0) {
    return null
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

const toOptionalString = (value: unknown): string | null => {
  if (typeof value !== 'string') {
    return null
  }

  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}

const toOptionalRoutePath = (value: unknown): string | null => {
  const path = toOptionalString(value)
  if (path === null) {
    return null
  }

  return path.startsWith('/') ? path : `/${path}`
}

const toOptionalRoutePathList = (value: unknown): string[] | null => {
  const raw = toOptionalString(value)
  if (raw === null) {
    return null
  }

  const normalized = raw
    .split(',')
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0)
    .map((entry) => (entry.startsWith('/') ? entry : `/${entry}`))

  if (normalized.length === 0) {
    return null
  }

  return [...new Set(normalized)]
}

const toOptionalPositiveInt = (value: unknown): number | null => {
  const parsed = toOptionalNumber(value)
  if (parsed === null || parsed <= 0) {
    return null
  }

  return Math.floor(parsed)
}

const toOptionalInt = (value: unknown): number | null => {
  const parsed = toOptionalNumber(value)
  if (parsed === null || !Number.isInteger(parsed)) {
    return null
  }

  return parsed
}

const toOptionalBoolean = (value: unknown): boolean | null => {
  if (typeof value !== 'string') {
    return null
  }

  const normalized = value.trim().toLowerCase()
  if (normalized === 'true') {
    return true
  }

  if (normalized === 'false') {
    return false
  }

  return null
}

const toOptionalPositiveNumber = (value: unknown): number | null => {
  const parsed = toOptionalNumber(value)
  if (parsed === null || parsed <= 0) {
    return null
  }

  return parsed
}

const parseRuntimeMessageOverrides = (value: unknown): RuntimeMessageOverrides => {
  const raw = toOptionalString(value)
  if (raw === null) {
    return { ui: {} }
  }

  let parsed: unknown
  try {
    parsed = JSON.parse(raw)
  } catch (error) {
    if (error instanceof SyntaxError) {
      return { ui: {} }
    }

    throw error
  }

  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
    return { ui: {} }
  }

  const source = parsed as Record<string, unknown>
  const topLevel: Record<string, string | Record<string, string>> = {}
  for (const [key, entry] of Object.entries(source)) {
    if (key === 'ui') {
      continue
    }

    if (typeof entry === 'string' && entry.trim().length > 0) {
      topLevel[key] = entry.trim()
    }
  }

  const uiSource = source.ui
  const ui: Record<string, string> = {}
  if (uiSource !== null && typeof uiSource === 'object' && !Array.isArray(uiSource)) {
    for (const [key, entry] of Object.entries(uiSource as Record<string, unknown>)) {
      if (typeof entry === 'string' && entry.trim().length > 0) {
        ui[key] = entry.trim()
      }
    }
  }

  return {
    ...topLevel,
    ui,
  }
}

const throwMissingRuntimeConfig = (key: string): never => {
  throw new Error(`Missing or invalid runtime config: ${key}`)
}

const requireString = (value: unknown, key: string): string =>
  toOptionalString(value) ?? throwMissingRuntimeConfig(key)

const requireRoutePath = (value: unknown, key: string): string =>
  toOptionalRoutePath(value) ?? throwMissingRuntimeConfig(key)

const requireRoutePathList = (value: unknown, key: string): readonly string[] =>
  toOptionalRoutePathList(value) ?? throwMissingRuntimeConfig(key)

const requireNumber = (value: unknown, key: string): number =>
  toOptionalNumber(value) ?? throwMissingRuntimeConfig(key)

const requireInt = (value: unknown, key: string): number =>
  toOptionalInt(value) ?? throwMissingRuntimeConfig(key)

const requirePositiveInt = (value: unknown, key: string): number =>
  toOptionalPositiveInt(value) ?? throwMissingRuntimeConfig(key)

const requirePositiveNumber = (value: unknown, key: string): number =>
  toOptionalPositiveNumber(value) ?? throwMissingRuntimeConfig(key)

const requireBoolean = (value: unknown, key: string): boolean =>
  toOptionalBoolean(value) ?? throwMissingRuntimeConfig(key)

const toUserVerificationRequirement = (
  value: string,
): WebAuthnUserVerificationRequirement => {
  if (value === 'required' || value === 'preferred' || value === 'discouraged') {
    return value
  }

  return throwMissingRuntimeConfig('VITE_PARENT_GATE_WEBAUTHN_USER_VERIFICATION')
}

const toAttestationPreference = (
  value: string,
): WebAuthnAttestationConveyancePreference => {
  if (value === 'none' || value === 'indirect' || value === 'direct' || value === 'enterprise') {
    return value
  }

  return throwMissingRuntimeConfig('VITE_PARENT_GATE_WEBAUTHN_ATTESTATION')
}

const toResidentKeyRequirement = (
  value: string,
): WebAuthnResidentKeyRequirement => {
  if (value === 'required' || value === 'preferred' || value === 'discouraged') {
    return value
  }

  return throwMissingRuntimeConfig('VITE_PARENT_GATE_WEBAUTHN_RESIDENT_KEY')
}

const storiesBaseRoute = requireRoutePath(import.meta.env.VITE_API_ROUTE_STORIES_BASE, 'VITE_API_ROUTE_STORIES_BASE')
const subscriptionBaseRoute = requireRoutePath(
  import.meta.env.VITE_API_ROUTE_SUBSCRIPTION_BASE,
  'VITE_API_ROUTE_SUBSCRIPTION_BASE',
)
const parentBaseRoute = requireRoutePath(import.meta.env.VITE_API_ROUTE_PARENT_BASE, 'VITE_API_ROUTE_PARENT_BASE')
const libraryBaseRoute = requireRoutePath(import.meta.env.VITE_API_ROUTE_LIBRARY_BASE, 'VITE_API_ROUTE_LIBRARY_BASE')
const configuredWebAuthnUserIdMaxLength = requirePositiveInt(
  import.meta.env.VITE_PARENT_GATE_WEBAUTHN_USER_ID_MAX_LENGTH,
  'VITE_PARENT_GATE_WEBAUTHN_USER_ID_MAX_LENGTH',
)
const configuredWebAuthnUserIdProtocolMaxLength = requirePositiveInt(
  import.meta.env.VITE_PARENT_GATE_WEBAUTHN_USER_ID_PROTOCOL_MAX_LENGTH,
  'VITE_PARENT_GATE_WEBAUTHN_USER_ID_PROTOCOL_MAX_LENGTH',
)

if (configuredWebAuthnUserIdMaxLength > configuredWebAuthnUserIdProtocolMaxLength) {
  throwMissingRuntimeConfig('VITE_PARENT_GATE_WEBAUTHN_USER_ID_MAX_LENGTH')
}

export const runtimeConfig = Object.freeze({
  apiRoutes: Object.freeze({
    homeStatus: requireRoutePath(import.meta.env.VITE_API_ROUTE_HOME_STATUS, 'VITE_API_ROUTE_HOME_STATUS'),
    storyGenerate: requireRoutePath(
      import.meta.env.VITE_API_ROUTE_STORIES_GENERATE,
      'VITE_API_ROUTE_STORIES_GENERATE',
    ),
    storiesBase: storiesBaseRoute,
    subscriptionBase: subscriptionBaseRoute,
    parentBase: parentBaseRoute,
    libraryBase: libraryBaseRoute,
  }),
  durationMinMinutes: requireNumber(import.meta.env.VITE_DEFAULT_DURATION_MINUTES, 'VITE_DEFAULT_DURATION_MINUTES'),
  durationMaxMinutes: requireNumber(
    import.meta.env.VITE_DEFAULT_DURATION_MAX_MINUTES,
    'VITE_DEFAULT_DURATION_MAX_MINUTES',
  ),
  durationSelection: requireNumber(
    import.meta.env.VITE_DEFAULT_DURATION_SELECTION,
    'VITE_DEFAULT_DURATION_SELECTION',
  ),
  defaultChildName: requireString(import.meta.env.VITE_DEFAULT_CHILD_NAME, 'VITE_DEFAULT_CHILD_NAME'),
  homeStatusFallback: Object.freeze({
    quickGenerateVisible: requireBoolean(
      import.meta.env.VITE_HOME_STATUS_QUICK_GENERATE_VISIBLE,
      'VITE_HOME_STATUS_QUICK_GENERATE_VISIBLE',
    ),
    durationSliderVisible: requireBoolean(
      import.meta.env.VITE_HOME_STATUS_DURATION_SLIDER_VISIBLE,
      'VITE_HOME_STATUS_DURATION_SLIDER_VISIBLE',
    ),
    parentControlsEnabled: requireBoolean(
      import.meta.env.VITE_HOME_STATUS_PARENT_CONTROLS_ENABLED,
      'VITE_HOME_STATUS_PARENT_CONTROLS_ENABLED',
    ),
  }),
  profileDefaults: Object.freeze({
    notificationsEnabled: requireBoolean(
      import.meta.env.VITE_DEFAULT_NOTIFICATIONS_ENABLED,
      'VITE_DEFAULT_NOTIFICATIONS_ENABLED',
    ),
    analyticsEnabled: requireBoolean(import.meta.env.VITE_DEFAULT_ANALYTICS_ENABLED, 'VITE_DEFAULT_ANALYTICS_ENABLED'),
  }),
  serviceWorkerPath: requireString(import.meta.env.VITE_SERVICE_WORKER_PATH, 'VITE_SERVICE_WORKER_PATH'),
  serviceWorkerCacheName: requireString(
    import.meta.env.VITE_SERVICE_WORKER_CACHE_NAME,
    'VITE_SERVICE_WORKER_CACHE_NAME',
  ),
  serviceWorkerAppShell: requireRoutePathList(
    import.meta.env.VITE_SERVICE_WORKER_APP_SHELL,
    'VITE_SERVICE_WORKER_APP_SHELL',
  ),
  libraryRecentLimit: requireNumber(import.meta.env.VITE_LIBRARY_RECENT_LIMIT, 'VITE_LIBRARY_RECENT_LIMIT'),
  storageKeys: Object.freeze({
    storyArtifacts: requireString(
      import.meta.env.VITE_STORAGE_KEY_STORY_ARTIFACTS,
      'VITE_STORAGE_KEY_STORY_ARTIFACTS',
    ),
    softUserId: requireString(import.meta.env.VITE_STORAGE_KEY_SOFT_USER_ID, 'VITE_STORAGE_KEY_SOFT_USER_ID'),
    childProfile: requireString(
      import.meta.env.VITE_STORAGE_KEY_CHILD_PROFILE,
      'VITE_STORAGE_KEY_CHILD_PROFILE',
    ),
    parentCredential: requireString(
      import.meta.env.VITE_STORAGE_KEY_PARENT_CREDENTIAL,
      'VITE_STORAGE_KEY_PARENT_CREDENTIAL',
    ),
  }),
  parentGate: Object.freeze({
    rpDisplayName: requireString(
      import.meta.env.VITE_PARENT_GATE_WEBAUTHN_RP_DISPLAY_NAME,
      'VITE_PARENT_GATE_WEBAUTHN_RP_DISPLAY_NAME',
    ),
    userDisplayName: requireString(
      import.meta.env.VITE_PARENT_GATE_WEBAUTHN_USER_DISPLAY_NAME,
      'VITE_PARENT_GATE_WEBAUTHN_USER_DISPLAY_NAME',
    ),
    timeoutMs: requirePositiveInt(
      import.meta.env.VITE_PARENT_GATE_WEBAUTHN_TIMEOUT_MS,
      'VITE_PARENT_GATE_WEBAUTHN_TIMEOUT_MS',
    ),
    userIdMaxLength: configuredWebAuthnUserIdMaxLength,
    userNamePrefix: requireString(
      import.meta.env.VITE_PARENT_GATE_WEBAUTHN_USER_NAME_PREFIX,
      'VITE_PARENT_GATE_WEBAUTHN_USER_NAME_PREFIX',
    ),
    attestation: toAttestationPreference(
      requireString(
        import.meta.env.VITE_PARENT_GATE_WEBAUTHN_ATTESTATION,
        'VITE_PARENT_GATE_WEBAUTHN_ATTESTATION',
      ),
    ),
    residentKey: toResidentKeyRequirement(
      requireString(
        import.meta.env.VITE_PARENT_GATE_WEBAUTHN_RESIDENT_KEY,
        'VITE_PARENT_GATE_WEBAUTHN_RESIDENT_KEY',
      ),
    ),
    coseAlgorithm: requireInt(
      import.meta.env.VITE_PARENT_GATE_WEBAUTHN_COSE_ALGORITHM,
      'VITE_PARENT_GATE_WEBAUTHN_COSE_ALGORITHM',
    ),
    userVerification: toUserVerificationRequirement(
      requireString(
        import.meta.env.VITE_PARENT_GATE_WEBAUTHN_USER_VERIFICATION,
        'VITE_PARENT_GATE_WEBAUTHN_USER_VERIFICATION',
      ),
    ),
    credentialKind: requireString(
      import.meta.env.VITE_PARENT_GATE_CREDENTIAL_KIND,
      'VITE_PARENT_GATE_CREDENTIAL_KIND',
    ),
    assertionType: requireString(
      import.meta.env.VITE_PARENT_GATE_ASSERTION_TYPE,
      'VITE_PARENT_GATE_ASSERTION_TYPE',
    ),
  }),
  posterParallax: Object.freeze({
    minAnimationDurationSeconds: requirePositiveNumber(
      import.meta.env.VITE_POSTER_PARALLAX_MIN_DURATION_SECONDS,
      'VITE_POSTER_PARALLAX_MIN_DURATION_SECONDS',
    ),
    durationDivisor: requirePositiveNumber(
      import.meta.env.VITE_POSTER_PARALLAX_DURATION_DIVISOR,
      'VITE_POSTER_PARALLAX_DURATION_DIVISOR',
    ),
    maxAnimationDelaySeconds: requirePositiveNumber(
      import.meta.env.VITE_POSTER_PARALLAX_MAX_DELAY_SECONDS,
      'VITE_POSTER_PARALLAX_MAX_DELAY_SECONDS',
    ),
    delayDivisor: requirePositiveNumber(
      import.meta.env.VITE_POSTER_PARALLAX_DELAY_DIVISOR,
      'VITE_POSTER_PARALLAX_DELAY_DIVISOR',
    ),
    depthScale: requirePositiveNumber(
      import.meta.env.VITE_POSTER_PARALLAX_DEPTH_SCALE,
      'VITE_POSTER_PARALLAX_DEPTH_SCALE',
    ),
  }),
  messages: Object.freeze(parseRuntimeMessageOverrides(import.meta.env.VITE_APP_MESSAGES_JSON)),
})

export const EMPTY_ONE_SHOT_DEFAULTS = Object.freeze({
  arcName: '',
  companionName: '',
  setting: '',
  mood: '',
  themeTrackId: '',
  narrationStyle: '',
})

export const resolveApiBaseUrl = (): string => {
  if (typeof window !== 'undefined') {
    const fromWindow = (window as StoryTimeWindow).__STORYTIME_API_BASE_URL__
    if (typeof fromWindow === 'string' && fromWindow.trim().length > 0) {
      return fromWindow.trim()
    }
  }

  const fromEnv = import.meta.env.VITE_API_BASE_URL
  if (typeof fromEnv === 'string' && fromEnv.trim().length > 0) {
    return fromEnv.trim()
  }

  if (
    typeof window !== 'undefined' &&
    typeof window.location.origin === 'string' &&
    window.location.origin.trim().length > 0
  ) {
    return window.location.origin.trim()
  }

  return ''
}
