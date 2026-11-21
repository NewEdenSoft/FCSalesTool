import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { accountApi, authApi, meQueryKey, type Me } from '../../api/auth'
import { formErrors } from '../../api/client'
import { Avatar } from '../../components/Avatar'
import { FormError, TextField } from '../../components/forms'
import { button, panel } from '../../components/ui'

/** Avatar, display name and sign out. */
export function ProfilePanel({ me, onSignedOut }: { me: Me; onSignedOut: () => void }) {
  const queryClient = useQueryClient()
  const [displayName, setDisplayName] = useState(me.displayName)

  const save = useMutation({
    mutationFn: accountApi.updateProfile,
    onSuccess: (updated) => {
      queryClient.setQueryData(meQueryKey, updated)
      setDisplayName(updated.displayName)
    },
  })
  const logout = useMutation({ mutationFn: authApi.logout, onSuccess: onSignedOut })
  const { fields, form } = formErrors(save.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    save.mutate(displayName)
  }

  return (
    <section aria-labelledby="profile-title" className={`${panel.base} flex-[1_1_360px] gap-6`}>
      <div className="flex items-center gap-4">
        <Avatar characterId={me.avatarCharacterId} name={me.displayName} size={64} />
        <div className="min-w-0">
          <h2 id="profile-title" className="truncate text-h3">
            {me.displayName}
          </h2>
          <p className="truncate text-body-sm text-ow-muted">{me.email ?? 'Signs in with EVE Online'}</p>
        </div>
      </div>

      <form onSubmit={submit} className="flex flex-col gap-4" noValidate>
        <TextField
          label="Display name"
          autoComplete="nickname"
          helper="What we call you on this site."
          value={displayName}
          onChange={(e) => {
            setDisplayName(e.target.value)
            save.reset()
          }}
          error={fields.displayName}
        />
        <FormError message={form} />
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            className={button.primary}
            disabled={save.isPending || displayName.trim() === me.displayName}
          >
            Save name
          </button>
          {save.isSuccess && (
            <span role="status" className="text-body-sm text-ow-plex">
              Saved
            </span>
          )}
        </div>
      </form>

      <div className="border-t border-ow-border pt-5">
        <button type="button" className={button.secondary} disabled={logout.isPending} onClick={() => logout.mutate()}>
          Sign out
        </button>
      </div>
    </section>
  )
}
