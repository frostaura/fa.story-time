/* ───────────────────────────────────────────────
 * API client – fetch wrapper with retry
 * ─────────────────────────────────────────────── */

import type { GenerationRequest, GenerationStatus, Tier } from '../types';
import { getSoftUserId } from './localStorage';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5171';
const MAX_RETRIES = 3;
const INITIAL_BACKOFF_MS = 500;

/* ── Internal helpers ── */

async function request<T>(
  path: string,
  options: RequestInit = {},
  retriesLeft = MAX_RETRIES,
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Soft-User-Id': getSoftUserId(),
    ...(options.headers as Record<string, string> | undefined),
  };

  try {
    const res = await fetch(`${BASE_URL}${path}`, { ...options, headers });
    if (!res.ok) {
      const body = await res.text().catch(() => '');
      throw new Error(`HTTP ${res.status}: ${body}`);
    }
    return (await res.json()) as T;
  } catch (err) {
    if (retriesLeft > 0) {
      const delay = INITIAL_BACKOFF_MS * 2 ** (MAX_RETRIES - retriesLeft);
      await new Promise((r) => setTimeout(r, delay));
      return request<T>(path, options, retriesLeft - 1);
    }
    throw err;
  }
}

/* ── Public API ── */

/** Start a story generation job */
export function generate(req: GenerationRequest): Promise<{ correlationId: string }> {
  return request('/api/stories/generate', {
    method: 'POST',
    body: JSON.stringify(req),
  });
}

/** Poll generation progress */
export function getGenerationStatus(correlationId: string): Promise<GenerationStatus> {
  return request(`/api/stories/status/${correlationId}`);
}

/** Get available subscription tiers */
export function getTiers(): Promise<Tier[]> {
  return request('/api/tiers');
}

/** Get feature flags */
export function getFlags(): Promise<Record<string, boolean>> {
  return request('/api/flags');
}

/** Create a Stripe checkout session */
export function createCheckout(priceId: string): Promise<{ url: string }> {
  return request('/api/subscriptions/checkout', {
    method: 'POST',
    body: JSON.stringify({ priceId }),
  });
}

/** Get the current user's subscription status */
export function getSubscriptionStatus(): Promise<{
  tier: string;
  active: boolean;
  trialEnd?: string;
}> {
  return request('/api/subscriptions/status');
}

/** Start a free trial */
export function startTrial(): Promise<{ trialEnd: string }> {
  return request('/api/subscriptions/trial', { method: 'POST' });
}
