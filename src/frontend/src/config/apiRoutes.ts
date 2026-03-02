import { runtimeConfig } from './runtime'

const trimTrailingSlash = (value: string): string => value.replace(/\/+$/u, '')
const trimLeadingSlash = (value: string): string => value.replace(/^\/+/u, '')

const encodePathSegment = (value: string): string => encodeURIComponent(value)
const joinPath = (basePath: string, suffix: string): string =>
  `${trimTrailingSlash(basePath)}/${trimLeadingSlash(suffix)}`

export const apiRoutes = Object.freeze({
  homeStatus: runtimeConfig.apiRoutes.homeStatus,
  storyGenerate: runtimeConfig.apiRoutes.storyGenerate,
  subscriptionCheckoutSession: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.subscriptionBase, `${encodePathSegment(softUserId)}/checkout/session`),
  subscriptionCheckoutComplete: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.subscriptionBase, `${encodePathSegment(softUserId)}/checkout/complete`),
  library: (softUserId: string, kidMode: boolean) =>
    `${joinPath(runtimeConfig.apiRoutes.libraryBase, encodePathSegment(softUserId))}?kidMode=${String(kidMode)}`,
  storyFavorite: (storyId: string) =>
    joinPath(runtimeConfig.apiRoutes.storiesBase, `${encodePathSegment(storyId)}/favorite`),
  storyApprove: (storyId: string) =>
    joinPath(runtimeConfig.apiRoutes.storiesBase, `${encodePathSegment(storyId)}/approve`),
  parentGateRegister: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.parentBase, `${encodePathSegment(softUserId)}/gate/register`),
  parentGateChallenge: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.parentBase, `${encodePathSegment(softUserId)}/gate/challenge`),
  parentGateVerify: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.parentBase, `${encodePathSegment(softUserId)}/gate/verify`),
  parentSettings: (softUserId: string) =>
    joinPath(runtimeConfig.apiRoutes.parentBase, `${encodePathSegment(softUserId)}/settings`),
  parentSettingsWithToken: (softUserId: string, gateToken: string) =>
    `${joinPath(runtimeConfig.apiRoutes.parentBase, `${encodePathSegment(softUserId)}/settings`)}?gateToken=${encodeURIComponent(gateToken)}`,
})

export const buildApiUrl = (apiBaseUrl: string, path: string): string => {
  const trimmedBase = apiBaseUrl.trim()
  if (trimmedBase.length === 0) {
    return path
  }

  return `${trimTrailingSlash(trimmedBase)}${path}`
}
