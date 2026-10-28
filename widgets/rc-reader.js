// The reading aids on a resource-center article (<rc-reader>, placed by the publisher at the end of the
// article): fills the reading progress in the left column, marks the section in view in the contents, and
// on a guide puts a tick beside each numbered step. Everything it touches is already in the page and works
// without it. A guide's ticks are kept in this browser only (localStorage, per page); nothing is sent.
class RcReader extends HTMLElement {
  connectedCallback() {
    if (this.dataset.live) return;
    this.dataset.live = '1';
    const frame = this.closest('.ip-ap-article');
    const article = this.closest('.ip-stack');
    if (!frame || !article) return;

    const prog = frame.querySelector('.rc-side .rc-prog[role="progressbar"]');
    const pct = frame.querySelector('.rc-side .rc-pct');
    const links = [...frame.querySelectorAll('.rc-toc a[href^="#"], .rc-onpage a[href^="#"], .rc-steps-nav a[href^="#"]')];
    const ids = [...new Set(links.map((a) => decodeURIComponent(a.hash.slice(1))))];
    const targets = ids.map((id) => document.getElementById(id)).filter(Boolean);
    const list = frame.querySelector('.rc-toc ol');

    let queued = false;
    const update = () => {
      queued = false;
      if (prog) {
        // How far through the article the reader is: 0 at its top, 100 when its end is well in view.
        const r = article.getBoundingClientRect();
        const value = Math.max(0, Math.min(100, ((-r.top + 90) / Math.max(1, r.height - innerHeight * 0.6)) * 100));
        const n = Math.round(value);
        prog.style.setProperty('--rc-pct', value + '%');
        prog.setAttribute('aria-valuenow', String(n));
        if (pct) pct.textContent = n + ' % read';
      }
      let current = null;
      for (const t of targets) if (t.getBoundingClientRect().top < 140) current = t;
      current ??= targets[0];
      for (const a of links) {
        if (current && a.hash === '#' + current.id) a.setAttribute('aria-current', 'true');
        else a.removeAttribute('aria-current');
      }
      const active = list?.querySelector('a[aria-current="true"]');
      if (list && active) {
        list.style.setProperty('--rc-mk-top', active.offsetTop + 'px');
        list.style.setProperty('--rc-mk-h', active.offsetHeight + 'px');
        list.classList.add('rc-marked');
      }
    };
    const queue = () => {
      if (!queued) {
        queued = true;
        requestAnimationFrame(update);
      }
    };
    addEventListener('scroll', queue, { passive: true });
    addEventListener('resize', queue);
    update();
    this.ticks(frame);
  }

  ticks(frame) {
    const nav = frame.querySelector('.rc-steps-nav');
    if (!nav) return;
    const items = [...nav.querySelectorAll('li[data-step]')];
    const key = 'rc-done:' + location.pathname;
    let done = new Set();
    try {
      done = new Set(JSON.parse(localStorage.getItem(key) || '[]'));
    } catch {
      done = new Set();
    }
    const box = frame.querySelector('.rc-steps-progress');
    const bar = box?.querySelector('.rc-prog');
    const label = box?.querySelector('.rc-prog-label');
    const buttons = [];

    items.forEach((item, k) => {
      const heading = document.getElementById(item.dataset.step);
      if (!heading) return;
      const tick = document.createElement('button');
      tick.type = 'button';
      tick.className = 'rc-chk';
      tick.dataset.step = item.dataset.step;
      tick.setAttribute('aria-label', 'Step ' + (k + 1) + ' done');
      tick.addEventListener('click', () => {
        if (done.has(tick.dataset.step)) done.delete(tick.dataset.step);
        else done.add(tick.dataset.step);
        try {
          localStorage.setItem(key, JSON.stringify([...done]));
        } catch {
          // A private window may refuse storage; the tick still holds for this visit.
        }
        paint();
      });
      // The heading and its tick side by side; the heading keeps its id and its own words.
      const head = document.createElement('div');
      head.className = 'rc-step-head';
      heading.replaceWith(head);
      head.append(tick, heading);
      buttons.push(tick);
    });
    if (buttons.length === 0) return;

    const paint = () => {
      let n = 0;
      for (const tick of buttons) {
        const on = done.has(tick.dataset.step);
        if (on) n++;
        tick.setAttribute('aria-pressed', String(on));
        tick.closest('.rc-step-head')?.classList.toggle('rc-done', on);
      }
      for (const item of items) {
        const on = done.has(item.dataset.step);
        item.classList.toggle('rc-done', on);
      }
      if (bar) bar.style.setProperty('--rc-pct', (100 * n) / buttons.length + '%');
      if (label) label.textContent = n === buttons.length ? 'All ' + n + ' steps done' : n + ' of ' + buttons.length + ' steps done';
    };
    if (box) box.hidden = false;
    paint();
  }
}

if (!customElements.get('rc-reader')) customElements.define('rc-reader', RcReader);
