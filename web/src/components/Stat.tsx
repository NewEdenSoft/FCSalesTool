import { MISSING } from '../lib/format'
import { num } from './ui'

/** Mono value above a muted label (DESIGN.md §3 Stat). `null` renders "—". */
export function Stat({ value, label }: { value: string | number | null; label: string }) {
  return (
    <div className="flex flex-col">
      <span className={`${num} text-num-lg font-semibold`}>{value ?? MISSING}</span>
      <span className="text-caption text-ow-muted">{label}</span>
    </div>
  )
}
