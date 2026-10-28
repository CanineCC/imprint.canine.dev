# LinkedIn posting: facts for P0 (gatekeeping) and P8 (build), 2026-10-27

Source: Microsoft Learn, "Posts API" (Community Management, version li-lms-2026-10),
https://learn.microsoft.com/en-us/linkedin/marketing/integrations/community-management/shares/posts-api

- **Create:** `POST https://api.linkedin.com/rest/posts`, headers `Authorization: Bearer <token>`,
  `Linkedin-Version: YYYYMM` (versions are sunset about a year after release: 202511 sunsets 16 Nov 2026, so the
  version is configuration, not a constant), `X-Restli-Protocol-Version: 2.0.0`, JSON body.
- **Body for our case (an article link):** `author` (`urn:li:organization:{id}`), `commentary` (the post text, in
  LinkedIn's "little" text format: `@`, `#`, `{`, `}`, `[`, `]`, `(`, `)`, `<`, `>`, `*`, `_`, `~`, `|`, `\` need
  escaping), `visibility: PUBLIC`, `distribution: {feedDistribution: MAIN_FEED, targetEntities: [],
  thirdPartyDistributionChannels: []}`, `lifecycleState: PUBLISHED`, `isReshareDisabledByAuthor: false`,
  `content.article: {source: <url>, title, description, thumbnail: urn:li:image:…}`.
- **No URL scraping:** the API does not fetch the link's preview; title, description and thumbnail must be sent.
  The thumbnail is an image uploaded first through the Images API (initializeUpload, then PUT the bytes) to get
  an `urn:li:image:…`. Imprint already has each article's social image (og:image) to upload.
- **Result:** `201`, the post id in the `x-restli-id` response header (`urn:li:share:…` or `urn:li:ugcPost:…`).
  `DELETE /rest/posts/{urn}` removes one (idempotent).
- **Errors to handle:** 401 (token expired), 403 ACCESS_DENIED (scope or page role missing), 422, 429 (rate
  limit: retry later), 409/500/503 (retry). There is no idempotency key: a retry after a timeout can double-post,
  so the outcome must be recorded before any retry and a retry must first look for the post
  (`GET /rest/posts?author=…&q=author`, newest first) when the earlier attempt's result is unknown.
- **Scope:** `w_organization_social`, restricted to organizations where the authenticated member has the page
  role ADMINISTRATOR, CONTENT_ADMIN or DIRECT_SPONSORED_CONTENT_POSTER. Reading back needs `r_organization_social`.
- **Product pages:** a showcase page is an "organization brand" and is addressed as `urn:li:organization:{id}`, so
  "from Canine or from the product" is a choice of organization URN, with the member holding a role on each.
- **What the owner must set up (the deferred question):** a LinkedIn developer app associated with the Canine
  company page, the Community Management API product granted to it (LinkedIn reviews this; a verified company is
  required), a member with a page role on Canine and on each product page to authorize the app once (3-legged
  OAuth), and the organization ids of the Canine page and each product page. Access tokens expire and are refreshed with a
  refresh token (the lifetimes, commonly cited as 60 days and a year, are NOT verified here: check LinkedIn's OAuth
  page before relying on them); the refresh token is a secret and never goes into the event stream.
