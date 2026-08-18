#!/usr/bin/env python3
"""
Find posts whose body was blanked, and put the previous text back — over the authoring API.

Why this exists: PostEditor.razor used to flush its in-memory body on the way out whenever
that body differed from the aggregate's. A reviewer who is not a collaborator was redirected
away BEFORE the body was read, so the component was disposed holding a loaded post and an
empty string -- and wrote the empty string. Opening the review link emptied the post.

Nothing is destroyed by that: the store is append-only, so the previous text is still in the
log. This reads it back through GET /posts/{id}/history and restores it with the ordinary
PUT /posts/{id}/body -- a compensating write through the domain, not a repair of the log.

Read-only unless --apply is passed.

    export IMPRINT_AUTHORING_TOKEN=...
    python3 restore-wiped-post-bodies.py --site <siteId>
    python3 restore-wiped-post-bodies.py --site <siteId> --apply
"""

import argparse
import json
import os
import sys
import urllib.error
import urllib.request

DEFAULT_API = "https://app.imprint.canine.dev"


def call(api, token, path, method="GET", payload=None):
    request = urllib.request.Request(
        f"{api.rstrip('/')}/api/authoring{path}",
        data=None if payload is None else json.dumps(payload).encode(),
        headers={"Content-Type": "application/json", "Authorization": f"Bearer {token}"},
        method=method,
    )
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.loads(response.read().decode() or "null")


def blanked(api, token, site):
    """Posts on the site whose current body is empty but whose history still holds text."""
    out = []
    for summary in call(api, token, f"/sites/{site}/posts"):
        post = call(api, token, f"/posts/{summary['id']}")
        for locale, text in (post.get("body") or {"en": ""}).items():
            if (text or "").strip():
                continue   # still has words

            history = call(api, token, f"/posts/{summary['id']}/history?body=true")["revisions"]
            bodies = [r for r in history
                      if r["change"] == "body" and (r.get("detail") or {}).get("locale") == locale]
            if not bodies or (bodies[-1]["detail"].get("markdown") or "").strip():
                continue   # empty for some other reason than a blanking write

            previous = next((r for r in reversed(bodies[:-1])
                             if (r["detail"].get("markdown") or "").strip()), None)
            if previous is None:
                continue   # never had text -- an empty draft, not a casualty

            out.append({
                "id": summary["id"],
                "title": (post.get("title") or {}).get(locale, ""),
                "locale": locale,
                "wipe": bodies[-1],
                "previous": previous,
            })
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--api", default=DEFAULT_API)
    parser.add_argument("--site", required=True, help="site id whose posts to check")
    parser.add_argument("--apply", action="store_true", help="write the recovered bodies back")
    args = parser.parse_args()

    token = os.environ.get("IMPRINT_AUTHORING_TOKEN")
    if not token:
        sys.exit("IMPRINT_AUTHORING_TOKEN is not set.")

    try:
        casualties = blanked(args.api, token, args.site)
    except urllib.error.HTTPError as error:
        sys.exit(f"{error.code} from the authoring API: {error.read().decode()[:300]}")

    if not casualties:
        print("No post on this site is blank with recoverable text behind it.")
        return

    print(f"{len(casualties)} post(s) blank now, with earlier text still in the log:\n")
    for entry in casualties:
        wipe, previous = entry["wipe"], entry["previous"]
        first = next((l for l in previous["detail"]["markdown"].splitlines() if l.strip()), "")
        print(f"  {entry['id']}  [{entry['locale']}]  {entry['title']}")
        print(f"      blanked at v{wipe['version']} by {wipe['actor']!r} {wipe['at']}")
        print(f"      recoverable: {previous['detail']['length']:,} chars from v{previous['version']}"
              f" by {previous['actor']!r} {previous['at']}")
        print(f"      starts: {first[:90]}")
        print()

    if not args.apply:
        print("Dry run. Re-run with --apply to put the text back.")
        return

    failures = 0
    for entry in casualties:
        markdown = entry["previous"]["detail"]["markdown"]
        try:
            result = call(args.api, token, f"/posts/{entry['id']}/body", "PUT",
                          {"Locale": entry["locale"], "Markdown": markdown})
            ok = result.get("length") == len(markdown)
            print(f"  {'restored' if ok else 'MISMATCH'} {entry['id']} -> {result}")
            failures += 0 if ok else 1
        except urllib.error.HTTPError as error:
            failures += 1
            print(f"  FAILED  {entry['id']} -> {error.code} {error.read().decode()[:200]}")

    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
