import { useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link, redirect, useRouter } from '@tanstack/react-router'
import { EVE_ERRORS, meQueryKey, meQueryOptions, safeRedirect, type Me } from '../api/auth'
import { FormError } from '../components/forms'
import { AuthLayout, EveSignIn } from '../features/auth/AuthLayout'
import { LoginForm } from '../features/auth/LoginForm'

interface LoginSearch {
  redirect?: string
  error?: string
}

export const Route = createFileRoute('/login')({
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    redirect: safeRedirect(search.redirect),
    error: typeof search.error === 'string' ? search.error : undefined,
  }),
  beforeLoad: async ({ context, search }) => {
    const me = await context.queryClient.ensureQueryData(meQueryOptions())
    if (me) throw redirect({ href: search.redirect ?? '/account' })
  },
  component: LoginPage,
})

function LoginPage() {
  const { redirect: returnTo, error } = Route.useSearch()
  const queryClient = useQueryClient()
  const router = useRouter()

  function onSignedIn(me: Me) {
    queryClient.setQueryData(meQueryKey, me)
    router.history.push(returnTo ?? '/')
  }

  return (
    <AuthLayout
      title="Sign in"
      intro="Use your EVE Online character or your email and password."
      aside={
        <>
          <h2 className="text-h4">New here?</h2>
          <p className="text-ow-muted">
            Signing in with EVE Online creates an account for you. Or{' '}
            <Link to="/register" search={returnTo ? { redirect: returnTo } : {}}>
              create one with your email
            </Link>
            .
          </p>
          <p className="text-body-sm text-ow-muted">
            Already have an email account? Sign in here, then link your characters from your account page.
          </p>
        </>
      }
    >
      <FormError message={error ? (EVE_ERRORS[error] ?? EVE_ERRORS.eve_failed) : undefined} />
      <EveSignIn returnUrl={returnTo} />
      <LoginForm onSignedIn={onSignedIn} />
    </AuthLayout>
  )
}
