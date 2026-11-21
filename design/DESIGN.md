# OmegaWatch Design Spec

The single source of truth for how OmegaWatch looks and behaves. Read this before building or changing any UI. Tokens live in `tokens.css` (CSS variables), `tokens.json` (raw values) and `tailwind.preset.js` (Tailwind). Never hard-code a color, size or font that exists as a token.

**In the web app (Tailwind v4):** `web/src/styles/app.css` imports `tokens.css` and maps every token to a utility (`bg-ow-surface`, `text-ow-muted`, `font-heading`, `text-h1`, `rounded-panel`, `max-w-content`, breakpoints `sm/md/lg/xl`). `tailwind.preset.js` is the equivalent for Tailwind v3 projects and is not used by the app. Shared class recipes for buttons, panels and tags live in `web/src/components/ui.ts`.

The design canvas these rules came from: https://claude.ai/artifact/GykVXk8av1SUrdSKtdDKu7

---

## 1. Principles

1. **The number is the hero.** Every screen leads with the one figure the user came for (real cost per month, total, saving). Numbers are big, monospaced and amber when they are "the best".
2. **Honest data.** Every price shows where it came from and when it ends. Never invent or estimate a figure in the UI. If a value is missing, show `—` or a "not available" state, never a guess.
3. **Calm, dark, instrument-panel.** Dark navy ground, flat panels separated by 1 px borders. No gradients, glows, glassmorphism, emoji or decorative space imagery.
4. **One accent means one thing.** Amber = best value / primary action. Cyan = PLEX (in-game currency) and savings. Nothing else gets color.
5. **Accessible by default.** Real `<button>`, `<a>`, `<input>` + `<label>`. Text contrast ≥ 4.5:1. Touch targets ≥ 44 px. Everything works by keyboard and at 360 px wide.

---

## 2. Tokens

### Color

| Token | Hex | Use |
|---|---|---|
| `--ow-bg` | `#0A0E13` | Page background, inputs |
| `--ow-surface` | `#11171F` | Panels, cards, table body |
| `--ow-surface-raised` | `#161E28` | Active nav item, secondary buttons, hover rows |
| `--ow-surface-highlight` | `#1A1710` | Background of the "best value" table row |
| `--ow-border` | `#222D3A` | All 1 px borders and dividers |
| `--ow-border-strong` | `#3A4757` | Neutral bars in charts, inactive timeline dots |
| `--ow-text` | `#E6EBF1` | Primary text |
| `--ow-text-muted` | `#93A1B2` | Secondary text, labels, table headers (passes 4.5:1 on bg and surface) |
| `--ow-text-on-accent` | `#0A0E13` | Text on amber |
| `--ow-accent` | `#F2A93B` | Best value, primary buttons, focus ring, selected controls, links |
| `--ow-accent-hover` | `#FFC566` | Hover state of amber |
| `--ow-plex` | `#6CC6F0` | PLEX amounts, savings, info states |
| `--ow-plex-border` | `#2B5A70` | Outline of PLEX / "Limited time" tags |
| `--ow-danger` | `#F0857A` | Form errors, expired/failed states only |

Rules:
- Amber appears **at most once per region** as a highlight (one best row, one best card, one primary button per form).
- Never put muted text on `--ow-surface-highlight` at sizes under 14 px.
- Charts: neutral series `--ow-border-strong`, highlighted series `--ow-accent`, PLEX series `--ow-plex`. Never rely on hue alone; label values directly.

### Typography

Fonts (Google Fonts): **Space Grotesk** 500/600/700 (headings), **Inter** 400/500/600 (body/UI), **JetBrains Mono** 400/600 (every price, count, date-time and number).

```html
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=JetBrains+Mono:wght@400;600&family=Space+Grotesk:wght@500;600;700&display=swap" rel="stylesheet">
```

