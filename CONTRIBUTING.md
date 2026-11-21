# Contributing to FCSalesTool

How we turn the plan in [docs/PLAN.md](docs/PLAN.md) into shipped code: small pieces, a predictable cycle, reviewed pull requests, and versioned releases.

---

## 1. Principles

- **`main` is always releasable.** It builds, tests pass, and it could be deployed at any moment.
- **Small, vertical pieces.** Each change delivers one thing end to end (API + UI + tests) or one clearly bounded layer. Aim for a PR that can be reviewed in under 30 minutes.
- **Everything goes through a PR.** No direct pushes to `main`.
- **Tests travel with the code.** A feature or fix is not done until its tests are in the same PR.

---

## 2. Planning: from roadmap to issues

| Level | GitHub concept | Example |
|---|---|---|
| Roadmap phase | **Milestone** | `v0.2 – Subscription Planner` |
| Feature | **Issue** labelled `feature` (may be a parent with sub-issues) | "Subscription optimiser" |
| Piece of work | **Issue** (one PR each) | "Optimiser: PLEX-pooled Omega redemption constraint" |

### Breaking work down

A good issue:

- Has a single, testable outcome ("Admin can add a sale with offers"), not an activity ("Work on admin").
- Fits in **1–2 days** of work and roughly **≤ 400 changed lines** (excluding generated code, migrations, lock files).
- States **acceptance criteria** as a checklist.
- Notes anything it depends on (`Blocked by #12`).

If a piece is bigger, split it by layer (domain → API → UI) or by scenario (happy path first, edge cases next), as long as each PR leaves `main` working. Unfinished UI can stay hidden behind a route that isn't linked yet.

### Labels

| Type | Area | Status |
|---|---|---|
| `feature`, `bug`, `chore`, `docs`, `refactor` | `api`, `web`, `domain`, `planner`, `prediction`, `admin`, `infra` | `blocked`, `needs-decision` |

---

## 3. Development cycle

```
pick issue → branch → build in small commits → local checks → PR → review → squash-merge → delete branch
```

1. **Pick** an issue from the current milestone and assign yourself.
2. **Branch** from up-to-date `main` (see naming below).
3. **Build** in small commits. Push early; open a **draft PR** if you want feedback.
4. **Run local checks** before asking for review:
   ```sh
   dotnet build && dotnet test                         # backend
   pnpm -C web lint && pnpm -C web typecheck && pnpm -C web test   # frontend
   ```
   If you changed API contracts, regenerate the TypeScript client and commit the result.
5. **Open / mark ready** the PR, linking the issue (`Closes #123`).
6. **Address review**, keep the branch up to date with `main` (rebase or merge, your choice — it's squashed anyway).
7. **Squash-merge** once CI is green and approved. The branch is deleted automatically.

### Branch naming

```
<type>/<issue-number>-<short-description>
```

Examples: `feat/12-sale-admin-form`, `fix/31-plex-rounding`, `chore/4-ci-pipeline`, `docs/7-contributing`.

Types match the commit types below. Branches are short-lived — merge within a few days; if one lives longer, it is probably too big.

### Commit messages — Conventional Commits

```
<type>(<optional scope>): <summary in imperative mood>

<optional body: why, not what>

<optional footer: Closes #123, BREAKING CHANGE: ...>
```

| Type | Use for | Version impact |
|---|---|---|
| `feat` | New user-visible behaviour | minor |
| `fix` | Bug fix | patch |
| `perf` | Performance improvement | patch |
| `refactor` | Code change with no behaviour change | — |
| `test` | Adding or fixing tests | — |
| `docs` | Documentation | — |
| `build` / `ci` | Build system, dependencies, CI | — |
| `chore` | Anything else (tooling, housekeeping) | — |

Scopes follow the area labels: `feat(planner): add budget-constrained mode`.
A `!` after the type or a `BREAKING CHANGE:` footer marks a breaking change.

The **PR title** must also follow this format, because it becomes the squash commit on `main`.

---

## 4. Pull requests

### Size and scope

- One issue per PR. Unrelated cleanups go in their own PR.
- Generated files (API client, EF migrations, lock files) are fine to include but should be called out in the description.

### PR description

```markdown
## What
Short summary of the change.

## Why
Link to the issue and any context. Closes #123

## How to test
Steps or commands a reviewer can run.

## Checklist
- [ ] Tests added/updated
- [ ] API client regenerated (if contracts changed)
- [ ] EF migration added and reviewed (if schema changed)
- [ ] Docs / PLAN.md updated (if behaviour or decisions changed)
- [ ] Screenshots attached (if UI changed)
```

### Review

- At least **one approval** before merging. If you're working solo, review your own diff on GitHub before merging and let CI be the gate.
- Reviewers focus on correctness, tests, and fit with [docs/PLAN.md](docs/PLAN.md); formatting is left to the linters.
- Comments prefixed `nit:` are optional; everything else should be resolved or answered.

### Definition of done

- Acceptance criteria in the issue are met.
- CI is green (backend build + tests, frontend lint + typecheck + tests + build).
- New logic in planners/prediction has unit tests covering the happy path and at least the main edge cases.
- No secrets, no commented-out code, no stray `console.log`.

### Branch protection on `main`

- Require a pull request before merging.
- Require status checks to pass (CI).
- Require linear history (squash merge only).
- Automatically delete head branches after merge.

---

## 5. Special cases

### Database migrations

- One migration per PR that changes the schema, named after the change (`AddSaleEventTag`).
- Never edit a migration that has reached `main`; add a new one.
- Destructive changes (dropping columns/tables) are done in two releases: stop using it, then remove it.

### API contract changes

- Additive changes (new endpoints, new optional fields) are preferred.
- Changing or removing fields used by the frontend requires updating the frontend in the same PR.
- Regenerate the TypeScript client and commit it with the change.

### Dependencies

- Dependency updates go in their own `build(deps): ...` PR, not mixed with features.

---

## 6. Releases

### Versioning

- **Semantic Versioning** (`MAJOR.MINOR.PATCH`), tags of the form `vX.Y.Z`.
- Stay on **`0.x`** until the public launch. Each roadmap phase is a minor release:

| Milestone | Contents |
|---|---|
| `v0.1` | Phase 1 — foundation, data model, admin entry, sales history, EVE SSO |
| `v0.2` | Phase 2 — Subscription Planner |
| `v0.3` | Phase 3 — Sale prediction, "buy now or wait" |
| `v0.4` | Phase 4 — Skillpoint Planner |
| `v0.5` | Phase 5 — Alerts, charts, polish |
| `v1.0` | Public launch |

- Bug fixes between milestones are patch releases (`v0.2.1`).

### Release process

1. Confirm the milestone's issues are closed (or moved to the next milestone).
2. Make sure `main` is green.
3. Create the tag and a **GitHub Release** from `main`. The release notes are generated from the Conventional Commit titles since the previous tag, grouped into Features / Fixes / Other.
4. The release tag triggers the production build and deploy (once hosting is decided — see open questions in the plan).
5. Close the milestone.

*(Later option: automate steps 3–4 with release-please, which opens a release PR and maintains `CHANGELOG.md` from commit history.)*

### Hotfixes

For an urgent production bug: branch `fix/<issue>-...` from `main`, PR, squash-merge, and immediately tag a patch release. There are no long-lived release branches.

---

## 7. Working with Claude Code

- Project conventions for agents live in `AGENTS.md`. Keep it in sync when conventions change.
- Shared Claude Code config (`.claude/settings.json`, project skills) is committed; personal settings (`.claude/settings.local.json`) are not.
- The same cycle applies: one issue, one branch, one PR.
