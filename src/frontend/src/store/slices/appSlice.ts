/* ───────────────────────────────────────────────
 * appSlice – global application state
 * ─────────────────────────────────────────────── */

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { AppSettings } from '../../types';
import {
  getAppSettings,
  saveAppSettings,
  isOnboardingComplete as lsOnboardingComplete,
  getProfiles,
} from '../../services/localStorage';

interface AppState {
  kidMode: boolean;
  currentProfileId: string | null;
  settings: AppSettings;
  onboardingComplete: boolean;
}

const settings = getAppSettings();
const profiles = getProfiles();

const initialState: AppState = {
  kidMode: settings.kidModeEnabled,
  currentProfileId: profiles[0]?.id ?? null,
  settings,
  onboardingComplete: lsOnboardingComplete(),
};

const appSlice = createSlice({
  name: 'app',
  initialState,
  reducers: {
    setKidMode(state, action: PayloadAction<boolean>) {
      state.kidMode = action.payload;
      state.settings.kidModeEnabled = action.payload;
      saveAppSettings(state.settings);
    },
    setCurrentProfile(state, action: PayloadAction<string>) {
      state.currentProfileId = action.payload;
    },
    updateSettings(state, action: PayloadAction<Partial<AppSettings>>) {
      state.settings = { ...state.settings, ...action.payload };
      saveAppSettings(state.settings);
    },
    setOnboardingComplete(state, action: PayloadAction<boolean>) {
      state.onboardingComplete = action.payload;
    },
  },
});

export const { setKidMode, setCurrentProfile, updateSettings, setOnboardingComplete } =
  appSlice.actions;
export default appSlice.reducer;
