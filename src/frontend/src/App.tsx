/* ───────────────────────────────────────────────
 * App – root component with routing
 * ─────────────────────────────────────────────── */

import { BrowserRouter, Routes, Route } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import HomePage from './pages/HomePage';
import StoryPage from './pages/StoryPage';
import OnboardingPage from './pages/OnboardingPage';
import SettingsPage from './pages/SettingsPage';
import { getSoftUserId } from './services/localStorage';
import { useEffect } from 'react';

export default function App() {
  /* Ensure softUserId exists on mount */
  useEffect(() => {
    getSoftUserId();
  }, []);

  return (
    <BrowserRouter>
      <Routes>
        {/* Onboarding (no layout shell) */}
        <Route path="/onboarding" element={<OnboardingPage />} />

        {/* Main app with layout */}
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/story/:id" element={<StoryPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
