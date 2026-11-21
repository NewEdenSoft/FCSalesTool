import { createFileRoute, Link, redirect } from '@tanstack/react-router'
import { meQueryOptions, safeRedirect } from '../api/auth'
import { AuthLayout, EveSignIn } from '../features/auth/AuthLayout'
import { RegisterForm } from '../features/auth/RegisterForm'

export const Route = createFileRoute('/register')({
  validateSearch: (search: Record<string, unknown>): { redirect?: string } => ({
    redirect: safeRedirect(search.redirect),
  }),
  beforeLoad: async ({ context }) => {
    if (await context.queryClient.ensureQueryData(meQueryOptions())) throw redirect({ to: '/account' })
  },
  component: RegisterPage,
})

function RegisterPage() {
  const { redirect: returnTo } = Route.useSearch()

  return (
    <AuthLayout
      title="Create an account"
      intro="Save your planner settings and get deal alerts."
      aside={
        <>
          <h2 className="text-h4">Already have an account?</h2>
          <p className="text-ow-muted">
            <Link to="/login" search={returnTo ? { redirect: returnTo } : {}}>
              Sign in
            </Link>{' '}
            instead.
          </p>
          <p className="text-body-sm text-ow-muted">
            You can link EVE characters to an email account at any time. Their portrait becomes your avatar.
          </p>
        </>
      }
    >
      <EveSignIn returnUrl={returnTo} />
      <RegisterForm />
    </AuthLayout>
  )
}
