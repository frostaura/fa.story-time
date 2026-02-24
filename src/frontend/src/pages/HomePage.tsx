/* ───────────────────────────────────────────────
 * HomePage – main screen with generation + library
 * ─────────────────────────────────────────────── */

import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';
import QuickGenerateCard from '../components/home/QuickGenerateCard';
import DurationSlider from '../components/home/DurationSlider';
import StoryLibrary from '../components/home/StoryLibrary';

export default function HomePage() {
  const { onboardingComplete, kidMode, currentProfileId } = useAppSelector(
    (s) => s.app,
  );
  const [duration, setDuration] = useState(5);

  if (!onboardingComplete) {
    return <Navigate to="/onboarding" replace />;
  }

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-lg)',
      }}
    >
      {/* Generation controls (hidden in Kid Mode) */}
      {!kidMode && currentProfileId && (
        <>
          <QuickGenerateCard duration={duration} />
          <DurationSlider value={duration} onChange={setDuration} />
        </>
      )}

      {/* Story library */}
      <StoryLibrary />
    </div>
  );
}
