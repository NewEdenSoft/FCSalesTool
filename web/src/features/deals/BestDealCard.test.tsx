import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { BestDealCard } from './BestDealCard'
import type { BestDeal } from './types'

// Test fixture only — never rendered in the app.
const deal: BestDeal = {
  id: 'omega-24-plex-1500',
  name: '24 months + 1,500 PLEX',
  currency: 'USD',
  price: 270.99,
  months: 24,
  effectiveMonthly: 8.58,
  baselineMonthly: 19.99,
  bonusPlex: 1500,
  sourceUrl: 'https://store.example.test/omega-24',
  endsAt: '2026-11-26T23:59:00Z',
}

describe('BestDealCard', () => {
  it('leads with the effective monthly price and its end date', () => {
    render(<BestDealCard deal={deal} />)

    expect(screen.getByText('$8.58')).toBeInTheDocument()
    expect(screen.getByText('Ends 26 Nov, 23:59 UTC')).toBeInTheDocument()
    expect(screen.getByText('vs $19.99 paying month by month')).toBeInTheDocument()
    expect(screen.getByText('$270.99')).toBeInTheDocument()
    expect(screen.getByText('1,500 PLEX')).toBeInTheDocument()
  })

  it('links to the store in a new tab and says so to screen readers', () => {
    render(<BestDealCard deal={deal} />)

    const link = screen.getByRole('link', { name: /open in eve store \(opens in new tab\)/i })
    expect(link).toHaveAttribute('href', deal.sourceUrl)
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noopener')
  })

  it('shows ongoing offers as always available', () => {
    render(<BestDealCard deal={{ ...deal, endsAt: null }} />)

    expect(screen.getByText('Always available')).toBeInTheDocument()
  })

  it('renders an empty state without data', () => {
    render(<BestDealCard deal={null} />)

    expect(screen.getByText('No live offers recorded yet')).toBeInTheDocument()
    expect(screen.queryByText(/per month$/)).not.toBeInTheDocument()
  })
})
