/* ───────────────────────────────────────────────
 * OnboardingPage – /onboarding
 * ─────────────────────────────────────────────── */

import { useNavigate } from 'react-router-dom';
import OnboardingWizard from '../components/onboarding/OnboardingWizard';
import { useAppDispatch } from '../store/hooks';
import { setOnboardingComplete, setCurrentProfile } from '../store/slices/appSlice';
import {
  saveProfile,
  setOnboardingComplete as lsSetOnboardingComplete,
} from '../services/localStorage';
import * as api from '../services/api';
import type { ChildProfile } from '../types';

export default function OnboardingPage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const handleComplete = async (profile: ChildProfile) => {
    // Save profile locally
    saveProfile(profile);
    lsSetOnboardingComplete(true);

    // Update Redux
    dispatch(setCurrentProfile(profile.id));
    dispatch(setOnboardingComplete(true));

    // Start trial (fire-and-forget)
    api.startTrial().catch(() => {});

    navigate('/', { replace: true });
  };

  return (
    <div
      style={{
        maxWidth: 480,
        margin: '0 auto',
        padding: 'var(--spacing-xl) var(--spacing-md)',
      }}
    >
      <OnboardingWizard onComplete={handleComplete} />
    </div>
  );
}
