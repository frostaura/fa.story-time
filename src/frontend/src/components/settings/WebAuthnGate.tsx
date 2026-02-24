/* ───────────────────────────────────────────────
 * WebAuthnGate – biometric / PIN before settings
 * ─────────────────────────────────────────────── */

import { useState, useCallback, type ReactNode } from 'react';
import { Fingerprint, KeyRound } from 'lucide-react';
import Button from '../common/Button';

interface WebAuthnGateProps {
  children: ReactNode;
}

const PIN_KEY = 'tw_parental_pin';

export default function WebAuthnGate({ children }: WebAuthnGateProps) {
  const [authenticated, setAuthenticated] = useState(false);
  const [pinInput, setPinInput] = useState('');
  const [showPinInput, setShowPinInput] = useState(false);
  const [error, setError] = useState('');

  const storedPin = localStorage.getItem(PIN_KEY);

  /* Try WebAuthn first */
  const tryWebAuthn = useCallback(async () => {
    try {
      if (!window.PublicKeyCredential) {
        setShowPinInput(true);
        return;
      }

      const available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
      if (!available) {
        setShowPinInput(true);
        return;
      }

      // Use a simple assertion to verify the user is present
      const challenge = crypto.getRandomValues(new Uint8Array(32));
      await navigator.credentials.get({
        publicKey: {
          challenge,
          rpId: window.location.hostname,
          userVerification: 'required',
          timeout: 60000,
        },
      });

      setAuthenticated(true);
    } catch {
      // If WebAuthn fails, fall back to PIN
      setShowPinInput(true);
    }
  }, []);

  const handlePin = useCallback(() => {
    if (!storedPin) {
      // First time: set the PIN
      if (pinInput.length >= 4) {
        localStorage.setItem(PIN_KEY, pinInput);
        setAuthenticated(true);
      } else {
        setError('PIN must be at least 4 digits');
      }
    } else {
      // Verify PIN
      if (pinInput === storedPin) {
        setAuthenticated(true);
      } else {
        setError('Incorrect PIN');
        setPinInput('');
      }
    }
  }, [pinInput, storedPin]);

  if (authenticated) return <>{children}</>;

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 'var(--spacing-lg)',
        padding: 'var(--spacing-2xl) var(--spacing-lg)',
        textAlign: 'center',
        minHeight: '60vh',
      }}
    >
      <Fingerprint
        size={48}
        style={{ color: 'var(--color-accent)', opacity: 0.8 }}
      />
      <h2 style={{ margin: 0, fontSize: 20, fontWeight: 700 }}>
        Ask a grown-up 🔒
      </h2>
      <p
        style={{
          margin: 0,
          fontSize: 14,
          color: 'var(--color-text-secondary)',
          maxWidth: 300,
          lineHeight: 1.5,
        }}
      >
        Parental verification is required to access settings.
      </p>

      {!showPinInput ? (
        <Button onClick={tryWebAuthn}>
          <Fingerprint size={18} />
          Verify Identity
        </Button>
      ) : (
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            gap: 12,
            width: '100%',
            maxWidth: 240,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <KeyRound size={18} style={{ color: 'var(--color-text-secondary)' }} />
            <input
              type="password"
              inputMode="numeric"
              pattern="[0-9]*"
              placeholder={storedPin ? 'Enter PIN' : 'Set a new PIN (4+ digits)'}
              value={pinInput}
              onChange={(e) => {
                setPinInput(e.target.value.replace(/\D/g, ''));
                setError('');
              }}
              onKeyDown={(e) => { if (e.key === 'Enter') handlePin(); }}
              style={{
                flex: 1,
                padding: '12px 16px',
                border: '1.5px solid var(--color-border)',
                borderRadius: 'var(--radius-sm)',
                fontSize: 18,
                fontFamily: 'var(--font-sans)',
                background: 'var(--color-bg)',
                color: 'var(--color-text)',
                textAlign: 'center',
                letterSpacing: 8,
                minHeight: 44,
              }}
              autoFocus
            />
          </div>
          {error && (
            <p style={{ margin: 0, fontSize: 13, color: '#EF4444' }}>{error}</p>
          )}
          <Button fullWidth onClick={handlePin}>
            {storedPin ? 'Unlock' : 'Set PIN'}
          </Button>
        </div>
      )}
    </div>
  );
}
