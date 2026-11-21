import { queryOptions } from '@tanstack/react-query'
import { SITE_NAME } from '../config/site'
import { api, ApiError } from './client'

export interface LinkedCharacter {
  characterId: number
  name: string
  linkedAt: string
}

/** The signed-in account (GET /api/me). */
export interface Me {
  id: string
  displayName: string
  email: string | null
  emailConfirmed: boolean
  hasPassword: boolean
  avatarCharacterId: number | null
  characters: LinkedCharacter[]
}

export interface AuthProviders {
  eve: boolean
}

/** Matches the API's Identity password policy. */
export const MIN_PASSWORD_LENGTH = 10

export const meQueryKey = ['me'] as const

/** Current account, or `null` when signed out (401). */
export const meQueryOptions = () =>
  queryOptions({
    queryKey: meQueryKey,
    queryFn: async ({ signal }) => {
      try {
        return await api<Me>('/api/me', { signal })
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) return null
        throw error
      }
    },
    staleTime: 5 * 60_000,
  })

export const authProvidersQueryOptions = () =>
  queryOptions({
    queryKey: ['auth', 'providers'] as const,
    queryFn: ({ signal }) => api<AuthProviders>('/api/auth/providers', { signal }),
    staleTime: Infinity,
  })

export const authApi = {
  login: (body: { email: string; password: string; rememberMe: boolean }) =>
    api<Me>('/api/auth/login', { method: 'POST', body }),
  register: (body: { email: string; password: string; displayName?: string }) =>
    api('/api/auth/register', { method: 'POST', body }),
  logout: () => api('/api/auth/logout', { method: 'POST' }),
  confirmEmail: (body: { userId: string; code: string }) => api('/api/auth/confirm-email', { method: 'POST', body }),
  confirmEmailChange: (body: { userId: string; email: string; code: string }) =>
    api('/api/auth/confirm-email-change', { method: 'POST', body }),
  resendConfirmation: (email: string) => api('/api/auth/resend-confirmation', { method: 'POST', body: { email } }),
  forgotPassword: (email: string) => api('/api/auth/forgot-password', { method: 'POST', body: { email } }),
  resetPassword: (body: { userId: string; code: string; newPassword: string }) =>
    api('/api/auth/reset-password', { method: 'POST', body }),
}

export const accountApi = {
  updateProfile: (displayName: string) => api<Me>('/api/me', { method: 'PATCH', body: { displayName } }),
  setAvatar: (characterId: number | null) => api<Me>('/api/me/avatar', { method: 'PUT', body: { characterId } }),
  unlinkCharacter: (characterId: number) => api<Me>(`/api/me/characters/${characterId}`, { method: 'DELETE' }),
  setPassword: (body: { currentPassword?: string; newPassword: string }) =>
    api('/api/me/password', { method: 'POST', body }),
  changeEmail: (newEmail: string) => api('/api/me/email', { method: 'POST', body: { newEmail } }),
}

/** Full-page navigations: the API runs the EVE SSO redirect dance. */
export const eveLoginUrl = (returnUrl?: string) =>
  `/auth/eve/login${returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : ''}`
export const eveLinkUrl = (returnUrl = '/account') => `/auth/eve/link?returnUrl=${encodeURIComponent(returnUrl)}`

export const portraitUrl = (characterId: number, size: 32 | 64 | 128 | 256) =>
  `https://images.evetech.net/characters/${characterId}/portrait?size=${size}`

/** Only same-site paths are followed after sign-in (no open redirects). */
export function safeRedirect(value: unknown): string | undefined {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') && !value.includes('\\')
    ? value
    : undefined
}

/** Messages for the `?error=` codes the API's EVE SSO redirects use. */
export const EVE_ERRORS: Record<string, string> = {
  eve_failed: "EVE Online sign-in didn't complete. Try again.",
  eve_unavailable: 'EVE Online sign-in isn’t available right now.',
  eve_linked_elsewhere:
    `That character is linked to a different ${SITE_NAME} account. Sign in with it and unlink it there first.`,
  eve_link_session: 'Your session changed while linking. Sign in and try again.',
}
