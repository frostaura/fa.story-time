/* ───────────────────────────────────────────────
 * Spinner – calm loading indicator
 * ─────────────────────────────────────────────── */

interface SpinnerProps {
  size?: number;
  color?: string;
}

export default function Spinner({
  size = 24,
  color = 'var(--color-accent)',
}: SpinnerProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      style={{ animation: 'tw-spin 1s linear infinite' }}
      aria-label="Loading"
      role="status"
    >
      <circle
        cx="12"
        cy="12"
        r="10"
        stroke={color}
        strokeWidth="3"
        strokeLinecap="round"
        strokeDasharray="50 14"
        opacity="0.8"
      />
      <style>{`
        @keyframes tw-spin {
          to { transform: rotate(360deg); }
        }
        @media (prefers-reduced-motion: reduce) {
          svg[role="status"] {
            animation: none !important;
            opacity: 0.6;
          }
        }
      `}</style>
    </svg>
  );
}
