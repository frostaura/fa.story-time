const workerUrl = new URL(self.location.href)
const parseScopePath = () => {
  try {
    return new URL(self.registration.scope).pathname
  } catch {
    return '/'
  }
}

const defaultShell = Array.from(
  new Set([
    new URL('./', self.registration.scope).pathname,
    new URL('index.html', self.registration.scope).pathname,
  ]),
)

const configuredShell = (workerUrl.searchParams.get('shell') ?? '')
  .split(',')
  .map((entry) => entry.trim())
  .filter((entry) => entry.length > 0)
  .map((entry) => (entry.startsWith('/') ? entry : `/${entry}`))

const APP_SHELL = configuredShell.length > 0 ? Array.from(new Set(configuredShell)) : defaultShell
const NAVIGATION_FALLBACK = APP_SHELL.find((entry) => entry.endsWith('/index.html')) ?? defaultShell[1]
const STATIC_CACHE =
  workerUrl.searchParams.get('cache')?.trim() ||
  `storytime-static-${parseScopePath().replace(/[^a-z0-9]+/gi, '-').replace(/(^-|-$)/g, '') || 'root'}`

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE).then((cache) => cache.addAll(APP_SHELL)).then(() => self.skipWaiting()),
  )
})

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys
            .filter((key) => key !== STATIC_CACHE)
            .map((key) => caches.delete(key)),
        ),
      )
      .then(() => self.clients.claim()),
  )
})

self.addEventListener('fetch', (event) => {
  const { request } = event
  if (request.method !== 'GET') {
    return
  }

  const url = new URL(request.url)
  if (url.pathname.startsWith('/api/')) {
    return
  }

  if (url.origin === self.location.origin) {
    event.respondWith(staleWhileRevalidateStatic(request))
  }
})

const staleWhileRevalidateStatic = async (request) => {
  const cache = await caches.open(STATIC_CACHE)
  const cached = await cache.match(request)
  const fetchPromise = fetch(request)
    .then(async (response) => {
      if (response.ok) {
        await cache.put(request, response.clone())
      }
      return response
    })
    .catch(() => null)

  if (cached) {
    void fetchPromise
    return cached
  }

  const network = await fetchPromise
  if (network) {
    return network
  }

  if (request.mode === 'navigate') {
    const shell = await cache.match(NAVIGATION_FALLBACK)
    if (shell) {
      return shell
    }
  }

  return new Response('Offline and no cached resource available.', { status: 503 })
}
