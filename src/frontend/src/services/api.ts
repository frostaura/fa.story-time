// Use environment variable for API base URL, fallback to relative path for dev proxy
const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export interface GenerateStoryRequest {
  softUserId: string;
  childProfileId?: string;
  age: number;
  theme: string;
  customTheme?: string;
}

export interface StoryResponse {
  id: string;
  title: string;
  summary: string;
  text: string;
  scenes: any[];
  visualPlan: any;
  posterLayers: any[] | null;
  audioBase64: string | null;
  tierSlug: 'trial' | 'plus' | 'premium';
  createdAt: string;
  expiresAt?: string;
}

export interface Tier {
  slug: string;
  name: string;
  price: number;
  features: string[];
}

// Generate Story
export async function generateStory(request: GenerateStoryRequest): Promise<StoryResponse> {
  const response = await fetch(`${API_BASE_URL}/api/stories/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to generate story');
  }
  return response.json();
}

// Get Tiers
export async function getTiers(): Promise<Tier[]> {
  const response = await fetch(`${API_BASE_URL}/api/tiers`);
  if (!response.ok) {
    throw new Error('Failed to fetch tiers');
  }
  return response.json();
}

// Generate Image
export async function generateImage(prompt: string, style: string): Promise<string> {
  const response = await fetch(`${API_BASE_URL}/api/images/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, style }),
  });
  if (!response.ok) {
    throw new Error('Failed to generate image');
  }
  const data = await response.json();
  return data.imageBase64;
}

// Generate Speech
export async function generateSpeech(text: string, voice?: string): Promise<string> {
  const response = await fetch(`${API_BASE_URL}/api/tts/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text, voice }),
  });
  if (!response.ok) {
    throw new Error('Failed to generate speech');
  }
  const data = await response.json();
  return data.audioBase64;
}

// Subscribe Push Notifications
export async function subscribePush(subscription: PushSubscription): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/push/subscribe`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(subscription),
  });
  if (!response.ok) {
    throw new Error('Failed to subscribe to push notifications');
  }
}

// Health Check
export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/health`);
    return response.ok;
  } catch {
    return false;
  }
}
