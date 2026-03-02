import { type CSSProperties } from 'react'
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
  testIdPrefix?: string
}

export function StoryCard({
  story,
  ui,
  getLayerStyle,
  onApproveStory,
  onToggleFavorite,
  testIdPrefix = 'story',
}: StoryCardProps) {
  const seriesBible =
    story.mode === storyModes.series && story.storyBible ? story.storyBible : null

  return (
    <li data-testid={`${testIdPrefix}-${story.storyId}`} className="story-card">
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
          <strong>{story.title}</strong>
          {seriesBible ? (
            <span className="series-progress" data-testid={`${testIdPrefix}-series-progress-${story.storyId}`}>
              {ui.seriesProgress(seriesBible.arcName, seriesBible.arcEpisodeNumber)}
            </span>
          ) : null}
          <small>{story.recap || ui.teaserNarrationReady}</small>
          <div className="audio-stack">
            <audio
              aria-label={ui.teaserNarration(story.title)}
              controls
              preload="none"
              src={story.teaserAudio}
              data-testid={`${testIdPrefix}-teaser-audio-${story.storyId}`}
            />
            {story.fullAudioReady && story.fullAudio ? (
              <audio
                aria-label={ui.fullNarration(story.title)}
                controls
                preload="none"
                src={story.fullAudio}
                data-testid={`${testIdPrefix}-full-audio-${story.storyId}`}
              />
            ) : null}
          </div>
        </div>
      </div>
      <div className="story-actions">
        {!story.fullAudioReady && story.approvalRequired ? (
          <button
            data-testid={`${testIdPrefix}-approve-${story.storyId}`}
            onClick={() => onApproveStory(story.storyId)}
            type="button"
          >
            {ui.approveFullNarration}
          </button>
        ) : null}
        <button
          data-testid={`${testIdPrefix}-toggle-favorite-${story.storyId}`}
          onClick={() => onToggleFavorite(story.storyId)}
          type="button"
        >
          {story.isFavorite ? ui.unfavorite : ui.favorite}
        </button>
      </div>
    </li>
  )
}
