import type { Mode } from '../config/modes'

export type PosterLayer = {
  role: string
  speedMultiplier: number
  dataUri: string
}

export type StoryBible = {
  seriesId?: string
  visualIdentity?: string
  recurringCharacter?: string
  arcName: string
  arcEpisodeNumber: number
  arcObjective: string
  previousEpisodeSummary: string
  continuityFacts?: string[]
  audioAnchorMetadata: {
    themeTrackId: string
    narrationStyle: string
  }
}

export type OneShotDefaults = {
  arcName: string
  companionName: string
  setting: string
  mood: string
  themeTrackId: string
  narrationStyle: string
}

export type StoryArtifact = {
  storyId: string
  title: string
  mode: Mode
  seriesId?: string
  recap: string
  scenes: string[]
  sceneCount: number
  posterLayers: PosterLayer[]
  approvalRequired: boolean
  teaserAudio: string
  fullAudio?: string
  fullAudioReady: boolean
  storyBible?: StoryBible
  reducedMotion: boolean
  generatedAt: string
  isFavorite: boolean
}

export type HomeStatusResponse = {
  quickGenerateVisible: boolean
  durationSliderVisible: boolean
  durationMinMinutes: number
  durationMaxMinutes: number
  durationDefaultMinutes: number
  defaultChildName: string
  parentControlsEnabled: boolean
  defaultTier: string
  oneShotDefaults?: OneShotDefaults
}

export type GenerateStoryResponse = Omit<StoryArtifact, 'isFavorite'>

export type ParentGateChallengeResponse = {
  challengeId: string
  challenge: string
  rpId: string
  expiresAt: string
}

export type ParentGateVerifyResponse = {
  gateToken: string
}

export type StoryApprovalResponse = {
  fullAudioReady: boolean
  fullAudio?: string
}

export type ParentSettingsResponse = {
  notificationsEnabled: boolean
  analyticsEnabled: boolean
  kidShelfEnabled: boolean
}

export type ChildProfile = {
  childName: string
  reducedMotion: boolean
  notificationsEnabled: boolean
  analyticsEnabled: boolean
}

export type ParentCredential = {
  credentialId: string
  publicKey: string
  credentialKind: string
}

export type ParentSignedAssertion = {
  credentialId: string
  clientDataJson: string
  authenticatorData: string
  signature: string
  type: string
}

export type LibraryItem = {
  storyId: string
  title: string
  mode: Mode
  seriesId?: string
  isFavorite: boolean
  fullAudioReady: boolean
  fullAudio?: string
  createdAt: string
}

export type LibraryResponse = {
  recent: LibraryItem[]
  favorites: LibraryItem[]
  kidShelfEnabled: boolean
}

export type SubscriptionPaywallResponse = {
  currentTier: string
  upgradeTier: string
  maxDurationMinutes: number
  upgradeUrl: string
  message: string
}

export type SubscriptionCheckoutSessionResponse = {
  sessionId: string
  currentTier: string
  upgradeTier: string
  checkoutUrl: string
  expiresAt: string
}

export type SubscriptionCheckoutCompleteResponse = {
  currentTier: string
  upgradeTier: string
}
