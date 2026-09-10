// A real browser, a real shadow root, real computed styles.
//
// These islands are CSS and shadow DOM; a string-matching test would assert the source text
// rather than the thing a reader sees. Chromium renders the BUILT bundle from widgets/ — the
// artefact that actually ships — so a source edit that was never rebuilt fails here rather
// than on the live page.
//
// Playwright and its browsers are not vendored in this repo (there is no Node toolchain here at
// all — see build.sh, which borrows an esbuild the same way). Point $PLAYWRIGHT_MODULE at a
// playwright install if the search below misses yours:
//
//   node --test widgets/_src/tests/
//
import { createRequire } from "node:module";
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HERE = path.dirname(fileURLToPath(import.meta.url));
export const WIDGETS_DIR = path.resolve(HERE, "..", "..");

const require = createRequire(import.meta.url);

function resolvePlaywright() {
  const candidates = [
    process.env.PLAYWRIGHT_MODULE,
    "/home/jimmy/kennel-localtest/node_modules/playwright",
    "/home/jimmy/RiderProjects/kennel.canine.dev/node_modules/playwright",
    "playwright",
  ].filter(Boolean);
  for (const c of candidates) {
    try {
      return require(c);
    } catch {
      /* try the next one */
    }
  }
  throw new Error(
    "playwright not found — set $PLAYWRIGHT_MODULE to a playwright install directory"
  );
}

const TYPES = { ".js": "text/javascript", ".svg": "image/svg+xml", ".json": "application/json" };

/** Serve widgets/ over http: an ES module import needs an origin, and file:// is not one. */
export async function serveWidgets() {
  const server = createServer(async (req, res) => {
    const rel = decodeURIComponent(req.url.split("?")[0]).replace(/^\/+/, "");
    const file = path.join(WIDGETS_DIR, rel);
    if (!file.startsWith(WIDGETS_DIR) || !existsSync(file)) {
      res.writeHead(404).end("not found");
      return;
    }
    res.writeHead(200, {
      "content-type": TYPES[path.extname(file)] || "text/plain",
      // setContent leaves the page on about:blank, so every module fetch is cross-origin.
      "access-control-allow-origin": "*",
    });
    res.end(await readFile(file));
  });
  await new Promise((r) => server.listen(0, "127.0.0.1", r));
  const origin = `http://127.0.0.1:${server.address().port}`;
  return { origin, close: () => new Promise((r) => server.close(r)) };
}

/**
 * Render one or more islands on a page of a given width and hand the caller the page.
 *
 * `elements` is a list of { tag, attrs }. The page carries the same 1120px content column the
 * corpus sheet uses, so a width measured here is the width a reader gets.
 */
export async function withIslands(elements, { width = 1280, height = 900, theme = "light" } = {}) {
  const { chromium } = resolvePlaywright();
  const site = await serveWidgets();
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width, height } });

  const markup = elements
    .map(({ tag, attrs }) => {
      const a = Object.entries(attrs || {})
        .map(([k, v]) => `${k}="${String(v).replace(/&/g, "&amp;").replace(/"/g, "&quot;")}"`)
        .join(" ");
      return `<${tag} ${a}></${tag}>`;
    })
    .join("\n");

  const tags = [...new Set(elements.map((e) => e.tag))];
  await page.setContent(
    `<!doctype html><html data-theme="${theme}"><head><style>
       html, body { margin: 0; }
       .column { max-width: 1120px; margin: 0 auto; padding: 24px 16px; }
     </style></head><body><div class="column">${markup}</div>
     ${tags.map((t) => `<script type="module" src="${site.origin}/${t}.js"></script>`).join("")}
     </body></html>`,
    { waitUntil: "load" }
  );
  for (const t of tags) {
    await page.waitForFunction(
      (tag) => document.querySelector(tag)?.shadowRoot?.childElementCount > 0,
      t,
      { timeout: 10000 }
    );
  }

  return {
    page,
    /** The shadow root's markup, whole — including the <style> block. */
    shadowHtml: (tag, index = 0) =>
      page.evaluate(
        ([t, i]) => document.querySelectorAll(t)[i].shadowRoot.innerHTML,
        [tag, index]
      ),
    /**
     * The shadow root's markup WITHOUT its <style> block — the backwards-compatibility subject.
     *
     * The stylesheet is deliberately excluded. A rail is new CSS scoped to new class names, and
     * a snapshot that included the style block would fail for every added rule while saying
     * nothing about whether the no-kicker path still renders what it rendered. What must not
     * move is the ELEMENTS: the same tags, classes, order and text. The legacy path's own
     * geometry (the 46rem cap, the auto margins) is asserted separately as COMPUTED style,
     * which is the claim that matters and which a text snapshot cannot make.
     */
    shadowMarkup: (tag, index = 0) =>
      page.evaluate(
        ([t, i]) =>
          document
            .querySelectorAll(t)[i]
            .shadowRoot.innerHTML.replace(/<style>[\s\S]*?<\/style>/, "")
            .trim(),
        [tag, index]
      ),
    /** One computed style value from inside a shadow root. */
    computed: (tag, selector, prop, index = 0) =>
      page.evaluate(
        ([t, s, p, i]) => {
          const el = document.querySelectorAll(t)[i].shadowRoot.querySelector(s);
          return el ? getComputedStyle(el)[p] : null;
        },
        [tag, selector, prop, index]
      ),
    /** A rendered box from inside a shadow root: what the reader's eye is served. */
    box: (tag, selector, index = 0) =>
      page.evaluate(
        ([t, s, i]) => {
          const el = document.querySelectorAll(t)[i].shadowRoot.querySelector(s);
          if (!el) return null;
          const r = el.getBoundingClientRect();
          return { x: r.x, y: r.y, width: r.width, height: r.height };
        },
        [tag, selector, index]
      ),
    /** Does the PAGE scroll sideways — the one thing these sheets must never do. */
    overflows: () =>
      page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1),
    close: async () => {
      await browser.close();
      await site.close();
    },
  };
}
