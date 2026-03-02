import { appMessages } from '../config/messages'
import { type Mode, storyModes } from '../config/modes'
import type { OneShotDefaults } from '../types/storyTime'

type UiMessages = typeof appMessages.ui

export type OneShotCustomization = {
  arcName: string
  companionName: string
  setting: string
  mood: string
  themeTrackId: string
  narrationStyle: string
}

type HomeStatusSummary = {
  defaultChildName: string
  durationSliderVisible: boolean
  durationMinMinutes: number
  durationMaxMinutes: number
  oneShotDefaults?: OneShotDefaults
}

type ProfileSummary = {
  childName: string
  reducedMotion: boolean
}

type QuickGenerateCardProps = {
  visible: boolean
  ui: UiMessages
  homeStatus: HomeStatusSummary
  profile: ProfileSummary
  durationMinutes: number
  mode: Mode
  oneShotCustomization: OneShotCustomization
  isGenerating: boolean
  error: string | null
  onChildNameChange: (value: string) => void
  onDurationChange: (value: number) => void
  onModeChange: (value: Mode) => void
  onOneShotChange: (key: keyof OneShotCustomization, value: string) => void
  onReducedMotionChange: (value: boolean) => void
  onGenerate: () => void
}

const ONESHOT_PLACEHOLDER_KEYS: Record<keyof OneShotCustomization, keyof UiMessages> = {
  arcName: 'oneShotPlaceholderArcName',
  companionName: 'oneShotPlaceholderCompanionName',
  setting: 'oneShotPlaceholderSetting',
  mood: 'oneShotPlaceholderMood',
  themeTrackId: 'oneShotPlaceholderThemeTrackId',
  narrationStyle: 'oneShotPlaceholderNarrationStyle',
}

