import { FAN_SITE_DISCLAIMER } from '../config/site'
import { useSettingsStore } from '../stores/settingsStore'
import { container } from './ui'

export function AppFooter() {
  const currency = useSettingsStore((s) => s.currency)

  return (
    <footer className="border-t border-ow-border">
      <div className={`${container} flex flex-wrap justify-between gap-3 py-6 text-caption text-ow-muted`}>
        <span>{FAN_SITE_DISCLAIMER}</span>
        <span>Prices in {currency}, before local tax.</span>
      </div>
    </footer>
  )
}
