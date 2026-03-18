import { type CSSProperties, type SyntheticEvent, useState } from 'react'
import { appMessages } from '../config/messages'
import { storyModes } from '../config/modes'
import type { PosterLayer, StoryArtifact } from '../types/storyTime'

type UiMessages = typeof appMessages.ui

export type StoryCardProps = {
  story: StoryArtifact
  ui: UiMessages
  getLayerStyle: (layer: PosterLayer, reducedMotion: boolean) => CSSProperties
  onApproveStory: (storyId: string) => void
  onToggleFavorite: (storyId: string) => void
  approvalLocked?: boolean
  approvalHint?: string | null
  isApproving?: boolean
  isFavoriting?: boolean
  errorMessage?: string | null
  testIdPrefix?: string
  variant?: 'default' | 'compact'
  collectionLabel?: string | null
  isHighlighted?: boolean
}

const formatAudioDuration = (seconds: number): string => {
  const totalSeconds = Math.max(1, Math.round(seconds))
  const minutes = Math.floor(totalSeconds / 60)
  const remainingSeconds = totalSeconds % 60
  return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`
}

export function StoryCard({
  story,
  ui,
  getLayerStyle,
  onApproveStory,
  onToggleFavorite,
  approvalLocked = false,
  approvalHint = null,
  isApproving = false,
  isFavoriting = false,
  errorMessage = null,
  testIdPrefix = 'story',
  variant = 'default',
  collectionLabel = null,
  isHighlighted = false,
}: StoryCardProps) {
  const [audioDurationSeconds, setAudioDurationSeconds] = useState<number | null>(null)
  const seriesBible =
    story.mode === storyModes.series && story.storyBible ? story.storyBible : null
  const isCompact = variant === 'compact'
  const hasFullNarration = story.fullAudioReady && Boolean(story.fullAudio)
  const activeAudioLabel = hasFullNarration ? ui.fullNarrationLabel : ui.previewNarrationLabel
  const activeAudioAriaLabel = hasFullNarration
    ? ui.fullNarration(story.title)
    : ui.teaserNarration(story.title)
  const activeAudioSource = hasFullNarration && story.fullAudio ? story.fullAudio : story.teaserAudio
  const modeLabel = story.mode === storyModes.series ? ui.modeSeries : ui.modeOneShot
  const fallbackDurationSeconds = hasFullNarration
    ? Math.max(75, story.sceneCount * 75)
    : Math.max(24, story.sceneCount * 18)
  const audioStatus = audioDurationSeconds && audioDurationSeconds > 0
    ? ui.audioLength(formatAudioDuration(audioDurationSeconds))
    : hasFullNarration
      ? ui.fullNarrationReadyStatus(formatAudioDuration(fallbackDurationSeconds))
      : ui.previewReadyStatus(formatAudioDuration(fallbackDurationSeconds))
  const approvalButtonClassName = isCompact
    ? 'btn-secondary story-action-compact'
    : 'btn-primary story-action-primary'

  const syncAudioDuration = (event: SyntheticEvent<HTMLAudioElement>) => {
    const duration = event.currentTarget.duration
    if (Number.isFinite(duration) && duration > 0) {
      setAudioDurationSeconds(duration)
    }
  }

  return (
    <li
      data-testid={`${testIdPrefix}-${story.storyId}`}
      id={`${testIdPrefix}-${story.storyId}`}
      className={`story-card story-card-${variant}${isHighlighted ? ' story-card-highlighted' : ''}`}
    >
      <div className="story-layout">
        <div className="poster-parallax" role="img" aria-label={ui.posterPreview(story.title)}>
          {story.posterLayers.map((layer) => (
            <div
              key={`${story.storyId}-${layer.role}`}
              className="poster-layer"
              style={getLayerStyle(layer, story.reducedMotion)}
            />
          ))}
        </div>
        <div className="story-summary">
          <div className="story-header">
            <div className="story-meta-row">
              <span className={`story-audio-state${hasFullNarration ? ' story-audio-state-full' : ''}`}>
                {activeAudioLabel}
              </span>
              {collectionLabel ? <span className="story-collection-chip">{collectionLabel}</span> : null}
              {seriesBible ? (
                <span
                  className="series-progress story-series-progress"
                  data-testid={`${testIdPrefix}-series-progress-${story.storyId}`}
                >
                  {ui.seriesProgress(seriesBible.arcName, seriesBible.arcEpisodeNumber)}
                </span>
              ) : null}
            </div>
            <strong className="story-title">{story.title}</strong>
            <div className="story-detail-row">
              <span className="story-detail-chip">{modeLabel}</span>
            </div>
          </div>
          <p className="story-recap">{story.recap || ui.teaserNarrationReady}</p>
          {isCompact ? (
            <div
              className="story-audio-inline"
              data-testid={`${testIdPrefix}-audio-summary-${story.storyId}`}
            >
              <span className={`story-audio-state${hasFullNarration ? ' story-audio-state-full' : ''}`}>
                {activeAudioLabel}
              </span>
              <span className="story-audio-inline-status">{audioStatus}</span>
            </div>
          ) : (
            <div className="story-audio-panel">
              <div className="story-audio-panel-meta">
                <span className="story-audio-panel-label">{activeAudioLabel}</span>
                <span className="story-audio-duration">{audioStatus}</span>
              </div>
              <audio
                aria-label={activeAudioAriaLabel}
                controls
                onDurationChange={syncAudioDuration}
                onLoadedMetadata={syncAudioDuration}
                preload="metadata"
                src={activeAudioSource}
                data-testid={`${testIdPrefix}-${hasFullNarration ? 'full' : 'teaser'}-audio-${story.storyId}`}
              />
              <span className="story-audio-status">{audioStatus}</span>
            </div>
          )}
          {errorMessage ? (
            <p className="story-feedback" data-testid={`${testIdPrefix}-feedback-${story.storyId}`} role="alert">
              {errorMessage}
            </p>
          ) : approvalHint && !isCompact ? (
            <p className="story-feedback" data-testid={`${testIdPrefix}-approval-hint-${story.storyId}`}>
              {approvalHint}
            </p>
          ) : null}
          <div className="story-actions">
            {!story.fullAudioReady && story.approvalRequired ? (
              <button
                className={approvalButtonClassName}
                data-testid={`${testIdPrefix}-approve-${story.storyId}`}
                disabled={approvalLocked || isApproving}
                onClick={() => onApproveStory(story.storyId)}
                type="button"
              >
                {isApproving ? ui.unlockingNarration : ui.approveFullNarration}
              </button>
            ) : null}
            <button
              className="btn-secondary story-action-secondary"
              data-testid={`${testIdPrefix}-toggle-favorite-${story.storyId}`}
              disabled={isFavoriting}
              onClick={() => onToggleFavorite(story.storyId)}
              type="button"
            >
              {isFavoriting ? (
                <span className="btn-spinner">
                  <span aria-hidden="true" className="spinner-icon" />
                  {ui.updatingFavorite}
                </span>
              ) : story.isFavorite ? (
                ui.unfavorite
              ) : (
                ui.favorite
              )}
            </button>
          </div>
        </div>
      </div>
    </li>
  )
}
