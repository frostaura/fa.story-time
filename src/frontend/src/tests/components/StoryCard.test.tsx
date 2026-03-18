import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StoryCard, type StoryCardProps } from '../../components/StoryCard'
import { appMessages } from '../../config/messages'
import { storyModes } from '../../config/modes'
import type { StoryArtifact } from '../../types/storyTime'

describe('StoryCard', () => {
  const ui = appMessages.ui
  const story: StoryArtifact = {
    storyId: 'story-1',
    title: 'Moonlight Adventure',
    mode: storyModes.series,
    seriesId: 'series-1',
    recap: 'A gentle recap.',
    scenes: ['Scene 1'],
    sceneCount: 1,
    posterLayers: [
      {
        role: 'BACKGROUND',
        speedMultiplier: 0.2,
        dataUri: 'data:image/svg+xml;base64,PHN2Zy8+',
      },
    ],
    approvalRequired: true,
    teaserAudio: 'data:audio/wav;base64,TEASER',
    fullAudioReady: false,
    reducedMotion: false,
    generatedAt: '2026-03-02T00:00:00.000Z',
    isFavorite: false,
  }

  const defaultProps: StoryCardProps = {
    story,
    ui,
    getLayerStyle: () => ({}),
    onApproveStory: vi.fn(),
    onToggleFavorite: vi.fn(),
    testIdPrefix: 'test-story',
  }

  it('renders a story with poster preview and teaser narration controls', () => {
    render(<StoryCard {...defaultProps} />)

    expect(screen.getByRole('img', { name: 'Poster preview for Moonlight Adventure' })).toBeInTheDocument()
    expect(screen.getAllByText('Preview clip').length).toBeGreaterThan(0)
    expect(screen.getByText('Series')).toBeInTheDocument()
    const teaserAudio = screen.getByTestId('test-story-teaser-audio-story-1') as HTMLAudioElement
    expect(teaserAudio).toBeInTheDocument()
    expect(teaserAudio).toHaveAttribute('src', 'data:audio/wav;base64,TEASER')
    expect(teaserAudio).toHaveAttribute('preload', 'metadata')
    expect(teaserAudio).toHaveAttribute('controls')
    expect(screen.getAllByText('Preview ready · about 0:24').length).toBeGreaterThan(0)

    expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
  })

  it('renders full audio when ready', () => {
    const fullStory: StoryArtifact = {
      ...story,
      fullAudioReady: true,
      fullAudio: 'data:audio/wav;base64,FULL',
    }
    render(<StoryCard {...defaultProps} story={fullStory} />)

    const fullAudio = screen.getByTestId('test-story-full-audio-story-1') as HTMLAudioElement
    expect(fullAudio).toBeInTheDocument()
    expect(fullAudio).toHaveAttribute('src', 'data:audio/wav;base64,FULL')
    expect(screen.getAllByText('Full narration unlocked').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Full narration ready · about 1:15').length).toBeGreaterThan(0)
    expect(screen.queryByTestId('test-story-teaser-audio-story-1')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve full narration' })).not.toBeInTheDocument()
  })

  it('renders compact favorites without duplicating the inline audio player', () => {
    render(
      <StoryCard
        {...defaultProps}
        approvalHint={ui.verifyParentToApprove}
        approvalLocked
        collectionLabel={ui.favorites}
        testIdPrefix="favorite-story"
        variant="compact"
      />,
    )

    expect(screen.getByTestId('favorite-story-audio-summary-story-1')).toHaveTextContent(
      'Preview clip',
    )
    expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeDisabled()
    expect(screen.queryByLabelText('Teaser narration for Moonlight Adventure')).not.toBeInTheDocument()
    expect(screen.queryByTestId('favorite-story-approval-hint-story-1')).not.toBeInTheDocument()
  })

  it('invokes approve and favorite callbacks', async () => {
    render(<StoryCard {...defaultProps} />)

    await userEvent.click(screen.getByRole('button', { name: 'Approve full narration' }))
    await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

    expect(defaultProps.onApproveStory).toHaveBeenCalledWith('story-1')
    expect(defaultProps.onToggleFavorite).toHaveBeenCalledWith('story-1')
  })

  it('shows inline feedback and loading state for narration approval', () => {
    render(
      <StoryCard
        {...defaultProps}
        errorMessage="We couldn't unlock the full narration just yet. Try approving again."
        isApproving
      />,
    )

    expect(screen.getByRole('button', { name: 'Unlocking narration...' })).toBeDisabled()
    expect(screen.getByRole('alert')).toHaveTextContent(
      "We couldn't unlock the full narration just yet. Try approving again.",
    )
  })

  it('keeps approval disabled until parent verification is complete', () => {
    render(
      <StoryCard
        {...defaultProps}
        approvalHint={ui.verifyParentToApprove}
        approvalLocked
      />,
    )

    expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeDisabled()
    expect(screen.getByTestId('test-story-approval-hint-story-1')).toHaveTextContent(
      ui.verifyParentToApprove,
    )
  })

  it('renders correct button state for favorites', () => {
    const favoriteStory: StoryArtifact = {
      ...story,
      isFavorite: true,
    }
    render(<StoryCard {...defaultProps} story={favoriteStory} />)

    expect(screen.getByRole('button', { name: 'Unfavorite' })).toBeInTheDocument()
  })

  it('shows a local saving state while favorite changes are pending', () => {
    render(<StoryCard {...defaultProps} isFavoriting />)

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
  })

  it('renders series progress badge when story has a story bible', () => {
    const seriesStory: StoryArtifact = {
      ...story,
      mode: storyModes.series,
      storyBible: {
        arcName: 'The Lost Star',
        arcEpisodeNumber: 3,
        arcObjective: 'Find the star',
        previousEpisodeSummary: 'The journey begins.',
        audioAnchorMetadata: {
          themeTrackId: 'night-chimes',
          narrationStyle: 'calm-storyteller',
        },
      },
    }
    render(<StoryCard {...defaultProps} story={seriesStory} />)

    const badge = screen.getByTestId('test-story-series-progress-story-1')
    expect(badge).toBeInTheDocument()
    expect(badge).toHaveTextContent('The Lost Star · Episode 3')
  })

  it('does not render series progress badge for one-shot stories', () => {
    const oneShotStory: StoryArtifact = {
      ...story,
      mode: storyModes.oneShot,
      storyBible: {
        arcName: 'Standalone Arc',
        arcEpisodeNumber: 1,
        arcObjective: 'Explore',
        previousEpisodeSummary: '',
        audioAnchorMetadata: {
          themeTrackId: 'theme-1',
          narrationStyle: 'style-1',
        },
      },
    }
    render(<StoryCard {...defaultProps} story={oneShotStory} />)

    expect(screen.queryByTestId('test-story-series-progress-story-1')).not.toBeInTheDocument()
  })
})
