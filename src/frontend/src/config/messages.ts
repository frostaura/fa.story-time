import messageDefaults from './messages.defaults.json'
import { runtimeConfig } from './runtime'

type MessageDefaults = {
  ui: Record<string, string>
} & Record<string, string | Record<string, string>>

const messageOverrides = runtimeConfig.messages
const uiOverrides = messageOverrides.ui
const defaults = messageDefaults as MessageDefaults
const defaultUiMessages = defaults.ui

const resolveConfiguredMessage = (
  source: Record<string, string | Record<string, string>>,
  key: string,
  scope: string,
): string => {
  const configuredValue = source[key]
  if (typeof configuredValue === 'string' && configuredValue.length > 0) {
    return configuredValue
  }

  const defaultValue = defaults[key]
  if (typeof defaultValue === 'string' && defaultValue.length > 0) {
    return defaultValue
  }

  throw new Error(`Missing message config: ${scope}.${key}`)
}

const resolveUiMessage = (key: string): string => {
  const configuredValue = uiOverrides[key]
  if (typeof configuredValue === 'string' && configuredValue.length > 0) {
    return configuredValue
  }

  const defaultValue = defaultUiMessages[key]
  if (typeof defaultValue === 'string' && defaultValue.length > 0) {
    return defaultValue
  }

  throw new Error(`Missing message config: ui.${key}`)
}

const withToken = (template: string, token: string, value: string): string =>
  template.replace(token, value)
const withStatus = (template: string, status: number): string =>
  withToken(template, '{status}', `${status}`)