| Token | Size / line-height | Font, weight | Use |
|---|---|---|---|
| `display` | 56 / 1.05, −0.02em | Space Grotesk 700 | Home hero headline (one per site) |
| `h1` | 44 / 1.1, −0.02em | Space Grotesk 700 | Page title |
| `h2` | 28 / 1.2 | Space Grotesk 600 | Section title |
| `h3` | 20 / 1.3 | Space Grotesk 600 | Card / panel title |
| `h4` | 18 / 1.3 | Space Grotesk 600 | Small card title |
| `body-lg` | 17–18 / 1.5 | Inter 400 | Page intro text |
| `body` | 15 / 1.5 | Inter 400 | Default |
| `body-sm` | 14 / 1.5 | Inter 400 | Table cells, secondary info |
| `caption` | 13 / 1.4 | Inter 400 | Footnotes, helper text |
| `eyebrow` | 12–13, 0.08em, UPPERCASE | JetBrains Mono 400 | Labels above headings, table headers |
| `num-xl` | 52 / 1 | JetBrains Mono 600 | Hero figure (best price, calculator total) |
| `num-lg` | 22–28 | JetBrains Mono 600 | Stats, secondary figures |
| `num` | 14–15 | JetBrains Mono 400/600 | Prices in tables |

Use `font-variant-numeric: tabular-nums` on all numbers so columns line up.

### Spacing, radius, layout

- 4 px base, mostly 8 px steps: `4, 8, 12, 16, 20, 24, 28, 32, 40, 48, 56, 80`.
- Radius: `4` tags, `7–8` controls and buttons, `10` panels and cards, `50%` dots.
- Content max width **1200 px**, side padding 24 px (16 px under 480 px).
- Page top padding 48–56 px, bottom 80 px. Gap between page sections 32–40 px. Gap inside panels 16–24 px. Panel padding 24 px (28 px for hero cards).
- Breakpoints: `sm 480`, `md 768`, `lg 1024`, `xl 1280`. Layouts are flex-wrap based, so most components need no breakpoint at all.

### Elevation and motion

- No shadows. Separation is borders + surface steps.
- The only "inset" effect: the best table row gets a 3 px amber bar on its left (`box-shadow: inset 3px 0 0 var(--ow-accent)`).
- Transitions: 120 ms ease-out on color/background only. No layout animation. Respect `prefers-reduced-motion`.

### Focus

All interactive elements: `outline: 2px solid var(--ow-accent); outline-offset: 2px` on `:focus-visible`. Never remove focus styles.

---

## 3. Components

Class names below are suggestions (BEM-ish, `ow-` prefix). Match the structure and tokens; the framework is up to the project.

### App header
- Full-width, bottom border `--ow-border`. Inner row max 1200 px, padding 16/24, `flex-wrap`, space-between.
- Left: logo (hexagon outline + circle, 28 px, amber stroke 2 px) + wordmark "OmegaWatch" Space Grotesk 700 20 px.
- Center: nav links, 10/14 padding, radius 8, Inter 500. Inactive `--ow-text-muted`; active has `--ow-surface-raised` background, `--ow-text`, and `aria-current="page"`.
- Right: currency `<select>` (44 px tall, labelled) + primary button "Get deal alerts" + **account chip** (see below).
- Under 768 px: nav wraps under the logo row; no hamburger needed until there are more than 5 links.

### Buttons
| Variant | Style |
|---|---|
| Primary | bg `--ow-accent`, text `--ow-text-on-accent`, 600, height 44 (48 for main CTAs), padding 0 18, radius 8. Hover `--ow-accent-hover`. |
| Secondary | bg `--ow-surface-raised`, 1 px `--ow-border`, text `--ow-text`. Hover border `--ow-border-strong`. |
| Ghost / table action | transparent, 1 px `--ow-border`, height 40, 14 px 500. |
| Icon button | 44×44, secondary style, needs `aria-label`. |
External links (to the EVE Store) are `<a>` styled as buttons, with `target="_blank" rel="noopener"` and an "opens in new tab" hint for screen readers.

### Segmented control (filters)
- Container: `--ow-surface`, 1 px border, radius 10, padding 4, gap 4, `role="group"` + `aria-label`.
- Options: `<button aria-pressed>`, min-height 40, padding 0 14, radius 7, 14 px 500. Off: transparent, muted text. On: amber bg, dark text.
- Wraps on small screens. Used for: Pay with (Cash / PLEX / ISK), Length (All / 1 / 3 / 6 / 12 / 24 mo).
- Filter state is mirrored in the URL query (`?pay=cash&months=12`) so views can be shared.

### Panel / card
- `--ow-surface`, 1 px `--ow-border`, radius 10, padding 24, flex column gap 8–16.
- **Highlight card** (best deal, calculator result): border `--ow-accent` instead of `--ow-border`, padding 28. Only one per screen.
- **Step card** (how-it-works): eyebrow number `01` in amber mono, h4 title, muted body.

