# Deploying Imprint (imprint.canine.dev)

This is the runbook for the live deployment of Imprint onto the CanineCC estate. It reflects
the actual topology of that estate rather than a generic cloud setup.

## Topology

```
                         Internet
                            │
        imprint.canine.dev  │  app.imprint.canine.dev   (api.imprint.canine.dev — reserved)
        (marketing)         │  (the editor)
               ▼            ▼
        ┌───────────────────────────────┐
        │  canine-dgx1                   │  nginx + Let's Encrypt — the estate's TLS edge
        │  (192.168.1.159)               │  (canine.dev, watchdog.canine.dev, auth.canine.dev, …)
        └───────┬───────────────┬────────┘
   static files │               │  proxy_pass http://192.168.1.10:8102   (LAN, http)
   (apex site)  ▼               ▼
        served on dgx1    ┌──────────────────────────────────────────┐
                          │  canine-wrx1  (192.168.1.10)             │
                          │                                          │
                          │  host nginx :8102                        │
                          │     include …/bluegreen/imprint.active   │
                          │        │                                 │
                          │        ├─▶ imprint-blue  127.0.0.1:18102 │  Blazor Server editor
                          │        └─▶ imprint-green 127.0.0.1:19102 │  (one colour live at a time)
                          │                                          │
                          │  + Keycloak (auth.canine.dev, realm `imprint`)
                          └──────────────────────────────────────────┘
                                   │  publishes static sites into web roots on…
                          ┌────────┴─────────┐
                          ▼                  ▼
                     wrx1 web roots     canine-ultra1 (Caddy)      ← per-site deploy targets
                     (marketing:        (other Imprint-maintained  (see docs/multi-site-saas.md)
                      watchdog/assay/     sites)
                      cai.canine.dev)
```

The estate convention (same as watchdog/cai/assay): **`imprint.canine.dev` is the marketing
site** (pages *about* Imprint, authored *in* Imprint — dogfooded), **`app.imprint.canine.dev`
is the editor**, and **`api.imprint.canine.dev`** is reserved for a future public API. TLS
terminates on dgx1; the delivery plane is plain static files; only the editor needs a login.

## 1. Authentication — Keycloak (in-app OIDC, "Sign in with Google")

The editor has **no built-in login by design** — it consumes an identity and leaves auth to
the platform. On this estate the platform is **Keycloak** (`auth.canine.dev`), and the editor
speaks OIDC to it directly, exactly like `remoteclaude.canine.dev` does. Imprint has its **own
realm** (`imprint`, one realm per product), and the realm brokers to **Google**, so a user
lands straight on "Sign in with Google" rather than a Keycloak password form (via
`Keycloak__IdpHint=google` → `kc_idp_hint`).

Configured by the `Keycloak` section (via `Keycloak__*` environment variables in the editor's
env file):

| Setting | Meaning | Value |
| --- | --- | --- |
| `Keycloak__Authority` | The OIDC issuer. **Auth is OFF until this is set.** | `https://auth.canine.dev/realms/imprint` |
| `Keycloak__ClientId` | The confidential client registered for the editor | `imprint` |
| `Keycloak__ClientSecret` | That client's secret | *(from Keycloak)* |
| `Keycloak__RequireHttps` | Validate metadata/tokens over HTTPS | `true` |
| `Keycloak__IdpHint` | Jump straight to this broker | `google` |
| `Imprint__AllowUnauthenticated` | Escape valve: run open in Production **only** on a trusted network | *(unset)* |

**Safety property:** in `Production`, the editor **refuses to start** with no Keycloak
configured unless `Imprint__AllowUnauthenticated=true` is set explicitly. An unauthenticated
editor is never exposed by accident. In `Development` (and the test suite) auth is simply off
and the OS user is the actor — the editor behaves exactly as it always has. The signed-in
user's email is recorded as the **actor** on every event, so sites are owned by their creator
(`SiteOverview.OwnedBy`).

The editor disables Pushed Authorization Requests (PAR) to match the rest of the estate. The
`imprint` client's redirect URIs point at the **app** host:
`https://app.imprint.canine.dev/signin-oidc` (login) and `https://app.imprint.canine.dev/`
(post-logout). Google's OAuth client lists the realm broker endpoint
`https://auth.canine.dev/realms/imprint/broker/google/endpoint` as an authorized redirect URI.

## 2. The editor on wrx1 — blue/green pair

The editor runs as a **published artifact** in a zero-downtime **blue/green** pair, the same
way as the estate's other .NET apps. Nothing here is built at startup.

