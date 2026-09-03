// Mobile nav toggle: one button, one class, aria-expanded kept true to the state.
// A checkbox hack would drop keyboard users (labels take no focus), and <details>
// cannot show its content closed on desktop — so this is honest, minimal JS.
(function () {
  var burger = document.querySelector(".ip-nav-burger");
  var header = document.querySelector(".ip-site-header");
  if (!burger || !header) return;
  burger.addEventListener("click", function () {
    var open = header.classList.toggle("ip-nav-open");
    burger.setAttribute("aria-expanded", open ? "true" : "false");
  });
})();
