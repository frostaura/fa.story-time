import { runtimeConfig } from './config/runtime'

export const registerServiceWorker = (): void => {
  if (typeof window === 'undefined' || !('serviceWorker' in navigator)) {
    return
  }

  window.addEventListener('load', () => {
    void navigator.serviceWorker.register(runtimeConfig.serviceWorkerPath)
  })
}
