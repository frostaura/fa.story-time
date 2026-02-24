/* ───────────────────────────────────────────────
 * LocalStorage key constants (prefixed tw_)
 * ─────────────────────────────────────────────── */

export const LS_KEYS = {
  softUserId: 'tw_softUserId',
  childrenProfiles: 'tw_childrenProfiles',
  stories: 'tw_stories',
  seriesBibles: 'tw_seriesBibles',
  appSettings: 'tw_appSettings',
  cooldownState: 'tw_cooldownState',
  onboardingComplete: 'tw_onboardingComplete',
} as const;

export type LSKey = (typeof LS_KEYS)[keyof typeof LS_KEYS];
