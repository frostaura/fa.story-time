export interface StoryScene {
  id: string;
  order: number;
  summary: string;
  text: string;
  visualDescriptor: {
    location: string;
    timeOfDay: string;
    mood: string;
    keyObjects: string[];
    charactersPresent: string[];
    paletteHints: string[];
  };
}

export interface StoryVisualPlan {
  storySeed: string;
  dominantPalette: string[];
  lowPolyStyle: 'storybook-lowpoly';
  keyScenesForPosters: string[];
  layers: {
    layerId: string;
    sceneId: string;
    depth: 'background' | 'midground' | 'foreground' | 'accent';
    prompt: string;
  }[];
}

export interface StoryRecord {
  id: string;
  childProfileId: string | null;
  tierSlug: 'trial' | 'plus' | 'premium';
  title: string;
  summary: string;
  text: string;
  scenes: StoryScene[];
  visualPlan: StoryVisualPlan;
  posterLayers: {
    layerId: string;
    depth: 'background' | 'midground' | 'foreground' | 'accent';
    imageBase64: string;
  }[] | null;
  audioBase64: string | null;
  createdAt: string;
  isFavorite: boolean;
  expiresAt?: string;
}

export interface ChildProfile {
  id: string;
  name: string;
  age: number;
  avatar: string;
  favoriteThemes: string[];
  createdAt: string;
}

export interface AppSettings {
  clarityEnabled: boolean;
  notificationsEnabled: boolean;
  theme: 'dark' | 'light';
}
