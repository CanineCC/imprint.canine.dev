// Send a first-time visitor to their own language when we publish it, and never argue with someone who
// has chosen. A click on the switcher is remembered forever; a remembered choice is never overridden.
//
// It reads the hreflang alternates the page already emits, so it is a no-op on a single-locale site: with
// nothing to switch to there are no alternates, and the loop finds nothing. No configuration, no per-site
// flag, and nothing to keep in sync with the locale list.
//
// The alternate href points at THIS page in that locale, so the path survives the redirect - a visitor
// deep in the site does not get bounced to the front page.
(function () {
  var KEY = "ip-lang", doc = document, root = doc.documentElement;
  var base = function (tag) { return String(tag || "").toLowerCase().split("-")[0]; };
  try {
    // Register before deciding, so the very click that sets the preference is captured on a page that is
    // about to redirect away.
    doc.addEventListener("click", function (e) {
      var a = e.target && e.target.closest && e.target.closest(".ip-lang a[hreflang]");
      if (a) { try { localStorage.setItem(KEY, a.getAttribute("hreflang")); } catch (ignored) {} }
    }, true);

    if (localStorage.getItem(KEY)) { return; }

    var alts = {};
    doc.querySelectorAll('link[rel="alternate"][hreflang]').forEach(function (link) {
      var tag = base(link.getAttribute("hreflang"));
      if (tag && tag !== "x") { alts[tag] = link.href; }
    });

    // Walk the browser's list in preference order. Whichever comes first wins: if their top choice is
    // the language already on screen, stay; if it is one we publish, go there.
    var wanted = navigator.languages || [navigator.language];
    for (var i = 0; i < wanted.length; i++) {
      var tag = base(wanted[i]);
      if (tag === base(root.lang)) { return; }
      if (alts[tag]) { location.replace(alts[tag]); return; }
    }
  } catch (ignored) {}
})();
