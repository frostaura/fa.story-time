import { type CSSProperties } from 'react'
import { appMessages } from '../config/messages'
import type { PosterLayer, StoryArtifact } from '../types/storyTime'
import { StoryCard } from './StoryCard'

type UiMessages = typeof appMessages.ui

export type FavoriteStoryItem = StoryArtifact

type FavoritesShelfProps = {
  ui: UiMessages
  stories: FavoriteStoryItem[]
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

export function FavoritesShelf({
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
}: FavoritesShelfProps) {
  const isEmpty = !isLoading && stories.length === 0

  return (
    <section
      aria-label={ui.favoriteStoriesAria}
      className={`shelf shelf-favorites${isEmpty ? ' shelf-empty' : ''}`}
      data-empty={isEmpty}
      data-testid="favorite-stories-shelf"
    >
      <div className="shelf-header">
        <div className="shelf-header-copy">
          <h3>{ui.favorites}</h3>
          <p className="shelf-description">{ui.favoritesShelfDescription}</p>
        </div>
        {stories.length > 0 ? <span className="shelf-count">{ui.storiesCount(stories.length)}</span> : null}
      </div>
      {isLoading ? (
        <div className="skeleton-container" data-testid="favorites-skeleton">
          <div className="skeleton-card" />
          <div className="skeleton-card" />
        </div>
      ) : stories.length === 0 ? (
        <div className="empty-state">
          <span aria-hidden="true" className="empty-state-icon">⭐</span>
          <p>{ui.noFavoritesYet}</p>
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
              testIdPrefix="favorite-story"
              variant="compact"
              collectionLabel={ui.favorites}
            />
          ))}
        </ul>
      )}
    </section>
  )
}
