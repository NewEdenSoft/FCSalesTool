import { useMutation } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { accountApi, MIN_PASSWORD_LENGTH, type Me } from '../../api/auth'
import { formErrors } from '../../api/client'
import { FormError, StatusPanel, TextField } from '../../components/forms'
import { button, panel } from '../../components/ui'

/** Change the email, or add one to an EVE-only account. Takes effect after confirmation. */
export function EmailPanel({ me }: { me: Me }) {
  const [newEmail, setNewEmail] = useState('')
  const change = useMutation({ mutationFn: accountApi.changeEmail })
  const { fields, form } = formErrors(change.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    change.mutate(newEmail)
  }

  return (
    <section aria-labelledby="email-title" className={`${panel.base} gap-5`}>
      <div className="flex flex-col gap-1">
        <h2 id="email-title" className="text-h3">
          Email
        </h2>
        <p className="text-body-sm text-ow-muted">
          {me.email ? (
            <>
              <span className="text-ow-text">{me.email}</span>
              {me.emailConfirmed ? ' · confirmed' : ' · not confirmed yet'}
            </>
          ) : (
            'Add an email to sign in with a password and to get deal alerts.'
          )}
        </p>
      </div>

      {change.isSuccess ? (
        <StatusPanel
          title="Check your inbox"
          action={
            <button type="button" className={button.secondary} onClick={() => change.reset()}>
              Use a different address
            </button>
          }
        >
          <p>
            We sent a link to <strong className="text-ow-text">{newEmail}</strong>. Your email changes once you open it.
          </p>
        </StatusPanel>
      ) : (
        <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
          <TextField
            label={me.email ? 'New email' : 'Email'}
            type="email"
            autoComplete="email"
            required
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            error={fields.newEmail}
          />
          <FormError message={form} />
          <button type="submit" className={`${button.primary} self-start`} disabled={change.isPending || !newEmail}>
            {me.email ? 'Change email' : 'Add email'}
          </button>
        </form>
      )}
    </section>
  )
}

/** Change the password, or add one to an EVE-only account. */
export function PasswordPanel({ me }: { me: Me }) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const save = useMutation({
    mutationFn: accountApi.setPassword,
    onSuccess: () => {
      setCurrentPassword('')
      setNewPassword('')
    },
  })
  const { fields, form } = formErrors(save.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    save.mutate(me.hasPassword ? { currentPassword, newPassword } : { newPassword })
  }

  return (
    <section aria-labelledby="password-title" className={`${panel.base} gap-5`}>
      <div className="flex flex-col gap-1">
        <h2 id="password-title" className="text-h3">
          Password
        </h2>
        {!me.hasPassword && (
          <p className="text-body-sm text-ow-muted">
            You sign in with EVE Online. Add a password to also sign in with your email
            {me.emailConfirmed ? '.' : ' once it is confirmed.'}
          </p>
        )}
      </div>

      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        {me.hasPassword && (
          <TextField
            label="Current password"
            type="password"
            autoComplete="current-password"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            error={fields.currentPassword}
          />
        )}
        <TextField
          label={me.hasPassword ? 'New password' : 'Password'}
          type="password"
          autoComplete="new-password"
          required
          minLength={MIN_PASSWORD_LENGTH}
          helper={`At least ${MIN_PASSWORD_LENGTH} characters.`}
          value={newPassword}
          onChange={(e) => {
            setNewPassword(e.target.value)
            save.reset()
          }}
          error={fields.newPassword}
        />
        <FormError message={form} />
        <div className="flex flex-wrap items-center gap-3">
          <button type="submit" className={button.primary} disabled={save.isPending || !newPassword}>
            {me.hasPassword ? 'Change password' : 'Add password'}
          </button>
          {save.isSuccess && (
            <span role="status" className="text-body-sm text-ow-plex">
              Password saved
            </span>
          )}
        </div>
      </form>
    </section>
  )
}
