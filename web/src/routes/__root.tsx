import type { QueryClient } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { createRootRouteWithContext, Outlet } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools'
import { AppFooter } from '../components/AppFooter'
import { AppHeader } from '../components/AppHeader'
import { container } from '../components/ui'

export interface RouterContext {
  queryClient: QueryClient
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
})

/** DESIGN.md §4: App header → main (max 1200, 48–56 / 24 / 80, gap 32–40) → Footer. */
function RootLayout() {
  return (
    <div className="flex min-h-dvh flex-col">
      <AppHeader />
      <main className={`${container} flex flex-1 flex-col gap-10 pt-12 pb-20 md:pt-14`}>
        <Outlet />
      </main>
      <AppFooter />
      {import.meta.env.DEV && (
        <>
          <TanStackRouterDevtools position="bottom-right" />
          <ReactQueryDevtools buttonPosition="bottom-left" />
        </>
      )}
    </div>
  )
}
