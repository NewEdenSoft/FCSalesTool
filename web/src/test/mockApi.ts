import { vi } from 'vitest'

export interface MockResponse {
  status?: number
  body?: unknown
}

type Handler = MockResponse | ((init: RequestInit | undefined) => MockResponse)

/**
 * Stubs `fetch` for API calls. Keys are `"METHOD /path"` (query string ignored).
 * Unmatched calls: `GET /api/me` → 401 (signed out), `/api/auth/providers` → EVE off, else 404.
 * Returns the mock so tests can assert on calls.
 */
export function mockApi(handlers: Record<string, Handler> = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(typeof input === 'string' ? input : input.toString(), 'http://localhost')
    const key = `${init?.method ?? 'GET'} ${url.pathname}`
    const handler = handlers[key] ?? defaults[key] ?? { status: 404 }
    const { status = 200, body } = typeof handler === 'function' ? handler(init) : handler
    return new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const defaults: Record<string, MockResponse> = {
  'GET /api/me': { status: 401 },
  'GET /api/auth/providers': { body: { eve: false } },
}

/** JSON body of the n-th call to `METHOD /path`. */
export function requestBody(fetchMock: ReturnType<typeof mockApi>, key: string, index = 0): unknown {
  const calls = fetchMock.mock.calls.filter(([input, init]) => {
    const url = new URL(String(input), 'http://localhost')
    return `${init?.method ?? 'GET'} ${url.pathname}` === key
  })
  const body = calls[index]?.[1]?.body
  return typeof body === 'string' ? JSON.parse(body) : undefined
}
