import { QueryClientProvider } from '@tanstack/react-query'
import { createMemoryHistory, createRouter, RouterProvider } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { createQueryClient } from '../api/queryClient'
import { FAN_SITE_DISCLAIMER } from '../config/site'
import { routeTree } from '../routeTree.gen'

async function renderHome() {
  const queryClient = createQueryClient()
  const router = createRouter({
    routeTree,
    context: { queryClient },
    history: createMemoryHistory({ initialEntries: ['/'] }),
  })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  await screen.findByRole('heading', { level: 1 })
}

describe('home page', () => {
  it('has a single h1 and the page landmarks', async () => {
    await renderHome()

    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
    expect(screen.getByRole('banner')).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toBeInTheDocument()
    expect(screen.getByRole('contentinfo')).toHaveTextContent(FAN_SITE_DISCLAIMER)
  })

  it('marks the current nav link', async () => {
    await renderHome()

    const nav = screen.getByRole('navigation', { name: 'Main' })
    expect(within(nav).getByRole('link', { name: 'Deals' })).toHaveAttribute('aria-current', 'page')
  })

  it('shows honest empty states instead of made-up numbers', async () => {
    await renderHome()

    const card = screen.getByRole('complementary', { name: 'Best deal right now' })
    expect(within(card).getByText('No live offers recorded yet')).toBeInTheDocument()
    expect(within(card).queryByRole('link')).not.toBeInTheDocument()
    expect(screen.getByText('offers tracked').previousSibling).toHaveTextContent('—')
  })

  it('explains how it works', async () => {
    await renderHome()

    expect(screen.getByRole('heading', { level: 2, name: 'How it works' })).toBeInTheDocument()
  })
})
