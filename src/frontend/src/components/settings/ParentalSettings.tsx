/* ───────────────────────────────────────────────
 * ParentalSettings – full settings panel
 * ─────────────────────────────────────────────── */

import { useState } from 'react';
import {
  Shield,
  Bell,
  BarChart3,
  CreditCard,
  Trash2,
  Eye,
} from 'lucide-react';
import Button from '../common/Button';
import ProfileManager from './ProfileManager';
import { useAppSelector, useAppDispatch } from '../../store/hooks';
import { updateSettings, setKidMode } from '../../store/slices/appSlice';
import { clearAllStories, clearAllData } from '../../services/localStorage';

function ToggleRow({
  label,
  description,
  value,
  onChange,
  icon,
}: {
  label: string;
  description?: string;
  value: boolean;
  onChange: (v: boolean) => void;
  icon?: React.ReactNode;
}) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        padding: '12px 0',
        borderBottom: '1px solid var(--color-border)',
      }}
    >
      {icon && (
        <span style={{ color: 'var(--color-text-secondary)', flexShrink: 0 }}>
          {icon}
        </span>
      )}
      <div style={{ flex: 1 }}>
        <p style={{ margin: 0, fontSize: 14, fontWeight: 500 }}>{label}</p>
        {description && (
          <p
            style={{
              margin: '2px 0 0',
              fontSize: 12,
              color: 'var(--color-text-secondary)',
            }}
          >
            {description}
          </p>
        )}
      </div>
      <button
        role="switch"
        aria-checked={value}
        onClick={() => onChange(!value)}
        style={{
          position: 'relative',
          width: 48,
          height: 28,
          borderRadius: 14,
          border: 'none',
          background: value ? 'var(--color-accent)' : 'var(--color-border)',
          cursor: 'pointer',
          flexShrink: 0,
          transition: 'background 200ms',
          minHeight: 44,
          display: 'flex',
          alignItems: 'center',
          padding: 2,
        }}
      >
        <div
          style={{
            width: 24,
            height: 24,
            borderRadius: '50%',
            background: '#fff',
            transform: value ? 'translateX(20px)' : 'translateX(0)',
            transition: 'transform 200ms',
            boxShadow: '0 1px 3px rgba(0,0,0,0.15)',
          }}
        />
      </button>
    </div>
  );
}

