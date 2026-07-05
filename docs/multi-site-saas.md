# Multi-site & SaaS delivery

Imprint has always been multi-site at the domain level — one `site-{id}` stream per
site, each with its own pages, theme, locales and navigation. This document covers the
layer built on top of that: the **dashboard**, the **per-site deploy pipeline**
(environments + promotion), and the **identity** work needed to turn "many sites" into
"many isolated tenants" — the one piece deliberately left to the operator.

## The shapes at a glance

```
 Draft (SAVE)            Published (PUBLISH)          Deployed (per environment)
 ───────────             ───────────────────          ──────────────────────────
 every edit,             page.Publish() snapshots     rendered static files in a
 auto-saved as           the page — the "ready"       folder; content-hashed and
 events. On no           content the delivery         immutable, so promotion is
 environment.            plane may serve.             a byte-for-byte copy.
```

Three distinct steps, three distinct meanings. Editing never touches a folder; a page
becoming *published* is a deliberate domain decision; a published site reaching a
*folder* is a deploy.

## The dashboard (`/`)

After sign-in the landing page is a card per site — name, page count, and a status chip
per environment. A card opens **that site's** editor; its gear opens the site's
settings; a "New site" card runs the create flow. A fresh install shows the create flow
directly, then drops into the new site.

The editor edits the site that owns the **open page**, not whichever site is first:
`EditorSession.ActiveSite` derives from `CurrentPage.SiteId`, and the editor chrome
(locales, theme, translations, the Pages panel, the canvas) all read it. That is what
makes a dashboard card open the right site. A persistent "← All sites" link and a gear
sit in the editor top bar.

## Per-site deploy: environments and promotion

Each site owns an **ordered list of deploy environments** (`Site.SetEnvironments`, event
`site.environments-changed`), configured in the site's settings. An environment is just
a **name** and a **folder**:

```
Test        →  /var/www/acme/test
Staging     →  /var/www/acme/staging
Production  →  /var/www/acme
```

Order is the promotion pipeline. Two rules make it work:

