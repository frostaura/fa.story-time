import { apiRoutes, buildApiUrl } from '../config/apiRoutes'

type JsonObject = Record<string, unknown>

const jsonHeaders = Object.freeze({
  'Content-Type': 'application/json',
})

const withGateTokenHeader = (gateToken: string): HeadersInit => ({
  'X-StoryTime-Gate-Token': gateToken,
})

const withJsonBody = (body: JsonObject): RequestInit => ({
  method: 'POST',
  headers: jsonHeaders,
  body: JSON.stringify(body),
})

export const createStoryTimeApi = (apiBaseUrl: string) =>
  Object.freeze({
    getHomeStatus: () => fetch(buildApiUrl(apiBaseUrl, apiRoutes.homeStatus)),
    getLibrary: (softUserId: string) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.library(softUserId))),
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
    approveStory: (storyId: string, body: JsonObject) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.storyApprove(storyId)), {
        ...withJsonBody(body),
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
    getParentSettings: (softUserId: string, gateToken: string) =>
      fetch(buildApiUrl(apiBaseUrl, apiRoutes.parentSettings(softUserId)), {
        headers: withGateTokenHeader(gateToken),
      }),
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
