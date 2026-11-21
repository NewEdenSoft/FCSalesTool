import { useMutation } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { authApi } from '../api/auth'
import { formErrors } from '../api/client'
import { FormError, StatusPanel, TextField } from '../components/forms'
import { button } from '../components/ui'
import { AuthLayout } from '../features/auth/AuthLayout'

export const Route = createFileRoute('/forgot-password')({
  component: ForgotPasswordPage,
})

function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const forgot = useMutation({ mutationFn: authApi.forgotPassword })

  function submit(event: FormEvent) {
    event.preventDefault()
    forgot.mutate(email)
  }

  return (
    <AuthLayout
      title="Reset your password"
      intro="Enter the email you signed up with and we'll send you a link."
      aside={
        <>
          <h2 className="text-h4">Signed in with EVE Online?</h2>
          <p className="text-ow-muted">
            Accounts created with EVE Online don't need a password. <Link to="/login">Sign in with EVE</Link> instead.
          </p>
        </>
      }
    >
      {forgot.isSuccess ? (
        <StatusPanel title="Check your inbox" action={<Link to="/login" className={button.secondary}>Back to sign in</Link>}>
          <p>
            If an account uses <strong className="text-ow-text">{email}</strong>, we sent it a link to choose a new
            password. The link works for 24 hours.
          </p>
        </StatusPanel>
      ) : (
        <form onSubmit={submit} className="flex flex-col gap-5" noValidate>
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <FormError message={formErrors(forgot.error).form} />
          <button type="submit" className={`${button.primary} self-start`} disabled={forgot.isPending}>
            Send reset link
          </button>
        </form>
      )}
    </AuthLayout>
  )
}
