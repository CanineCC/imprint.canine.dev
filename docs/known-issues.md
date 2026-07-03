# Known issues

## E2E: interop commits intermittently lost mid-suite (under investigation)

**Symptom.** In full `Imprint.E2E` batch runs, `Rich_text_bold_survives_the_canonical_normalizer`
sometimes loses its commits: the live canvas shows the edit (local contenteditable
state), no error surfaces anywhere, but no `page.text-changed` event reaches the
store — the JS→.NET interop calls (`CommitText`, `EndInlineEdit`) from that circuit
appear to vanish. Reload reveals the untouched original.

**What it is NOT** (each ruled out with a dedicated repro):
- Not the product edit path: the same flow passes standalone 3/3, passes as
  heading-edit in-batch, passes after publish, passes with two live contexts, and
  passes with the exact preceding delete/undo sequence replayed.
- Not CPU contention alone (reproduced on a quiet box), though load worsens it.
- Not the pre-circuit lost-click class (fixed: `[data-interactive]` beacon +
  onboarding controls disabled until interactive).
- Not a wrong-word/test bug (was one contributing failure, fixed: keyboard word
  selection instead of pixel targeting).
- Not JS exceptions or rejected interop promises (instrumented: `invoke()` logs both;
  nothing fires — calls neither resolve nor reject).

**Current best hypothesis.** Interplay between sequentially opened/closed Blazor
Server circuits in one app process (xunit collection reuses the fixture): earlier
circuits' disconnected-but-retained renderers still hold projection `Changed`
subscriptions; something in the notify path stalls the newest circuit's interop
processing. Needs eyes on .NET 10 circuit retention/pause semantics vs the
in-process, synchronous projection notification design.

**Repro.** `dotnet test tests/Imprint.E2E` (full batch) fails ~most runs on the
rich-text test; every narrower extraction passes. Fixture writes `editor-console.log`
and `js-console.log` into its temp data dir (path printed in the failure message).

**Impact.** Editor E2E flakiness; a real-user analogue would be "edits silently stop
saving in a long-lived tab after other tabs closed" — severity high if confirmed
outside the test harness, which has not been observed.
