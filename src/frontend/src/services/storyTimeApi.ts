import { apiRoutes, buildApiUrl } from '../config/apiRoutes'

type JsonObject = Record<string, unknown>

const jsonHeaders = Object.freeze({
  'Content-Type': 'application/json',
})

const withJsonBody = (body: JsonObject): RequestInit => ({
  method: 'POST',
  headers: jsonHeaders,
  body: JSON.stringify(body),
})

export const createStoryTimeApi = (apiBaseUrl: string) =>
  Object.freeze({
    getHomeStatus: () => fetch(buildApiUrl(apiBaseUrl, apiRoutes.homeStatus)),
    getLibrary: (softUserId: string, kidMode: boolean) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.library(softUserId, kidMode))),
    generateStory: (body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.storyGenerate), {
        ...withJsonBody(body),
      }),
    setStoryFavorite: (storyId: string, isFavorite: boolean) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.storyFavorite(storyId)), {
        method: 'PUT',
        headers: jsonHeaders,
        body: JSON.stringify({ isFavorite }),
      }),
    approveStory: (storyId: string) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.storyApprove(storyId)), {
        method: 'POST',
      }),
    registerParentCredential: (softUserId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentGateRegister(softUserId)), {
        ...withJsonBody(body),
      }),
    createParentGateChallenge: (softUserId: string) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentGateChallenge(softUserId)), {
        method: 'POST',
      }),
    verifyParentGate: (softUserId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentGateVerify(softUserId)), {
        ...withJsonBody(body),
      }),
    getParentSettingsWithToken: (softUserId: string, gateToken: string) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentSettingsWithToken(softUserId, gateToken))),
    updateParentSettings: (softUserId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentSettings(softUserId)), {
        method: 'PUT',
        headers: jsonHeaders,
        body: JSON.stringify(body),
      }),
    createCheckoutSession: (softUserId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.subscriptionCheckoutSession(softUserId)), {
        ...withJsonBody(body),
      }),
    completeCheckoutSession: (softUserId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.subscriptionCheckoutComplete(softUserId)), {
        ...withJsonBody(body),
      }),
  })
