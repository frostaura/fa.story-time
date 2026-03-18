import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ParentControlsSection } from '../../components/ParentControlsSection'
import { appMessages } from '../../config/messages'

describe('ParentControlsSection', () => {
  const ui = appMessages.ui

  it('does not render when hidden', () => {
    const { container } = render(
      <ParentControlsSection
        hasParentGateToken={false}
        isUnlockingParent={false}
        isParentVerificationSupported
        kidShelfEnabled={false}
        onKidShelfChange={() => {}}
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
        parentVerificationHint={ui.parentVerificationReadyHint}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible={false}
      />,
    )

    expect(container).toBeEmptyDOMElement()
  })

  it('renders unlock controls and uses verifying label while unlocking', () => {
    render(
      <ParentControlsSection
        hasParentGateToken={false}
        isUnlockingParent
        isParentVerificationSupported
        kidShelfEnabled={false}
        onKidShelfChange={() => {}}
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
        parentVerificationHint={ui.parentVerificationReadyHint}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible
      />,
    )

    expect(screen.getByRole('heading', { name: 'Parent Controls' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Verifying...' })).toBeDisabled()
    expect(screen.getByText(ui.parentControlsIntro)).toBeInTheDocument()
    expect(screen.getByText(ui.parentVerificationReadyHint)).toBeInTheDocument()
    expect(screen.getByTestId('parent-controls-locked-note')).toHaveTextContent(ui.parentControlsLockedSummary)
    expect(screen.getByLabelText('Kid Shelf')).toBeDisabled()
    expect(screen.getByLabelText('Notifications enabled')).toBeDisabled()
    expect(screen.getByLabelText('Analytics enabled')).toBeDisabled()
    expect(screen.getByText(ui.notificationsScopeHint)).toBeInTheDocument()
    expect(screen.getByText(ui.analyticsScopeHint)).toBeInTheDocument()
  })

  it('allows parent setting changes after gate unlock', async () => {
    const onUnlockParentSettings = vi.fn()
    const onKidShelfChange = vi.fn()
    const onNotificationsChange = vi.fn()
    const onAnalyticsChange = vi.fn()

    render(
      <ParentControlsSection
        hasParentGateToken
        isUnlockingParent={false}
        isParentVerificationSupported
        kidShelfEnabled={false}
        onKidShelfChange={onKidShelfChange}
        onAnalyticsChange={onAnalyticsChange}
        onNotificationsChange={onNotificationsChange}
        onUnlockParentSettings={onUnlockParentSettings}
        parentVerificationHint={ui.parentVerificationReadyHint}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Verify parent with passkey' }))
    await userEvent.click(screen.getByLabelText('Kid Shelf'))
    await userEvent.click(screen.getByLabelText('Notifications enabled'))
    await userEvent.click(screen.getByLabelText('Analytics enabled'))

    expect(onUnlockParentSettings).toHaveBeenCalledTimes(1)
    expect(onKidShelfChange).toHaveBeenCalledWith(true)
    expect(onNotificationsChange).toHaveBeenCalledWith(true)
    expect(onAnalyticsChange).toHaveBeenCalledWith(true)
  })

  it('disables parent verification on unsupported hosts and shows guidance', () => {
    const unsupportedHint = ui.parentVerificationUnsupportedLocalHost('http://localhost:4184/')

    render(
      <ParentControlsSection
        hasParentGateToken={false}
        isUnlockingParent={false}
        isParentVerificationSupported={false}
        kidShelfEnabled={false}
        onKidShelfChange={() => {}}
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
        parentVerificationHint={unsupportedHint}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        supportActionHref="http://localhost:4184/"
        supportActionLabel={ui.openLocalhostVersion}
        ui={ui}
        visible
      />,
    )

    expect(screen.getByRole('button', { name: 'Verify parent with passkey' })).toBeDisabled()
    expect(screen.getByText(ui.parentControlsLockedSummary)).toBeInTheDocument()
    expect(screen.getByText(unsupportedHint)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: ui.openLocalhostVersion })).toHaveAttribute('href', 'http://localhost:4184/')
  })

  it('renders parent feedback inline with the controls card', () => {
    render(
      <ParentControlsSection
        errorMessage="Parent challenge failed with status 400"
        hasParentGateToken={false}
        isUnlockingParent={false}
        isParentVerificationSupported
        kidShelfEnabled={false}
        onKidShelfChange={() => {}}
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
        parentVerificationHint={ui.parentVerificationReadyHint}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible
      />,
    )

    expect(screen.getByRole('alert')).toHaveTextContent('Parent challenge failed with status 400')
  })
})
