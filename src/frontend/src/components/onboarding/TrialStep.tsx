/* ───────────────────────────────────────────────
 * TrialStep – explain trial, start adventure
 * ─────────────────────────────────────────────── */

import { Sparkles, Clock, Shield } from 'lucide-react';
import Button from '../common/Button';

interface TrialStepProps {
  onComplete: () => void;
  onBack: () => void;
}

const features = [
  { icon: <Sparkles size={20} />, text: '7-day free trial with full access' },
  { icon: <Clock size={20} />, text: 'Generate stories up to 10 minutes long' },
  { icon: <Shield size={20} />, text: 'Parental controls always included' },
];

export default function TrialStep({ onComplete, onBack }: TrialStepProps) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-lg)' }}>
      <h2 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>
        You're all set!
      </h2>
      <p style={{ margin: 0, fontSize: 15, color: 'var(--color-text-secondary)', lineHeight: 1.5 }}>
        Your 7-day free trial starts now. During the trial you can generate up to 3 stories
        per day with a short cooldown between each. Upgrade anytime for more.
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {features.map((f, i) => (
          <div
            key={i}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '12px 16px',
              borderRadius: 'var(--radius-sm)',
              background: 'var(--color-bg)',
              border: '1px solid var(--color-border)',
            }}
          >
            <span style={{ color: 'var(--color-accent)' }}>{f.icon}</span>
            <span style={{ fontSize: 14, fontWeight: 500 }}>{f.text}</span>
          </div>
        ))}
      </div>

      <div style={{ display: 'flex', gap: 12, marginTop: 'var(--spacing-md)' }}>
        <Button variant="ghost" onClick={onBack} style={{ flex: 1 }}>
          Back
        </Button>
        <Button fullWidth onClick={onComplete} style={{ flex: 2 }}>
          Start Your Adventure ✨
        </Button>
      </div>
    </div>
  );
}
