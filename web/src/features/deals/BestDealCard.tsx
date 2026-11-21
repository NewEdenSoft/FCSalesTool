import { button, eyebrow, num, panel } from '../../components/ui'
import { formatEndsAt, formatMoney, formatPlex } from '../../lib/format'
import type { BestDeal } from './types'

/**
 * DESIGN.md §3 Best-deal card — the one highlight card on the home page.
 * With no data it renders an honest empty state instead of a placeholder price.
 */
export function BestDealCard({ deal }: { deal: BestDeal | null }) {
  if (!deal) {
    return (
      <aside aria-label="Best deal right now" className={`${panel.base} min-w-0 flex-[1_1_380px]`}>
        <span className={`${eyebrow} text-ow-muted`}>Best value now</span>
        <h3 className="text-h3 font-semibold">No live offers recorded yet</h3>
        <p className="text-ow-muted">
          As soon as a current Omega or PLEX offer is recorded, its real cost per month shows up here,
          with where it comes from and when it ends.
        </p>
      </aside>
    )
  }

  return (
    <aside aria-label="Best deal right now" className={`${panel.highlight} min-w-0 flex-[1_1_380px]`}>
      <div className="flex flex-wrap justify-between gap-3">
        <span className={`${eyebrow} text-ow-accent`}>Best value now</span>
        <span className="text-caption text-ow-muted">
          {deal.endsAt ? `Ends ${formatEndsAt(deal.endsAt)}` : formatEndsAt(null)}
        </span>
      </div>

      <h3 className="text-[26px] leading-tight font-semibold">{deal.name}</h3>

      <div className="flex flex-wrap items-baseline gap-2.5">
        <span className={`${num} text-num-xl font-semibold text-ow-accent`}>
          {formatMoney(deal.effectiveMonthly, deal.currency)}
        </span>
        <span className="text-ow-muted">per month</span>
      </div>

      {deal.baselineMonthly != null && (
        <p className="text-ow-muted">
          vs {formatMoney(deal.baselineMonthly, deal.currency)} paying month by month
        </p>
      )}

      <hr className="w-full border-0 border-t border-ow-border" />

      <dl className="grid grid-cols-2 gap-3 text-body-sm">
        <div>
          <dt className="text-ow-muted">You pay</dt>
          <dd className={`${num} font-semibold`}>{formatMoney(deal.price, deal.currency)}</dd>
        </div>
        <div>
          <dt className="text-ow-muted">Bonus</dt>
          <dd className={`${num} font-semibold text-ow-plex`}>{formatPlex(deal.bonusPlex)}</dd>
        </div>
      </dl>

      <a className={`${button.primaryCta} w-full`} href={deal.sourceUrl} target="_blank" rel="noopener">
        Open in EVE Store <span className="sr-only">(opens in new tab)</span>
      </a>
    </aside>
  )
}
