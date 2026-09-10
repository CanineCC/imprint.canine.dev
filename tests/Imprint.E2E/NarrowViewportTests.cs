using Microsoft.Playwright;

namespace Imprint.E2E;

/// <summary>
/// A published page has to fit the phone it is read on. Every string measured here is an
/// identifier a producer publishes by the thousand — <c>owner/name</c>, an advisory id, a package
/// name, an artefact digest — and none of them wraps the way a sentence does.
///
/// <para>These are measurements, not string assertions. The delivery contract's byte tests already
/// read the stylesheet, and a stylesheet containing the right declaration can still lay the page
/// out wrongly; only a browser at 400px settles it. The last fact is the control that keeps the
/// cure honest: whatever fixes a long token must not start breaking ordinary words.</para>
///
/// <para>Not every fact here was red before the fix, and that is the point of measuring rather than
/// assuming. <c>GHSA-35jh-r3h4-6jhm</c> and the scoped package name both fitted already, because a
/// hyphen IS a break opportunity a browser takes and a solidus is not — they are here to hold that
/// ground, not to have claimed it.</para>
/// </summary>
[Collection("editor")]
public sealed class NarrowViewportTests(NarrowViewportFixture fixture)
{
    [Fact]
    public async Task A_heading_of_owner_slash_name_does_not_scroll_the_page_sideways()
    {
        var width = await fixture.Measure("/registry/github/nordisk/harbour-api/");

        Assert.True(
            width.Overflow <= 0,
            $"'{NarrowViewportFixture.RepositoryHeading}' pushes the page {width.Overflow}px past its own "
            + $"width — {width}. A visitor on a phone has to scroll sideways to read the heading.");
    }

    [Fact]
    public async Task An_advisory_identifier_does_not_scroll_the_page_sideways()
    {
        var width = await fixture.Measure("/advisories/ghsa-35jh-r3h4-6jhm/");

        Assert.True(
            width.Overflow <= 0,
            $"'{NarrowViewportFixture.AdvisoryHeading}' pushes the page {width.Overflow}px past its own "
            + $"width — {width}.");
    }

    [Fact]
    public async Task A_long_package_name_in_a_table_cell_does_not_scroll_the_page_sideways()
    {
        var width = await fixture.Measure("/packages/");

        Assert.True(
            width.Overflow <= 0,
            $"'{NarrowViewportFixture.PackageName}' in a table cell pushes the page {width.Overflow}px "
            + $"past its own width — {width}.");
    }

    [Fact]
    public async Task A_digest_in_a_table_cell_folds_instead_of_making_the_table_scroll()
    {
        // .ip-table-wrap is a horizontal scroller by design, and that design is right for a table
        // that is genuinely wide — many columns of real data. It is the wrong answer for ONE
        // unbreakable token, which is a token the reader then has to drag a scrollbar to finish
        // reading. This is also the fact that distinguishes the two candidate rules: under
        // `overflow-wrap: break-word` the cell wraps but its min-content contribution does not
        // drop, so the auto table layout keeps the column — and the scroller — exactly as wide.
        var width = await fixture.Measure("/artifacts/");
        var scrollers = await fixture.HorizontalScrollersOn("/artifacts/");

        Assert.True(width.Overflow <= 0, $"the page scrolls sideways — {width}.");
        Assert.True(
            scrollers.Length == 0,
            $"a {NarrowViewportFixture.Digest.Length}-character digest left the reader a horizontal "
            + "scrollbar inside the page: " + string.Join("; ", scrollers));
    }

    [Fact]
    public async Task A_bare_url_in_body_copy_does_not_scroll_the_page_sideways()
    {
        var width = await fixture.Measure("/prose/");

        Assert.True(
            width.Overflow <= 0,
            $"'{NarrowViewportFixture.AdvisoryUrl}' in a paragraph pushes the page {width.Overflow}px "
            + $"past its own width — {width}.");
    }

    [Fact]
    public async Task Ordinary_prose_still_wraps_at_spaces_and_is_never_broken_mid_word()
    {
        // A word the browser laid out across two lines occupies two client rects. Ranges are
        // measured rather than the text re-flowed by hand, so this reads the lines the visitor
        // sees, in the fonts the visitor is served, at the width the visitor holds.
        const string Measure =
            """
            () => {
                const node = document.querySelector('main .ip-prose p').firstChild;
                const text = node.textContent;
                const broken = [];
                const tops = new Set();
                for (const match of text.matchAll(/\S+/g)) {
                    const range = document.createRange();
                    range.setStart(node, match.index);
                    range.setEnd(node, match.index + match[0].length);
                    const rects = range.getClientRects();
                    if (rects.length > 1) { broken.push(match[0]); }
                    tops.add(Math.round(rects[0].top));
                }
                return { broken, lines: tops.size };
            }
            """;

        var (lines, broken) = await fixture.OnNarrowPage("/prose/", async page => (
            await page.EvaluateAsync<int>($"({Measure})().lines"),
            await page.EvaluateAsync<string[]>($"({Measure})().broken")));

        Assert.True(
            lines > 1,
            $"the control paragraph fitted on {lines} line(s) at phone width, so it proves nothing "
            + "about wrapping — give it more words.");
        Assert.True(
            broken.Length == 0,
            "ordinary prose was broken mid-word at phone width: " + string.Join(", ", broken)
            + ". Breaking a word that would have fitted on a line of its own is "
            + "word-break: break-all's failure mode, not a wrap rule's.");
    }
}
