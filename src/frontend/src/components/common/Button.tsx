/* ───────────────────────────────────────────────
 * Button – primary / secondary / ghost variants
 * ─────────────────────────────────────────────── */

import { type ButtonHTMLAttributes, type CSSProperties } from 'react';
import { motion } from 'framer-motion';

type Variant = 'primary' | 'secondary' | 'ghost';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  fullWidth?: boolean;
}

const base: CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 8,
  minHeight: 44,
  padding: '10px 24px',
  borderRadius: 'var(--radius-sm)',
  fontSize: 15,
  fontWeight: 600,
  fontFamily: 'var(--font-sans)',
  cursor: 'pointer',
  border: 'none',
  outline: 'none',
  transition: 'background 200ms, box-shadow 200ms',
  WebkitTapHighlightColor: 'transparent',
};

const variants: Record<Variant, CSSProperties> = {
  primary: {
    background: 'var(--color-accent)',
    color: '#FFFFFF',
  },
  secondary: {
    background: 'transparent',
    color: 'var(--color-text)',
    border: '1.5px solid var(--color-border)',
  },
  ghost: {
    background: 'transparent',
    color: 'var(--color-text-secondary)',
  },
};

export default function Button({
  variant = 'primary',
  fullWidth,
  style,
  children,
  disabled,
  ...rest
}: ButtonProps) {
  return (
    <motion.button
      whileHover={disabled ? undefined : { scale: 1.02 }}
      whileTap={disabled ? undefined : { scale: 0.97 }}
      style={{
        ...base,
        ...variants[variant],
        width: fullWidth ? '100%' : undefined,
        opacity: disabled ? 0.5 : 1,
        cursor: disabled ? 'not-allowed' : 'pointer',
        ...style,
      }}
      disabled={disabled}
      {...(rest as Record<string, unknown>)}
    >
      {children}
    </motion.button>
  );
}
