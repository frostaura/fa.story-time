import { type CSSProperties } from 'react'
import { appMessages } from '../config/messages'
import type { PosterLayer, StoryArtifact } from '../types/storyTime'
import { StoryCard } from './StoryCard'

type UiMessages = typeof appMessages.ui

export type RecentStoryItem = StoryArtifact

type RecentStoriesShelfProps = {
  ui: UiMessages
  stories: RecentStoryItem[]
  isLoading?: boolean
  approvingStoryId?: string | null
  favoritingStoryId?: string | null
  highlightedStoryId?: string | null
  storyFeedbackStoryId?: string | null
  storyFeedbackMessage?: string | null
  approvalLocked?: boolean
  approvalHint?: string | null
  getLayerStyle: (layer: PosterLayer, reducedMotion: boolean) => CSSProperties
  onApproveStory: (storyId: string) => void
  onToggleFavorite: (storyId: string) => void
}

export function RecentStoriesShelf({
  ui,
  stories,
  isLoading,
  approvingStoryId,
  favoritingStoryId,
  highlightedStoryId,
  storyFeedbackStoryId,
  storyFeedbackMessage,
  approvalLocked = false,
  approvalHint = null,
  getLayerStyle,
  onApproveStory,
  onToggleFavorite,
}: RecentStoriesShelfProps) {
  const isEmpty = !isLoading && stories.length === 0

  return (
    <section
      aria-label={ui.recentStoriesAria}
      className={`shelf shelf-recent${isEmpty ? ' shelf-empty' : ''}`}
      data-empty={isEmpty}
      data-testid="recent-stories-shelf"
    >
      <div className="shelf-header">
        <div className="shelf-header-copy">
          <h3>{ui.recent}</h3>
          <p className="shelf-description">{ui.recentShelfDescription}</p>
        </div>
        {stories.length > 0 ? <span className="shelf-count">{ui.storiesCount(stories.length)}</span> : null}
      </div>
      {isLoading ? (
        <div className="skeleton-container" data-testid="recent-skeleton">
          <div className="skeleton-card" />
          <div className="skeleton-card" />
        </div>
      ) : stories.length === 0 ? (
        <div className="empty-state">
          <span aria-hidden="true" className="empty-state-icon">🌙</span>
          <p>{ui.noStoriesGeneratedYet}</p>
        </div>
      ) : (
        <ul>
          {stories.map((story) => (
            <StoryCard
              key={story.storyId}
              story={story}
              ui={ui}
              getLayerStyle={getLayerStyle}
               isHighlighted={highlightedStoryId === story.storyId}
               isApproving={approvingStoryId === story.storyId}
               isFavoriting={favoritingStoryId === story.storyId}
               approvalLocked={approvalLocked}
               approvalHint={approvalLocked ? approvalHint : null}
               errorMessage={storyFeedbackStoryId === story.storyId ? storyFeedbackMessage : null}
               onApproveStory={onApproveStory}
               onToggleFavorite={onToggleFavorite}
              testIdPrefix="recent-story"
              variant="default"
            />
          ))}
        </ul>
      )}
    </section>
  )
}