export function QuickGenerateCard({
  visible,
  ui,
  homeStatus,
  profile,
  durationMinutes,
  mode,
  oneShotCustomization,
  isGenerating,
  error,
  onChildNameChange,
  onDurationChange,
  onModeChange,
  onOneShotChange,
  onReducedMotionChange,
  onGenerate,
}: QuickGenerateCardProps) {
  if (!visible) {
    return null
  }

  const placeholderFor = (key: keyof OneShotCustomization): string => {
    const backend = homeStatus.oneShotDefaults?.[key]
    if (typeof backend === 'string' && backend.length > 0) {
      return backend
    }
    return ui[ONESHOT_PLACEHOLDER_KEYS[key]] as string
  }

  const sliderProgress = homeStatus.durationMaxMinutes > homeStatus.durationMinMinutes
    ? ((durationMinutes - homeStatus.durationMinMinutes) / (homeStatus.durationMaxMinutes - homeStatus.durationMinMinutes)) * 100
    : 50

  return (
    <section aria-label={ui.quickGenerate} className="quick-generate-card" data-testid="quick-generate-card">
      <h2>{ui.quickGenerate}</h2>

      <div className="form-group">
        <label htmlFor="child-name">{ui.childName}</label>
        <input
          data-testid="child-name-input"
          id="child-name"
          name="childName"
          onChange={(event) => onChildNameChange(event.target.value)}
          placeholder={homeStatus.defaultChildName}
          type="text"
          value={profile.childName}
        />
      </div>

      {homeStatus.durationSliderVisible ? (
        <div className="form-group">
          <label htmlFor="duration">{ui.duration(durationMinutes)}</label>
          <div className="slider-container">
            <div className="slider-track">
              <span className="slider-bound">{homeStatus.durationMinMinutes} min</span>
              <input
                aria-label={ui.durationAriaLabel}
                aria-valuetext={`${durationMinutes} minutes`}
                data-testid="duration-slider"
                id="duration"
                max={homeStatus.durationMaxMinutes}
                min={homeStatus.durationMinMinutes}
                onChange={(event) => onDurationChange(Number(event.target.value))}
                step={1}
                style={{ '--slider-progress': `${sliderProgress}%` } as React.CSSProperties}
                type="range"
                value={durationMinutes}
              />
              <span className="slider-bound">{homeStatus.durationMaxMinutes} min</span>
            </div>
          </div>
        </div>
      ) : null}

      <div className="form-group">
        <label htmlFor="mode">{ui.mode}</label>
        <select
          data-testid="mode-select"
          id="mode"
          onChange={(event) => onModeChange(event.target.value as Mode)}
          value={mode}
        >
          <option value={storyModes.series}>{ui.modeSeries}</option>
          <option value={storyModes.oneShot}>{ui.modeOneShot}</option>
        </select>
      </div>

      {mode === storyModes.oneShot ? (
        <div className="oneshot-fields">
          <hr className="oneshot-divider" />
          <p className="oneshot-heading">{ui.oneShotAdvancedOptions ?? 'Advanced options'}</p>
          <div className="form-group">
            <label htmlFor="one-shot-arc">{ui.oneShotStoryArc}</label>
            <input
              id="one-shot-arc"
              name="oneShotArc"
              onChange={(event) => onOneShotChange('arcName', event.target.value)}
              placeholder={placeholderFor('arcName')}
              type="text"
              value={oneShotCustomization.arcName}
            />
          </div>

          <div className="form-group">
            <label htmlFor="one-shot-companion">{ui.oneShotCompanion}</label>
            <input
              id="one-shot-companion"
              name="oneShotCompanion"
              onChange={(event) => onOneShotChange('companionName', event.target.value)}
              placeholder={placeholderFor('companionName')}
              type="text"
              value={oneShotCustomization.companionName}
            />
          </div>

          <div className="form-group">
            <label htmlFor="one-shot-setting">{ui.oneShotSetting}</label>
            <input
              id="one-shot-setting"
              name="oneShotSetting"
              onChange={(event) => onOneShotChange('setting', event.target.value)}
              placeholder={placeholderFor('setting')}
              type="text"
              value={oneShotCustomization.setting}
            />
          </div>

          <div className="form-group">
            <label htmlFor="one-shot-mood">{ui.oneShotMood}</label>
            <input
              id="one-shot-mood"
              name="oneShotMood"
              onChange={(event) => onOneShotChange('mood', event.target.value)}
              placeholder={placeholderFor('mood')}
              type="text"
              value={oneShotCustomization.mood}
            />
          </div>

          <div className="form-group">
            <label htmlFor="one-shot-theme-track">{ui.oneShotThemeTrack}</label>
            <input
              id="one-shot-theme-track"
              name="oneShotThemeTrack"
              onChange={(event) => onOneShotChange('themeTrackId', event.target.value)}
              placeholder={placeholderFor('themeTrackId')}
              type="text"
              value={oneShotCustomization.themeTrackId}
            />
          </div>

          <div className="form-group">
            <label htmlFor="one-shot-narration-style">{ui.oneShotNarrationStyle}</label>
            <input
              id="one-shot-narration-style"
              name="oneShotNarrationStyle"
              onChange={(event) => onOneShotChange('narrationStyle', event.target.value)}
              placeholder={placeholderFor('narrationStyle')}
              type="text"
              value={oneShotCustomization.narrationStyle}
            />
          </div>
        </div>
      ) : null}

      <label className="toggle">
        <input
          aria-label={ui.reducedMotionAriaLabel}
          checked={profile.reducedMotion}
          data-testid="reduced-motion-toggle"
          onChange={(event) => onReducedMotionChange(event.target.checked)}
          type="checkbox"
        />
        {ui.reducedMotionPlayback}
      </label>

      <button className="btn-primary" data-testid="generate-story-button" disabled={isGenerating} onClick={onGenerate} type="button">
        {isGenerating ? (
          <span className="btn-spinner">
            <span aria-hidden="true" className="spinner-icon" />
            {ui.generatingStory}
          </span>
        ) : (
          <><span aria-hidden="true" className="sparkle">✨ </span>{ui.generateStory}</>
        )}
      </button>

      {error ? (
        <div className="error-banner" data-testid="inline-error" role="alert">
          <span aria-hidden="true" className="error-banner-icon">⚠️</span>
          <span className="error-banner-text">{error}</span>
        </div>
      ) : null}
    </section>
  )
}
