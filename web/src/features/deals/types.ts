import type { Currency } from '../../lib/format'

/**
 * The best current offer in the user's currency (DESIGN.md §3 Best-deal card, §5 data rules).
 * Shape mirrors what the deals API will return; amounts are already in `currency`.
 */
export interface BestDeal {
  id: string
  name: string
  currency: Currency
  /** What the user pays up front. */
  price: number
  months: number
  /** (price − CCP-stated bonus value) ÷ months. */
  effectiveMonthly: number
  /** 1-month list price, for the "vs … paying month by month" line. */
  baselineMonthly: number | null
  bonusPlex: number | null
  sourceUrl: string
  /** ISO date-time; null = ongoing. */
  endsAt: string | null
}

/** Headline numbers for the home page stats row. */
export interface DealsSummary {
  offersTracked: number
  limitedTimeLive: number
}
