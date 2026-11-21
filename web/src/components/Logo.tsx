/** Hexagon outline + circle, amber 2 px stroke (DESIGN.md §3 App header). */
export function LogoMark({ size = 28 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 28 28" fill="none" aria-hidden="true">
      <path d="M14 2 25 8.5v11L14 26 3 19.5v-11Z" stroke="var(--ow-accent)" strokeWidth="2" />
      <circle cx="14" cy="14" r="4" stroke="var(--ow-accent)" strokeWidth="2" />
    </svg>
  )
}
