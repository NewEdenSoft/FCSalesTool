import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Me } from '../api/auth'
import { mockApi, requestBody } from '../test/mockApi'
import { renderApp } from '../test/renderApp'

const me: Me = {
  id: '0190b7c4-0000-7000-8000-000000000001',
  displayName: 'Jita Trader',
  email: 'pilot@example.com',
  emailConfirmed: true,
  hasPassword: true,
  avatarCharacterId: 90000001,
  characters: [
    { characterId: 90000001, name: 'Pilot One', linkedAt: '2026-11-01T00:00:00Z' },
    { characterId: 90000002, name: 'Pilot Two', linkedAt: '2026-11-02T00:00:00Z' },
  ],
}

function fillLogin(email: string, password: string) {
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Password'), { target: { value: password } })
  fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))
}

describe('header account control', () => {
  it('offers sign in when signed out', async () => {
    renderApp('/')

    const banner = await screen.findByRole('banner')
    expect(await within(banner).findByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login?redirect=%2F')
  })

  it('shows the display name when signed in', async () => {
    mockApi({ 'GET /api/me': { body: me } })
    renderApp('/')

    const banner = await screen.findByRole('banner')
    expect(await within(banner).findByRole('link', { name: 'Jita Trader' })).toHaveAttribute('href', '/account')
  })
})

describe('sign in page', () => {
  it('signs in and goes to the requested page', async () => {
    const fetchMock = mockApi({ 'POST /api/auth/login': { body: me } })
    const { router } = renderApp('/login?redirect=%2Faccount')

    await screen.findByRole('heading', { level: 1, name: 'Sign in' })
    fillLogin('pilot@example.com', 'correct horse battery')

    await waitFor(() => expect(router.state.location.pathname).toBe('/account'))
    expect(requestBody(fetchMock, 'POST /api/auth/login')).toEqual({
      email: 'pilot@example.com',
      password: 'correct horse battery',
      rememberMe: true,
    })
    expect(fetchMock.mock.calls.find(([url]) => url === '/api/auth/login')?.[1]?.headers).toMatchObject({
      'X-Requested-With': 'fetch',
    })
  })

  it('shows the error for wrong credentials', async () => {
    mockApi({
      'POST /api/auth/login': { status: 401, body: { title: 'Email or password is incorrect.', code: 'invalid_credentials' } },
    })
    renderApp('/login')

    await screen.findByRole('heading', { level: 1, name: 'Sign in' })
    fillLogin('pilot@example.com', 'nope')

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is incorrect.')
  })

  it('offers a new confirmation link when the email is not confirmed', async () => {
    const fetchMock = mockApi({
      'POST /api/auth/login': { status: 403, body: { title: 'Confirm your email first.', code: 'email_not_confirmed' } },
      'POST /api/auth/resend-confirmation': { status: 202 },
    })
    renderApp('/login')

    await screen.findByRole('heading', { level: 1, name: 'Sign in' })
    fillLogin('pilot@example.com', 'correct horse battery')
    fireEvent.click(await screen.findByRole('button', { name: 'Send a new link' }))

    expect(await screen.findByText('New link sent. Check your inbox.')).toBeInTheDocument()
    expect(requestBody(fetchMock, 'POST /api/auth/resend-confirmation')).toEqual({ email: 'pilot@example.com' })
  })

  it('shows EVE sign-in only when the server supports it', async () => {
    mockApi({ 'GET /api/auth/providers': { body: { eve: true } } })
    renderApp('/login?redirect=%2Faccount')

    expect(await screen.findByRole('link', { name: 'Sign in with EVE Online' })).toHaveAttribute(
      'href',
      '/auth/eve/login?returnUrl=%2Faccount',
    )
  })

  it('explains EVE sign-in errors from the redirect', async () => {
    renderApp('/login?error=eve_failed')

    expect(await screen.findByRole('alert')).toHaveTextContent("EVE Online sign-in didn't complete.")
  })

  it('ignores redirects to other sites', async () => {
    mockApi({ 'POST /api/auth/login': { body: me } })
    const { router } = renderApp('/login?redirect=%2F%2Fevil.example')

    await screen.findByRole('heading', { level: 1, name: 'Sign in' })
    fillLogin('pilot@example.com', 'correct horse battery')

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
  })
})

describe('register page', () => {
  it('shows field errors from the API', async () => {
    mockApi({
      'POST /api/auth/register': {
        status: 400,
        body: { title: 'One or more validation errors occurred.', errors: { password: ['Passwords must be at least 10 characters.'] } },
      },
    })
    renderApp('/register')

    await screen.findByRole('heading', { level: 1, name: 'Create an account' })
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'new@example.com' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'short' } })
    fireEvent.click(screen.getByRole('button', { name: 'Create account' }))

    const password = screen.getByLabelText('Password')
    await waitFor(() => expect(password).toHaveAttribute('aria-invalid', 'true'))
    expect(password).toHaveAccessibleDescription(/Passwords must be at least 10 characters\./)
  })

  it('asks the user to check their inbox after registering', async () => {
    mockApi({ 'POST /api/auth/register': { status: 202 } })
    renderApp('/register')

    await screen.findByRole('heading', { level: 1, name: 'Create an account' })
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'new@example.com' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'correct horse battery' } })
    fireEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByRole('heading', { name: 'Check your inbox' })).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('new@example.com')
  })
})

