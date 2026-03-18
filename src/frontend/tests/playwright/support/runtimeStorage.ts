import { loadEnv } from 'vite'

const loadedEnv = loadEnv('production', process.cwd(), '')

const resolveStorageKey = (envName: string, fallback: string): string => {
  const fromProcess = process.env[envName]
  if (typeof fromProcess === 'string' && fromProcess.trim().length > 0) {
    return fromProcess.trim()
  }

  const fromEnvFile = loadedEnv[envName]
  if (typeof fromEnvFile === 'string' && fromEnvFile.trim().length > 0) {
    return fromEnvFile.trim()
  }

  return fallback
}

export const browserStorageKeys = Object.freeze({
  storyArtifacts: resolveStorageKey('VITE_STORAGE_KEY_STORY_ARTIFACTS', 'storyArtifacts'),
  softUserId: resolveStorageKey('VITE_STORAGE_KEY_SOFT_USER_ID', 'softUserId'),
})
