import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { authProvidersQueryOptions, eveLoginUrl } from '../../api/auth'
import { button, eyebrow, panel } from '../../components/ui'

interface AuthLayoutProps {
  title: string
  intro?: string
  children: ReactNode
  aside?: ReactNode
}

/** DESIGN.md §4 template 4 — Form + aside. Used by every sign-in / sign-up page. */
export function AuthLayout({ title, intro, children, aside }: AuthLayoutProps) {
  return (
    <>
      <header className="flex flex-col gap-3">
        <p className={`${eyebrow} text-ow-plex`}>Account</p>
        <h1 className="text-h1 font-bold">{title}</h1>
        {intro && <p className="max-w-[60ch] text-body-lg text-ow-muted">{intro}</p>}
      </header>
      <div className="flex flex-wrap items-start gap-8">
        <div className={`${panel.base} min-w-0 flex-[999_1_560px] gap-6`}>{children}</div>
        {aside && <aside className={`${panel.base} flex-[1_1_340px] gap-3`}>{aside}</aside>}
      </div>
    </>
  )
}

/**
 * Full-page link to the API's EVE SSO flow. Hidden when the server has no EVE SSO app
 * configured. Text only — DESIGN.md §7 rules out EVE logos.
 */
export function EveSignIn({ returnUrl }: { returnUrl?: string }) {
  const { data } = useQuery(authProvidersQueryOptions())
  if (!data?.eve) return null

  return (
    <>
      <div className="flex flex-col gap-2">
        <a href={eveLoginUrl(returnUrl)} className={`${button.secondary} w-full`}>
          Sign in with EVE Online
        </a>
        <p className="text-caption text-ow-muted">
          We only learn which character you are. No ESI access, nothing stored from EVE.
        </p>
      </div>
      <div className="flex items-center gap-3 text-caption text-ow-muted" role="separator" aria-label="or">
        <span className="h-px flex-1 bg-ow-border" />
        or
        <span className="h-px flex-1 bg-ow-border" />
      </div>
    </>
  )
}
