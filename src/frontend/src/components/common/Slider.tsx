/* ───────────────────────────────────────────────
 * Slider – range input with label & value
 * ─────────────────────────────────────────────── */

import { type CSSProperties } from 'react';

interface SliderProps {
  min: number;
  max: number;
  step?: number;
  value: number;
  onChange: (value: number) => void;
  label?: string;
  unit?: string;
  disabled?: boolean;
}

const trackStyle: CSSProperties = {
  width: '100%',
  height: 6,
  borderRadius: 3,
  appearance: 'none',
  WebkitAppearance: 'none',
  background: 'var(--color-border)',
  outline: 'none',
  cursor: 'pointer',
};

export default function Slider({
  min,
  max,
  step = 1,
  value,
  onChange,
  label,
  unit = '',
  disabled = false,
}: SliderProps) {
  const percent = ((value - min) / (max - min)) * 100;

  return (
    <div style={{ width: '100%' }}>
      {label && (
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 8,
          }}
        >
          <span
            style={{
              fontSize: 13,
              fontWeight: 500,
              color: 'var(--color-text-secondary)',
            }}
          >
            {label}
          </span>
          <span
            style={{
              fontSize: 15,
              fontWeight: 600,
              color: 'var(--color-text)',
            }}
          >
            {value}
            {unit}
          </span>
        </div>
      )}
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(Number(e.target.value))}
        style={{
          ...trackStyle,
          background: `linear-gradient(to right, var(--color-accent) 0%, var(--color-accent) ${percent}%, var(--color-border) ${percent}%, var(--color-border) 100%)`,
          opacity: disabled ? 0.5 : 1,
          cursor: disabled ? 'not-allowed' : 'pointer',
          minHeight: 44,
        }}
      />
    </div>
  );
}
