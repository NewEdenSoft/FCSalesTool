import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { createFileRoute, Navigate, redirect, useNavigate } from '@tanstack/react-router'
import { meQueryKey, meQueryOptions } from '../api/auth'
import { eyebrow } from '../components/ui'
import { CharactersPanel } from '../features/account/CharactersPanel'
import { ProfilePanel } from '../features/account/ProfilePanel'
import { EmailPanel, PasswordPanel } from '../features/account/SignInMethodsPanels'

interface AccountSearch {
  linked?: number
  error?: string
}

export const Route = createFileRoute('/account')({
  validateSearch: (search: Record<string, unknown>): AccountSearch => {
    const linked = Number(search.linked)
    return {
      linked: Number.isSafeInteger(linked) && linked > 0 ? linked : undefined,
      error: typeof search.error === 'string' ? search.error : undefined,
    }
  },
  beforeLoad: async ({ context, location }) => {
    const me = await context.queryClient.ensureQueryData(meQueryOptions())
    if (!me) throw redirect({ to: '/login', search: { redirect: location.href } })
  },
  component: AccountPage,
})

/** DESIGN.md §4 template 3 — Sidebar form (profile) + supporting panels. */
function AccountPage() {
  const { data: me } = useSuspenseQuery(meQueryOptions())
  const { linked, error } = Route.useSearch()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  // Signed out elsewhere (or session expired): back to sign-in.
  if (!me) return <Navigate to="/login" />

  function onSignedOut() {
    queryClient.setQueryData(meQueryKey, null)
    queryClient.removeQueries({ predicate: (q) => q.queryKey[0] !== meQueryKey[0] && q.queryKey[0] !== 'auth' })
    void navigate({ to: '/' })
  }

  return (
    <>
      <header className="flex flex-col gap-3">
        <p className={`${eyebrow} text-ow-plex`}>Account</p>
        <h1 className="text-h1 font-bold">Your account</h1>
      </header>
      <div className="flex flex-wrap items-start gap-8">
        <ProfilePanel key={me.id} me={me} onSignedOut={onSignedOut} />
        <div className="flex min-w-0 flex-[999_1_560px] flex-col gap-6">
          <CharactersPanel me={me} linked={linked} error={error} />
          <EmailPanel me={me} />
          <PasswordPanel me={me} />
        </div>
      </div>
    </>
  )
}
