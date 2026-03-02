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

    expect(register).toHaveBeenCalledTimes(1)
    const firstCall = register.mock.calls[0] as unknown[] | undefined
    expect(firstCall).toBeDefined()
    const registrationPath = firstCall?.[0]
    expect(typeof registrationPath).toBe('string')
    const [serviceWorkerPath, query] = String(registrationPath).split('?')
    const params = new URLSearchParams(query)
    expect(serviceWorkerPath).toBe('/service-worker.js')
    expect(params.get('cache')).toBe('storytime-static-v1')
    expect(params.get('shell')).toBe('/,/index.html')
  })
})
