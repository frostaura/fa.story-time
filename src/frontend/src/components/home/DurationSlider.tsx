/* ───────────────────────────────────────────────
 * DurationSlider – story duration selector
 * ─────────────────────────────────────────────── */

import Slider from '../common/Slider';

interface DurationSliderProps {
  value: number;
  onChange: (value: number) => void;
  maxUnlocked?: number; // Premium unlocks up to 15
}

export default function DurationSlider({
  value,
  onChange,
  maxUnlocked = 10,
}: DurationSliderProps) {
  const labels = [
    { range: '2–5 min', label: 'Short' },
    { range: '5–10 min', label: 'Medium' },
    { range: '10–15 min', label: 'Long' },
  ];

  const activeLabel =
    value <= 5 ? labels[0] : value <= 10 ? labels[1] : labels[2];

  return (
    <div
      style={{
        background: 'var(--color-surface)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--color-border)',
        padding: 'var(--spacing-md) var(--spacing-lg)',
      }}
    >
      <Slider
        min={2}
        max={maxUnlocked}
        value={Math.min(value, maxUnlocked)}
        onChange={onChange}
        label="Duration"
        unit=" min"
      />

      {/* Segment labels */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          marginTop: 4,
        }}
      >
        {labels.map((l) => (
          <span
            key={l.label}
            style={{
              fontSize: 11,
              fontWeight: l.label === activeLabel.label ? 600 : 400,
              color:
                l.label === activeLabel.label
                  ? 'var(--color-accent)'
                  : 'var(--color-text-secondary)',
              opacity: l.label === 'Long' && maxUnlocked < 15 ? 0.4 : 1,
            }}
          >
            {l.label}
            {l.label === 'Long' && maxUnlocked < 15 && ' 🔒'}
          </span>
        ))}
      </div>
    </div>
  );
}
