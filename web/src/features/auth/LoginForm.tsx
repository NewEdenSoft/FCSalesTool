import { useMutation } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { authApi, type Me } from '../../api/auth'
import { ApiError, formErrors } from '../../api/client'
import { CheckboxCard, FormError, TextField } from '../../components/forms'
import { button } from '../../components/ui'

/** Email + password sign-in. Reports the signed-in account through `onSignedIn`. */
export function LoginForm({ onSignedIn }: { onSignedIn: (me: Me) => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(true)

  const login = useMutation({ mutationFn: authApi.login, onSuccess: onSignedIn })
  const resend = useMutation({ mutationFn: authApi.resendConfirmation })

  const notConfirmed = login.error instanceof ApiError && login.error.code === 'email_not_confirmed'
  const { form } = formErrors(login.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    resend.reset()
    login.mutate({ email, password, rememberMe })
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-5" noValidate>
      <TextField
        label="Email"
        type="email"
        autoComplete="email"
        placeholder="capsuleer@example.com"
        required
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <TextField
        label="Password"
        type="password"
        autoComplete="current-password"
        required
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      <CheckboxCard
        title="Keep me signed in"
        description="Stay signed in on this device for 14 days."
        checked={rememberMe}
        onChange={(e) => setRememberMe(e.target.checked)}
      />

      {notConfirmed ? (
        <div role="alert" className="flex flex-col items-start gap-2 text-body-sm">
          <p className="text-ow-danger">Confirm your email first. We sent you a link when you registered.</p>
          {resend.isSuccess ? (
            <p className="text-ow-plex">New link sent. Check your inbox.</p>
          ) : (
            <button
              type="button"
              className={button.ghost}
              disabled={resend.isPending}
              onClick={() => resend.mutate(email)}
            >
              Send a new link
            </button>
          )}
        </div>
      ) : (
        <FormError message={form} />
      )}

      <div className="flex flex-wrap items-center justify-between gap-4">
        <button type="submit" className={button.primary} disabled={login.isPending}>
          {login.isPending ? 'Signing in…' : 'Sign in'}
        </button>
        <Link to="/forgot-password" className="text-body-sm">
          Forgot your password?
        </Link>
      </div>
    </form>
  )
}
