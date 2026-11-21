import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import { CURRENCIES, type Currency } from '../lib/format'

/** Browser-persisted app settings (AGENTS.md: Zustand + persist for browser storage). */
interface SettingsState {
  currency: Currency
  setCurrency: (currency: Currency) => void
}

type PersistedSettings = Pick<SettingsState, 'currency'>

const DEFAULTS: PersistedSettings = { currency: 'USD' }

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      ...DEFAULTS,
      setCurrency: (currency) => set({ currency }),
    }),
    {
      name: 'ow-settings',
      version: 1,
      storage: createJSONStorage(() => localStorage),
      partialize: (state): PersistedSettings => ({ currency: state.currency }),
      // Future shape changes go here; unknown/old data falls back to defaults.
      migrate: (persisted) => sanitize(persisted),
      merge: (persisted, current) => ({ ...current, ...sanitize(persisted) }),
    },
  ),
)

function sanitize(persisted: unknown): PersistedSettings {
  const value = (persisted as Partial<PersistedSettings> | undefined)?.currency
  return {
    currency: CURRENCIES.includes(value as Currency) ? (value as Currency) : DEFAULTS.currency,
  }
}
