import { appMessages } from '../config/messages'

type UiMessages = typeof appMessages.ui

type ProfileSummary = {
  notificationsEnabled: boolean
  analyticsEnabled: boolean
}

type ParentControlsSectionProps = {
  visible: boolean
  ui: UiMessages
  profile: ProfileSummary
  kidShelfEnabled: boolean
  errorMessage?: string | null
  isUnlockingParent: boolean
  isParentVerificationSupported: boolean
  hasParentGateToken: boolean
  parentVerificationHint: string
  statusMessage?: string | null
  supportActionHref?: string | null
  supportActionLabel?: string | null
  onUnlockParentSettings: () => void
  onKidShelfChange: (value: boolean) => void
  onNotificationsChange: (value: boolean) => void
  onAnalyticsChange: (value: boolean) => void
}

export function ParentControlsSection({
  visible,
  ui,
  profile,
  kidShelfEnabled,
  errorMessage = null,
  isUnlockingParent,
  isParentVerificationSupported,
  hasParentGateToken,
  parentVerificationHint,
  statusMessage = null,
  supportActionHref = null,
  supportActionLabel = null,
  onUnlockParentSettings,
  onKidShelfChange,
  onNotificationsChange,
  onAnalyticsChange,
}: ParentControlsSectionProps) {
  if (!visible) {
    return null
  }

  return (
    <section aria-label={ui.parentControlsAria} className="parent-controls-card" data-testid="parent-controls-section">
      <h3 className="parent-controls-heading">
        <span aria-hidden="true" className="lock-icon">🔒</span>
        {ui.parentControls}
      </h3>
      <p className="parent-controls-intro">{ui.parentControlsIntro}</p>
      <div
        className={`parent-controls-support${isParentVerificationSupported ? ' parent-controls-support-ready' : ' parent-controls-support-warning'}`}
      >
        <div className="parent-controls-support-copy">
          <p className="parent-controls-support-title">{ui.verifyParentWithPasskey}</p>
          <p className={`parent-controls-hint${isParentVerificationSupported ? '' : ' parent-controls-hint-warning'}`}>
            {parentVerificationHint}
          </p>
        </div>
        {!isParentVerificationSupported && supportActionHref && supportActionLabel ? (
          <a
            className="btn-secondary parent-controls-support-action"
            href={supportActionHref}
          >
            {supportActionLabel}
          </a>
        ) : null}
      </div>
      <div className="parent-controls-row">
        <button
          className="btn-parent-gate"
          data-testid="unlock-parent-settings-button"
          disabled={isUnlockingParent || !isParentVerificationSupported}
          onClick={onUnlockParentSettings}
          type="button"
        >
          {isUnlockingParent ? (
            <span className="btn-spinner">
              <span className="spinner-icon" />
              {ui.verifyingParent}
            </span>
          ) : (
            ui.verifyParentWithPasskey
          )}
        </button>
      </div>
      {statusMessage ? (
        <div className="feedback-banner feedback-banner-info parent-controls-feedback" role="status">
          <span aria-hidden="true" className="feedback-banner-icon">⏳</span>
          <span className="feedback-banner-text">{statusMessage}</span>
        </div>
      ) : null}
      {errorMessage ? (
        <div className="feedback-banner feedback-banner-error parent-controls-feedback" role="alert">
          <span aria-hidden="true" className="feedback-banner-icon">⚠️</span>
          <span className="feedback-banner-text">{errorMessage}</span>
        </div>
      ) : null}

      {!hasParentGateToken ? (
        <p className="parent-controls-locked-note" data-testid="parent-controls-locked-note">
          {ui.parentControlsLockedSummary}
        </p>
      ) : null}

      <div className="parent-controls-settings">
        <div className="parent-control-item">
          <label className="toggle">
            <input
              aria-label={ui.kidShelf}
              checked={kidShelfEnabled}
              data-testid="kid-shelf-parent-toggle"
              disabled={!hasParentGateToken}
              onChange={(event) => onKidShelfChange(event.target.checked)}
              type="checkbox"
            />
            {ui.kidShelf}
          </label>
        </div>

        <div className="parent-control-item">
          <label className="toggle">
            <input
              aria-label={ui.notificationsEnabledAria}
              checked={profile.notificationsEnabled}
              data-testid="notifications-toggle"
              disabled={!hasParentGateToken}
              onChange={(event) => onNotificationsChange(event.target.checked)}
              type="checkbox"
            />
            {ui.notificationsEnabled}
          </label>
          <p className="toggle-support-copy">{ui.notificationsScopeHint}</p>
        </div>

        <div className="parent-control-item">
          <label className="toggle">
            <input
              aria-label={ui.analyticsEnabledAria}
              checked={profile.analyticsEnabled}
              data-testid="analytics-toggle"
              disabled={!hasParentGateToken}
              onChange={(event) => onAnalyticsChange(event.target.checked)}
              type="checkbox"
            />
            {ui.analyticsEnabled}
          </label>
          <p className="toggle-support-copy">{ui.analyticsScopeHint}</p>
        </div>
      </div>
    </section>
  )
}
