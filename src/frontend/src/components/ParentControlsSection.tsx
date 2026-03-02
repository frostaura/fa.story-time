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
  isUnlockingParent: boolean
  hasParentGateToken: boolean
  onUnlockParentSettings: () => void
  onNotificationsChange: (value: boolean) => void
  onAnalyticsChange: (value: boolean) => void
}

export function ParentControlsSection({
  visible,
  ui,
  profile,
  isUnlockingParent,
  hasParentGateToken,
  onUnlockParentSettings,
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
      <div className="parent-controls-row">
        <button
          className="btn-secondary"
          data-testid="unlock-parent-settings-button"
          disabled={isUnlockingParent}
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
      {!hasParentGateToken && (
        <p className="toggle-disabled-hint">{ui.verifyToChange}</p>
      )}

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
      {!hasParentGateToken && (
        <p className="toggle-disabled-hint">{ui.verifyToChange}</p>
      )}
    </section>
  )
}
