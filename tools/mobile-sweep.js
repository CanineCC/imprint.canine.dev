#!/usr/bin/env node
// Mobile overflow sweep: renders every URL at 390x844, measures horizontal overflow
// (documentElement.scrollWidth vs innerWidth), names the offending elements, captures
// console errors, and saves a full-page screenshot per page.
//
//   S=/tmp/sweep node tools/mobile-sweep.js       # reads $S/mobile-urls.txt (one URL per line)
//   → $S/mob/<slug>.png + $S/mobile-report.json
//
// Written for the 2026-09-03 sweep that found the burger pushing every header off-screen
// (54/64 pages overflowed); run it after any chrome change. A page whose only wide
// elements sit INSIDE an overflow-x:auto container can still be flagged — check `dw`
// (the document scroll width) to separate page-level overflow from internal scrollers.
const { chromium } = require('playwright');
const fs = require('fs');
const urls = fs.readFileSync(process.env.S+'/mobile-urls.txt','utf8').trim().split('\n');
(async()=>{
  const b = await chromium.launch({});
  const results=[];
  for (const url of urls){
    const p = await b.newPage({viewport:{width:390,height:844}});
    const errors=[];
    p.on('console', m=>{ if(m.type()==='error') errors.push(m.text().slice(0,160)); });
    p.on('pageerror', e=>errors.push(('pageerror: '+e.message).slice(0,160)));
    try {
      const resp = await p.goto(url, {waitUntil:'networkidle', timeout:45000});
      await p.waitForTimeout(400);
      const m = await p.evaluate(()=>{
        const dw=document.documentElement.scrollWidth, vw=window.innerWidth;
        const bad=[];
        if (dw>vw+1){
          for (const el of document.querySelectorAll('body *')){
            const r=el.getBoundingClientRect();
            if (r.right>vw+1 && r.width>8){
              const sel=el.tagName.toLowerCase()+(el.className&&typeof el.className==='string'?'.'+el.className.split(' ').slice(0,2).join('.'):'');
              bad.push({sel, right:Math.round(r.right), w:Math.round(r.width)});
              if (bad.length>=6) break;
            }
          }
        }
        return {dw, vw, bad, h:document.documentElement.scrollHeight};
      });
      const slug = url.replace(/https:\/\//,'').replace(/[^a-z0-9.]+/gi,'_').replace(/_+$/,'').slice(0,90);
      await p.screenshot({path:`${process.env.S}/mob/${slug}.png`, fullPage:true});
      results.push({url, status:resp?resp.status():0, overflow:m.dw>m.vw+1, dw:m.dw, vw:m.vw, h:m.h, bad:m.bad, errors:errors.slice(0,4), shot:`mob/${slug}.png`});
      process.stdout.write((m.dw>m.vw+1?'!':'.'));
    } catch(e){ results.push({url, error:String(e).slice(0,140)}); process.stdout.write('E'); }
    await p.close();
  }
  await b.close();
  fs.writeFileSync(process.env.S+'/mobile-report.json', JSON.stringify(results,null,1));
  console.log('\ndone', results.length);
})();
