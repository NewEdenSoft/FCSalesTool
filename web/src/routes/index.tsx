import { createFileRoute } from '@tanstack/react-router'
import { HomeHero } from '../features/home/HomeHero'
import { HowItWorks } from '../features/home/HowItWorks'

export const Route = createFileRoute('/')({
  component: HomePage,
})

function HomePage() {
  // No deals API yet: render honest empty states (DESIGN.md §1.2, §7 "no fake data").
  // Replace with TanStack Query data once /api/deals exists.
  return (
    <>
      <HomeHero bestDeal={null} summary={null} />
      <HowItWorks />
    </>
  )
}
