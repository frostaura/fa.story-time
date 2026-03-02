# 🔴 StoryTime — Manual QA Teardown Report

> **Date:** 2026-03-02  
> **Tester:** Copilot QA Agent (hyper-critical UX/UI audit)  
> **App:** StoryTime PWA — `http://localhost:5174/`  
> **Backend:** Docker-compose API on `http://127.0.0.1:8080/`  
> **Viewports tested:** 320px, 375px, 768px, 1440px, 1920px  
> **Total screenshots:** 18 (in `qa-screenshots/`)

---

## Executive Summary

StoryTime's mobile-first layout looks decent on 375px — its primary target viewport. But beyond that sweet spot, the app falls apart. Desktop users stare at 60% empty gradient. The backend CORS configuration is broken out of the box for the most common dev scenario (port collision on 5173). The single-column 56rem cap with zero responsive breakpoints means this "mobile-first" app is really "mobile-only." Several spec-required features couldn't be verified due to the CORS failure blocking all API calls. The "calm-first" design philosophy is undermined by aggressive contrast choices in Parent Controls. Overall: **good bones on mobile, neglected on everything else**.

---

## Severity Legend

| Level | Meaning |
|-------|---------|
| 🔴 **CRITICAL** | Blocks core functionality — user cannot complete primary tasks |
| 🟠 **MAJOR** | Significant UX/visual problem — degrades experience substantially |
| 🟡 **MODERATE** | Noticeable issue — a competent user can work around it |
| 🔵 **MINOR** | Cosmetic or polish issue — doesn't block but looks unprofessional |
| ⚪ **SUGGESTION** | Not a bug — an improvement opportunity for a better product |

---

## 🔴 CRITICAL Issues

### CRIT-01: CORS Misconfiguration Blocks All API Calls

- **Location:** Backend → `AllowedOrigins` configuration; affects entire app
- **Screenshot:** `desktop-generate-error.png`
- **What happens:** Backend CORS policy only allows `http://localhost:5173` and `http://127.0.0.1:5173`. If port 5173 is occupied (common in dev), Vite auto-assigns 5174. Every single API call fails with `ERR_FAILED` — story generation, library fetch, status check — all dead.
- **Console evidence:** 8 CORS errors on every page load. `Access to fetch at 'http://127.0.0.1:8080/...' from origin 'http://localhost:5174' has been blocked by CORS policy.`
- **Expected:** Backend should accept requests from the actual frontend origin. In development, either:
  - (a) Use a wildcard or configurable allowed-origins list that includes common fallback ports (5174, 5175), OR
  - (b) Frontend `vite.config.ts` should enforce `strictPort: true` so it fails fast instead of silently binding to a wrong port, OR
  - (c) Use Vite's proxy to route API calls through the same origin (eliminating CORS entirely in dev).
- **Impact:** **100% of server-dependent features are broken.** Story generation, library loading, status checks — all fail. The app is a static shell. This is the single most important fix.

### CRIT-02: Error Banner Appears Below the Fold — Easy to Miss

- **Location:** Error alert between Parent Controls and Recent shelf
- **Screenshot:** `desktop-generate-error.png`, `mobile-series-mode.png`
- **What happens:** When the user clicks "Generate story" and it fails (CORS or network error), the error banner `⚠️ Unable to connect — stories you've already generated are still available below.` appears between the Parent Controls section and the Recent shelf. On mobile at 375px, this is **below the fold** — the user must scroll past the entire Parent Controls card to see it. On desktop, it's similarly buried.
- **Expected:** Error feedback should appear:
  - (a) Immediately adjacent to the Generate button (inline error), OR
  - (b) As a toast/snackbar at the top or bottom of the viewport with auto-dismiss, OR
  - (c) The Generate button itself should show the error state (red outline, shake animation, error text below it).
