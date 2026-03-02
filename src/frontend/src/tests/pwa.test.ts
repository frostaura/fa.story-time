import { registerServiceWorker } from '../pwa'

describe('registerServiceWorker', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('registers the service worker on window load when supported', () => {
    const register = vi.fn(() => Promise.resolve({} as ServiceWorkerRegistration))
    Object.defineProperty(navigator, 'serviceWorker', {
      configurable: true,
      value: { register },
    })

    registerServiceWorker()
    window.dispatchEvent(new Event('load'))

    expect(register).toHaveBeenCalledWith('/service-worker.js')
  })
})