- **The first environment auto-syncs.** Whenever content is published, the background
  `PublisherHostedService` renders each site's published content into its *first*
  environment (the pipeline's lowest rung, e.g. Test). Higher environments are never
  written by the auto-sync.
- **Higher environments are promotion targets.** `SiteDeployService.Promote(site, from,
  to)` copies the *exact rendered bytes* of one environment's folder onto the next. This
  is sound precisely because the output is content-hashed and immutable: copying the
  files reproduces the environment byte-for-byte, with no re-render and no chance of
  in-flight drafts leaking in. What you verified on Test is what reaches Production.

The settings page also offers **direct** `Publish to <env>` (render the current published
content straight into any environment's folder) for re-deploys or one-off pushes. Both
the pipeline and the direct button are supported; pick per environment.

A site with **no environments** falls back — only if it is the first-created site — to
the single global `PublishingOptions.OutputPath`, so a single-site install behaves
exactly as it always did.

### Where deploy status comes from

There is no separate deploy database. Each environment folder's own
`publish-manifest.json` *is* the record: `SiteDeployService.StatusOf(site)` reads it to
report the deployed site version, page count and last-write time. The folder on disk is
always the truth.

## Configuration

`PublishingOptions` (passed to `AddImprintPublishing`):

| Option | Meaning |
| --- | --- |
| `OutputPath` | Default output folder for the single-site (no-environments) fallback. |
| `DeployRoot` | **Sandbox root.** When set, every environment folder is a path *relative to this root*, and anything resolving outside it (via `..` or an absolute-looking value) is rejected. When null, folders are used as absolute paths as typed. |
| `BaseUrl` | Absolute site origin for canonical URLs / sitemap, applied **only** to the single-site `OutputPath` fallback. Environment-folder output is always rendered **portable** (root-relative, no absolute origin) — a single global origin would be wrong for every site but one, and root-relative links are what make byte-copy promotion valid across domains. Per-environment canonical origins are a documented follow-up. |
| `WidgetsDirectory` | Built-in widget manifest + bundles. |

**`DeployRoot` is the multi-tenant safety switch.** With it null (trust mode) a single
operator can publish straight into their own web roots — the shape of the initial SaaS
deployment. Set it before you let *tenants* type folder paths: a value like
`../../etc/cron.d` or `/etc/imprint` is then confined under the root instead of escaping
it (`DeployPathResolver`, covered by tests in `SiteDeployTests`).

## Identity & auth for SaaS — the recommendation

**You do not need Keycloak.** What Imprint actually needs to become multi-tenant is one
fact: *the signed-in user's email*, to own sites and filter the dashboard. Three ways to
get it:

1. **Auth proxy in front (recommended).** `oauth2-proxy` (or Cloudflare Access,
   Authelia, Pomerium) performs "Login with Google" itself and forwards the email as a
   header. Imprint reads it. No auth code in the app — exactly the boundary
   [architecture.md](architecture.md) already draws ("auth is the reverse proxy's job").
   oauth2-proxy talks to Google directly; **Keycloak is not required in the middle.**
2. **In-app OIDC.** Bake OpenID Connect into the Blazor app pointing at Google. Self
   contained, but you own the session/cookie handling.
3. **Keycloak.** Worth it only if you need self-hosted user management, *multiple* login
   providers, roles/groups, account linking, or a branded login. Overkill for "log in
   with Google → manage my sites."

Start with **oauth2-proxy + Google, no Keycloak.** The app-side work is identical
whichever you pick, so Keycloak can slot in behind the proxy later with **zero** Imprint
changes. And for a single operator today it works with **no auth at all** — Imprint uses
the OS user as the actor and shows every site; auth only becomes necessary when a second
customer must not see your sites.

### The app-side wiring (built)

Ownership exists and is now **enforced** whenever auth is enabled:

- **Owner stamped on create.** `EventMetadataProvider.ActorSource` is pointed at the
  signed-in user's email (`EditorActor` bridges the circuit identity to the write
  path), so every new site is owned by its creator.
- **Access enforced at the UI entry points.** `SiteAccess` (per-circuit) answers
  "which sites may this user touch": the dashboard lists `SiteOverview.AccessibleTo`,
  and the `/edit/{page}` and `/sites/{id}/settings` routes bounce users who fail
  `CanAccess` — the same "not found" treatment as an unknown id, so existence is not
  revealed. In Blazor Server these entry points are the attack surface: every command
  is dispatched from a component that first had to get past one of them.
- **Collaborators.** A site's settings page has a **People** card: the owner adds or
  removes editors by the email they sign in with (`site.collaborator-added` /
  `site.collaborator-removed` on the Site aggregate). A collaborator sees the site on
  their dashboard and can edit and publish it; only the owner manages the list. With
  auth off the list is kept but not enforced.

> **Blazor Server trap.** The interactive circuit has **no `HttpContext`**, so the
> forwarded identity header is unreadable from inside a component the normal way, and the
> command dispatcher runs on the *root* provider (it cannot see per-circuit scope).
> Capture the email once at circuit start — via `AuthenticationStateProvider` or
> `PersistentComponentState` — hold it per-circuit, and set `ActorSource` from it for the
> duration of that circuit's commands. This is the same small change regardless of proxy
> vs in-app OIDC.

### Remaining sharp edges

- A legacy site with an empty owner stays visible to everyone (so single-tenant
  installs keep working and no site is orphaned) — right for a demo, tighten before
  onboarding real tenants.
- `/media` requires login but is not owner-scoped: any signed-in user who knows a
  storage key can fetch any site's media bytes.
- Owner-only management of the People card is enforced in the UI, not in the command
  handler — fine while every dispatch path runs behind the gated components.
- There are no roles beyond owner/editor: a collaborator can edit, publish and change
  settings, but not manage access.
