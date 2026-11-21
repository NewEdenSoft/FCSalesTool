import { useQuery } from '@tanstack/react-query'
import { Link, useLocation } from '@tanstack/react-router'
import { meQueryOptions } from '../api/auth'
import { Avatar } from './Avatar'
import { button } from './ui'

/**
 * DESIGN.md §3 Account chip: "Sign in" (secondary; amber stays for the page's own CTA)
 * or the signed-in avatar + display name linking to the account page.
 */
export function AccountNav() {
  const { data: me, isPending } = useQuery(meQueryOptions())
  const location = useLocation()

  // Reserve the space while /api/me loads so the header doesn't jump.
  if (isPending) return <span className="h-11 w-24" aria-hidden="true" />

  if (!me) {
    const onAuthPage = ['/login', '/register'].includes(location.pathname)
    return (
      <Link
        to="/login"
        search={onAuthPage ? {} : { redirect: location.href }}
        className={button.secondary}
      >
        Sign in
      </Link>
    )
  }

  return (
    <Link
      to="/account"
      className="flex h-11 max-w-[220px] items-center gap-2.5 rounded-control px-2 font-medium text-ow-text no-underline transition-colors hover:bg-ow-raised hover:text-ow-text"
      activeProps={{ className: 'bg-ow-raised' }}
    >
      <Avatar characterId={me.avatarCharacterId} name={me.displayName} />
      <span className="truncate">{me.displayName}</span>
    </Link>
  )
}
