import { QueryClientProvider } from '@tanstack/react-query'
import { createMemoryHistory, createRouter, RouterProvider } from '@tanstack/react-router'
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { createQueryClient } from '../api/queryClient'
import { routeTree } from '../routeTree.gen'

describe('home page', () => {
  it('renders the heading', async () => {
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

    expect(
      await screen.findByRole('heading', { name: /subscription specials/i }),
    ).toBeInTheDocument()
  })
})
