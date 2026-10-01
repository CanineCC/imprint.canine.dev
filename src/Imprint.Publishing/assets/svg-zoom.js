// Wide diagrams on a phone: a diagram drawn wider than the screen shrinks to fit the
// column, and its labels shrink with it. Each such diagram gets a "Tap to enlarge" button
// that opens it full-screen at its drawn size, where it can be panned and pinch-zoomed.
// Without this script the diagram simply stays shrunk; nothing else depends on it.
//
// The full-screen view MOVES the figure (both theme renditions) into a <dialog> and puts
// it back on close. A copy would duplicate the ids its markers and gradients point at, and
// the moved pair keeps switching light/dark through the same CSS as on the page.
(function () {
  var phone = window.matchMedia("(max-width: 700px)");
  // Offered only when fitting the column costs the labels more than a fifth of their size.
  var SHRINK = 0.8;
  var figures = [];
  var dialog, body, current;

  Array.prototype.forEach.call(document.querySelectorAll("main .ip-svg:not(.ip-img-dark)"), function (light) {
    var twin = light.nextElementSibling;
    var dark = twin && twin.classList.contains("ip-svg") && twin.classList.contains("ip-img-dark") ? twin : null;
    var width = drawnWidth(light);
    if (width > 0) figures.push({ light: light, dark: dark, width: width, frame: null });
  });
  if (!figures.length) return;

  // The authored width when the editor set one (SvgView's --ip-svg-w), else the viewBox:
  // either way the size at which the labels read as drawn.
  function drawnWidth(el) {
    var authored = parseFloat(getComputedStyle(el).getPropertyValue("--ip-svg-w"));
    if (authored > 0) return authored;
    var svg = el.querySelector("svg");
    var box = svg && svg.viewBox && svg.viewBox.baseVal;
    return box && box.width > 0 ? box.width : 0;
  }

  function shownWidth(f) {
    return Math.max(f.light.getBoundingClientRect().width, f.dark ? f.dark.getBoundingClientRect().width : 0);
  }

  // The frame exists only while a figure is enlargeable, so a page on a wide screen keeps
  // exactly the markup it was published with.
  function wrap(f) {
    var frame = document.createElement("div");
    frame.className = "ip-svg-frame";
    f.light.parentNode.insertBefore(frame, f.light);
    frame.appendChild(f.light);
    if (f.dark) frame.appendChild(f.dark);
    var button = document.createElement("button");
    button.type = "button";
    button.className = "ip-svg-zoom";
    button.innerHTML = '<span aria-hidden="true">⤢</span> Tap to enlarge';
    frame.appendChild(button);
    frame.addEventListener("click", function () { open(f); });
    f.frame = frame;
  }

  function unwrap(f) {
    var frame = f.frame;
    frame.parentNode.insertBefore(f.light, frame);
    if (f.dark) frame.parentNode.insertBefore(f.dark, frame);
    frame.parentNode.removeChild(frame);
    f.frame = null;
  }

  function update() {
    figures.forEach(function (f) {
      if (f === current) return;
      var shown = shownWidth(f);
      var enlarge = phone.matches && shown > 0 && shown < f.width * SHRINK;
      if (enlarge && !f.frame) wrap(f);
      else if (!enlarge && f.frame) unwrap(f);
    });
  }

  function build() {
    dialog = document.createElement("dialog");
    dialog.className = "ip-svg-lightbox";
    var close = document.createElement("button");
    close.type = "button";
    close.className = "ip-svg-lightbox-close";
    close.innerHTML = '<span aria-hidden="true">✕</span> Close';
    close.addEventListener("click", function () { dialog.close(); });
    body = document.createElement("div");
    body.className = "ip-svg-lightbox-body";
    dialog.appendChild(close);
    dialog.appendChild(body);
    document.body.appendChild(dialog);

    // Every way out (the button, Escape, the back gesture) ends here.
    dialog.addEventListener("close", function () {
      restore();
      if (history.state && history.state.ipSvgZoom) history.back();
    });
    window.addEventListener("popstate", function () {
      if (dialog.open) dialog.close();
    });
  }

  function open(f) {
    if (current) return;
    if (!dialog) build();
    if (!dialog.showModal) return; // a browser without <dialog>: the diagram stays as it is
    current = f;
    // Hold the figure's place so the page behind does not reflow while it is away.
    f.frame.style.minHeight = f.frame.offsetHeight + "px";
    body.style.setProperty("--ip-zoom-w", f.width + "px");
    body.appendChild(f.light);
    if (f.dark) body.appendChild(f.dark);
    dialog.setAttribute("aria-label", f.light.getAttribute("aria-label") || "Diagram");
    dialog.showModal();
    body.scrollTo(0, 0);
    // An entry of its own, so the phone's back gesture closes the view, not the page.
    try { history.pushState({ ipSvgZoom: true }, ""); } catch (e) { /* closes by button only */ }
  }

  function restore() {
    var f = current;
    if (!f) return;
    current = null;
    var button = f.frame.lastChild;
    f.frame.insertBefore(f.light, button);
    if (f.dark) f.frame.insertBefore(f.dark, button);
    f.frame.style.minHeight = "";
    update();
  }

  var queued = false;
  window.addEventListener("resize", function () {
    if (queued) return;
    queued = true;
    requestAnimationFrame(function () { queued = false; update(); });
  });
  if (phone.addEventListener) phone.addEventListener("change", update);
  else phone.addListener(update); // Safari < 14
  update();
})();
