import { num, panel } from '../../components/ui'

const STEPS = [
  {
    title: 'Every offer, recorded',
    body: 'Omega subscriptions, PLEX packs, bundles and skillpoint deals are logged with their price, where they are sold, and when they start and end.',
  },
  {
    title: 'Effective price per month',
    body: 'Price minus the value of any bonus PLEX as stated by CCP, divided by the months you get. That is the number offers are ranked by.',
  },
  {
    title: 'Know when to buy',
    body: 'Past sales show when the next one is likely, so you can decide whether to buy now or wait for a better price.',
  },
] as const

/** DESIGN.md §4 template 5 — Explainer grid of step cards. */
export function HowItWorks() {
  return (
    <section id="how-it-works" aria-labelledby="how-it-works-title" className="flex scroll-mt-6 flex-col gap-4">
      {/* Heading sits outside the grid so auto-fit can collapse unused tracks. */}
      <h2 id="how-it-works-title" className="text-h2 font-semibold">
        How it works
      </h2>
      <ol className="grid grid-cols-[repeat(auto-fit,minmax(260px,1fr))] gap-4">
        {STEPS.map((step, i) => (
          <li key={step.title} className={`${panel.base} gap-2`}>
            <span className={`${num} text-ow-accent`} aria-hidden="true">
              {String(i + 1).padStart(2, '0')}
            </span>
            <h3 className="text-h4 font-semibold">{step.title}</h3>
            <p className="text-ow-muted">{step.body}</p>
          </li>
        ))}
      </ol>
    </section>
  )
}
