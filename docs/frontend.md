# TaleWeaver — Frontend Specification

> Design language, component catalogue, LocalStorage schema, and accessibility requirements.

---

## 1 Design Language

### 1.1 Design Direction

**Monday.com × Apple App Store** — clean, neutral surfaces with generous whitespace. The UI itself is calm and recessive; **color is reserved for story poster art and accent actions only**.

| Attribute | Guideline |
|---|---|
| **Surfaces** | Neutral whites and light grays (`#FAFAFA`, `#F5F5F5`, `#FFFFFF`). Dark mode: `#1A1A2E`, `#16213E`. |
| **Typography** | System font stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`). Headings: 600 weight. Body: 400 weight. |
| **Color** | Primary accent: `#6C5CE7` (soft purple). Secondary accent: `#00B894` (mint). Destructive: `#FF6B6B`. All other UI is grayscale. |
| **Whitespace** | Minimum 16px padding on all content areas. 24px between card groups. 32px page margins on mobile. |
| **Elevation** | Cards use subtle box-shadow (`0 2px 8px rgba(0,0,0,0.08)`). Modals use `0 8px 32px rgba(0,0,0,0.12)`. |
| **Border Radius** | Cards: 16px. Buttons: 12px. Inputs: 8px. Posters: 20px. |
| **Motion** | Subtle spring transitions (200-300ms). Respect `prefers-reduced-motion`. |
| **Iconography** | Lucide icons (outlined, 24px default). Filled variants for active states. |

### 1.2 Layout

| Breakpoint | Behavior |
|---|---|
| **Mobile** (< 768px) | Single column, full-width cards, bottom navigation |
| **Tablet** (768px–1024px) | Single column, centered with max-width 640px |
| **Desktop** (> 1024px) | Centered column, max-width 480px (phone-like container with surrounding whitespace) |

The app is **mobile-first**. Desktop users see a centered phone-width column to maintain the intimate bedtime-story feel.

---

## 2 Component Catalogue

### 2.1 QuickGenerateCard

The hero component on the home screen. A large, inviting card that initiates story generation.

| Property | Detail |
|---|---|
| **Position** | Top of home screen, full width |
| **Content** | Active child name + avatar, "Generate Story" CTA button, DurationSlider |
| **States** | `idle`, `generating` (shimmer + progress), `cooldown` (countdown timer), `offline` (queued badge) |
| **Interactions** | Tap CTA → triggers UC-1. Long-press → opens profile selector. |

### 2.2 DurationSlider

A segmented control for selecting story length.

| Property | Detail |
|---|---|
| **Options** | `Short` (3-5 min), `Medium` (8-12 min), `Long` (15-20 min) |
| **Tier Gating** | Disabled options show a lock icon + "Upgrade" tooltip. Only `AllowedLengths` from the user's tier are selectable. |
| **Style** | Pill-shaped segmented control. Active segment uses primary accent. |

### 2.3 StoryCard

A library card representing a single generated story.

| Property | Detail |
|---|---|
| **Layout** | Horizontal card with parallax thumbnail (left), title + metadata (right) |
| **Thumbnail** | First poster layer, 80×120px with 12px radius. Subtle parallax on scroll. |
| **Metadata** | Title, child name, date, duration badge, series badge (if applicable) |
| **Actions** | Tap → open StoryModal. Swipe left → delete (with confirmation). Long-press → "Continue Series" (if Story Bible exists). |

### 2.4 StoryLibrary

The main story browsing view.

| Property | Detail |
|---|---|
| **Layout** | Vertical scrolling list of StoryCards |
| **Filtering** | Tabs: "All", "Series", "Favorites". Search bar (title search). |
| **Sorting** | Most recent first (default). Alphabetical. By child profile. |
| **Empty State** | Friendly illustration + "Generate your first story!" CTA |

### 2.5 StoryModal

Full-screen story playback experience.

| Property | Detail |
|---|---|
| **Layout** | Full-screen overlay with ParallaxPoster as background, text overlay in lower third |
| **Text** | Scene-by-scene display. Large readable font (18px mobile, 20px desktop). White text on dark gradient overlay. |
| **Audio Controls** | Play/pause (center), skip back/forward (±1 scene), volume slider |
| **Gestures** | Swipe left/right to navigate scenes. Tap to toggle controls visibility. |
| **Exit** | Top-left close button (X) or swipe down. |

### 2.6 ParallaxPoster

