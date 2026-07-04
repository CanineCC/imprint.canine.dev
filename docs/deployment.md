# Deploying Imprint (imprint.canine.dev)

This is the runbook for the first real deployment of the Imprint editor onto the CanineCC
estate. It reflects the actual topology of that estate rather than a generic cloud setup.

## Topology

```
            Internet
               │  imprint.canine.dev  ─┐
               ▼                        │  (DNS → the estate's public IP)
        ┌──────────────┐               │
        │  canine-dgx1 │  nginx + Let's Encrypt  ── the SSL reverse proxy for the whole estate
        │  (192.168.1.159)             │  (canine.dev, watchdog.canine.dev, auth.canine.dev, …)
        └──────┬───────┘               │
               │  proxy_pass http://192.168.1.10:8102   (LAN, http)
               ▼
        ┌──────────────┐
        │  canine-wrx1 │  the Imprint EDITOR runs here as a systemd service (Blazor Server)
        │  (192.168.1.10)              │  + Keycloak (wrx1-keycloak → auth.canine.dev)
        └──────┬───────┘
               │  publishes static sites into web roots on…
      ┌────────┴─────────┐
      ▼                  ▼
  wrx1 web roots     canine-ultra1 (Caddy)         ← per-site deploy environments
  (marketing:        (other sites Imprint          (see docs/multi-site-saas.md)
   watchdog/assay/     maintains)
   cai.canine.dev)
```

Three moving parts: the **editor** (authoring app, on wrx1), the **reverse proxy** (TLS +
routing, on dgx1), and the **deploy targets** (web roots the editor publishes into, on wrx1
and ultra1). The delivery plane is plain static files; only the editor needs a login.

## 1. Authentication — Keycloak (in-app OIDC)

The editor has **no built-in login by design** — it consumes an identity and leaves auth to
the platform. On this estate the platform is **Keycloak** (`auth.canine.dev`), and the editor
speaks OIDC to it directly, exactly like `remoteclaude.canine.dev` does. This is configured
by the `Keycloak` section (usually via `Keycloak__*` environment variables):

| Setting | Meaning | Example |
| --- | --- | --- |
| `Keycloak__Authority` | The OIDC issuer. **Auth is OFF until this is set.** | `https://auth.canine.dev/realms/master` |
| `Keycloak__ClientId` | The confidential client registered for the editor | `imprint` |
| `Keycloak__ClientSecret` | That client's secret | *(from Keycloak)* |
| `Keycloak__RequireHttps` | Validate metadata/tokens over HTTPS | `true` |
| `Imprint__AllowUnauthenticated` | Escape valve: run open in Production **only** on a trusted network | `false` |

**Safety property:** in `Production`, the editor **refuses to start** with no Keycloak
configured unless `Imprint__AllowUnauthenticated=true` is set explicitly. An unauthenticated
editor is never exposed by accident. In `Development` (and in the test suite) auth is simply
off and the OS user is the actor — the editor behaves exactly as it always has.

The signed-in user's email is recorded as the **actor** on every event, so sites are owned by
their creator (`SiteOverview.OwnedBy`) — the one wire the multi-tenant story needed.

### Minting the `imprint` Keycloak client (one-time, operator action)

Creating an OIDC client is a privileged identity-service operation. Do it in the Keycloak
admin console (`https://auth.canine.dev`, realm **master**):

1. **Clients → Create client** → Client ID `imprint`, type *OpenID Connect*.
2. **Client authentication: On** (confidential). **Standard flow: On**; Direct access &
   service accounts **Off**.
3. **Valid redirect URIs:** `https://imprint.canine.dev/signin-oidc`
   **Valid post-logout redirect URIs:** `https://imprint.canine.dev/`
   **Web origins:** `https://imprint.canine.dev`
4. Advanced → **Proof Key for Code Exchange (PKCE)**: `S256`.
5. **Credentials** tab → copy the **Client secret** → put it in the editor's env file
   (below). Never commit it.

The editor disables Pushed Authorization Requests (PAR) to match the rest of the estate, so
no PAR-specific client settings are required.

## 2. The editor service on wrx1

Deployed the same way as the estate's other .NET apps (`/home/jimmy/apps/<name>/app`, a
`systemd` unit, an env file under `~/.config/<name>/`):

- **Publish:** `dotnet publish src/Imprint.Editor -c Release -o /home/jimmy/apps/imprint/app`
  (wrx1 has the .NET 10 SDK). Keep the previous build as `app.prev` / `app.rollback`.
- **Data dir:** `ImprintData=/home/jimmy/imprint-data` (event store, media, published output).
- **Bind:** `ASPNETCORE_URLS=http://0.0.0.0:8102`, reachable only from the LAN (ufw), so the
  reverse proxy on dgx1 is the sole ingress.
- **Env file** `~/.config/imprint/imprint.env`:
  ```
  Keycloak__Authority=https://auth.canine.dev/realms/master
  Keycloak__ClientId=imprint
  Keycloak__ClientSecret=<from Keycloak>
  ImprintBaseUrl=https://imprint.canine.dev
  ```
- **Unit** `imprint-editor.service` (mirrors `remoteclaude-web.service`): `User=jimmy`,
  `ASPNETCORE_ENVIRONMENT=Production`, `Restart=always`, `EnvironmentFile=…/imprint.env`.

## 3. The reverse-proxy vhost on dgx1

An **additive** nginx vhost, mirroring `remoteclaude.canine.dev` (port 80 acme-challenge +
301→https; port 443 TLS → `proxy_pass http://192.168.1.10:8102` with WebSocket upgrade and a
long `proxy_read_timeout` for Blazor circuits, plus a friendly `@down` page). Certificate via
`certbot --nginx -d imprint.canine.dev`. Always `nginx -t` before `systemctl reload nginx` —
dgx1 fronts the entire live estate.

## 4. Deploy targets (prepared, not yet publishing)

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
