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
  getLayerStyle: (layer: PosterLayer, reducedMotion: boolean) => CSSProperties
  onApproveStory: (storyId: string) => void
  onToggleFavorite: (storyId: string) => void
}

export function FavoritesShelf({ ui, stories, isLoading, getLayerStyle, onApproveStory, onToggleFavorite }: FavoritesShelfProps) {
  return (
    <section aria-label={ui.favoriteStoriesAria} className="shelf" data-testid="favorite-stories-shelf">
      <h3>{ui.favorites}</h3>
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
      ) : null}
      <ul>
        {stories.map((story) => (
          <StoryCard
            key={story.storyId}
            story={story}
            ui={ui}
            getLayerStyle={getLayerStyle}
            onApproveStory={onApproveStory}
            onToggleFavorite={onToggleFavorite}
            testIdPrefix="favorite-story"
          />
        ))}
      </ul>
    </section>
  )
}
