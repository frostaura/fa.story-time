import '../../index.css'
import '../../App.css'

const parseRgbColor = (value: string): [number, number, number] => {
  const matched = value.match(
    /rgba?\(\s*([0-9]+(?:\.[0-9]+)?)\s*,\s*([0-9]+(?:\.[0-9]+)?)\s*,\s*([0-9]+(?:\.[0-9]+)?)/i,
  )
  if (!matched) {
    throw new Error(`Unsupported color format: ${value}`)
  }

  return [Number(matched[1]), Number(matched[2]), Number(matched[3])]
}

const relativeLuminance = ([r, g, b]: [number, number, number]): number => {
  const normalize = (channel: number): number => {
    const value = channel / 255
    return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4
  }

  const [red, green, blue] = [normalize(r), normalize(g), normalize(b)]
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue
}

const contrastRatio = (foreground: string, background: string): number => {
  const foregroundLum = relativeLuminance(parseRgbColor(foreground))
  const backgroundLum = relativeLuminance(parseRgbColor(background))
  const lighter = Math.max(foregroundLum, backgroundLum)
  const darker = Math.min(foregroundLum, backgroundLum)
  return (lighter + 0.05) / (darker + 0.05)
}

describe('App visual accessibility', () => {
  it('keeps key text surfaces at WCAG AA contrast or better', () => {
    const shell = document.createElement('main')
    shell.className = 'app-shell'
    shell.textContent = 'StoryTime'
    document.body.append(shell)

    const shelf = document.createElement('section')
    shelf.className = 'shelf'
    shelf.textContent = 'Recent'
    shell.append(shelf)

    const error = document.createElement('p')
    error.className = 'error'
    error.textContent = 'Unable to load home status'
    shelf.append(error)

    const button = document.createElement('button')
    button.type = 'button'
    button.textContent = 'Generate story'
    shelf.append(button)

    const shellColor = getComputedStyle(shell).color
    const appBackground = getComputedStyle(document.documentElement).backgroundColor
    const shelfBackground = getComputedStyle(shelf).backgroundColor
    const errorColor = getComputedStyle(error).color
    const buttonStyles = getComputedStyle(button)

    expect(contrastRatio(shellColor, appBackground)).toBeGreaterThanOrEqual(4.5)
    expect(contrastRatio(shellColor, shelfBackground)).toBeGreaterThanOrEqual(4.5)
    expect(contrastRatio(errorColor, shelfBackground)).toBeGreaterThanOrEqual(4.5)
    expect(contrastRatio(buttonStyles.color, buttonStyles.backgroundColor)).toBeGreaterThanOrEqual(4.5)

    shell.remove()
  })
})
