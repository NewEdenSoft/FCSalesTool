import { useMutation } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { authApi, MIN_PASSWORD_LENGTH } from '../api/auth'
import { ApiError, formErrors } from '../api/client'
import { FormError, StatusPanel, TextField } from '../components/forms'
import { button } from '../components/ui'
import { AuthLayout } from '../features/auth/AuthLayout'

interface ResetSearch {
  userId?: string
  code?: string
}

export const Route = createFileRoute('/reset-password')({
  validateSearch: (search: Record<string, unknown>): ResetSearch => ({
    userId: typeof search.userId === 'string' ? search.userId : undefined,
    code: typeof search.code === 'string' ? search.code : undefined,
  }),
  component: ResetPasswordPage,
})

function ResetPasswordPage() {
  const { userId, code } = Route.useSearch()
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [mismatch, setMismatch] = useState(false)
  const reset = useMutation({ mutationFn: authApi.resetPassword })

  const invalidLink = !userId || !code || (reset.error instanceof ApiError && reset.error.code === 'invalid_link')
  const { fields, form } = formErrors(reset.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    if (password !== confirm) {
      setMismatch(true)
      return
    }
    setMismatch(false)
    reset.mutate({ userId: userId!, code: code!, newPassword: password })
  }

  return (
    <AuthLayout title="Choose a new password">
      {reset.isSuccess ? (
        <StatusPanel title="Password changed" action={<Link to="/login" className={button.secondary}>Sign in</Link>}>
          <p>Sign in with your new password.</p>
        </StatusPanel>
      ) : invalidLink ? (
        <StatusPanel
          title="This link doesn't work"
          action={<Link to="/forgot-password" className={button.secondary}>Get a new link</Link>}
        >
          <p>It may have expired or already been used.</p>
        </StatusPanel>
      ) : (
        <form onSubmit={submit} className="flex flex-col gap-5" noValidate>
          <TextField
            label="New password"
            type="password"
            autoComplete="new-password"
            required
            minLength={MIN_PASSWORD_LENGTH}
            helper={`At least ${MIN_PASSWORD_LENGTH} characters.`}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={fields.newPassword}
          />
          <TextField
            label="Repeat new password"
            type="password"
            autoComplete="new-password"
            required
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            error={mismatch ? "The passwords don't match." : undefined}
          />
          <FormError message={form} />
          <button type="submit" className={`${button.primary} self-start`} disabled={reset.isPending}>
            Save password
          </button>
        </form>
      )}
    </AuthLayout>
  )
}
