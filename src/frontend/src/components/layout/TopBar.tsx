/* ───────────────────────────────────────────────
 * TopBar – app name + profile switcher + settings
 * ─────────────────────────────────────────────── */

import { Settings, ChevronDown, ShieldCheck } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../../store/hooks';
import { setCurrentProfile } from '../../store/slices/appSlice';
import { getProfiles } from '../../services/localStorage';

export default function TopBar() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { kidMode, currentProfileId } = useAppSelector((s) => s.app);
  const profiles = getProfiles();
  const currentProfile = profiles.find((p) => p.id === currentProfileId);

  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!dropdownOpen) return;
    const close = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [dropdownOpen]);

  return (
    <header
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 var(--spacing-lg)',
        height: 56,
        background: 'var(--color-surface)',
        borderBottom: '1px solid var(--color-border)',
        position: 'sticky',
        top: 0,
        zIndex: 100,
      }}
    >
      {/* Left – app name */}
      <h1
        style={{
          margin: 0,
          fontSize: 20,
          fontWeight: 700,
          color: 'var(--color-text)',
          letterSpacing: '-0.02em',
        }}
      >
        TaleWeaver
        {kidMode && (
          <ShieldCheck
            size={16}
            style={{ marginLeft: 6, color: 'var(--color-accent)', verticalAlign: 'middle' }}
            aria-label="Kid Mode active"
          />
        )}
      </h1>

      {/* Right – profile + settings */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        {/* Profile switcher */}
        {profiles.length > 0 && (
          <div ref={dropdownRef} style={{ position: 'relative' }}>
            <button
              onClick={() => setDropdownOpen(!dropdownOpen)}
              aria-label="Switch profile"
              aria-expanded={dropdownOpen}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                padding: '6px 12px',
                background: 'var(--color-bg)',
                border: '1px solid var(--color-border)',
                borderRadius: 'var(--radius-sm)',
                fontSize: 14,
                fontWeight: 500,
                color: 'var(--color-text)',
                cursor: 'pointer',
                minHeight: 36,
                fontFamily: 'var(--font-sans)',
              }}
            >
              {currentProfile?.name ?? 'Select Profile'}
              <ChevronDown size={14} />
            </button>

            {dropdownOpen && (
              <div
                role="listbox"
                style={{
                  position: 'absolute',
                  top: '100%',
                  right: 0,
                  marginTop: 4,
                  minWidth: 180,
                  background: 'var(--color-surface)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--radius-md)',
                  boxShadow: '0 8px 24px var(--color-shadow)',
                  padding: '4px 0',
                  zIndex: 200,
                }}
              >
                {profiles.map((p) => (
                  <button
                    key={p.id}
                    role="option"
                    aria-selected={p.id === currentProfileId}
                    onClick={() => {
                      dispatch(setCurrentProfile(p.id));
                      setDropdownOpen(false);
                    }}
                    style={{
                      display: 'block',
                      width: '100%',
                      padding: '10px 16px',
                      background:
                        p.id === currentProfileId
                          ? 'var(--color-bg)'
                          : 'transparent',
                      border: 'none',
                      fontSize: 14,
                      fontWeight: p.id === currentProfileId ? 600 : 400,
                      color: 'var(--color-text)',
                      cursor: 'pointer',
                      textAlign: 'left',
                      minHeight: 44,
                      fontFamily: 'var(--font-sans)',
                    }}
                  >
                    {p.name} · {p.age}y
                  </button>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Settings (hidden in kid mode) */}
        {!kidMode && (
          <button
            onClick={() => navigate('/settings')}
            aria-label="Settings"
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 44,
              height: 44,
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--color-text-secondary)',
            }}
          >
            <Settings size={20} />
          </button>
        )}
      </div>
    </header>
  );
}