Multi-layered illustration with depth effect.

| Property | Detail |
|---|---|
| **Layers** | Background (static), midground (slow parallax), foreground (fast parallax) |
| **Trigger** | Device gyroscope tilt (mobile) or mouse position (desktop) |
| **Fallback** | Static centered image if gyroscope unavailable or reduced motion preferred |
| **Performance** | GPU-accelerated transforms only. Layers are pre-composited images. |

### 2.7 OnboardingWizard

Multi-step first-run wizard (see UC-3).

| Property | Detail |
|---|---|
| **Steps** | 4 steps: Welcome → Child Profile → Voice Selection → Trial Activation |
| **Navigation** | Back/Next buttons. Step indicator dots. Cannot skip Child Profile. |
| **Style** | Full-screen modal with slide transitions between steps. |
| **Voice Preview** | Audio play buttons next to each voice option with a sample sentence. |

### 2.8 ParentalSettings

WebAuthn-gated settings panel (see UC-5).

| Property | Detail |
|---|---|
| **Access** | Gear icon in header → WebAuthn challenge → settings panel |
| **Sections** | Profiles, Subscription, Content Filters, Voices, Kid Mode, Data Management |
| **Layout** | Grouped list with section headers. Toggle switches for boolean settings. |

### 2.9 KidShelf

Child-friendly story browsing (see UC-6).

| Property | Detail |
|---|---|
| **Layout** | Large square tiles (2-column grid) with character illustrations |
| **Style** | Rounded corners (24px), playful sans-serif font, bright story-derived colors |
| **Interactions** | Tap to play. No swipe-delete. No settings. No generation controls. |
| **Exit** | Hidden long-press zone (bottom-right corner) → WebAuthn challenge |

### 2.10 NotificationToast

Non-blocking notification overlay.

| Property | Detail |
|---|---|
| **Position** | Top of screen, slides down from edge |
| **Types** | `info` (gray), `success` (mint), `warning` (amber), `error` (red) |
| **Duration** | Auto-dismiss after 4 seconds. Swipe up to dismiss early. |
| **Stacking** | Max 3 visible. Older toasts slide up and out. |

---

## 3 LocalStorage Key Specifications

All user data is stored in browser LocalStorage under these keys:

### 3.1 Key Map

| Key | Type | Description |
|---|---|---|
| `tw_softUserId` | `string` | UUID v4, generated on first launch. Never changes. |
| `tw_childrenProfiles` | `ChildProfile[]` | Array of child profile objects |
| `tw_stories` | `Story[]` | Array of generated story objects |
| `tw_seriesBibles` | `StoryBible[]` | Array of series continuity documents |
| `tw_posterLayers` | `PosterLayerSet[]` | Poster layer image data per story |
| `tw_audioCache` | `AudioEntry[]` | TTS audio blobs per story/scene |
| `tw_appSettings` | `AppSettings` | User preferences and configuration |
| `tw_cooldownState` | `CooldownState` | Client-side cooldown mirror |

> All keys are prefixed with `tw_` to avoid collisions with other apps on the same origin.

### 3.2 Type Definitions