describe('confirm email page', () => {
  it('confirms the email from the link', async () => {
    const fetchMock = mockApi({ 'POST /api/auth/confirm-email': { status: 204 } })
    renderApp('/confirm-email?userId=u1&code=c1')

    expect(await screen.findByRole('heading', { name: 'Email confirmed' })).toBeInTheDocument()
    expect(requestBody(fetchMock, 'POST /api/auth/confirm-email')).toEqual({ userId: 'u1', code: 'c1' })
    expect(fetchMock.mock.calls.filter(([url]) => url === '/api/auth/confirm-email')).toHaveLength(1)
  })

  it('confirms an email change when the link carries the new address', async () => {
    const fetchMock = mockApi({ 'POST /api/auth/confirm-email-change': { status: 204 } })
    renderApp('/confirm-email?userId=u1&code=c1&email=new%40example.com')

    expect(await screen.findByText('Your account now uses new@example.com.')).toBeInTheDocument()
    expect(requestBody(fetchMock, 'POST /api/auth/confirm-email-change')).toEqual({
      userId: 'u1',
      code: 'c1',
      email: 'new@example.com',
    })
  })

  it('explains an expired link', async () => {
    mockApi({ 'POST /api/auth/confirm-email': { status: 400, body: { code: 'invalid_link' } } })
    renderApp('/confirm-email?userId=u1&code=old')

    expect(await screen.findByRole('heading', { name: "This link doesn't work" })).toBeInTheDocument()
  })
})

describe('account page', () => {
  it('sends signed-out visitors to sign in', async () => {
    const { router } = renderApp('/account')

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(router.state.location.search).toEqual({ redirect: '/account' })
  })

  it('shows the profile and linked characters', async () => {
    mockApi({ 'GET /api/me': { body: me } })
    renderApp('/account')

    expect(await screen.findByRole('heading', { level: 1, name: 'Your account' })).toBeInTheDocument()
    expect(screen.getByLabelText('Display name')).toHaveValue('Jita Trader')
    const characters = screen.getByRole('region', { name: 'EVE characters' })
    expect(within(characters).getByText('Avatar')).toBeInTheDocument()
    expect(within(characters).getByRole('button', { name: 'Use as avatar: Pilot Two' })).toBeInTheDocument()
  })

  it('unlinks a character', async () => {
    const fetchMock = mockApi({
      'GET /api/me': { body: me },
      'DELETE /api/me/characters/90000002': { body: { ...me, characters: [me.characters[0]] } },
    })
    renderApp('/account')

    fireEvent.click(await screen.findByRole('button', { name: 'Unlink: Pilot Two' }))

    await waitFor(() => expect(screen.queryByText('Pilot Two')).not.toBeInTheDocument())
    expect(fetchMock).toHaveBeenCalledWith('/api/me/characters/90000002', expect.objectContaining({ method: 'DELETE' }))
  })

  it('lets an EVE-only account add a password without a current one', async () => {
    const eveOnly: Me = { ...me, email: null, emailConfirmed: false, hasPassword: false }
    const fetchMock = mockApi({ 'GET /api/me': { body: eveOnly }, 'POST /api/me/password': { status: 204 } })
    renderApp('/account')

    const panel = await screen.findByRole('region', { name: 'Password' })
    expect(within(panel).queryByLabelText('Current password')).not.toBeInTheDocument()
    fireEvent.change(within(panel).getByLabelText('Password'), { target: { value: 'my first password' } })
    fireEvent.click(within(panel).getByRole('button', { name: 'Add password' }))

    expect(await within(panel).findByText('Password saved')).toBeInTheDocument()
    expect(requestBody(fetchMock, 'POST /api/me/password')).toEqual({ newPassword: 'my first password' })
  })

  it('confirms a linked character after returning from EVE SSO', async () => {
    mockApi({ 'GET /api/me': { body: me } })
    renderApp('/account?linked=90000002')

    expect(await screen.findByRole('heading', { name: 'Pilot Two is linked' })).toBeInTheDocument()
  })
})
