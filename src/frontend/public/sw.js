/* ───────────────────────────────────────────────
 * Service Worker – offline generation queue
 *
 * NOTE: The vite-plugin-pwa generates the main
 * service worker via Workbox. This file provides
 * additional offline-queue logic that gets
 * imported by the generated SW if needed.
 * ─────────────────────────────────────────────── */

const QUEUE_STORE = 'tw-offline-queue';
const API_PATTERN = /\/api\//;

// Open IndexedDB for queuing
function openQueue() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(QUEUE_STORE, 1);
    req.onupgradeneeded = () => {
      req.result.createObjectStore('requests', { autoIncrement: true });
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

// Store a failed request for later replay
async function enqueue(request) {
  const db = await openQueue();
  const tx = db.transaction('requests', 'readwrite');
  tx.objectStore('requests').add({
    url: request.url,
    method: request.method,
    headers: Object.fromEntries(request.headers.entries()),
    body: await request.text(),
    timestamp: Date.now(),
  });
  return tx.complete;
}

// Replay all queued requests (FIFO)
async function replayQueue() {
  const db = await openQueue();
  const tx = db.transaction('requests', 'readwrite');
  const store = tx.objectStore('requests');
  const all = await new Promise((resolve) => {
    const req = store.getAll();
    req.onsuccess = () => resolve(req.result);
  });

  for (const item of all) {
    try {
      await fetch(item.url, {
        method: item.method,
        headers: item.headers,
        body: item.method !== 'GET' ? item.body : undefined,
      });
    } catch {
      // Still offline — stop replaying
      break;
    }
  }

  // Clear replayed items
  const clearTx = db.transaction('requests', 'readwrite');
  clearTx.objectStore('requests').clear();
}

// Listen for fetch events — queue POST/PUT to API when offline
self.addEventListener('fetch', (event) => {
  if (!API_PATTERN.test(event.request.url)) return;
  if (event.request.method === 'GET') return;

  event.respondWith(
    fetch(event.request.clone()).catch(async () => {
      await enqueue(event.request.clone());
      return new Response(
        JSON.stringify({ queued: true, message: 'Request queued for when you are back online.' }),
        { status: 202, headers: { 'Content-Type': 'application/json' } },
      );
    }),
  );
});

// Replay queue when back online
self.addEventListener('sync', (event) => {
  if (event.tag === 'tw-replay-queue') {
    event.waitUntil(replayQueue());
  }
});

// Also try replaying on activation
self.addEventListener('activate', (event) => {
  event.waitUntil(replayQueue());
});
