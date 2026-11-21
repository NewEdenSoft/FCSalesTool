import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { accountApi, authProvidersQueryOptions, EVE_ERRORS, eveLinkUrl, meQueryKey, type Me } from '../../api/auth'
import { formErrors } from '../../api/client'
import { Avatar } from '../../components/Avatar'
import { FormError, StatusPanel } from '../../components/forms'
import { button, panel, tag } from '../../components/ui'

interface CharactersPanelProps {
  me: Me
  /** Character id from `?linked=` after returning from EVE SSO. */
  linked?: number
  /** Error code from `?error=` after returning from EVE SSO. */
  error?: string
}

/** EVE characters that can sign in to this account; one of them is the avatar. */
export function CharactersPanel({ me, linked, error }: CharactersPanelProps) {
  const queryClient = useQueryClient()
  const { data: providers } = useQuery(authProvidersQueryOptions())
  const onUpdated = (updated: Me) => queryClient.setQueryData(meQueryKey, updated)

  const setAvatar = useMutation({ mutationFn: accountApi.setAvatar, onSuccess: onUpdated })
  const unlink = useMutation({ mutationFn: accountApi.unlinkCharacter, onSuccess: onUpdated })
  const busy = setAvatar.isPending || unlink.isPending
  const linkedCharacter = me.characters.find((c) => c.characterId === linked)

  return (
    <section aria-labelledby="characters-title" className={`${panel.base} gap-5`}>
      <div className="flex flex-col gap-1">
        <h2 id="characters-title" className="text-h3">
          EVE characters
        </h2>
        <p className="text-body-sm text-ow-muted">
          Any linked character can sign in to this account. Its portrait can be your avatar.
        </p>
      </div>

      {linkedCharacter && (
        <StatusPanel title={`${linkedCharacter.name} is linked`}>
          <p>You can now sign in with this character.</p>
        </StatusPanel>
      )}
      <FormError message={error ? (EVE_ERRORS[error] ?? EVE_ERRORS.eve_failed) : undefined} />
      <FormError message={formErrors(setAvatar.error ?? unlink.error).form} />

      {me.characters.length === 0 ? (
        <p className="text-ow-muted">No characters linked yet.</p>
      ) : (
        <ul className="flex flex-col">
          {me.characters.map((character) => {
            const isAvatar = character.characterId === me.avatarCharacterId
            return (
              <li
                key={character.characterId}
                className="flex flex-wrap items-center gap-3 border-t border-ow-border py-3 first:border-t-0 first:pt-0"
              >
                <Avatar characterId={character.characterId} name={character.name} />
                <span className="min-w-0 flex-1 truncate font-medium">{character.name}</span>
                {isAvatar ? (
                  <span className={tag.limited}>Avatar</span>
                ) : (
                  <button
                    type="button"
                    className={button.ghost}
                    disabled={busy}
                    onClick={() => setAvatar.mutate(character.characterId)}
                  >
                    Use as avatar<span className="sr-only">: {character.name}</span>
                  </button>
                )}
                <button
                  type="button"
                  className={button.ghost}
                  disabled={busy}
                  onClick={() => unlink.mutate(character.characterId)}
                >
                  Unlink<span className="sr-only">: {character.name}</span>
                </button>
              </li>
            )
          })}
        </ul>
      )}

      {providers?.eve && (
        <a href={eveLinkUrl()} className={`${button.secondary} self-start`}>
          Link a character
        </a>
      )}
    </section>
  )
}
