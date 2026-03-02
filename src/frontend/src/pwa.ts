import { logClientError } from './config/clientLogger'
import { runtimeConfig } from './config/runtime'

export const registerServiceWorker = (): void => {
  if (typeof window === 'undefined' || !('serviceWorker' in navigator)) {
    return
  }

  window.addEventListener('load', () => {
    const serviceWorkerUrl = new URL(runtimeConfig.serviceWorkerPath, window.location.origin)
    serviceWorkerUrl.searchParams.set('cache', runtimeConfig.serviceWorkerCacheName)
    serviceWorkerUrl.searchParams.set('shell', runtimeConfig.serviceWorkerAppShell.join(','))
    const registrationPath = `${serviceWorkerUrl.pathname}${serviceWorkerUrl.search}`

    void navigator.serviceWorker.register(registrationPath).catch((error) => {
      logClientError('Service worker registration failed.', error)
    })
  })
}
