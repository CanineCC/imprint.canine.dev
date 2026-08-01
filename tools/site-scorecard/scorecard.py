#!/usr/bin/env python3
"""Measure how findable a published imprint site is, the same way Watchdog measures code:
one reproducible number, computed from evidence anyone can re-fetch.

The premise: "to be read" is judged by a human, "to be found" is judged by a machine — so
judge it with a machine. Every check below is a fact about bytes served on the public web,
not an opinion about prose. Run it against the live sites, diff two runs, and the argument
about whether an optimisation run worked stops being a matter of taste.

    python3 scorecard.py                         # all sites, human summary
    python3 scorecard.py --site watchdog         # one site
    python3 scorecard.py --json out.json         # machine-readable, stable ordering
    python3 scorecard.py --min-score 93          # exit 1 below the bar (CI gate)

Stdlib only, on purpose: this has to run on a build agent with nothing provisioned.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, field
from html import unescape
from urllib.parse import urljoin, urlsplit

UA = "imprint-site-scorecard/1.0 (+https://canine.dev)"
TIMEOUT = 20

SITES = {
    "watchdog": "https://watchdog.canine.dev/",
    "assay": "https://assay.canine.dev/",
    "cai": "https://cai.canine.dev/",
    "www": "https://canine.dev/",
}

# A site whose pages are generated from one template is audited as a template class: every
# page shares the template's virtues and defects, so a deterministic sample measures the
# class without 2,700 fetches. The sample is evenly spaced, never random — two runs of the
# same corpus must audit the same pages or the score is not comparable.
TEMPLATE_CLASSES = {"cai": ("/surveys/github/", 25)}

GRADES = [
    (97, "A+"), (93, "A"), (90, "A-"), (87, "B+"), (83, "B"), (80, "B-"),
    (77, "C+"), (73, "C"), (70, "C-"), (67, "D+"), (63, "D"), (60, "D-"),
]


def grade(score: float) -> str:
    for floor, letter in GRADES:
        if score >= floor:
            return letter
    return "F"


# ---------------------------------------------------------------------------- fetching


@dataclass
class Response:
    url: str
    status: int
    body: str = ""
    hops: int = 0
    final_url: str = ""
    error: str = ""


class Fetcher:
    """One fetch per unique URL, ever. Redirects are followed by hand so a hop can be
    counted — an internal link that 301s is a real defect worth its own check, and a
    library that follows redirects silently would hide it."""

    def __init__(self, workers: int = 8):
        self._cache: dict[tuple[str, bool], Response] = {}
        self._workers = workers

    def get(self, url: str, body: bool = True) -> Response:
        key = (url, body)
        if key in self._cache:
            return self._cache[key]
        result = self._fetch(url, body)
        self._cache[key] = result
        return result

    def get_many(self, urls: list[str], body: bool = True) -> dict[str, Response]:
        todo = [u for u in dict.fromkeys(urls) if (u, body) not in self._cache]
        if todo:
            with ThreadPoolExecutor(max_workers=self._workers) as pool:
                for url, result in zip(todo, pool.map(lambda u: self._fetch(u, body), todo)):
                    self._cache[(url, body)] = result
        return {u: self._cache[(u, body)] for u in urls}

    def _fetch(self, url: str, body: bool) -> Response:
        hops, current = 0, url
        while hops <= 5:
            request = urllib.request.Request(
                current, method="GET" if body else "HEAD", headers={"User-Agent": UA}
            )
            try:
                opener = urllib.request.build_opener(_NoRedirect)
                with opener.open(request, timeout=TIMEOUT) as response:
                    text = ""
                    if body:
                        raw = response.read()
                        text = raw.decode(response.headers.get_content_charset() or "utf-8", "replace")
                    return Response(url, response.status, text, hops, current)
            except urllib.error.HTTPError as err:
                if err.status in (301, 302, 307, 308) and err.headers.get("Location"):
                    current = urljoin(current, err.headers["Location"])
                    hops += 1
                    continue
                # Not every server answers HEAD honestly — unfold.canine.dev serves 404 to
                # HEAD and 200 to GET. A link checker that trusts HEAD reports phantom
                # breakage, so any HEAD failure is re-tried as a GET before we accuse it.
                if not body and err.status >= 400:
                    return self._fetch(url, body=True)
                return Response(url, err.status, "", hops, current)
            except Exception as err:  # noqa: BLE001 — a DNS/TLS failure is a finding, not a crash
                return Response(url, 0, "", hops, current, error=type(err).__name__)
        return Response(url, 0, "", hops, current, error="TooManyRedirects")


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *_args, **_kwargs):
        return None


# ---------------------------------------------------------------------------- parsing


def strip_noise(html: str) -> str:
    return re.sub(r"<(script|style)\b.*?</\1>", " ", html, flags=re.S | re.I)


def one(pattern: str, html: str) -> str | None:
    match = re.search(pattern, html, re.S | re.I)
    return unescape(match.group(1)).strip() if match else None


@dataclass
class Page:
    url: str
    status: int
    html: str
    title: str | None = None
    description: str | None = None
    canonical: str | None = None
    h1s: list[str] = field(default_factory=list)
    words: int = 0
    og: dict[str, str] = field(default_factory=dict)
    twitter: dict[str, str] = field(default_factory=dict)
    jsonld: list[dict] = field(default_factory=list)
    jsonld_broken: int = 0
    hreflang: list[str] = field(default_factory=list)
    links: list[str] = field(default_factory=list)

    @classmethod
    def parse(cls, url: str, response: Response) -> "Page":
        html = response.body
        page = cls(url=url, status=response.status, html=html)
        if not html:
            return page

        page.title = one(r"<title[^>]*>(.*?)</title>", html)
        page.description = one(r'<meta\s+name="description"\s+content="(.*?)"', html)
        page.canonical = one(r'<link\s+rel="canonical"\s+href="(.*?)"', html)
        page.hreflang = [m.lower() for m in re.findall(r'hreflang="([^"]+)"', html, re.I)]

        for prop, content in re.findall(
            r'<meta\s+(?:property|name)="(og:[^"]+)"[^>]*content="([^"]*)"', html, re.I
        ):
            page.og[prop.lower()] = unescape(content)
        for prop, content in re.findall(
            r'<meta\s+(?:name|property)="(twitter:[^"]+)"[^>]*content="([^"]*)"', html, re.I
        ):
            page.twitter[prop.lower()] = unescape(content)

        for block in re.findall(
            r'<script[^>]+type="application/ld\+json"[^>]*>(.*?)</script>', html, re.S | re.I
        ):
            try:
                parsed = json.loads(block)
            except json.JSONDecodeError:
                page.jsonld_broken += 1  # present but unparseable is worse than absent
                continue
            page.jsonld.extend(parsed if isinstance(parsed, list) else [parsed])

        visible = strip_noise(html)
        body = visible.split("<body", 1)[-1]
        page.h1s = [
            re.sub(r"\s+", " ", re.sub(r"<[^>]*>", "", m)).strip()
            for m in re.findall(r"<h1[^>]*>(.*?)</h1>", body, re.S | re.I)
        ]
        page.words = len(re.sub(r"\s+", " ", re.sub(r"<[^>]*>", " ", body)).split())

        for href in re.findall(r'<a[^>]+href="([^"#?][^"]*)"', body, re.I):
            absolute = urljoin(url, href)
            if urlsplit(absolute).scheme in ("http", "https"):
                page.links.append(absolute.split("#")[0])
        return page

    def jsonld_types(self) -> list[str]:
        found: list[str] = []
        for block in self.jsonld:
            kind = block.get("@type")
            found.extend(kind if isinstance(kind, list) else [kind] if kind else [])
            for nested in block.get("@graph", []) or []:
                nested_kind = nested.get("@type") if isinstance(nested, dict) else None
                found.extend(
                    nested_kind if isinstance(nested_kind, list)
                    else [nested_kind] if nested_kind else []
                )
        return sorted(set(found))


# ---------------------------------------------------------------------------- checks

# Weights encode consequence, not effort. A missing <title> costs more than a thin page
# because a machine reading the page can recover from thin prose and cannot recover from
# no name. Categories are reported separately so a run says *what kind* of thing is wrong.
GENERIC_TITLES = {"home", "page", "untitled", "index", "the registry"}


@dataclass
class Check:
    id: str
    category: str
    weight: int
    ok: bool
    detail: str = ""


def check_page(page: Page, ctx: "SiteContext") -> list[Check]:
    checks: list[Check] = []

    def add(cid: str, category: str, weight: int, ok: bool, detail: str = "") -> None:
        checks.append(Check(cid, category, weight, ok, detail))

    # --- identity: what this page says it is -------------------------------------
    title = page.title or ""
    stem = title.split("·")[0].strip()  # the site suffix is chrome, not the name
    add("title.present", "identity", 10, bool(title), "" if title else "no <title>")
    add("title.not-generic", "identity", 8, bool(stem) and stem.lower() not in GENERIC_TITLES,
        f"generic title: {title!r}" if stem.lower() in GENERIC_TITLES else "")
    add("title.length", "identity", 3, 15 <= len(title) <= 65,
        f"{len(title)} chars (want 15-65)" if title and not 15 <= len(title) <= 65 else "")
    add("title.unique", "identity", 5, ctx.title_counts.get(title, 0) <= 1,
        f"shared with {ctx.title_counts.get(title, 1) - 1} other page(s)" if title else "")
    add("title.no-doubled-suffix", "identity", 2,
        not re.search(r"(·\s*[\w ]+)\s*·\s*\1?$", title or "") and
        not (title.count("·") > 1 and title.split("·")[-1].strip() == title.split("·")[-2].strip()),
        f"suffix repeated: {title!r}" if title.count("·") > 1 and
        title.split("·")[-1].strip() == title.split("·")[-2].strip() else "")

    description = page.description or ""
    add("description.present", "identity", 8, bool(description))
    add("description.length", "identity", 3, 70 <= len(description) <= 165,
        f"{len(description)} chars (want 70-165)" if description and not 70 <= len(description) <= 165 else "")
    add("description.unique", "identity", 3, ctx.description_counts.get(description, 0) <= 1,
        "duplicated" if description and ctx.description_counts.get(description, 0) > 1 else "")

    add("canonical.present", "identity", 5, bool(page.canonical))
    add("canonical.self", "identity", 3,
        not page.canonical or _same_url(page.canonical, page.url),
        f"points elsewhere: {page.canonical}" if page.canonical and not _same_url(page.canonical, page.url) else "")

    # --- machine layer: what a crawler or a model can extract without prose -------
    add("og.title", "machine", 6, "og:title" in page.og)
    add("og.description", "machine", 4, "og:description" in page.og)
    add("og.type", "machine", 2, "og:type" in page.og)
    add("og.url", "machine", 2, "og:url" in page.og)
    add("og.image", "machine", 5, "og:image" in page.og)
    # Presence is not enough. A share card is fetched by link scrapers, not browsers, and
    # several of them — LinkedIn most consequentially — skip a WebP og:image and render a
    # no-image card. A page can pass "og:image present" and still share as a bare link, so
    # the format is its own check.
    image_url = page.og.get("og:image", "")
    add("og.image-scrapable", "machine", 4,
        not image_url or not image_url.split("?")[0].lower().endswith((".webp", ".avif")),
        f"scrapers may skip this format: {image_url.rsplit('/', 1)[-1]}" if image_url else "")
    add("twitter.card", "machine", 3, "twitter:card" in page.twitter)
    add("jsonld.present", "machine", 12, bool(page.jsonld),
        "no structured data" if not page.jsonld else "")
    add("jsonld.parses", "machine", 4, page.jsonld_broken == 0,
        f"{page.jsonld_broken} unparseable block(s)" if page.jsonld_broken else "")
    add("jsonld.typed", "machine", 4, bool(page.jsonld_types()),
        "structured data has no @type" if page.jsonld and not page.jsonld_types() else "")

    # --- structure: how the page is built ----------------------------------------
    add("h1.exactly-one", "structure", 6, len(page.h1s) == 1, f"{len(page.h1s)} h1 tags")
    add("h1.non-empty", "structure", 2, bool(page.h1s and page.h1s[0].strip()))
    add("content.depth", "structure", 4, page.words >= 300, f"{page.words} words (want >=300)")
    add("links.outbound-internal", "structure", 3, ctx.internal_link_count(page) >= 3,
        f"{ctx.internal_link_count(page)} internal links")

    if ctx.locales:
        add("hreflang.present", "structure", 3, bool(page.hreflang))

    # --- hygiene: links that cost a crawl budget or a citation -------------------
    broken = ctx.broken_from(page)
    redirecting = ctx.redirecting_from(page)
    add("links.no-broken", "hygiene", 10, not broken,
        f"{len(broken)} broken: {', '.join(sorted(broken)[:3])}" if broken else "")
    add("links.no-redirect-hops", "hygiene", 4, not redirecting,
        f"{len(redirecting)} redirecting: {', '.join(sorted(redirecting)[:3])}" if redirecting else "")

    return checks


def _same_url(a: str, b: str) -> bool:
    def norm(u: str) -> str:
        parts = urlsplit(u)
        return f"{parts.netloc}{parts.path.rstrip('/')}".lower()
    return norm(a) == norm(b)


def check_site(ctx: "SiteContext") -> list[Check]:
    checks: list[Check] = []

    def add(cid: str, weight: int, ok: bool, detail: str = "") -> None:
        checks.append(Check(cid, "site", weight, ok, detail))

    add("robots.present", 6, ctx.robots is not None)
    add("robots.declares-sitemap", 6, bool(ctx.robots and "sitemap:" in ctx.robots.lower()))
    add("sitemap.present", 8, bool(ctx.sitemap_urls))
    add("sitemap.all-resolve", 8, not ctx.sitemap_broken,
        f"{len(ctx.sitemap_broken)} non-200: {', '.join(sorted(ctx.sitemap_broken)[:3])}" if ctx.sitemap_broken else "")
    add("sitemap.no-orphans", 6, not ctx.orphans,
        f"{len(ctx.orphans)} linked but unlisted: {', '.join(sorted(ctx.orphans)[:3])}" if ctx.orphans else "")

    # llms.txt is the one file written for machines to read whole. A dead link in it is a
    # model following our own map into a wall; an oversized one is a file no model finishes.
    add("llms.present", 10, ctx.llms is not None)
    add("llms.size-sane", 6, ctx.llms is None or len(ctx.llms) <= 50_000,
        f"{len(ctx.llms):,} bytes (want <=50,000)" if ctx.llms and len(ctx.llms) > 50_000 else "")
    add("llms.links-resolve", 10, not ctx.llms_broken,
        f"{len(ctx.llms_broken)} dead: {', '.join(sorted(ctx.llms_broken)[:3])}" if ctx.llms_broken else "")
    return checks


# ---------------------------------------------------------------------------- context


@dataclass
class SiteContext:
    slug: str
    root: str
    pages: list[Page] = field(default_factory=list)
    robots: str | None = None
    llms: str | None = None
    sitemap_urls: list[str] = field(default_factory=list)
    sitemap_broken: set[str] = field(default_factory=set)
    llms_broken: set[str] = field(default_factory=set)
    orphans: set[str] = field(default_factory=set)
    locales: list[str] = field(default_factory=list)
    link_status: dict[str, Response] = field(default_factory=dict)
    title_counts: dict[str, int] = field(default_factory=dict)
    description_counts: dict[str, int] = field(default_factory=dict)
    sampled_of: int = 0

    def host(self) -> str:
        return urlsplit(self.root).netloc

    def _internal(self, page: Page) -> list[str]:
        return [u for u in page.links if urlsplit(u).netloc.endswith("canine.dev")]

    def internal_link_count(self, page: Page) -> int:
        return len(set(self._internal(page)))

    def broken_from(self, page: Page) -> set[str]:
        return {
            u for u in set(self._internal(page))
            if (r := self.link_status.get(u)) and (r.status >= 400 or r.status == 0)
        }

    def redirecting_from(self, page: Page) -> set[str]:
        return {
            u for u in set(self._internal(page))
            if (r := self.link_status.get(u)) and r.hops > 0 and r.status < 400
        }


def parse_sitemap(xml: str) -> list[str]:
    return re.findall(r"<loc>\s*([^<\s]+)\s*</loc>", xml or "")


def audit_site(slug: str, root: str, fetcher: Fetcher, full: bool) -> SiteContext:
    ctx = SiteContext(slug=slug, root=root)

    robots = fetcher.get(urljoin(root, "/robots.txt"))
    ctx.robots = robots.body if robots.status == 200 else None
    llms = fetcher.get(urljoin(root, "/llms.txt"))
    ctx.llms = llms.body if llms.status == 200 else None

    sitemap = fetcher.get(urljoin(root, "/sitemap.xml"))
    all_urls = parse_sitemap(sitemap.body) if sitemap.status == 200 else []
    ctx.sitemap_urls = all_urls

    # Sample the template class rather than fetch it whole; audit everything else.
    to_audit = list(all_urls)
    if not full and slug in TEMPLATE_CLASSES:
        marker, sample_size = TEMPLATE_CLASSES[slug]
        templated = [u for u in all_urls if marker in u]
        rest = [u for u in all_urls if marker not in u]
        if len(templated) > sample_size:
            step = len(templated) / sample_size
            sampled = [templated[int(i * step)] for i in range(sample_size)]
            ctx.sampled_of = len(templated)
            to_audit = rest + sampled

    responses = fetcher.get_many(to_audit)
    ctx.sitemap_broken = {u for u, r in responses.items() if r.status != 200}
    ctx.pages = [Page.parse(u, r) for u, r in responses.items() if r.status == 200]

    for page in ctx.pages:
        if page.title:
            ctx.title_counts[page.title] = ctx.title_counts.get(page.title, 0) + 1
        if page.description:
            ctx.description_counts[page.description] = ctx.description_counts.get(page.description, 0) + 1
        for tag in page.hreflang:
            if tag not in ctx.locales and tag != "x-default":
                ctx.locales.append(tag)

    # Every distinct internal link target, checked once with HEAD.
    targets = sorted({
        u for page in ctx.pages for u in page.links
        if urlsplit(u).netloc.endswith("canine.dev")
    })
    ctx.link_status = fetcher.get_many(targets, body=False)

    # A page linked from the site but absent from the sitemap is a page we publish and
    # do not admit to publishing.
    # An orphan is a page we publish and do not admit to publishing. Judge that by where a
    # link *lands*, not where it points: /dpa redirects to the listed /page-dpa/, so it is
    # a redirect defect (already counted in hygiene) and not a second, invented one.
    listed = {_same_url_key(u) for u in all_urls}
    ctx.orphans = {
        u for u, r in ctx.link_status.items()
        if urlsplit(u).netloc == ctx.host() and r.status == 200
        and _same_url_key(r.final_url or u) not in listed
    }

    if ctx.llms:
        # Prose wraps URLs in punctuation. Trailing sentence marks are not part of the
        # address, and treating them as one manufactures dead links that do not exist.
        cited = sorted({
            u.rstrip(".,;:!?)]}\"'")
            for u in re.findall(r"https://[\w.-]+\.canine\.dev[^\s)\]\"'>]*", ctx.llms)
        })
        statuses = fetcher.get_many(cited, body=False)
        ctx.llms_broken = {u for u, r in statuses.items() if r.status >= 400 or r.status == 0}

    return ctx


def _same_url_key(url: str) -> str:
    parts = urlsplit(url)
    return f"{parts.netloc}{parts.path.rstrip('/')}".lower()


# ---------------------------------------------------------------------------- scoring


def score_checks(checks: list[Check]) -> tuple[float, dict[str, float]]:
    total = sum(c.weight for c in checks)
    earned = sum(c.weight for c in checks if c.ok)
    by_category: dict[str, float] = {}
    for category in sorted({c.category for c in checks}):
        subset = [c for c in checks if c.category == category]
        possible = sum(c.weight for c in subset)
        by_category[category] = round(
            100.0 * sum(c.weight for c in subset if c.ok) / possible, 1
        ) if possible else 100.0
    return (round(100.0 * earned / total, 1) if total else 100.0), by_category


def build_report(contexts: list[SiteContext]) -> dict:
    sites: list[dict] = []
    for ctx in contexts:
        page_checks: list[Check] = []
        page_rows: list[dict] = []
        for page in sorted(ctx.pages, key=lambda p: p.url):
            checks = check_page(page, ctx)
            page_checks.extend(checks)
            page_score, _ = score_checks(checks)
            page_rows.append({
                "url": page.url,
                "score": page_score,
                "grade": grade(page_score),
                "title": page.title,
                "words": page.words,
                "jsonld_types": page.jsonld_types(),
                "failed": [
                    {"id": c.id, "detail": c.detail}
                    for c in checks if not c.ok
                ],
            })

        site_level = check_site(ctx)
        combined = page_checks + site_level
        site_score, by_category = score_checks(combined)

        failures: dict[str, int] = defaultdict(int)
        for check in combined:
            if not check.ok:
                failures[check.id] += 1

        sites.append({
            "site": ctx.slug,
            "root": ctx.root,
            "score": site_score,
            "grade": grade(site_score),
            "by_category": by_category,
            "pages_audited": len(ctx.pages),
            "pages_in_sitemap": len(ctx.sitemap_urls),
            "sampled_from_template_class": ctx.sampled_of,
            "locales": sorted(ctx.locales),
            "top_failures": sorted(
                ({"id": k, "pages": v} for k, v in failures.items()),
                key=lambda r: (-r["pages"], r["id"]),
            ),
            "site_checks": [
                {"id": c.id, "ok": c.ok, "detail": c.detail} for c in site_level
            ],
            "pages": page_rows,
        })

    overall = round(sum(s["score"] for s in sites) / len(sites), 1) if sites else 0.0
    return {
        "schema": "imprint.site-scorecard/1",
        "overall": {"score": overall, "grade": grade(overall)},
        "sites": sites,
    }


# ---------------------------------------------------------------------------- output


def print_summary(report: dict) -> None:
    overall = report["overall"]
    print()
    print(f"  OVERALL  {overall['score']:>5}  {overall['grade']}")
    print("  " + "-" * 68)
    for site in report["sites"]:
        categories = "  ".join(f"{k}:{v}" for k, v in sorted(site["by_category"].items()))
        note = (f"  (sampled {site['pages_audited']} of {site['pages_in_sitemap']})"
                if site["sampled_from_template_class"] else "")
        print(f"  {site['site']:<10} {site['score']:>5}  {site['grade']:<3} {categories}{note}")

    print()
    print("  Biggest levers (weight lost x pages affected):")
    print("  " + "-" * 68)
    for site in report["sites"]:
        for failure in site["top_failures"][:6]:
            print(f"  {site['site']:<10} {failure['id']:<28} {failure['pages']:>4} page(s)")
        print()

    for site in report["sites"]:
        broken = [c for c in site["site_checks"] if not c["ok"]]
        if broken:
            print(f"  {site['site']} — site-level failures:")
            for check in broken:
                print(f"      {check['id']:<26} {check['detail']}")
            print()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--site", action="append", choices=sorted(SITES), help="limit to one site (repeatable)")
    parser.add_argument("--json", help="write the full report here")
    parser.add_argument("--min-score", type=float, help="exit 1 if the overall score is below this")
    parser.add_argument("--full", action="store_true", help="audit template classes in full, not sampled")
    parser.add_argument("--workers", type=int, default=8)
    args = parser.parse_args()

    chosen = args.site or sorted(SITES)
    fetcher = Fetcher(workers=args.workers)

    contexts = []
    for slug in chosen:
        print(f"  auditing {slug} ...", file=sys.stderr)
        contexts.append(audit_site(slug, SITES[slug], fetcher, args.full))

    report = build_report(contexts)

    if args.json:
        with open(args.json, "w", encoding="utf-8") as handle:
            json.dump(report, handle, indent=2, sort_keys=False)
            handle.write("\n")
        print(f"  wrote {args.json}", file=sys.stderr)

    print_summary(report)

    if args.min_score is not None and report["overall"]["score"] < args.min_score:
        print(f"  FAIL: {report['overall']['score']} < {args.min_score}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
