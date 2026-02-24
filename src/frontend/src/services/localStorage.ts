/* ───────────────────────────────────────────────
 * Typed LocalStorage wrapper – all keys prefixed tw_
 * ─────────────────────────────────────────────── */

import { LS_KEYS } from '../types/localStorage';
import type {
  AppSettings,
  ChildProfile,
  CooldownState,
  Story,
  StoryBible,
} from '../types';

/* ── helpers ── */

function read<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

function write<T>(key: string, value: T): void {
  localStorage.setItem(key, JSON.stringify(value));
}

function generateUUID(): string {
  return crypto.randomUUID?.() ?? 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(
    /[xy]/g,
    (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    },
  );
}

/* ── Soft User ID ── */

export function getSoftUserId(): string {
  let id = localStorage.getItem(LS_KEYS.softUserId);
  if (!id) {
    id = generateUUID();
    localStorage.setItem(LS_KEYS.softUserId, id);
  }
  return id;
}

/* ── Profiles ── */

export function getProfiles(): ChildProfile[] {
  return read<ChildProfile[]>(LS_KEYS.childrenProfiles) ?? [];
}

export function saveProfile(profile: ChildProfile): void {
  const profiles = getProfiles();
  const idx = profiles.findIndex((p) => p.id === profile.id);
  if (idx >= 0) {
    profiles[idx] = profile;
  } else {
    profiles.push(profile);
  }
  write(LS_KEYS.childrenProfiles, profiles);
}

export function deleteProfile(id: string): void {
  const profiles = getProfiles().filter((p) => p.id !== id);
  write(LS_KEYS.childrenProfiles, profiles);
}

/* ── Stories ── */

export function getStories(): Story[] {
  return read<Story[]>(LS_KEYS.stories) ?? [];
}

export function saveStory(story: Story): void {
  const stories = getStories();
  const idx = stories.findIndex((s) => s.id === story.id);
  if (idx >= 0) {
    stories[idx] = story;
  } else {
    stories.unshift(story);
  }
  write(LS_KEYS.stories, stories);
}

export function toggleFavorite(storyId: string): void {
  const stories = getStories();
  const story = stories.find((s) => s.id === storyId);
  if (story) {
    story.isFavorite = !story.isFavorite;
    write(LS_KEYS.stories, stories);
  }
}

export function deleteStory(storyId: string): void {
  const stories = getStories().filter((s) => s.id !== storyId);
  write(LS_KEYS.stories, stories);
}

/* ── Story Bibles ── */

export function getBibles(): StoryBible[] {
  return read<StoryBible[]>(LS_KEYS.seriesBibles) ?? [];
}

export function getBible(seriesId: string): StoryBible | undefined {
  return getBibles().find((b) => b.seriesId === seriesId);
}

export function saveBible(bible: StoryBible): void {
  const bibles = getBibles();
  const idx = bibles.findIndex((b) => b.seriesId === bible.seriesId);
  if (idx >= 0) {
    bibles[idx] = bible;
  } else {
    bibles.push(bible);
  }
  write(LS_KEYS.seriesBibles, bibles);
}

/* ── App Settings ── */

const DEFAULT_SETTINGS: AppSettings = {
  approvalStepEnabled: true,
  notificationsEnabled: false,
  notificationToggles: {
    generationComplete: true,
    queueProgress: false,
    cooldownComplete: true,
    subscriptionEvents: true,
    promos: false,
  },
  analyticsEnabled: false,
  kidModeEnabled: false,
  reducedMotion: false,
};

export function getAppSettings(): AppSettings {
  return read<AppSettings>(LS_KEYS.appSettings) ?? { ...DEFAULT_SETTINGS };
}

export function saveAppSettings(settings: AppSettings): void {
  write(LS_KEYS.appSettings, settings);
}

/* ── Cooldown ── */

const DEFAULT_COOLDOWN: CooldownState = {
  lastGenerationAt: null,
  cooldownMinutes: 10,
};

export function getCooldownState(): CooldownState {
  return read<CooldownState>(LS_KEYS.cooldownState) ?? { ...DEFAULT_COOLDOWN };
}

export function saveCooldownState(state: CooldownState): void {
  write(LS_KEYS.cooldownState, state);
}

/* ── Onboarding ── */

export function isOnboardingComplete(): boolean {
  return read<boolean>(LS_KEYS.onboardingComplete) === true;
}

export function setOnboardingComplete(complete: boolean): void {
  write(LS_KEYS.onboardingComplete, complete);
}

/* ── Danger zone ── */

export function clearAllStories(): void {
  localStorage.removeItem(LS_KEYS.stories);
  localStorage.removeItem(LS_KEYS.seriesBibles);
}

export function clearAllData(): void {
  Object.values(LS_KEYS).forEach((key) => localStorage.removeItem(key));
}