```typescript
interface ChildProfile {
  id: string;               // UUID v4
  name: string;             // Display name
  ageRange: "2-4" | "5-7" | "8-10" | "11-13";
  interests: string[];      // e.g., ["dinosaurs", "space", "princesses"]
  themes: string[];         // e.g., ["adventure", "friendship", "courage"]
  voiceId: string;          // Coqui TTS voice identifier
  avatarSeed: string;       // Seed for deterministic avatar generation
  createdAt: string;        // ISO 8601
}

interface Story {
  id: string;               // UUID v4
  childProfileId: string;   // FK to ChildProfile.id
  title: string;
  duration: "short" | "medium" | "long";
  scenes: Scene[];
  storyBibleId?: string;    // FK to StoryBible.id (if series)
  rating?: number;          // 1-5 stars (optional)
  isRead: boolean;
  createdAt: string;        // ISO 8601
}

interface Scene {
  index: number;
  text: string;
  illustrationPrompt: string;
  posterLayerSetId?: string;
  audioEntryId?: string;
}

interface StoryBible {
  id: string;               // UUID v4
  seriesTitle: string;
  characters: Character[];
  worldRules: string[];     // Continuity rules
  plotPoints: PlotPoint[];  // Key events that have occurred
  createdAt: string;
  updatedAt: string;
}

interface Character {
  name: string;
  description: string;
  traits: string[];
  visualDescription: string;  // For consistent illustrations
}

interface PlotPoint {
  episodeId: string;        // FK to Story.id
  summary: string;
  impact: string;           // How this affects future episodes
}

interface PosterLayerSet {
  id: string;               // UUID v4
  storyId: string;          // FK to Story.id
  sceneIndex: number;
  layers: PosterLayer[];
}

interface PosterLayer {
  depth: "background" | "midground" | "foreground";
  imageData: string;        // base64-encoded PNG or blob URL
}

interface AudioEntry {
  id: string;               // UUID v4
  storyId: string;          // FK to Story.id
  sceneIndex: number;
  audioBlob: string;        // base64-encoded audio or blob URL
  durationMs: number;
}

interface AppSettings {
  theme: "light" | "dark" | "system";
  volume: number;           // 0.0 - 1.0
  reducedMotion: boolean;
  kidModeEnabled: boolean;
  parentalPinHash?: string; // Fallback PIN (bcrypt hash)
  activeProfileId: string;  // FK to ChildProfile.id
}

interface CooldownState {
  lastGenerationAt: string; // ISO 8601
  cooldownMinutes: number;  // Mirrors server tier cooldown
}
```

### 3.3 Storage Budget

| Data Type | Estimated Size | Notes |
|---|---|---|
| Profiles | ~1 KB each | Lightweight JSON |
| Story text | ~5-15 KB each | Depends on duration |
| Story Bible | ~2-5 KB each | Grows with episodes |
| Poster layers | ~200-500 KB each | Compressed PNGs |
| Audio per scene | ~100-300 KB each | Compressed audio |
| **Total per story** | **~500 KB – 2 MB** | With all assets |

LocalStorage limit is typically 5-10 MB. For larger collections, overflow to **IndexedDB** with the same key structure.

---

## 4 Accessibility Requirements

### 4.1 Touch Targets

- **Minimum size**: 44×44px for all interactive elements (WCAG 2.5.8 AAA).
- Buttons, cards, toggles, and slider handles must all meet this minimum.
- Additional padding around tight icon buttons to meet the target.

### 4.2 Reduced Motion

- Respect `prefers-reduced-motion: reduce` media query.
- When active:
  - Disable parallax poster effects (show static image).
  - Replace slide transitions with instant cuts.
  - Disable spring animations on cards.
  - Keep essential state-change transitions (e.g., modal open/close) but reduce to simple fade (150ms).

### 4.3 High Contrast

- Support `prefers-contrast: more` media query.
- Increase border width to 2px on interactive elements.
- Ensure minimum contrast ratio of 7:1 (WCAG AAA) for all text.
- Story text on poster overlay: always white on a dark gradient with minimum 80% opacity.

### 4.4 Screen Reader Support

- All interactive elements have descriptive `aria-label` attributes.
- Story playback announces scene transitions with `aria-live="polite"`.
- StoryLibrary uses `role="list"` with `role="listitem"` for each StoryCard.
- DurationSlider uses `role="radiogroup"` with `role="radio"` for each option.
- Modal components use `role="dialog"` with `aria-modal="true"`.
- Focus trap active in all modals and overlays.

### 4.5 Keyboard Navigation

- Full tab navigation through all interactive elements.
- `Enter` / `Space` activates buttons and toggles.
- `Escape` closes modals and overlays.
- Arrow keys navigate DurationSlider options and KidShelf tiles.
- Visible focus ring (3px solid primary accent, 2px offset) on all focusable elements.

---

## 5 PWA Configuration

| Property | Value |
|---|---|
| `name` | TaleWeaver |
| `short_name` | TaleWeaver |
| `theme_color` | `#6C5CE7` |
| `background_color` | `#FAFAFA` |
| `display` | `standalone` |
| `orientation` | `portrait-primary` |
| `start_url` | `/` |
| `scope` | `/` |
| `icons` | 192px + 512px, maskable + any-purpose |

### Service Worker Strategy

| Route Pattern | Strategy |
|---|---|
| App shell (`/`, `/index.html`, JS/CSS) | **Cache-first** (pre-cached on install) |
| API calls (`/api/*`) | **Network-first** with offline queue fallback |
| Static assets (images, fonts) | **Stale-while-revalidate** |
| TTS audio blobs | **Cache-only** (stored after generation) |
