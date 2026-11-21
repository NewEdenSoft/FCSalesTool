import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, beforeEach, vi } from 'vitest'
import { mockApi } from './mockApi'

beforeEach(() => {
  // Every test starts signed out with no real network; override with mockApi({...}).
  mockApi()
  // jsdom doesn't implement scrolling; the router calls it on navigation.
  vi.stubGlobal('scrollTo', vi.fn())
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})
