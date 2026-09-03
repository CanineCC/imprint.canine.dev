// Mobile nav: one button opens the sheet, and inside it each group is an accordion.
// A checkbox hack would drop keyboard users (labels take no focus), and <details>
// cannot show its content closed on desktop — so this is honest, minimal JS.
//
// aria-expanded is set on the group triggers ONLY below the breakpoint. Above it the
// dropdowns open on hover and focus-within, with no JS involved, so a hard-coded
// "false" would tell a screen reader something the stylesheet contradicts.
(function () {
  var header = document.querySelector(".ip-site-header");
  var burger = header && header.querySelector(".ip-nav-burger");
  if (!header || !burger) return;

  var triggers = Array.prototype.slice.call(header.querySelectorAll(".ip-nav-trigger"));
  var sheet = window.matchMedia("(max-width: 900px)");

  function collapseAll() {
    triggers.forEach(function (t) { t.setAttribute("aria-expanded", "false"); });
  }

  function closeSheet() {
    header.classList.remove("ip-nav-open");
    burger.setAttribute("aria-expanded", "false");
  }

  // Above the breakpoint the attribute must not linger: the panel is hover-driven there,
  // and a stale aria-expanded would outlive the accordion it described.
  function sync() {
    if (sheet.matches) {
      collapseAll();
    } else {
      closeSheet();
      triggers.forEach(function (t) { t.removeAttribute("aria-expanded"); });
    }
  }

  burger.addEventListener("click", function () {
    var open = header.classList.toggle("ip-nav-open");
    burger.setAttribute("aria-expanded", open ? "true" : "false");
    if (open) collapseAll();
  });

  triggers.forEach(function (trigger) {
    trigger.addEventListener("click", function () {
      if (!sheet.matches) return;
      var wasOpen = trigger.getAttribute("aria-expanded") === "true";
      collapseAll(); // one section at a time — the whole point is a short list
      trigger.setAttribute("aria-expanded", wasOpen ? "false" : "true");
    });
  });

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && header.classList.contains("ip-nav-open")) {
      closeSheet();
      burger.focus();
    }
  });

  if (sheet.addEventListener) sheet.addEventListener("change", sync);
  else sheet.addListener(sync); // Safari < 14
  sync();
})();
