export const storyModes = Object.freeze({
  series: 'series',
  oneShot: 'one-shot',
})

export type Mode = (typeof storyModes)[keyof typeof storyModes]
