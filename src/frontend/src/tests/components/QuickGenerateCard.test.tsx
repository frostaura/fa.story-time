import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { appMessages } from '../../config/messages'
import { storyModes } from '../../config/modes'
import { QuickGenerateCard } from '../../components/QuickGenerateCard'

describe('QuickGenerateCard', () => {
  const ui = appMessages.ui

  const defaultProps = {
    durationMinutes: 7,
    homeStatus: {
      defaultChildName: 'Dreamer',
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
    mode: storyModes.series as 'series' | 'one-shot',
    onChildNameChange: vi.fn(),
    onDurationChange: vi.fn(),
    onGenerate: vi.fn(),
    onModeChange: vi.fn(),
    onOneShotChange: vi.fn(),
    onReducedMotionChange: vi.fn(),
    oneShotCustomization: {
      arcName: '',
      companionName: '',
      setting: '',
      mood: '',
      themeTrackId: '',
      narrationStyle: '',
    },
    profile: { childName: 'Ari', reducedMotion: false },
    ui,
    visible: true,
  }

  it('renders one-shot customization fields when mode is one-shot', () => {
    render(<QuickGenerateCard {...defaultProps} mode={storyModes.oneShot} />)

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

  it('calls onOneShotChange when one-shot customization fields change', () => {
    const onOneShotChange = vi.fn()
    render(
      <QuickGenerateCard
        {...defaultProps}
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
})
