/** RFC 7807 problem details as returned by the API (plus our `code` and validation `errors`). */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `Request failed (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  get code() {
    return this.problem.code
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

/**
 * JSON fetch against our own API (same origin, cookie auth). Always sends
 * `X-Requested-With`, which the API requires on state-changing requests (CSRF).
 * Resolves `undefined` for empty responses (202/204).
 */
export async function api<T = void>(path: string, { method = 'GET', body, signal }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json', 'X-Requested-With': 'fetch' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  const response = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: 'same-origin',
    signal,
  })

  const text = await response.text()
  const data: unknown = text ? safeJson(text) : undefined

  if (!response.ok) {
    throw new ApiError(response.status, (data as ProblemDetails | undefined) ?? { status: response.status })
  }

  return data as T
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

/** Splits an error into per-field messages (first message each) and one form-level message. */
export function formErrors(error: unknown): { fields: Record<string, string>; form?: string } {
  if (!error) return { fields: {} }
  if (!(error instanceof ApiError)) return { fields: {}, form: 'Something went wrong. Check your connection and try again.' }

  const fields: Record<string, string> = {}
  let form: string | undefined
  for (const [key, messages] of Object.entries(error.problem.errors ?? {})) {
    if (key === 'form') form = messages.join(' ')
    else if (messages[0]) fields[key] = messages[0]
  }

  if (!form && Object.keys(fields).length === 0) {
    form =
      error.status === 429
        ? 'Too many attempts. Wait a minute and try again.'
        : [error.problem.title, error.problem.detail].filter(Boolean).join(' ') || 'Something went wrong.'
  }

  return { fields, form }
}
