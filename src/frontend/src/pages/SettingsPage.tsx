/* ───────────────────────────────────────────────
 * SettingsPage – /settings (behind WebAuthnGate)
 * ─────────────────────────────────────────────── */

import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import WebAuthnGate from '../components/settings/WebAuthnGate';
import ParentalSettings from '../components/settings/ParentalSettings';

export default function SettingsPage() {
  const navigate = useNavigate();

  return (
    <div>
      <button
        onClick={() => navigate('/')}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          color: 'var(--color-text-secondary)',
          fontSize: 14,
          fontWeight: 500,
          padding: '8px 0',
          marginBottom: 'var(--spacing-md)',
          minHeight: 44,
          fontFamily: 'var(--font-sans)',
        }}
      >
        <ArrowLeft size={16} />
        Back
      </button>

      <WebAuthnGate>
        <ParentalSettings />
      </WebAuthnGate>
    </div>
  );
}
