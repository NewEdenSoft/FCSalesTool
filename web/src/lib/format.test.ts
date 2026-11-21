import { describe, expect, it } from 'vitest'
import { formatEndsAt, formatIsk, formatMoney, formatPlex, MISSING } from './format'

describe('formatMoney', () => {
  it('uses the currency symbol and two decimals', () => {
    expect(formatMoney(8.58, 'USD')).toBe('$8.58')
    expect(formatMoney(270.9, 'USD')).toBe('$270.90')
    expect(formatMoney(19.99, 'EUR')).toBe('€19.99')
  })

  it('renders missing values as a dash', () => {
    expect(formatMoney(null, 'USD')).toBe(MISSING)
    expect(formatMoney(Number.NaN, 'USD')).toBe(MISSING)
  })
})

describe('formatPlex', () => {
  it('adds thousands separators and the unit', () => {
    expect(formatPlex(1500)).toBe('1,500 PLEX')
    expect(formatPlex(500)).toBe('500 PLEX')
  })

  it('renders zero or missing bonus as a dash', () => {
    expect(formatPlex(0)).toBe(MISSING)
    expect(formatPlex(undefined)).toBe(MISSING)
  })
})

describe('formatIsk', () => {
  it('abbreviates with a unit', () => {
    expect(formatIsk(2_100_000_000)).toBe('2.1B ISK')
    expect(formatIsk(850_000_000)).toBe('850M ISK')
    expect(formatIsk(4_500_000)).toBe('4.5M ISK')
    expect(formatIsk(999)).toBe('999 ISK')
  })
})

describe('formatEndsAt', () => {
  it('formats end times in UTC', () => {
    expect(formatEndsAt('2026-11-26T23:59:00Z')).toBe('26 Nov, 23:59 UTC')
    expect(formatEndsAt('2026-01-05T08:00:00+02:00')).toBe('5 Jan, 06:00 UTC')
  })

  it('shows ongoing offers as always available', () => {
    expect(formatEndsAt(null)).toBe('Always available')
  })

  it('renders invalid dates as a dash', () => {
    expect(formatEndsAt('not a date')).toBe(MISSING)
  })
})
