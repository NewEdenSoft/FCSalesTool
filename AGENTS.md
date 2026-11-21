# AGENTS.md

Guidance for AI coding agents (and humans) working in this repo. The product plan is in [docs/PLAN.md](docs/PLAN.md); the workflow (branches, commits, PRs, releases) is in [CONTRIBUTING.md](CONTRIBUTING.md).

## Layout

```
FCSalesTool.slnx
aspire.config.json            # points the Aspire CLI at the AppHost
src/
  FCSalesTool.AppHost/        # Aspire AppHost: postgres, api, web
  FCSalesTool.ServiceDefaults/# OpenTelemetry, health checks, service discovery
  FCSalesTool.Domain/         # entities, value objects — no dependencies
  FCSalesTool.Application/    # planners, prediction, use cases — depends on Domain
  FCSalesTool.Infrastructure/ # EF Core, ESI client, jobs — depends on Application
  FCSalesTool.Api/            # minimal API endpoints, EVE SSO (BFF)
tests/                        # xUnit v3 on Microsoft.Testing.Platform
web/                          # React 19 + TS (Vite, pnpm)
```

Dependency direction: `Api → Infrastructure → Application → Domain`. Domain and Application never reference EF Core or ASP.NET Core.

## Running

- **Everything:** `aspire start --non-interactive`, then `aspire wait <resource>` for `postgres`, `api`, `web`. Stop with `aspire stop`. Requires Docker for Postgres.
- Postgres runs as a **persistent** container with a data volume. Don't use `aspire stop --force` / `--volumes` without explicit approval — it deletes data.
- The web app is a Vite dev server that proxies `/api` and `/auth` to the API (see `web/vite.config.ts`); the API URL comes from Aspire service discovery env vars.
- **Fixed local ports** (set in `AppHost.cs`, locked by `tests/FCSalesTool.AppHost.Tests`):

  | Resource | URL | Why fixed |
  |---|---|---|
  | web (Vite) | http://localhost:5173 | EVE SSO callback `http://localhost:5173/auth/eve/callback` must match the registered URL exactly |
  | api | http://localhost:5257, https://localhost:7102 | Stable for `.http` files and tools; the browser goes through the Vite proxy |
  | mailpit UI | http://localhost:8025 | Bookmarkable inbox |

  The EVE callback goes to the Vite origin, not the API, so auth cookies stay same-origin; the proxy sends `X-Forwarded-Host` so the API builds links and the redirect URI for that origin. Changing the web port means updating the EVE app registration.
