import { Stat } from '../../components/Stat'
import { eyebrow } from '../../components/ui'
import { BestDealCard } from '../deals/BestDealCard'
import type { BestDeal, DealsSummary } from '../deals/types'

interface HomeHeroProps {
  bestDeal: BestDeal | null
  summary: DealsSummary | null
}

/** DESIGN.md §4 template 1 — Hero + highlight. */
export function HomeHero({ bestDeal, summary }: HomeHeroProps) {
  return (
    <section aria-labelledby="home-title" className="flex flex-wrap items-start gap-10">
      <div className="flex min-w-0 flex-[999_1_520px] flex-col gap-5">
        <p className={`${eyebrow} text-ow-plex`}>EVE Online · Omega &amp; PLEX deals</p>
        <h1 id="home-title" className="text-h1 font-bold sm:text-display">
          Pay less for Omega.
        </h1>
        <p className="max-w-[60ch] text-body-lg text-ow-muted">
          Every Omega, PLEX and skillpoint offer in one place, ranked by what it really costs you per
          month. See when sales end, and when the next one is likely.
        </p>
        <div className="flex flex-wrap gap-6 pt-2">
          <Stat value={summary?.offersTracked ?? null} label="offers tracked" />
          <Stat value={summary?.limitedTimeLive ?? null} label="limited-time deals live" />
        </div>
      </div>

      <BestDealCard deal={bestDeal} />
    </section>
  )
}
