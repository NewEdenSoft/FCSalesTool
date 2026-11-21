import { Link } from '@tanstack/react-router'
import { SITE_NAME } from '../config/site'
import { AccountNav } from './AccountNav'
import { CurrencySelect } from './CurrencySelect'
import { LogoMark } from './Logo'
import { container } from './ui'

const navLinkClass =
  'rounded-control px-3.5 py-2.5 font-medium text-ow-muted no-underline transition-colors hover:text-ow-text'

/**
 * DESIGN.md §3 App header. Nav links are added as their pages ship; the
 * "Get deal alerts" button joins once alerts exist (no dead links).
 */
export function AppHeader() {
  return (
    <header className="border-b border-ow-border">
      <div className={`${container} flex flex-wrap items-center justify-between gap-4 py-4`}>
        <Link
          to="/"
          className="flex items-center gap-2.5 font-heading text-[20px] font-bold text-ow-text no-underline hover:text-ow-text"
        >
          <LogoMark />
          {SITE_NAME}
        </Link>

        <nav aria-label="Main" className="flex flex-wrap gap-1">
          <Link
            to="/"
            className={navLinkClass}
            activeOptions={{ exact: true, includeHash: false }}
            activeProps={{ className: 'bg-ow-raised text-ow-text' }}
          >
            Deals
          </Link>
          <Link to="/" hash="how-it-works" className={navLinkClass} activeOptions={{ includeHash: true }}>
            How it works
          </Link>
        </nav>

        <div className="flex flex-wrap items-center gap-4">
          <CurrencySelect />
          <AccountNav />
        </div>
      </div>
    </header>
  )
}
