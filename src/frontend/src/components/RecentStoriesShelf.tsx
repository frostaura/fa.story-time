import { type CSSProperties } from 'react'
import { appMessages } from '../config/messages'
import type { PosterLayer, StoryArtifact } from '../types/storyTime'
import { StoryCard } from './StoryCard'

type UiMessages = typeof appMessages.ui

export type RecentStoryItem = StoryArtifact

type RecentStoriesShelfProps = {
  ui: UiMessages
  stories: RecentStoryItem[]
  getLayerStyle: (layer: PosterLayer, reducedMotion: boolean) => CSSProperties
  onApproveStory: (storyId: string) => void
  onToggleFavorite: (storyId: string) => void
}

export function RecentStoriesShelf({
  ui,
  stories,
  getLayerStyle,
  onApproveStory,
  onToggleFavorite,
}: RecentStoriesShelfProps) {
  return (
    <section aria-label={ui.recentStoriesAria} className="shelf" data-testid="recent-stories-shelf">
      <h3>{ui.recent}</h3>
      {stories.length === 0 ? (
        <div className="empty-state">
          <span aria-hidden="true" className="empty-state-icon">🌙</span>
          <p>{ui.noStoriesGeneratedYet}</p>
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
            testIdPrefix="recent-story"
          />
        ))}
      </ul>
    </section>
  )
}
