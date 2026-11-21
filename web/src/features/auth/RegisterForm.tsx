import { useMutation } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { authApi, MIN_PASSWORD_LENGTH } from '../../api/auth'
import { formErrors } from '../../api/client'
import { FormError, StatusPanel, TextField } from '../../components/forms'
import { button } from '../../components/ui'

export function RegisterForm() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')

  const register = useMutation({ mutationFn: authApi.register })
  const resend = useMutation({ mutationFn: authApi.resendConfirmation })
  const { fields, form } = formErrors(register.error)

  if (register.isSuccess) {
    return (
      <StatusPanel
        title="Check your inbox"
        action={
          resend.isSuccess ? (
            <p className="text-body-sm text-ow-plex">New link sent.</p>
          ) : (
            <button type="button" className={button.secondary} disabled={resend.isPending} onClick={() => resend.mutate(email)}>
              Send the link again
            </button>
          )
        }
      >
        <p>
          We sent a confirmation link to <strong className="text-ow-text">{email}</strong>. Open it to finish
          creating your account, then sign in.
        </p>
      </StatusPanel>
    )
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    register.mutate({ email, password, displayName: displayName.trim() || undefined })
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
        error={fields.email}
      />
      <TextField
        label="Password"
        type="password"
        autoComplete="new-password"
        required
        minLength={MIN_PASSWORD_LENGTH}
        helper={`At least ${MIN_PASSWORD_LENGTH} characters. A short sentence works well.`}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        error={fields.password}
      />
      <TextField
        label="Display name"
        optional
        autoComplete="nickname"
        helper="Shown on your account. Defaults to your email address; you can change it later."
        value={displayName}
        onChange={(e) => setDisplayName(e.target.value)}
        error={fields.displayName}
      />
      <FormError message={form} />
      <button type="submit" className={`${button.primary} self-start`} disabled={register.isPending}>
        {register.isPending ? 'Creating account…' : 'Create account'}
      </button>
    </form>
  )
}
