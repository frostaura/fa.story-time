import { v4 as uuidv4 } from 'uuid';
import { ChildProfile, StoryRecord, AppSettings } from '../types/story';

const STORAGE_KEYS = {
  SOFT_USER_ID: 'storytime_soft_user_id',
  PROFILES: 'storytime_profiles',
  STORIES: 'storytime_stories',
  SETTINGS: 'storytime_settings',
};

// Generate or retrieve SoftUserId
export function getSoftUserId(): string {
  const userId = localStorage.getItem(STORAGE_KEYS.SOFT_USER_ID);
  if (!userId) {
    const newUserId = uuidv4();
    localStorage.setItem(STORAGE_KEYS.SOFT_USER_ID, newUserId);
    return newUserId;
  }
  return userId;
}

// Child Profiles
export function getProfiles(): ChildProfile[] {
  const data = localStorage.getItem(STORAGE_KEYS.PROFILES);
  return data ? JSON.parse(data) : [];
}

export function saveProfile(profile: ChildProfile): void {
  const profiles = getProfiles();
  const index = profiles.findIndex((p) => p.id === profile.id);
  if (index >= 0) {
    profiles[index] = profile;
  } else {
    profiles.push(profile);
  }
  localStorage.setItem(STORAGE_KEYS.PROFILES, JSON.stringify(profiles));
}

export function deleteProfile(id: string): void {
  const profiles = getProfiles().filter((p) => p.id !== id);
  localStorage.setItem(STORAGE_KEYS.PROFILES, JSON.stringify(profiles));
}

// Stories
export function getStories(): StoryRecord[] {
  const data = localStorage.getItem(STORAGE_KEYS.STORIES);
  return data ? JSON.parse(data) : [];
}

export function saveStory(story: StoryRecord): void {
  const stories = getStories();
  const index = stories.findIndex((s) => s.id === story.id);
  if (index >= 0) {
    stories[index] = story;
  } else {
    stories.push(story);
  }
  localStorage.setItem(STORAGE_KEYS.STORIES, JSON.stringify(stories));
}

export function deleteStory(id: string): void {
  const stories = getStories().filter((s) => s.id !== id);
  localStorage.setItem(STORAGE_KEYS.STORIES, JSON.stringify(stories));
}

export function toggleFavorite(id: string): void {
  const stories = getStories();
  const story = stories.find((s) => s.id === id);
  if (story) {
    story.isFavorite = !story.isFavorite;
    localStorage.setItem(STORAGE_KEYS.STORIES, JSON.stringify(stories));
  }
}

export function getFavorites(): StoryRecord[] {
  return getStories().filter((s) => s.isFavorite);
}

// Settings
export function getSettings(): AppSettings {
  const data = localStorage.getItem(STORAGE_KEYS.SETTINGS);
  return data
    ? JSON.parse(data)
    : { clarityEnabled: false, notificationsEnabled: false, theme: 'dark' };
}

export function saveSettings(settings: AppSettings): void {
  localStorage.setItem(STORAGE_KEYS.SETTINGS, JSON.stringify(settings));
}

// Cleanup
export function cleanExpiredStories(): void {
  const stories = getStories();
  const now = new Date().toISOString();
  const validStories = stories.filter((s) => !s.expiresAt || s.expiresAt > now);
  localStorage.setItem(STORAGE_KEYS.STORIES, JSON.stringify(validStories));
}
