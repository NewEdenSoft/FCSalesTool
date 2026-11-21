import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useEffect, useRef } from 'react'
import { authApi, meQueryKey, meQueryOptions } from '../api/auth'
import { ApiError } from '../api/client'
import { StatusPanel } from '../components/forms'
import { button } from '../components/ui'
import { AuthLayout } from '../features/auth/AuthLayout'

interface ConfirmSearch {
  userId?: string
  code?: string
  /** Present for an email change (the new address). */
  email?: string
}

export const Route = createFileRoute('/confirm-email')({
  validateSearch: (search: Record<string, unknown>): ConfirmSearch => ({
    userId: typeof search.userId === 'string' ? search.userId : undefined,
    code: typeof search.code === 'string' ? search.code : undefined,
    email: typeof search.email === 'string' ? search.email : undefined,
  }),
  component: ConfirmEmailPage,
})

/** Landing page for the links in confirmation emails. Confirms automatically on load. */
function ConfirmEmailPage() {
  const { userId, code, email } = Route.useSearch()
  const queryClient = useQueryClient()
  const { data: me } = useQuery(meQueryOptions())

  const confirm = useMutation({
    mutationFn: () =>
      email
        ? authApi.confirmEmailChange({ userId: userId!, code: code!, email })
        : authApi.confirmEmail({ userId: userId!, code: code! }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: meQueryKey }),
  })

  // Fire once per page load (StrictMode runs effects twice; the ref survives).
  const started = useRef(false)
  const { mutate } = confirm
  const hasLink = Boolean(userId && code)
  useEffect(() => {
    if (!hasLink || started.current) return
    started.current = true
    mutate()
  }, [hasLink, mutate])

  const next = me ? (
    <Link to="/account" className={button.secondary}>
      Go to your account
    </Link>
  ) : (
    <Link to="/login" className={button.secondary}>
      Sign in
    </Link>
  )

  const errorCode = confirm.error instanceof ApiError ? confirm.error.code : undefined

  return (
    <AuthLayout title={email ? 'Confirm your new email' : 'Confirm your email'}>
      {!hasLink || errorCode === 'invalid_link' ? (
        <StatusPanel title="This link doesn't work" action={next}>
          <p>It may have expired or already been used. Sign in to ask for a new one.</p>
        </StatusPanel>
      ) : errorCode === 'email_taken' ? (
        <StatusPanel title="That email is already in use" action={next}>
          <p>Another account uses this address, so your email wasn't changed.</p>
        </StatusPanel>
      ) : confirm.isError ? (
        <StatusPanel
          title="We couldn't confirm your email"
          action={
            <button type="button" className={button.secondary} onClick={() => mutate()}>
              Try again
            </button>
          }
        >
          <p>Check your connection and try again.</p>
        </StatusPanel>
      ) : confirm.isSuccess ? (
        <StatusPanel title="Email confirmed" action={next}>
          <p>{email ? `Your account now uses ${email}.` : 'You can now sign in with your email and password.'}</p>
        </StatusPanel>
      ) : (
        <p role="status" className="text-ow-muted">
          Confirming…
        </p>
      )}
    </AuthLayout>
  )
}
