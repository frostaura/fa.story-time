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
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
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
        onAnalyticsChange={() => {}}
        onNotificationsChange={() => {}}
        onUnlockParentSettings={() => {}}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible
      />,
    )

    expect(screen.getByRole('heading', { name: 'Parent Controls' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Verifying...' })).toBeDisabled()
    expect(screen.getByLabelText('Notifications enabled')).toBeDisabled()
    expect(screen.getByLabelText('Analytics enabled')).toBeDisabled()
  })

  it('allows parent setting changes after gate unlock', async () => {
    const onUnlockParentSettings = vi.fn()
    const onNotificationsChange = vi.fn()
    const onAnalyticsChange = vi.fn()

    render(
      <ParentControlsSection
        hasParentGateToken
        isUnlockingParent={false}
        onAnalyticsChange={onAnalyticsChange}
        onNotificationsChange={onNotificationsChange}
        onUnlockParentSettings={onUnlockParentSettings}
        profile={{ notificationsEnabled: false, analyticsEnabled: false }}
        ui={ui}
        visible
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Verify parent with passkey' }))
    await userEvent.click(screen.getByLabelText('Notifications enabled'))
    await userEvent.click(screen.getByLabelText('Analytics enabled'))

    expect(onUnlockParentSettings).toHaveBeenCalledTimes(1)
    expect(onNotificationsChange).toHaveBeenCalledWith(true)
    expect(onAnalyticsChange).toHaveBeenCalledWith(true)
  })
})
