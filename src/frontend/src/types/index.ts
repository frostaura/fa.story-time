/* ───────────────────────────────────────────────
 * TaleWeaver – shared TypeScript interfaces
 * ─────────────────────────────────────────────── */

export interface ChildProfile {
  id: string;
  name: string;
  age: number;
  themes: string[];
  favoriteCharacters: string[];
  narratorVoice: string;
  defaultDuration: number; // minutes
  preferredMode: 'series' | 'oneshot';
}

export interface Story {
  id: string;
  title: string;
  seriesId?: string;
  episodeNumber?: number;
  childProfileId: string;
  mode: 'series' | 'oneshot';
  duration: number;
  scenes: Scene[];
  posterLayers: PosterLayer[];
  audioBase64?: string;
  teaserAudioBase64?: string;
  isFavorite: boolean;
  createdAt: string;
  playbackPosition?: number;
}

export interface Scene {
  index: number;
  setting: string;
  mood: string;
  narrationText: string;
  musicTrackQuery?: string;
  sfxKeywords?: string[];
}

export interface PosterLayer {
  role: 'BACKGROUND' | 'MIDGROUND_1' | 'MIDGROUND_2' | 'FOREGROUND' | 'PARTICLES';
  imageBase64: string;
  width: number;
  height: number;
}

export interface StoryBible {
  seriesId: string;
  visualIdentity: {
    paletteSeed: string;
    styleToken: string;
    silhouetteToken: string;
    environmentToken: string;
  };
  characters: Array<{
    name: string;
    role: string;
    traits: string[];
    fixedVisualTraits: string[];
  }>;
  locations: Array<{ name: string; descriptor: string }>;
  continuityFacts: string[];
  arcState: {
    goal: string;
    obstacles: string[];
    progress: string;
    unresolvedThreads: string[];
  };
  lastEpisodeSummary: string;
  anchoredAudio: Record<number, string>;
}

export interface AppSettings {
  approvalStepEnabled: boolean;
  notificationsEnabled: boolean;
  notificationToggles: {
    generationComplete: boolean;
    queueProgress: boolean;
    cooldownComplete: boolean;
    subscriptionEvents: boolean;
    promos: boolean;
  };
  analyticsEnabled: boolean;
  kidModeEnabled: boolean;
  reducedMotion: boolean;
}

export interface CooldownState {
  lastGenerationAt: string | null;
  cooldownMinutes: number;
}

export interface GenerationRequest {
  childProfileId: string;
  mode: 'series' | 'oneshot';
  duration: number;
  seriesId?: string;
  storyBible?: StoryBible;
  themes?: string[];
  characters?: string[];
}

export interface GenerationStatus {
  correlationId: string;
  status: 'queued' | 'generating' | 'complete' | 'failed';
  progress?: string;
  story?: Story;
  error?: string;
}

export interface Tier {
  id: string;
  name: string;
  concurrency: number;
  cooldownMinutes: number;
  allowedLengths: string[];
  hasLockScreenArt: boolean;
  hasLongStories: boolean;
  hasHighQualityBudget: boolean;
}
