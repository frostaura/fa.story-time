/* ───────────────────────────────────────────────
 * PreferencesStep – voice, duration, mode
 * ─────────────────────────────────────────────── */

import { useState } from 'react';
import Button from '../common/Button';
import Slider from '../common/Slider';

interface PreferencesStepProps {
  onNext: (data: {
    narratorVoice: string;
    defaultDuration: number;
    preferredMode: 'series' | 'oneshot';
  }) => void;
  onBack: () => void;
}

const VOICE_OPTIONS = [
  { id: 'warm-female', label: 'Warm (Female)' },
  { id: 'calm-male', label: 'Calm (Male)' },
  { id: 'playful-female', label: 'Playful (Female)' },
  { id: 'soothing-male', label: 'Soothing (Male)' },
];

export default function PreferencesStep({ onNext, onBack }: PreferencesStepProps) {
  const [voice, setVoice] = useState('warm-female');
  const [duration, setDuration] = useState(5);
  const [mode, setMode] = useState<'series' | 'oneshot'>('oneshot');

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-lg)' }}>
      <h2 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>
        Story preferences
      </h2>

      {/* Narrator voice */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 8,
          }}
        >
          Narrator voice
        </label>
        <div
          role="radiogroup"
          aria-label="Narrator voice"
          style={{ display: 'flex', flexDirection: 'column', gap: 6 }}
        >
          {VOICE_OPTIONS.map((v) => (
            <label
              key={v.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                padding: '10px 14px',
                borderRadius: 'var(--radius-sm)',
                border: '1.5px solid',
                borderColor: voice === v.id ? 'var(--color-accent)' : 'var(--color-border)',
                background: voice === v.id ? 'rgba(99,102,241,0.06)' : 'transparent',
                cursor: 'pointer',
                minHeight: 44,
              }}
            >
              <input
                type="radio"
                name="voice"
                value={v.id}
                checked={voice === v.id}
                onChange={() => setVoice(v.id)}
                style={{ accentColor: 'var(--color-accent)' }}
              />
              <span style={{ fontSize: 14, fontWeight: 500 }}>{v.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Duration */}
      <Slider
        min={2}
        max={10}
        value={duration}
        onChange={setDuration}
        label="Default story length"
        unit=" min"
      />

      {/* Mode */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 8,
          }}
        >
          Story mode
        </label>
        <div style={{ display: 'flex', gap: 8 }}>
          {(['oneshot', 'series'] as const).map((m) => (
            <button
              key={m}
              onClick={() => setMode(m)}
              style={{
                flex: 1,
                padding: '12px 16px',
                borderRadius: 'var(--radius-sm)',
                border: '1.5px solid',
                borderColor: mode === m ? 'var(--color-accent)' : 'var(--color-border)',
                background: mode === m ? 'var(--color-accent)' : 'transparent',
                color: mode === m ? '#fff' : 'var(--color-text)',
                fontSize: 14,
                fontWeight: 600,
                cursor: 'pointer',
                minHeight: 44,
                fontFamily: 'var(--font-sans)',
              }}
            >
              {m === 'oneshot' ? 'One-shot' : 'Series'}
            </button>
          ))}
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12 }}>
        <Button variant="ghost" onClick={onBack} style={{ flex: 1 }}>
          Back
        </Button>
        <Button
          fullWidth
          onClick={() =>
            onNext({ narratorVoice: voice, defaultDuration: duration, preferredMode: mode })
          }
          style={{ flex: 2 }}
        >
          Next
        </Button>
      </div>
    </div>
  );
}
