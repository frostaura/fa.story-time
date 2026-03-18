import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FavoritesShelf, type FavoriteStoryItem } from '../../components/FavoritesShelf'
import { appMessages } from '../../config/messages'
import { storyModes } from '../../config/modes'

describe('FavoritesShelf', () => {
  const ui = appMessages.ui
  const story: FavoriteStoryItem = {
    storyId: 'story-2',
    title: 'Starlit Harbor',
    mode: storyModes.oneShot,
    recap: 'A cozy harbor recap.',
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
    teaserAudio: 'data:audio/wav;base64,AAA=',
    fullAudioReady: false,
    reducedMotion: false,
    generatedAt: '2026-03-02T00:00:00.000Z',
    isFavorite: true,
  }

  it('renders favorite story poster, compact audio summary, and actions', () => {
    render(
      <FavoritesShelf
        getLayerStyle={() => ({})}
        onApproveStory={() => {}}
        onToggleFavorite={() => {}}
        stories={[story]}
        ui={ui}
      />,
    )

    expect(screen.getByRole('img', { name: 'Poster preview for Starlit Harbor' })).toBeInTheDocument()
    expect(screen.getByTestId('favorite-story-audio-summary-story-2')).toHaveTextContent('Preview clip')
    expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Unfavorite' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Teaser narration for Starlit Harbor')).not.toBeInTheDocument()
  })

  it('renders a compact empty favorites state', () => {
    render(
      <FavoritesShelf
        getLayerStyle={() => ({})}
        onApproveStory={() => {}}
        onToggleFavorite={() => {}}
        stories={[]}
        ui={ui}
      />,
    )

    expect(screen.getByTestId('favorite-stories-shelf')).toHaveAttribute('data-empty', 'true')
    expect(screen.getByText('Star your favorites to find them here!')).toBeInTheDocument()
  })

  it('invokes approve and unfavorite callbacks', async () => {
    const onApproveStory = vi.fn()
    const onToggleFavorite = vi.fn()

    render(
      <FavoritesShelf
        getLayerStyle={() => ({})}
        onApproveStory={onApproveStory}
        onToggleFavorite={onToggleFavorite}
        stories={[story]}
        ui={ui}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Approve full narration' }))
    await userEvent.click(screen.getByRole('button', { name: 'Unfavorite' }))

    expect(onApproveStory).toHaveBeenCalledWith('story-2')
    expect(onToggleFavorite).toHaveBeenCalledWith('story-2')
  })
})