- **Email:** the AppHost runs **Mailpit**; every email the API sends (confirmation, reset) shows up in its web UI (open the `mailpit` resource's http endpoint from the dashboard). Without SMTP configured, Development logs emails instead.
- **Migrations:** `dotnet ef migrations add <Name> --project src/FCSalesTool.Infrastructure --output-dir Persistence/Migrations` (tool pinned in `dotnet-tools.json`; run `dotnet tool restore` once). Development applies migrations on startup.

## Configuration

Local dev needs **no manual setup** except the optional EVE SSO credentials: the AppHost passes every API setting as an environment variable. Its inputs are Aspire parameters scaffolded (empty) under `Parameters` in `src/FCSalesTool.AppHost/appsettings.Development.json`, which is committed. **Secrets never go in that file.** Put them in `src/FCSalesTool.AppHost/appsettings.Development.local.json` (git-ignored via `appsettings.*.local.json`; same shape, overrides the committed file and user secrets):

```json
{
  "Parameters": {
    "eve-client-id": "<id>",
    "eve-client-secret": "<secret>",
    "eve-scopes": ""
  }
}
```

User secrets (`dotnet user-secrets --project src/FCSalesTool.AppHost set "Parameters:eve-client-secret" "<secret>"`) work too.

For EVE SSO, create an app at developers.eveonline.com with callback `http://localhost:5173/auth/eve/callback` and no scopes. Without it the EVE buttons are hidden.

API settings (`src/FCSalesTool.Api/appsettings.json` shows the shape and defaults; production sets them as env vars, `:` → `__`):

| Setting (env var) | Required | Local dev source | Notes |
|---|---|---|---|
| `ConnectionStrings__fcsalestool` | yes | Aspire (postgres) | |
| `App__PublicBaseUrl` | outside Development | web endpoint (`http://localhost:5173`) | Origin for links in emails |
| `App__SiteName` | no | default `OmegaWatch` | Used in email text |
| `Auth__Eve__ClientId` | no | parameter `eve-client-id` | Empty disables EVE SSO |
| `Auth__Eve__ClientSecret` | with ClientId | parameter `eve-client-secret` (secret) | |
| `Auth__Eve__Scopes` | no | parameter `eve-scopes` | Space-separated; empty = auth only |
| `Auth__RateLimitPerMinute` | no | default `20` | Per IP, anonymous auth endpoints |
| `Email__FromAddress` / `Email__FromName` | no | parameters `email-from-address` / `email-from-name` | |
| `Email__Smtp__Host` | outside Development | Mailpit | Unset in Development → emails are logged |
| `Email__Smtp__Port` | no | Mailpit | default `587` |
| `Email__Smtp__Username` / `Email__Smtp__Password` | if the server needs auth | — | Password is a secret |
| `Email__Smtp__Security` | no | `None` (Mailpit) | `Auto` \| `None` \| `StartTls` \| `SslOnConnect` |
| `Database__MigrateOnStartup` | no | default (true in Development) | |

When you add a setting: give it a default in the API's `appsettings.json`, wire it in `AppHost.cs` (parameter scaffolded in `appsettings.Development.json` if it needs a local value), and add a row here.

## Checks (run before opening a PR)

```sh
dotnet build
dotnet test
pnpm -C web lint
pnpm -C web typecheck
pnpm -C web test
pnpm -C web build
```

## Backend conventions

- .NET 10, nullable enabled. SDK pinned in `global.json`; tests use Microsoft.Testing.Platform (`dotnet test` works directly).
- Aspire 17 packages; keep all `Aspire.*` packages on the same version. Add integrations with `aspire add <name>`.
- The AppHost uses `AddDotnetProject(name, path)` (experimental, `ASPIREDOTNETPROJECT001`), not the obsolete `AddProject<T>`.
- API endpoints live under `MapGroup("/api")`, use `TypedResults`, and return DTOs (never EF entities). Errors use `ProblemDetails`.
- Use `TimeProvider` for "now" — no `DateTime.UtcNow` in Domain/Application.
- Planners and prediction are pure services in Application; most tests go there.
- `Money(decimal Amount, Currency Currency)` for all prices; mixing currencies throws.
- Auth: ASP.NET Core Identity with local accounts (`AppUser`, `LinkedCharacter` in Infrastructure); EVE SSO only identifies a character — never store EVE tokens. See PLAN.md §7.1.
- State-changing requests need the `X-Requested-With` header (CSRF); the web client's `api()` in `src/api/client.ts` adds it.
- API tests use `ApiFactory` (in-memory SQLite, captured email). Simulate EVE SSO with `EveSsoSimulator.CompleteAsync`.

## Frontend conventions

- React 19, TypeScript, Tailwind CSS v4 (tokens in `src/styles/app.css` `@theme`; no tailwind config file).
- **TanStack Router**, file-based routes in `src/routes/`. `src/routeTree.gen.ts` is generated by the Vite plugin and committed — don't edit it by hand. Test files in `routes/` are prefixed with `-` so the router ignores them.
- **TanStack Query** for all server state; query option factories in `src/api/`.
- Shareable UI state (planner inputs, filters) goes in router search params.
- Global client state, if needed, uses **Zustand**. Browser-persisted settings use a Zustand store with the `persist` middleware (unique `name`, `version` + `migrate`, `partialize`).
- Tests: Vitest + React Testing Library (`*.test.tsx`). `fetch` is stubbed per test with `mockApi()` (`src/test/mockApi.ts`; default: signed out); `renderApp(path)` renders the real route tree.
- Lint: oxlint (`.oxlintrc.json`).

## Design
All UI follows `design/DESIGN.md`. Read it before building or changing any screen.
- Use tokens from `design/tokens.css` (or the Tailwind preset). Never hard-code colors, fonts or sizes.
- Build new screens from the page templates in DESIGN.md §4 and the components in §3.
- `design/components.html` is the visual reference for component markup.
- If a feature needs a component that isn't in the spec, design it from the existing tokens and add it to DESIGN.md in the same change.