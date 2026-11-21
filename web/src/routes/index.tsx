import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/')({
  component: HomePage,
})

function HomePage() {
  return (
    <section className="space-y-2">
      <h1 className="text-2xl font-bold">EVE Online subscription specials</h1>
      <p className="text-slate-600 dark:text-slate-400">
        Track Omega, PLEX and skillpoint deals, and plan when to buy.
      </p>
    </section>
  )
}