- **Artifacts:** `/home/jimmy/apps/imprint/app-blue` and `…/app-green` (the two colours);
  `…/app.new` is the staging dir a deploy publishes into before cutover.
- **Services:** `imprint-blue.service` / `imprint-green.service` — `User=jimmy`,
  `ASPNETCORE_ENVIRONMENT=Production`, bound to `127.0.0.1:18102` / `127.0.0.1:19102`,
  `EnvironmentFile=/home/jimmy/.config/imprint/imprint.env`. One colour is live at a time.
- **Front door on wrx1:** host nginx listens on `:8102` and `include`s
  `/etc/nginx/bluegreen/imprint.active`, which is a single `proxy_pass` line pointing at the
  live colour's port. It carries WebSocket upgrade + `$bg_proto`, a long `proxy_read_timeout`
  (Blazor circuits), and enlarged buffers/header limits (OIDC auth cookies).
- **Data dir:** `ImprintData=/home/jimmy/imprint-data` (event store, media, published output),
  shared by both colours. **Widgets:** `ImprintWidgets=/home/jimmy/apps/imprint/widgets`
  (delivery-plane widget JS + manifest, refreshed from the repo's `widgets/` on each deploy).
- **Env file** `~/.config/imprint/imprint.env`: the `Keycloak__*` values above, plus
  `ImprintBaseUrl=https://app.imprint.canine.dev`, `ImprintData`, `ImprintWidgets`.

### Blue/green config

`/etc/bluegreen/imprint.conf` drives the cutover script:

```
EXT_PORT=8102  BLUE_PORT=18102  GREEN_PORT=19102
APP_BASE=/home/jimmy/apps/imprint  HEALTH_PATH=/healthz  SVC=imprint  COLORDIR=app
```

## 3. The reverse-proxy vhosts on dgx1

Additive nginx vhosts (mirroring `remoteclaude.canine.dev`): `app.imprint.canine.dev` on
:443 → `proxy_pass http://192.168.1.10:8102` (WebSocket upgrade, long `proxy_read_timeout`),
and `imprint.canine.dev` serving the static marketing site from a dgx1 web root. Certificates
via `certbot --nginx`. Always `nginx -t` before `systemctl reload nginx` — dgx1 fronts the
entire live estate.

## 4. Continuous deployment — push to main = zero-downtime cutover

A push to `main` auto-deploys via `.github/workflows/deploy.yml`, running on the repo's
**self-hosted runner on wrx1** (`canine-wrx1-imprint`). The job:

1. **restore → build (Release) → test** (unit + integration; the Playwright **E2E** suite is
   excluded — it needs a browser + live editor the deploy host doesn't carry);
2. **publish** `src/Imprint.Editor` into `…/app.new`;
3. **refresh widgets** from the repo's `widgets/` into `…/imprint/widgets`;
4. **cutover** — `sudo /usr/local/bin/bg-cutover.sh imprint …/app.new` copies the build into
   the **idle** colour, health-checks `/healthz` on its port, flips
   `/etc/nginx/bluegreen/imprint.active` (graceful reload), then stops the old colour.

**Safety model:** every step before the cutover leaves the live colour serving, so a broken
build or a failing test is a **safe no-op**. The cutover is **health-gated** — if the new
colour never answers `/healthz` it is not flipped in and the old colour keeps serving
(automatic rollback). Deploys are serialized (`concurrency: deploy-main`), so a second push
queues behind the running cutover instead of racing it. Keycloak and the SQLite event store
are never touched — a code deploy bounces only the idle editor colour (~2s).

Manual deploy / rollback: re-run the workflow (`workflow_dispatch`), or on the box run
`sudo /usr/local/bin/bg-cutover.sh imprint <artifact-dir>` to flip colours directly.

> Editing `.github/workflows/deploy.yml` needs a token with `workflow` scope — push it with a
> normal git push, not a deploy/checkout token.

## 5. Deploy targets (prepared, not yet publishing)

The editor's per-site **deploy environments** (see [multi-site-saas.md](multi-site-saas.md))
point each site at a folder. For this estate:

- **wrx1 marketing** — `watchdog.canine.dev`, `assay.canine.dev`, `cai.canine.dev` are today
  served by the `cms-*` Node containers. Imprint is being prepared to take these over, but
  **publishes nothing yet** and does not touch those containers or their vhosts.
- **canine-ultra1** — a deploy target (Caddy) for other Imprint-maintained sites.

`PublishingOptions.DeployRoot` sandboxes tenant-typed folders; leave it null only while a
single trusted operator manages every site. The go-live switch for each marketing site is a
later, deliberate step: point its environment at the real web root, publish, then repoint the
dgx1/Caddy vhost from the old container to the published folder.