- **Why this matters:** A first-time user clicks Generate, sees the button return to normal, and has no idea anything went wrong unless they scroll down. This violates basic feedback heuristics (Nielsen #1: Visibility of system status).

---

## 🟠 MAJOR Issues

### MAJ-01: Desktop Layout Wastes 40–55% of Screen Width

- **Location:** `.app-shell` in `App.css` line 42 — `max-width: 56rem` (896px)
- **Screenshots:** `desktop-1440-home-full.png`, `desktop-1920-home-full.png`, `desktop-wide-1920.png`
- **What happens:** At 1440px, the content column is 896px wide — leaving 544px (38%) as empty gradient. At 1920px, it's 1024px of empty space (53%). The lavender-to-peach gradient background is pleasant but staring at it filling more than half the screen feels like a broken layout.
- **Expected:** For a "mobile-first" app, this might be intentional. But if desktop users are a target audience at all, consider:
  - (a) A two-column layout at ≥1024px: Quick Generate on the left, Recent/Favorites shelves on the right.
  - (b) A wider max-width (72rem / 1152px) with card-based layout for story shelves at desktop widths.
  - (c) At minimum, increase `max-width` to `64rem` (1024px) so the content doesn't look like a phone emulator on a monitor.
- **Spec reference:** `system_spec.md` says "mobile-first" — but says nothing about intentionally degrading the desktop experience. The spec's acceptance criteria for Home layout (`Quick Generate and duration slider visible on Home`) is met, but the spirit of good UX is not.

### MAJ-02: Zero Responsive Breakpoints — Same Layout at Every Size

- **Location:** `App.css` — no `@media` breakpoints for layout adaptation (only one for background gradient at 64rem)
- **Screenshots:** Compare `mobile-375-home-full.png` vs `tablet-768-home-full.png` vs `desktop-1440-home-full.png`
- **What happens:** The app renders identically at 375px, 768px, and 1440px — a single column of stacked cards. The only responsive CSS is the background gradient appearing at 64rem+. No grid, no multi-column, no reflow.
- **Expected:** At minimum:
  - **768px (tablet):** Quick Generate and Parent Controls could sit side-by-side. Story shelves could use a 2-column grid for story cards.
  - **1024px+ (desktop):** Three-column layout possible — controls on left, stories in center, favorites on right. Or at least a 2-column layout with controls and shelves side by side.
- **Why this matters:** A tablet user (iPad at 768px or 1024px) sees a narrow phone layout with huge margins. This screams "we didn't think about you."

### MAJ-03: No Loading/Spinner State on Generate Button

- **Location:** Generate button (`✨ Generate story`)
- **Screenshot:** `desktop-generate-error.png` (error appears but no intermediate loading state was visible)
- **What happens:** When the user clicks "Generate story", the CORS failure happens so fast that no loading indicator is visible. Even with a working backend, there should be a clear loading state — the button should show a spinner, disable itself, and indicate "Generating..." to prevent double-clicks and provide feedback.
- **Expected:** 
  - Button text changes to "Generating..." or shows a spinner icon
  - Button becomes disabled during the request
  - If using optimistic UI, show a skeleton card in the Recent shelf immediately
- **Spec reference:** UC-001 requires "one-tap generation" — but one-tap should still mean one tap with *visible feedback*, not one tap into a void.

---

## 🟡 MODERATE Issues

### MOD-01: Duration Slider Has No Filled Track Indicator

- **Location:** Duration slider control in Quick Generate
- **Screenshots:** `mobile-slider-min.png`, `mobile-slider-max.png`
- **What happens:** The slider track is a uniform gray line. There's no visual distinction between the "filled" portion (left of thumb) and "unfilled" portion (right of thumb). The thumb is a small indigo circle. At min (5 min) or max (15 min), the only feedback is the label text "Duration (5 min)" / "Duration (15 min)".
- **Expected:** 
  - The track left of the thumb should be filled with the accent color (indigo) to show progress
  - The current value should be more prominent — either a tooltip above the thumb or a larger numeric display
  - Consider adding tick marks at 5, 10, 15 for visual anchoring
- **Why this matters:** Range sliders without filled tracks are a well-known UX anti-pattern. Users can't quickly gauge relative position at a glance.

### MOD-02: "Kid Shelf" Label Wraps at 320px

- **Location:** Header toggle — "Kid Shelf" label next to the toggle switch
- **Screenshot:** `mobile-320-home-full.png`
- **What happens:** At 320px viewport width, the "Kid Shelf" label text wraps to two lines next to the toggle, creating an awkward layout where "Kid" is on one line and "Shelf" on the next.
- **Expected:** 
  - Use `white-space: nowrap` on the label to prevent wrapping
  - Or abbreviate to a kid-friendly icon (e.g., 📚) at narrow viewports
  - Or reduce the header padding/font-size at `@media (max-width: 360px)` to keep it on one line

### MOD-03: "Reduced motion playback" Text Wraps Awkwardly at 320px

- **Location:** Quick Generate card — reduced motion checkbox label
- **Screenshot:** `mobile-320-home-full.png`
- **What happens:** At 320px, the label "Reduced motion playback" wraps mid-phrase, creating visual clutter inside the Quick Generate card.
- **Expected:** 
  - Shorten the label to "Reduced motion" (the word "playback" is implied)
  - Or use a responsive font-size that scales down at narrow widths

### MOD-04: Passkey Button Feels Aggressive for "Calm-First" Design

- **Location:** Parent Controls section — "Verify parent with passkey" button
- **Screenshots:** `desktop-passkey-hover.png`, `mobile-375-home-full.png`
- **What happens:** The passkey button uses `#171717` (near-black) background against the `#f5f0ff` lavender background of the Parent Controls card. This creates a stark, high-contrast element that feels aggressive and corporate — not "calm" or "bedtime."
- **Expected:** The design spec calls for a "calm-first" aesthetic. A softer approach:
  - Use a muted indigo/purple outline button instead of solid black
  - Or use the same accent indigo (`#6366f1`) at reduced opacity
  - The current black button belongs in a fintech app, not a children's bedtime story generator

### MOD-05: Empty State Messages Are Functional but Uninspiring

- **Location:** Recent shelf ("No stories generated yet.") and Favorites shelf ("No favorites yet.")
- **Screenshots:** `mobile-375-home-full.png`, `mobile-375-kidshelf-on.png`
- **What happens:** Empty states show a single emoji (🌙 / ⭐) with plain text. While technically correct, they miss an opportunity to delight users in a children's app.
- **Expected:** For a "calm-first" bedtime story app:
  - Use illustrated empty states (sleeping character, starry sky placeholder)
  - Add a gentle call-to-action: "Generate your first bedtime adventure!" instead of the dry "No stories generated yet."
  - In Kid Shelf mode, make the empty state especially engaging since a child is the audience

---

## 🔵 MINOR Issues

### MIN-01: Header Logo and "StoryTime" Heading Vertical Alignment

- **Location:** App header — logo image + h1 "StoryTime" + Kid Shelf toggle
- **Screenshots:** `mobile-375-home-viewport.png`, `desktop-1440-home-viewport.png`
- **What happens:** The logo, app name, and Kid Shelf toggle are all on one line. The vertical alignment appears adequate but the spacing between the logo and "StoryTime" text and between the text and the toggle feels inconsistent — there's no clear visual grouping.
- **Expected:** 
  - Group logo + app name as a single unit with consistent gap
  - Add a subtle vertical divider or increased spacing before the Kid Shelf toggle to separate branding from controls

### MIN-02: Mode Dropdown Uses Native Select Styling

- **Location:** Quick Generate card — Mode dropdown (Series / One-shot)
- **Screenshots:** `mobile-375-home-viewport.png`, `desktop-1440-home-viewport.png`
- **What happens:** The Mode selector uses a native `<select>` element with browser-default dropdown styling. This looks different across browsers and platforms and doesn't match the custom-styled inputs elsewhere in the form (the text input has custom styling, the slider has custom styling, but the dropdown is raw native).
- **Expected:** 
  - Use a custom-styled dropdown that matches the design language of other inputs
  - Or at minimum, apply consistent border-radius, padding, and focus styling that matches the child name input

### MIN-03: Gap Between Quick Generate Card and Parent Controls Card

- **Location:** Space between the two main cards on the page
- **Screenshots:** `mobile-375-home-full.png`, `desktop-1440-home-full.png`
- **What happens:** There's a noticeable gap between the Quick Generate card (white, rounded) and the Parent Controls card (lavender, rounded). This gap is functional but feels inconsistent — it's wider than the gap between Parent Controls and the error banner, and wider than the gap between Recent and Favorites shelves.
- **Expected:** Consistent vertical rhythm — all section gaps should use the same spacing token (e.g., `1.5rem` or `2rem`) throughout the page.

### MIN-04: One-Shot Mode Shows 6 Extra Fields Without Visual Grouping

- **Location:** Quick Generate card when Mode is set to "One-shot"
- **Screenshots:** `mobile-375-oneshot-mode.png`, `desktop-oneshot-mode.png`
- **What happens:** Switching to One-shot mode reveals 6 additional fields (Story arc, Companion, Setting, Mood, Theme track, Narration style) as plain dropdowns stacked vertically. There's no visual grouping, divider, or sub-heading to distinguish these "advanced" fields from the core fields above.
- **Expected:** 
  - Add a subtle divider or "Advanced options" sub-heading between the base fields (name, duration, mode) and the one-shot-specific fields
  - Consider an accordion/expandable section so the form doesn't become overwhelming
  - The current presentation makes the Quick Generate card feel anything but "quick"

### MIN-05: No Favicon / PWA Icon Visible in Browser Tab

- **Location:** Browser tab
- **What happens:** The browser tab shows "StoryTime" as the title but uses a generic/default favicon. For a PWA that aims to be installable, a distinctive favicon is essential.
- **Expected:** A branded favicon matching the app logo visible in `README.icon.png`.

---

## ⚪ Suggestions (Not Bugs)

### SUG-01: Add Dark Mode Support

- **Current state:** No `prefers-color-scheme: dark` media query exists. No dark mode toggle.
- **Why:** Parents use this app at bedtime. Blinding white/lavender screens in a dark room at 9 PM is the antithesis of "calm-first." Dark mode should be a priority feature, not an afterthought.

### SUG-02: Consider Haptic Feedback on Mobile Generate Button

- **Current state:** The Generate button has a hover state (visible on desktop) but no tactile feedback on mobile.
- **Why:** A satisfying micro-interaction (vibration pattern via Vibration API) on tap would reinforce the "one-tap magic" feeling. Small touch, big delight.

### SUG-03: Story Shelf Cards Could Use Skeleton Loading States

- **Current state:** Empty shelves show static text. There's no transition state between "loading" and "loaded."
- **Why:** Skeleton cards (gray pulsing rectangles) during API calls would communicate that something is happening and make the app feel faster.

### SUG-04: Add Keyboard Shortcut for Generate (⌘/Ctrl+Enter)

- **Current state:** Generation requires clicking/tapping the button.
- **Why:** Power users (parents who use this nightly) would benefit from a keyboard shortcut. Especially on desktop where the layout already feels underutilized.

### SUG-05: Animate the ✨ Emoji on the Generate Button

- **Current state:** The sparkle emoji (✨) is static text.
- **Why:** A subtle sparkle/twinkle CSS animation on the emoji (respecting `prefers-reduced-motion`) would add magic and draw the eye to the primary CTA. This is a children's storytelling app — lean into the whimsy.

---

## Spec Compliance Cross-Reference

| Spec Requirement | Status | Notes |
|---|---|---|
| Quick Generate visible on Home | ✅ PASS | Immediately visible at all viewports |
| Duration slider on Home | ✅ PASS | Works correctly, range 5–15 min, label updates dynamically |
| Recent shelf displays stories | ⚠️ UNTESTABLE | CORS blocks API — empty state shown with correct fallback text |
| Favorites shelf displays stories | ⚠️ UNTESTABLE | CORS blocks API — empty state shown with correct fallback text |
| Kid Shelf toggle hides controls | ✅ PASS | Correctly hides Quick Generate, Parent Controls, error banner |
| Kid Shelf shows filtered library | ⚠️ UNTESTABLE | CORS blocks `kidMode=true` API call |
| One-shot mode shows extra fields | ✅ PASS | 6 additional dropdowns appear correctly |
| Parent gate (passkey) flow | ⚠️ UNTESTABLE | WebAuthn requires HTTPS + working backend |
| Reduced motion toggle | ✅ PASS | Toggle exists, CSS `prefers-reduced-motion` media query correctly kills animations |
| Touch targets ≥ 44px (A11Y-01) | ✅ PASS | Toggle switches have `min-height: 2.75rem` (44px) |
| Error surfaces friendly message | ✅ PASS | Network errors show friendly copy, not raw error text |
| Poster parallax layers | ⚠️ UNTESTABLE | No story generated to trigger poster rendering |
| Tier/paywall enforcement | ⚠️ UNTESTABLE | CORS blocks API |
| Checkout UI for upgrades | ⚠️ UNTESTABLE | CORS blocks API |
| PWA installable | 🔍 NOT VERIFIED | Would need to test install prompt / manifest |

---

## Screenshots Index

| File | Viewport | State |
|---|---|---|
| `desktop-1440-home-full.png` | 1440×900 | Full page, default state |
| `desktop-1440-home-viewport.png` | 1440×900 | Viewport only, above the fold |
| `desktop-1440-input-focus.png` | 1440×900 | Child name input focused (blue ring) |
| `desktop-1920-home-full.png` | 1920×1080 | Full page, shows extreme width waste |
| `desktop-wide-1920.png` | 1920×1080 | Full page with error banner visible |
| `desktop-generate-hover.png` | 1440×900 | Generate button hover state |
| `desktop-passkey-hover.png` | 1440×900 | Passkey button hover state |
| `desktop-generate-error.png` | 1440×900 | Error banner after failed generate |
| `desktop-oneshot-mode.png` | 1440×900 | One-shot mode with 6 extra fields |
| `mobile-375-home-viewport.png` | 375×812 | Viewport only, mobile default |
| `mobile-375-home-full.png` | 375×812 | Full page, mobile default |
| `mobile-375-oneshot-mode.png` | 375×812 | One-shot mode on mobile |
| `mobile-375-kidshelf-on.png` | 375×812 | Kid Shelf enabled — controls hidden |
| `mobile-320-home-full.png` | 320×568 | Full page, smallest viewport |
| `mobile-series-mode.png` | 375×812 | Series mode with error banner |
| `mobile-slider-min.png` | 375×812 | Slider at minimum (5 min) |
| `mobile-slider-max.png` | 375×812 | Slider at maximum (15 min) |
| `tablet-768-home-full.png` | 768×1024 | Full page, tablet portrait |

---

## Verdict

**Mobile (375px):** 7/10 — Solid foundation. The single-column layout works at this width. The calm color palette, rounded cards, and adequate touch targets show design intention. Marred by the error placement issue and lack of loading states.

**Tablet (768px):** 4/10 — Phone layout with extra padding. No responsive adaptation whatsoever. A tablet user gets a worse experience than a phone user because the wasted space is obvious but not as extreme as desktop.

**Desktop (1440px):** 3/10 — A mobile app trapped in a desktop browser. 38% empty gradient. No multi-column layout. No advantage to having a larger screen. Feels like using a phone emulator.

**Desktop (1920px):** 2/10 — Over half the screen is empty gradient. The content column looks like a narrow strip in a sea of lavender. Anyone using a full HD or ultrawide monitor will think the app is broken.

**Overall:** The app needs (1) CORS fix for dev, (2) error banner repositioning, (3) at least one responsive breakpoint at 768px+, and (4) desktop layout consideration. The mobile-first foundation is solid — now make it responsive-second.

---

*Report generated by manual Playwright MCP visual inspection. No automated tests were written. All findings based on screenshot analysis and DOM inspection across 5 viewport widths.*