### Best-deal card
Order: eyebrow "Best value now" (amber) + end date (muted, right) → offer name (h3 26 px) → `num-xl` effective price in amber + "per month" muted → comparison line ("vs $19.99 paying month by month") → divider → 2-col facts (You pay / Bonus in cyan) → full-width primary CTA "Open in EVE Store".
- **Empty state** (no live offer recorded): regular panel, not highlight (nothing is "best"), muted eyebrow "Best value now", h3 "No live offers recorded yet", one muted sentence on what will appear. No price, no CTA.

### Deals table
- Wrapper: panel styling with `overflow-x: auto`; inner min-width 980 px so it scrolls rather than squashes on phones. Use a real `<table>` in production (the mock uses grid).
- Columns: Offer · Where · Length · Price · Bonus · Real $/mo (or PLEX/mo, ISK/mo) · Saving · Ends · action.
- Header: eyebrow style, muted, 12/20 padding, bottom border.
- Rows: 16/20 padding, bottom border, vertically centered. Numbers in mono. Bonus in cyan. Hover: `--ow-surface-raised`.
- Best row (lowest effective price in current filter): `--ow-surface-highlight` bg, amber inset bar, effective price amber 600, "Best value" tag.
- Tags under the offer name: **Best value** (amber fill, dark text) and **Limited time** (cyan text, `--ow-plex-border` outline). 11 px mono uppercase, radius 4.
- Empty state: centered muted sentence in the table body ("No offers at this length for this payment method.").
- Footnote under the table (caption, muted) explains the formula for the current pay mode.
- Phone (< 768 px) alternative allowed: render each row as a stacked card (name + tags, then effective price large, then a 2-col grid of the other fields).

### Stat
Mono `num-lg` value above a 13 px muted label. Used in a wrapping row with gap 24. With no data the value is `—`.

### Form fields
- Label above, Inter 600 15 px. Optional marker in muted 400.
- Input: height 44, padding 0 12, bg `--ow-bg`, 1 px `--ow-border`, radius 8, text `--ow-text`, placeholder `--ow-text-muted`.
- Helper text under: caption, muted. Error: caption in `--ow-danger`, input border `--ow-danger`, linked via `aria-describedby`.
- **Checkbox card**: whole row is the `<label>`, padding 12/14, bg `--ow-bg`, border, radius 8; 20 px checkbox with amber `accent-color`; title 500 + muted description.
- **Range slider**: full width, amber `accent-color`, 44 px hit area; current value shown top-right of the label row in amber mono; tick labels under it in muted mono 12 px.
- **Stepper**: icon button − / value in mono 22 px / icon button +.

### Status / confirmation
Panel with 1 px `--ow-plex` border, `role="status"`, h3 + muted body + secondary button.

### Avatar
- Round (50%), 32 px in lists and the header, 64 px on the account page. 1 px `--ow-border`, `--ow-surface-raised` behind.
- Shows the portrait of the account's chosen EVE character (`images.evetech.net`). With no character, or if the image fails, show 1–2 initials in Space Grotesk 600 on `--ow-surface-raised`.
- Always decorative (`alt=""`): the name is shown next to it.

