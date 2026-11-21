// Display formatting rules from design/DESIGN.md §5. Missing values always render as "—".

export const MISSING = '—'

export const CURRENCIES = ['USD', 'EUR'] as const
export type Currency = (typeof CURRENCIES)[number]

const moneyFormatters = new Map<Currency, Intl.NumberFormat>()

/** Currency symbol + 2 decimals: `$8.58`, `€8.58`. */
export function formatMoney(amount: number | null | undefined, currency: Currency): string {
  if (amount == null || !Number.isFinite(amount)) return MISSING
  let formatter = moneyFormatters.get(currency)
  if (!formatter) {
    formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
    moneyFormatters.set(currency, formatter)
  }
  return formatter.format(amount)
}

const integerFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 })

/** Thousands separator + " PLEX": `1,500 PLEX`. Zero or missing renders as "—". */
export function formatPlex(amount: number | null | undefined): string {
  if (amount == null || !Number.isFinite(amount) || amount === 0) return MISSING
  return `${integerFormatter.format(amount)} PLEX`
}

/** Abbreviated with unit: `2.1B ISK`, `850M ISK`. */
export function formatIsk(amount: number | null | undefined): string {
  if (amount == null || !Number.isFinite(amount)) return MISSING
  const units: [number, string][] = [
    [1e12, 'T'],
    [1e9, 'B'],
    [1e6, 'M'],
    [1e3, 'K'],
  ]
  for (const [size, suffix] of units) {
    if (Math.abs(amount) >= size) {
      const value = amount / size
      const digits = Math.abs(value) >= 100 ? 0 : 1
      return `${Number(value.toFixed(digits))}${suffix} ISK`
    }
  }
  return `${integerFormatter.format(amount)} ISK`
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** End time in UTC: `26 Nov, 23:59 UTC`; `null` means the offer is ongoing. */
export function formatEndsAt(endsAt: string | Date | null): string {
  if (endsAt == null) return 'Always available'
  const date = typeof endsAt === 'string' ? new Date(endsAt) : endsAt
  if (Number.isNaN(date.getTime())) return MISSING
  const hh = String(date.getUTCHours()).padStart(2, '0')
  const mm = String(date.getUTCMinutes()).padStart(2, '0')
  return `${date.getUTCDate()} ${MONTHS[date.getUTCMonth()]}, ${hh}:${mm} UTC`
}
