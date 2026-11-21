import { useState } from 'react'
import { portraitUrl } from '../api/auth'

interface AvatarProps {
  /** Linked EVE character whose portrait to show; `null` shows initials. */
  characterId: number | null
  name: string
  size?: 32 | 64
}

/**
 * DESIGN.md §3 Avatar: round EVE character portrait, or initials on the raised surface.
 * Decorative (the name is always shown next to it), so it has an empty alt.
 */
export function Avatar({ characterId, name, size = 32 }: AvatarProps) {
  const [failedId, setFailedId] = useState<number | null>(null)
  const box = size === 64 ? 'size-16 text-h4' : 'size-8 text-caption'

  if (characterId !== null && failedId !== characterId) {
    return (
      <img
        src={portraitUrl(characterId, size === 64 ? 128 : 64)}
        alt=""
        width={size}
        height={size}
        onError={() => setFailedId(characterId)}
        className={`${box} shrink-0 rounded-full border border-ow-border bg-ow-raised object-cover`}
      />
    )
  }

  return (
    <span
      aria-hidden="true"
      className={`${box} inline-flex shrink-0 items-center justify-center rounded-full border border-ow-border bg-ow-raised font-heading font-semibold text-ow-text`}
    >
      {initials(name)}
    </span>
  )
}

function initials(name: string) {
  const parts = name.replace(/@.*/, '').split(/[\s._-]+/).filter(Boolean)
  const letters = parts.length > 1 ? parts[0][0] + parts[1][0] : (parts[0] ?? '?').slice(0, 2)
  return letters.toUpperCase()
}
