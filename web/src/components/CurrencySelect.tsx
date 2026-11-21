import { useId } from 'react'
import { CURRENCIES, type Currency } from '../lib/format'
import { useSettingsStore } from '../stores/settingsStore'

const LABELS: Record<Currency, string> = { USD: 'USD $', EUR: 'EUR €' }

export function CurrencySelect() {
  const id = useId()
  const currency = useSettingsStore((s) => s.currency)
  const setCurrency = useSettingsStore((s) => s.setCurrency)

  return (
    <div className="flex items-center gap-3">
      <label htmlFor={id} className="text-caption text-ow-muted">
        Currency
      </label>
      <select
        id={id}
        value={currency}
        onChange={(e) => setCurrency(e.target.value as Currency)}
        className="h-11 rounded-control border border-ow-border bg-ow-bg px-3 text-ow-text"
      >
        {CURRENCIES.map((c) => (
          <option key={c} value={c}>
            {LABELS[c]}
          </option>
        ))}
      </select>
    </div>
  )
}
