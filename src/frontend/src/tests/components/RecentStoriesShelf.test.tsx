import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { appMessages } from '../../config/messages'
import { storyModes } from '../../config/modes'
import { RecentStoriesShelf, type RecentStoryItem } from '../../components/RecentStoriesShelf'

describe('RecentStoriesShelf', () => {
  const ui = appMessages.ui
  const story: RecentStoryItem = {
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
    teaserAudio: 'data:audio/wav;base64,AAA=',
    fullAudioReady: false,
    reducedMotion: false,
    generatedAt: '2026-03-02T00:00:00.000Z',
    isFavorite: false,
  }

  it('renders a story with poster preview and teaser narration controls', () => {
    render(
      <RecentStoriesShelf
        getLayerStyle={() => ({})}
        onApproveStory={() => {}}
        onToggleFavorite={() => {}}
        stories={[story]}
        ui={ui}
      />,
    )

    expect(screen.getByRole('img', { name: 'Poster preview for Moonlight Adventure' })).toBeInTheDocument()
    expect(screen.getByLabelText('Teaser narration for Moonlight Adventure')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Approve full narration' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Favorite' })).toBeInTheDocument()
  })

  it('renders a compact empty shelf state', () => {
    render(
      <RecentStoriesShelf
        getLayerStyle={() => ({})}
        onApproveStory={() => {}}
        onToggleFavorite={() => {}}
        stories={[]}
        ui={ui}
      />,
    )

    expect(screen.getByTestId('recent-stories-shelf')).toHaveAttribute('data-empty', 'true')
    expect(screen.getByText('Generate your first bedtime adventure!')).toBeInTheDocument()
  })

  it('invokes approve and favorite callbacks', async () => {
    const onApproveStory = vi.fn()
    const onToggleFavorite = vi.fn()

    render(
      <RecentStoriesShelf
        getLayerStyle={() => ({})}
        onApproveStory={onApproveStory}
        onToggleFavorite={onToggleFavorite}
        stories={[story]}
        ui={ui}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Approve full narration' }))
    await userEvent.click(screen.getByRole('button', { name: 'Favorite' }))

    expect(onApproveStory).toHaveBeenCalledWith('story-1')
    expect(onToggleFavorite).toHaveBeenCalledWith('story-1')
  })
})