export default function ParentalSettings() {
  const dispatch = useAppDispatch();
  const { settings, kidMode } = useAppSelector((s) => s.app);
  const [confirmClear, setConfirmClear] = useState<'stories' | 'all' | null>(null);

  const updateSetting = <K extends keyof typeof settings>(
    key: K,
    value: (typeof settings)[K],
  ) => {
    dispatch(updateSettings({ [key]: value }));
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-xl)' }}>
      {/* Profiles */}
      <ProfileManager />

      {/* Approval */}
      <section>
        <h3 style={{ margin: '0 0 var(--spacing-sm)', fontSize: 16, fontWeight: 700 }}>
          Content
        </h3>
        <ToggleRow
          label="Review before delivery"
          description="Preview and approve stories before they're available to play"
          value={settings.approvalStepEnabled}
          onChange={(v) => updateSetting('approvalStepEnabled', v)}
          icon={<Shield size={18} />}
        />
      </section>

      {/* Notifications */}
      <section>
        <h3 style={{ margin: '0 0 var(--spacing-sm)', fontSize: 16, fontWeight: 700 }}>
          Notifications
        </h3>
        <ToggleRow
          label="Enable notifications"
          value={settings.notificationsEnabled}
          onChange={(v) => updateSetting('notificationsEnabled', v)}
          icon={<Bell size={18} />}
        />
        {settings.notificationsEnabled && (
          <div style={{ paddingLeft: 30 }}>
            <ToggleRow
              label="Generation complete"
              value={settings.notificationToggles.generationComplete}
              onChange={(v) =>
                updateSetting('notificationToggles', {
                  ...settings.notificationToggles,
                  generationComplete: v,
                })
              }
            />
            <ToggleRow
              label="Queue progress"
              value={settings.notificationToggles.queueProgress}
              onChange={(v) =>
                updateSetting('notificationToggles', {
                  ...settings.notificationToggles,
                  queueProgress: v,
                })
              }
            />
            <ToggleRow
              label="Cooldown complete"
              value={settings.notificationToggles.cooldownComplete}
              onChange={(v) =>
                updateSetting('notificationToggles', {
                  ...settings.notificationToggles,
                  cooldownComplete: v,
                })
              }
            />
            <ToggleRow
              label="Subscription events"
              value={settings.notificationToggles.subscriptionEvents}
              onChange={(v) =>
                updateSetting('notificationToggles', {
                  ...settings.notificationToggles,
                  subscriptionEvents: v,
                })
              }
            />
            <ToggleRow
              label="Promotions"
              value={settings.notificationToggles.promos}
              onChange={(v) =>
                updateSetting('notificationToggles', {
                  ...settings.notificationToggles,
                  promos: v,
                })
              }
            />
          </div>
        )}
      </section>

      {/* Analytics */}
      <section>
        <ToggleRow
          label="Analytics (Clarity)"
          description="Help us improve by sharing anonymous usage data"
          value={settings.analyticsEnabled}
          onChange={(v) => updateSetting('analyticsEnabled', v)}
          icon={<BarChart3 size={18} />}
        />
      </section>

      {/* Subscription */}
      <section>
        <h3 style={{ margin: '0 0 var(--spacing-sm)', fontSize: 16, fontWeight: 700 }}>
          Subscription
        </h3>
        <div
          style={{
            padding: '14px 16px',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--color-border)',
            display: 'flex',
            alignItems: 'center',
            gap: 12,
          }}
        >
          <CreditCard size={18} style={{ color: 'var(--color-text-secondary)' }} />
          <div style={{ flex: 1 }}>
            <p style={{ margin: 0, fontSize: 14, fontWeight: 500 }}>Free Trial</p>
            <p style={{ margin: 0, fontSize: 12, color: 'var(--color-text-secondary)' }}>
              Manage your subscription plan
            </p>
          </div>
          <Button variant="secondary" style={{ padding: '6px 14px', minHeight: 36, fontSize: 13 }}>
            Upgrade
          </Button>
        </div>
      </section>

      {/* Kid Mode */}
      <section>
        <ToggleRow
          label="Kid Mode"
          description="Hides settings and generation controls. Only shows the story library."
          value={kidMode}
          onChange={(v) => dispatch(setKidMode(v))}
          icon={<Eye size={18} />}
        />
      </section>

      {/* Reduced Motion */}
      <section>
        <ToggleRow
          label="Reduced motion"
          description="Disable parallax and animations for comfort"
          value={settings.reducedMotion}
          onChange={(v) => updateSetting('reducedMotion', v)}
        />
      </section>

      {/* Storage / Danger zone */}
      <section>
        <h3 style={{ margin: '0 0 var(--spacing-sm)', fontSize: 16, fontWeight: 700, color: '#EF4444' }}>
          Danger Zone
        </h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {confirmClear === 'stories' ? (
            <div style={{ display: 'flex', gap: 8 }}>
              <Button variant="ghost" onClick={() => setConfirmClear(null)} style={{ flex: 1 }}>
                Cancel
              </Button>
              <Button
                variant="primary"
                onClick={() => { clearAllStories(); setConfirmClear(null); }}
                style={{ flex: 1, background: '#EF4444' }}
              >
                Confirm Delete
              </Button>
            </div>
          ) : (
            <Button
              variant="secondary"
              fullWidth
              onClick={() => setConfirmClear('stories')}
            >
              <Trash2 size={16} /> Clear all stories
            </Button>
          )}

          {confirmClear === 'all' ? (
            <div style={{ display: 'flex', gap: 8 }}>
              <Button variant="ghost" onClick={() => setConfirmClear(null)} style={{ flex: 1 }}>
                Cancel
              </Button>
              <Button
                variant="primary"
                onClick={() => { clearAllData(); window.location.reload(); }}
                style={{ flex: 1, background: '#EF4444' }}
              >
                Confirm Reset
              </Button>
            </div>
          ) : (
            <Button
              variant="secondary"
              fullWidth
              onClick={() => setConfirmClear('all')}
            >
              <Trash2 size={16} /> Reset all data
            </Button>
          )}
        </div>
      </section>
    </div>
  );
}
