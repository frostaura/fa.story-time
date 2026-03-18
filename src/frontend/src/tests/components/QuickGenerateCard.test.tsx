import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { appMessages } from '../../config/messages'
import { storyModes } from '../../config/modes'
import { QuickGenerateCard } from '../../components/QuickGenerateCard'

describe('QuickGenerateCard', () => {
  const ui = appMessages.ui

  const defaultProps = {
    durationMinutes: 7,
    error: null as string | null,
    feedback: null as { message: string; tone: 'error' | 'success' | 'info' } | null,
    generateButtonLabel: ui.generateStory,
    homeStatus: {
      defaultChildName: 'Child',
      durationSliderVisible: true,
      durationMinMinutes: 5,
      durationMaxMinutes: 15,
      oneShotDefaults: {
        arcName: 'Arc',
        companionName: 'Companion',
        setting: 'Setting',
        mood: 'Mood',
        themeTrackId: 'Theme',
        narrationStyle: 'Narration',
      },
    },
    isGenerating: false,
    isOneShotDetailsExpanded: false,
    mode: storyModes.series as 'series' | 'one-shot',
    onChildNameChange: vi.fn(),
    onDurationChange: vi.fn(),
    onGenerate: vi.fn(),
    onModeChange: vi.fn(),
    onOneShotDetailsExpandedChange: vi.fn(),
    onSelectedSeriesIdChange: vi.fn(),
    onOneShotChange: vi.fn(),
    onReducedMotionChange: vi.fn(),
    selectedSeriesId: 'new',
    seriesOptions: [],
    oneShotCustomization: {
      arcName: '',
      companionName: '',
      setting: '',
      mood: '',
      themeTrackId: '',
      narrationStyle: '',
    },
    profile: { childName: 'Ari', reducedMotion: false },
    selectedSeriesLabel: null as string | null,
    ui,
    visible: true,
  }

  it('keeps one-shot customization collapsed until requested', () => {
    render(<QuickGenerateCard {...defaultProps} mode={storyModes.oneShot} />)

    expect(screen.getByText('Keep it simple or add a few custom touches. StoryTime will fill in anything you leave blank.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add optional details' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Story arc')).not.toBeInTheDocument()
  })

  it('renders one-shot customization fields when expanded', () => {
    render(
      <QuickGenerateCard
        {...defaultProps}
        isOneShotDetailsExpanded
        mode={storyModes.oneShot}
      />,
    )

    expect(screen.getByLabelText('Story arc')).toBeInTheDocument()
    expect(screen.getByLabelText('Companion')).toBeInTheDocument()
    expect(screen.getByLabelText('Setting')).toBeInTheDocument()
    expect(screen.getByLabelText('Mood')).toBeInTheDocument()
    expect(screen.getByLabelText('Theme track')).toBeInTheDocument()
    expect(screen.getByLabelText('Narration style')).toBeInTheDocument()
  })

  it('calls callbacks for duration and reduced motion changes', async () => {
    const onDurationChange = vi.fn()
    const onReducedMotionChange = vi.fn()
    const onGenerate = vi.fn()

    render(
      <QuickGenerateCard
        {...defaultProps}
        onDurationChange={onDurationChange}
        onGenerate={onGenerate}
        onReducedMotionChange={onReducedMotionChange}
      />,
    )

    fireEvent.change(screen.getByLabelText('Duration'), { target: { value: '8' } })
    await userEvent.click(screen.getByLabelText('Reduced motion'))
    await userEvent.click(screen.getByRole('button', { name: 'Generate story' }))

    expect(onDurationChange).toHaveBeenCalled()
    expect(onReducedMotionChange).toHaveBeenCalledWith(true)
    expect(onGenerate).toHaveBeenCalledTimes(1)
  })

  it('returns null when visible is false', () => {
    const { container } = render(<QuickGenerateCard {...defaultProps} visible={false} />)

    expect(container.innerHTML).toBe('')
  })

  it('calls onChildNameChange when child name input changes', async () => {
    const onChildNameChange = vi.fn()
    render(<QuickGenerateCard {...defaultProps} onChildNameChange={onChildNameChange} />)

    fireEvent.change(screen.getByTestId('child-name-input'), { target: { value: 'Luna' } })

    expect(onChildNameChange).toHaveBeenCalledWith('Luna')
  })

  it('calls onModeChange when mode select changes', async () => {
    const onModeChange = vi.fn()
    render(<QuickGenerateCard {...defaultProps} onModeChange={onModeChange} />)

    fireEvent.change(screen.getByTestId('mode-select'), { target: { value: storyModes.oneShot } })

    expect(onModeChange).toHaveBeenCalledWith(storyModes.oneShot)
  })

  it('shows explicit series selection when prior series exist', () => {
    render(
      <QuickGenerateCard
        {...defaultProps}
        seriesOptions={[{ seriesId: 'series-1', label: 'Moonlit Meadow · Episode 2' }]}
      />,
    )

    expect(screen.getByLabelText('Series continuation')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Start a new series' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Moonlit Meadow · Episode 2' })).toBeInTheDocument()
  })

  it('shows a calm support note when saved series are temporarily unavailable', () => {
    render(
      <QuickGenerateCard
        {...defaultProps}
        seriesSupportMessage={ui.seriesSyncHint}
      />,
    )

    expect(screen.getByTestId('series-support-copy')).toHaveTextContent(ui.seriesSyncHint)
  })

  it('calls onOneShotChange when one-shot customization fields change', () => {
    const onOneShotChange = vi.fn()
    render(
      <QuickGenerateCard
        {...defaultProps}
        isOneShotDetailsExpanded
        mode={storyModes.oneShot}
        onOneShotChange={onOneShotChange}
      />,
    )

    fireEvent.change(screen.getByLabelText('Story arc'), { target: { value: 'Dragon Quest' } })
    fireEvent.change(screen.getByLabelText('Companion'), { target: { value: 'Spark' } })
    fireEvent.change(screen.getByLabelText('Setting'), { target: { value: 'Forest' } })
    fireEvent.change(screen.getByLabelText('Mood'), { target: { value: 'whimsical' } })
    fireEvent.change(screen.getByLabelText('Theme track'), { target: { value: 'piano-soft' } })
    fireEvent.change(screen.getByLabelText('Narration style'), { target: { value: 'gentle' } })

    expect(onOneShotChange).toHaveBeenCalledWith('arcName', 'Dragon Quest')
    expect(onOneShotChange).toHaveBeenCalledWith('companionName', 'Spark')
    expect(onOneShotChange).toHaveBeenCalledWith('setting', 'Forest')
    expect(onOneShotChange).toHaveBeenCalledWith('mood', 'whimsical')
    expect(onOneShotChange).toHaveBeenCalledWith('themeTrackId', 'piano-soft')
    expect(onOneShotChange).toHaveBeenCalledWith('narrationStyle', 'gentle')
  })

  it('calls onOneShotDetailsExpandedChange when the disclosure button is pressed', async () => {
    const onOneShotDetailsExpandedChange = vi.fn()

    render(
      <QuickGenerateCard
        {...defaultProps}
        mode={storyModes.oneShot}
        onOneShotDetailsExpandedChange={onOneShotDetailsExpandedChange}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Add optional details' }))

    expect(onOneShotDetailsExpandedChange).toHaveBeenCalledWith(true)
  })

  it('hides duration slider when durationSliderVisible is false', () => {
    render(
      <QuickGenerateCard
        {...defaultProps}
        homeStatus={{ ...defaultProps.homeStatus, durationSliderVisible: false }}
      />,
    )

    expect(screen.queryByTestId('duration-slider')).toBeNull()
  })

  it('disables generate button while generating', () => {
    render(<QuickGenerateCard {...defaultProps} isGenerating />)

    expect(screen.getByTestId('generate-story-button')).toBeDisabled()
  })

  it('uses a mode-specific label for one-shot generation', () => {
    render(
      <QuickGenerateCard
        {...defaultProps}
        generateButtonLabel={ui.generateOneShot}
        mode={storyModes.oneShot}
      />,
    )

    expect(screen.getByRole('button', { name: 'Generate one-shot' })).toBeInTheDocument()
  })
})
