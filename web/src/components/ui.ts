// Shared class recipes for DESIGN.md §3 components. Compose these instead of re-typing
// token classes, so every button/panel/tag looks the same.

const buttonBase =
  'inline-flex items-center justify-center gap-2 rounded-control border px-[18px] font-semibold no-underline transition-colors cursor-pointer'

export const button = {
  primary: `${buttonBase} h-11 border-transparent bg-ow-accent text-ow-on-accent hover:bg-ow-accent-hover hover:text-ow-on-accent`,
  primaryCta: `${buttonBase} h-12 border-transparent bg-ow-accent text-ow-on-accent hover:bg-ow-accent-hover hover:text-ow-on-accent`,
  secondary: `${buttonBase} h-11 border-ow-border bg-ow-raised font-medium text-ow-text hover:border-ow-border-strong hover:text-ow-text`,
  ghost: `${buttonBase} h-10 border-ow-border bg-transparent text-body-sm font-medium text-ow-text hover:bg-ow-raised hover:text-ow-text`,
} as const

export const panel = {
  base: 'flex flex-col gap-4 rounded-panel border border-ow-border bg-ow-surface p-6',
  /** Best deal / calculator result. Only one per screen. */
  highlight: 'flex flex-col gap-4 rounded-panel border border-ow-accent bg-ow-surface p-7',
  status: 'flex flex-col gap-2 rounded-panel border border-ow-plex bg-ow-surface p-6',
} as const

export const eyebrow = 'font-mono text-eyebrow uppercase'

export const num = 'font-mono tabular-nums'

export const tag = {
  best: 'self-start inline-block rounded-tag bg-ow-accent px-1.5 py-px font-mono text-[11px] uppercase tracking-[0.06em] text-ow-on-accent',
  limited:
    'self-start inline-block rounded-tag border border-ow-plex-border px-1.5 font-mono text-[11px] uppercase tracking-[0.06em] text-ow-plex',
} as const

/** Page content container: max 1200 px, 24 px gutter (16 px under 480 px). */
export const container = 'mx-auto w-full max-w-content px-4 sm:px-6'