export const appMessages = Object.freeze({
  unableToLoadHomeStatus: resolveConfiguredMessage(messageOverrides, 'unableToLoadHomeStatus', 'messages'),
  unableToLoadLibrary: resolveConfiguredMessage(messageOverrides, 'unableToLoadLibrary', 'messages'),
  upgradeRequiredForLength: resolveConfiguredMessage(messageOverrides, 'upgradeRequiredForLength', 'messages'),
  unknownGenerationError: resolveConfiguredMessage(messageOverrides, 'unknownGenerationError', 'messages'),
  generationTemporarilyUnavailable: resolveConfiguredMessage(
    messageOverrides,
    'generationTemporarilyUnavailable',
    'messages',
  ),
  generationRateLimited: resolveConfiguredMessage(messageOverrides, 'generationRateLimited', 'messages'),
  unableToUpdateFavorite: resolveConfiguredMessage(messageOverrides, 'unableToUpdateFavorite', 'messages'),
  unableToApproveStory: resolveConfiguredMessage(messageOverrides, 'unableToApproveStory', 'messages'),
  approvalTemporarilyUnavailable: resolveConfiguredMessage(
    messageOverrides,
    'approvalTemporarilyUnavailable',
    'messages',
  ),
  webCryptoUnavailable: resolveConfiguredMessage(messageOverrides, 'webCryptoUnavailable', 'messages'),
  webauthnRequiredForVerification: resolveConfiguredMessage(
    messageOverrides,
    'webauthnRequiredForVerification',
    'messages',
  ),
  parentCredentialRegistrationCancelled: resolveConfiguredMessage(
    messageOverrides,
    'parentCredentialRegistrationCancelled',
    'messages',
  ),
  webauthnPublicKeyUnavailable: resolveConfiguredMessage(
    messageOverrides,
    'webauthnPublicKeyUnavailable',
    'messages',
  ),
  parentVerificationCancelled: resolveConfiguredMessage(messageOverrides, 'parentVerificationCancelled', 'messages'),
  unableToUnlockParentSettings: resolveConfiguredMessage(
    messageOverrides,
    'unableToUnlockParentSettings',
    'messages',
  ),
  unableToUpdateParentSettings: resolveConfiguredMessage(
    messageOverrides,
    'unableToUpdateParentSettings',
    'messages',
  ),
  unlockParentSettingsFirst: resolveConfiguredMessage(messageOverrides, 'unlockParentSettingsFirst', 'messages'),
  unlockBeforeUpgrade: resolveConfiguredMessage(messageOverrides, 'unlockBeforeUpgrade', 'messages'),
  unableToCompleteUpgradeCheckout: resolveConfiguredMessage(
    messageOverrides,
    'unableToCompleteUpgradeCheckout',
    'messages',
  ),
  upgradeCheckoutCompleted: resolveConfiguredMessage(messageOverrides, 'upgradeCheckoutCompleted', 'messages'),
  unexpectedUpgradeTier: resolveConfiguredMessage(messageOverrides, 'unexpectedUpgradeTier', 'messages'),
  unableToParseStoryArtifacts: resolveConfiguredMessage(messageOverrides, 'unableToParseStoryArtifacts', 'messages'),
  unableToParseProfile: resolveConfiguredMessage(messageOverrides, 'unableToParseProfile', 'messages'),
  unableToParseParentCredential: resolveConfiguredMessage(
    messageOverrides,
    'unableToParseParentCredential',
    'messages',
  ),
  parentCredentialRegistrationFailed: (status: number) =>
    withStatus(
      resolveConfiguredMessage(messageOverrides, 'parentCredentialRegistrationFailedTemplate', 'messages'),
      status,
    ),
  loadingLibraryFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'loadingLibraryFailedTemplate', 'messages'), status),
  homeStatusFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'homeStatusFailedTemplate', 'messages'), status),
  generationFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'generationFailedTemplate', 'messages'), status),
  favoriteUpdateFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'favoriteUpdateFailedTemplate', 'messages'), status),
  approvalFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'approvalFailedTemplate', 'messages'), status),
  parentChallengeFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'parentChallengeFailedTemplate', 'messages'), status),
  parentVerificationFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'parentVerificationFailedTemplate', 'messages'), status),
  loadingParentSettingsFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'loadingParentSettingsFailedTemplate', 'messages'), status),
  updatingParentSettingsFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'updatingParentSettingsFailedTemplate', 'messages'), status),
  checkoutSessionFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'checkoutSessionFailedTemplate', 'messages'), status),
  checkoutCompletionFailed: (status: number) =>
    withStatus(resolveConfiguredMessage(messageOverrides, 'checkoutCompletionFailedTemplate', 'messages'), status),
  ui: Object.freeze({
    appTitle: resolveUiMessage('appTitle'),
    kidShelf: resolveUiMessage('kidShelf'),
    quickGenerate: resolveUiMessage('quickGenerate'),
    childName: resolveUiMessage('childName'),
    duration: (minutes: number) =>
      withToken(resolveUiMessage('durationTemplate'), '{minutes}', `${minutes}`),
    durationAriaLabel: resolveUiMessage('durationAriaLabel'),
    mode: resolveUiMessage('mode'),
    modeSeries: resolveUiMessage('modeSeries'),
    modeOneShot: resolveUiMessage('modeOneShot'),
    seriesSelection: resolveUiMessage('seriesSelection'),
    startNewSeries: resolveUiMessage('startNewSeries'),
    oneShotStoryArc: resolveUiMessage('oneShotStoryArc'),
    oneShotCompanion: resolveUiMessage('oneShotCompanion'),
    oneShotSetting: resolveUiMessage('oneShotSetting'),
    oneShotMood: resolveUiMessage('oneShotMood'),
    oneShotThemeTrack: resolveUiMessage('oneShotThemeTrack'),
    oneShotNarrationStyle: resolveUiMessage('oneShotNarrationStyle'),
    oneShotAdvancedOptions: resolveUiMessage('oneShotAdvancedOptions'),
    oneShotOptionalDetailsDescription: resolveUiMessage('oneShotOptionalDetailsDescription'),
    oneShotAddOptionalDetails: resolveUiMessage('oneShotAddOptionalDetails'),
    oneShotHideOptionalDetails: resolveUiMessage('oneShotHideOptionalDetails'),
    oneShotDetailsCollapsedHint: resolveUiMessage('oneShotDetailsCollapsedHint'),
    oneShotDetailsExpandedHint: resolveUiMessage('oneShotDetailsExpandedHint'),
    reducedMotionAriaLabel: resolveUiMessage('reducedMotionAriaLabel'),
    reducedMotionPlayback: resolveUiMessage('reducedMotionPlayback'),
    generatingStory: resolveUiMessage('generatingStory'),
    generateStory: resolveUiMessage('generateStory'),
    continueSeries: resolveUiMessage('continueSeries'),
    generateOneShot: resolveUiMessage('generateOneShot'),
    generatingStoryStatus: (childName: string) =>
      withToken(resolveUiMessage('generatingStoryStatusTemplate'), '{childName}', childName),
    generatingContinuationStatus: (seriesLabel: string, childName: string) =>
      withToken(
        withToken(resolveUiMessage('generatingContinuationStatusTemplate'), '{seriesLabel}', seriesLabel),
        '{childName}',
        childName,
      ),
    generatingOneShotStatus: (childName: string) =>
      withToken(resolveUiMessage('generatingOneShotStatusTemplate'), '{childName}', childName),
    storyGenerated: (title: string) =>
      withToken(resolveUiMessage('storyGeneratedTemplate'), '{title}', title),
    continuationGenerated: (title: string) =>
      withToken(resolveUiMessage('continuationGeneratedTemplate'), '{title}', title),
    oneShotGenerated: (title: string) =>
      withToken(resolveUiMessage('oneShotGeneratedTemplate'), '{title}', title),
    continuingSeries: (seriesLabel: string) =>
      withToken(resolveUiMessage('continuingSeriesTemplate'), '{seriesLabel}', seriesLabel),
    parentControls: resolveUiMessage('parentControls'),
    parentControlsAria: resolveUiMessage('parentControlsAria'),
    verifyingParent: resolveUiMessage('verifyingParent'),
    verifyParentWithPasskey: resolveUiMessage('verifyParentWithPasskey'),
    unlockingNarration: resolveUiMessage('unlockingNarration'),
    parentVerificationReadyHint: resolveUiMessage('parentVerificationReadyHint'),
    parentVerificationUnsupportedLocalHost: (url: string) =>
      withToken(resolveUiMessage('parentVerificationUnsupportedLocalHostTemplate'), '{url}', url),
    parentVerificationUnsupportedHost: (host: string, url: string) =>
      withToken(
        withToken(resolveUiMessage('parentVerificationUnsupportedHostTemplate'), '{host}', host),
        '{url}',
        url,
      ),
    parentVerificationRetry: (url: string) =>
      withToken(resolveUiMessage('parentVerificationRetryTemplate'), '{url}', url),
    parentVerificationRetryCurrentTab: resolveUiMessage('parentVerificationRetryCurrentTab'),
    parentVerificationFocusRetry: resolveUiMessage('parentVerificationFocusRetry'),
    parentVerificationProgress: resolveUiMessage('parentVerificationProgress'),
    parentControlsUnlocked: resolveUiMessage('parentControlsUnlocked'),
    openLocalhostVersion: resolveUiMessage('openLocalhostVersion'),
    parentControlsIntro: resolveUiMessage('parentControlsIntro'),
    parentControlsLockedSummary: resolveUiMessage('parentControlsLockedSummary'),
    notificationsEnabledAria: resolveUiMessage('notificationsEnabledAria'),
    notificationsEnabled: resolveUiMessage('notificationsEnabled'),
    notificationsScopeHint: resolveUiMessage('notificationsScopeHint'),
    analyticsEnabledAria: resolveUiMessage('analyticsEnabledAria'),
    analyticsEnabled: resolveUiMessage('analyticsEnabled'),
    analyticsScopeHint: resolveUiMessage('analyticsScopeHint'),
    unlockLongerStories: resolveUiMessage('unlockLongerStories'),
    upgradePaywallAria: resolveUiMessage('upgradePaywallAria'),
    currentTier: resolveUiMessage('currentTier'),
    upgradePath: resolveUiMessage('upgradePath'),
    paywallDurationLimit: (minutes: number) =>
      withToken(resolveUiMessage('paywallDurationLimitTemplate'), '{minutes}', `${minutes}`),
    upgrading: resolveUiMessage('upgrading'),
    upgradeVerificationProgress: resolveUiMessage('upgradeVerificationProgress'),
    updatingFavorite: resolveUiMessage('updatingFavorite'),
    confirmUpgrade: (tier: string) =>
      withToken(resolveUiMessage('confirmUpgradeTemplate'), '{tier}', tier),
    verifyParentToContinueUpgrade: resolveUiMessage('verifyParentToContinueUpgrade'),
    upgradeFlowHint: resolveUiMessage('upgradeFlowHint'),
    upgradeRouteManaged: resolveUiMessage('upgradeRouteManaged'),
    tierBadge: (tier: string) =>
      withToken(resolveUiMessage('tierBadgeTemplate'), '{tier}', tier),
    seriesProgress: (arcName: string, episode: number) =>
      withToken(
        withToken(resolveUiMessage('seriesProgressTemplate'), '{arcName}', arcName),
        '{episode}',
        `${episode}`,
      ),
    oneShotPlaceholderArcName: resolveUiMessage('oneShotPlaceholderArcName'),
    oneShotPlaceholderCompanionName: resolveUiMessage('oneShotPlaceholderCompanionName'),
    oneShotPlaceholderSetting: resolveUiMessage('oneShotPlaceholderSetting'),
    oneShotPlaceholderMood: resolveUiMessage('oneShotPlaceholderMood'),
    oneShotPlaceholderThemeTrackId: resolveUiMessage('oneShotPlaceholderThemeTrackId'),
    oneShotPlaceholderNarrationStyle: resolveUiMessage('oneShotPlaceholderNarrationStyle'),
    recent: resolveUiMessage('recent'),
    recentStoriesAria: resolveUiMessage('recentStoriesAria'),
    recentShelfDescription: resolveUiMessage('recentShelfDescription'),
    noStoriesGeneratedYet: resolveUiMessage('noStoriesGeneratedYet'),
    storiesCount: (count: number) =>
      withToken(resolveUiMessage('storiesCountTemplate'), '{count}', `${count}`),
    teaserNarrationReady: resolveUiMessage('teaserNarrationReady'),
    previewNarrationLabel: resolveUiMessage('previewNarrationLabel'),
    fullNarrationLabel: resolveUiMessage('fullNarrationLabel'),
    audioLength: (duration: string) =>
      withToken(resolveUiMessage('audioLengthTemplate'), '{duration}', duration),
    previewReadyStatus: (duration: string) =>
      withToken(resolveUiMessage('previewReadyTemplate'), '{duration}', duration),
    fullNarrationReadyStatus: (duration: string) =>
      withToken(resolveUiMessage('fullNarrationReadyTemplate'), '{duration}', duration),
    approveFullNarration: resolveUiMessage('approveFullNarration'),
    verifyParentToApprove: resolveUiMessage('verifyParentToApprove'),
    unfavorite: resolveUiMessage('unfavorite'),
    favorite: resolveUiMessage('favorite'),
    favorites: resolveUiMessage('favorites'),
    favoriteStoriesAria: resolveUiMessage('favoriteStoriesAria'),
    favoritesShelfDescription: resolveUiMessage('favoritesShelfDescription'),
    noFavoritesYet: resolveUiMessage('noFavoritesYet'),
    errorFriendly: resolveUiMessage('errorFriendly'),
    librarySyncTitle: resolveUiMessage('librarySyncTitle'),
    librarySyncBody: resolveUiMessage('librarySyncBody'),
    retryLoadingStories: resolveUiMessage('retryLoadingStories'),
    seriesSyncHint: resolveUiMessage('seriesSyncHint'),
    verifyToChange: resolveUiMessage('verifyToChange'),
    posterPreview: (title: string) =>
      withToken(resolveUiMessage('posterPreviewTemplate'), '{title}', title),
    teaserNarration: (title: string) =>
      withToken(resolveUiMessage('teaserNarrationTemplate'), '{title}', title),
    fullNarration: (title: string) =>
      withToken(resolveUiMessage('fullNarrationTemplate'), '{title}', title),
  }),
})