### Account chip (header)
- Signed out: secondary button "Sign in" (amber is kept for the page's primary CTA).
- Signed in: a link to `/account`, 44 px tall, radius 8, padding 0 8: 32 px avatar + display name (Inter 500, truncated at ~220 px). Hover/active: `--ow-surface-raised` background.
- While the session is loading, reserve the space (no layout jump, no flash of "Sign in").

### Sign-in options
- "Sign in with EVE Online" is a full-width **secondary** button (text only — no EVE logo, see §7), with a caption saying what we learn from EVE. The email form's submit is the one primary button.
- Between the two options: a divider row — 1 px `--ow-border` lines either side of a muted caption "or" (`role="separator"`).
- Form-level errors (wrong password, SSO failed) are a `role="alert"` line in `--ow-danger` 14 px above the form actions; field errors follow Form fields.
- "Check your inbox" and similar results use the Status / confirmation panel.

### Linked character row
Rows separated by top borders, 12 px padding: 32 px avatar + name (500, truncated) + actions on the right — "Avatar" tag (cyan outline, like Limited time) on the current avatar, otherwise a ghost "Use as avatar" button; ghost "Unlink". Action buttons carry the character name in visually hidden text.

### Bar chart (cost per month)
- Rows: 64 px mono label column + bars. Each bar 14 px tall, radius 3, width proportional to the max (the 1-month price). Value label right of the bar in mono 13 px.
- Two series max: list price (`--ow-border-strong`) and with bundle (`--ow-accent`). Legend with 12 px swatches, top right.
- For anything more complex, use a chart library themed with these tokens.

### Timeline list (recent offers)
Rows separated by top borders, 12 px padding: 10 px dot (amber = live, `--ow-border-strong` = past) + title 500 + muted "dates · status" line.

### Footer
Top border, muted 13 px, wrapping row: fan-site disclaimer left ("OmegaWatch is a fan site, not affiliated with CCP Games. EVE Online is a trademark of CCP hf.") and pricing note right. The disclaimer is required on every page.

---

## 4. Page templates

Every page = App header → `<main>` (max 1200, padding 48–56 / 24 / 80, flex column gap 32–40) → Footer.

1. **Hero + highlight** (home): row with `flex-wrap`; text column `flex: 999 1 520px` (eyebrow in cyan, display headline, body-lg intro, stats row) and highlight card `flex: 1 1 380px`.
2. **Filter + table** (any list of offers): section header row (h2 left, segmented controls right, wraps) → table panel → footnote.
3. **Sidebar form + results** (calculator, settings, account): inputs panel `flex: 1 1 360px`, results column `flex: 999 1 560px` with highlight card first, then supporting panels. Stacks on phones with inputs first.
4. **Form + aside** (alerts, sign-in, sign-up, password reset): page header (eyebrow + h1 + optional intro), then form panel `flex: 999 1 560px`, aside panel `flex: 1 1 340px`.
5. **Explainer grid**: h2 spanning all columns, then `grid-template-columns: repeat(auto-fit, minmax(260px, 1fr))` of step cards.

New pages should be composed from these templates before inventing new layouts.

---

## 5. Data & content rules

- **Effective price per month** = `(price − value of bonus PLEX) ÷ months`. The bonus value is CCP's own stated saving for that bundle, not a market estimate. This is the default sort key.
- **Saving %** = `1 − effective / baseline`, baseline = 1-month list price in cash mode, 500 PLEX/month in PLEX mode. Show `—` for the baseline itself.
- **Pay modes:** Cash (EVE Store, real money), PLEX (New Eden Store, in game), ISK (PLEX cost × live PLEX market price). Column header and footnote change with the mode.
- Every offer record needs: `id, name, source, sourceUrl, months, price, currency, bonusPlex, bonusValue, startsAt, endsAt (null = ongoing), lastCheckedAt`. Hide offers past `endsAt` automatically.
- Dates: "26 Nov, 23:59 UTC" style in tables; always state UTC for end times. Show "Always available" for ongoing offers.
- Money: currency symbol + 2 decimals (`$8.58`). PLEX: thousands separator + " PLEX" (`1,500 PLEX`). ISK: abbreviated with unit (`2.1B ISK`).
- Missing values render as `—`. Never show placeholder text like `[live ISK]` in production.
- Copy voice: short, direct, second person, EVE vocabulary where players use it (Omega, PLEX, ISK, capsuleer, Jita). No hype words ("insane", "epic").

---

## 6. Accessibility checklist

- Landmarks: `header`, `nav[aria-label]`, `main`, `footer`; one `h1` per page.
- Segmented options use `aria-pressed`; current nav link uses `aria-current="page"`.
- Tables use `<table>`, `<th scope="col">`, and a visually hidden caption.
- Icon-only buttons have `aria-label`; decorative SVGs have `aria-hidden="true"`.
- Live result figures (calculator total) sit in an `aria-live="polite"` region.
- Test at 360 px width and 200% zoom; nothing may clip or overlap.

---

## 7. Don'ts

- No EVE Online logos, CCP artwork, or in-game screenshots as decoration.
- No gradients, glows, drop shadows, blurred backgrounds, emoji, or colored left-border cards.
- No new accent colors. No amber on more than one thing per region.
- No numbers in proportional fonts.
- No fake data in production builds.
