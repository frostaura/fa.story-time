/* ───────────────────────────────────────────────
 * OnboardingWizard – 3-step wizard with progress
 * ─────────────────────────────────────────────── */

import { useState } from 'react';
import ProfileStep from './ProfileStep';
import PreferencesStep from './PreferencesStep';
import TrialStep from './TrialStep';
import type { ChildProfile } from '../../types';

interface OnboardingWizardProps {
  onComplete: (profile: ChildProfile) => void;
}

export default function OnboardingWizard({ onComplete }: OnboardingWizardProps) {
  const [step, setStep] = useState(0);
  const [profile, setProfile] = useState<Partial<ChildProfile>>({});

  const handleProfileNext = (data: {
    name: string;
    age: number;
    themes: string[];
    favoriteCharacters: string[];
  }) => {
    setProfile((p) => ({ ...p, ...data }));
    setStep(1);
  };

  const handlePrefsNext = (data: {
    narratorVoice: string;
    defaultDuration: number;
    preferredMode: 'series' | 'oneshot';
  }) => {
    setProfile((p) => ({ ...p, ...data }));
    setStep(2);
  };

  const handleComplete = () => {
    const fullProfile: ChildProfile = {
      id: crypto.randomUUID(),
      name: profile.name ?? '',
      age: profile.age ?? 5,
      themes: profile.themes ?? [],
      favoriteCharacters: profile.favoriteCharacters ?? [],
      narratorVoice: profile.narratorVoice ?? 'warm-female',
      defaultDuration: profile.defaultDuration ?? 5,
      preferredMode: profile.preferredMode ?? 'oneshot',
    };
    onComplete(fullProfile);
  };

  return (
    <div>
      {/* Progress dots */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          gap: 8,
          marginBottom: 'var(--spacing-xl)',
        }}
      >
        {[0, 1, 2].map((i) => (
          <div
            key={i}
            style={{
              width: i === step ? 24 : 8,
              height: 8,
              borderRadius: 4,
              background: i <= step ? 'var(--color-accent)' : 'var(--color-border)',
              transition: 'all 300ms ease',
            }}
          />
        ))}
      </div>

      {/* Steps */}
      {step === 0 && <ProfileStep onNext={handleProfileNext} />}
      {step === 1 && (
        <PreferencesStep onNext={handlePrefsNext} onBack={() => setStep(0)} />
      )}
      {step === 2 && (
        <TrialStep onComplete={handleComplete} onBack={() => setStep(1)} />
      )}
    </div>
  );
}
