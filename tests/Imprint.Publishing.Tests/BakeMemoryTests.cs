using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The last bake that worked, kept so a fetch that fails cannot blank a published page.
/// </summary>
/// <remarks>
/// ★★ WRITTEN AFTER IT COST A LIVE PRICING PAGE. A deploy queued during an outage restarted the
/// publisher the moment the runner returned; the publisher synchronises everything at startup; the
/// service those pages bake from was still forty minutes away. Every fetch returned null, every bake
/// key went missing, and six pages were rewritten with their placeholders — including
/// <c>watchdog.canine.dev/pricing</c>, which served "Pricing is published here" and no prices for two
/// and a half hours. A bake is a STATIC FILE: rendered once, served to everyone, until something
/// renders it again.
/// </remarks>
public sealed class BakeMemoryTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "imprint-bake-memory-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void A_bake_that_worked_is_recalled()
    {
        var memory = new BakeMemory(Output());

        memory.Remember("https://app/pricing|packages|", "<table><tr><td>€24</td></tr></table>");

        Assert.Equal("<table><tr><td>€24</td></tr></table>", memory.Recall("https://app/pricing|packages|"));
    }

    /// <summary>★★ THE WHOLE POINT: a NEW process recalls what an older one baked.</summary>
    /// <remarks>
    /// The failure was a restart, so a memory that lived in the process would have been empty at
    /// exactly the moment it was needed. A second instance over the same output directory is what a
    /// restarted publisher is.
    /// </remarks>
    [Fact]
    public void A_restarted_publisher_still_remembers()
    {
        new BakeMemory(Output()).Remember("k", "<p>remembered</p>");

        Assert.Equal("<p>remembered</p>", new BakeMemory(Output()).Recall("k"));
    }

    [Fact]
    public void Nothing_is_recalled_for_a_bake_that_never_worked()
    {
        Assert.Null(new BakeMemory(Output()).Recall("never-baked"));
    }

    /// <summary>★ Two bakes of the same URL through different templates are different memories.</summary>
    /// <remarks>
    /// The pricing page reads ONE endpoint through two templates — the packages and the self-hosted
    /// rows. Keyed by URL alone they would overwrite each other, which is a defect this codebase has
    /// already had once, in the bake keys themselves.
    /// </remarks>
    [Fact]
    public void Two_templates_over_one_url_are_two_memories()
    {
        var memory = new BakeMemory(Output());

        memory.Remember("https://app/pricing|packages|", "<p>packages</p>");
        memory.Remember("https://app/pricing|onprem|", "<p>on-prem</p>");

        Assert.Equal("<p>packages</p>", memory.Recall("https://app/pricing|packages|"));
        Assert.Equal("<p>on-prem</p>", memory.Recall("https://app/pricing|onprem|"));
    }

    /// <summary>★★ NEVER INSIDE THE OUTPUT, because the output root is served to the public verbatim.</summary>
    [Fact]
    public void The_memory_is_not_written_inside_the_published_output()
    {
        var output = Output();
        Directory.CreateDirectory(output);

        new BakeMemory(output).Remember("k", "<p>x</p>");

        Assert.Empty(Directory.GetFileSystemEntries(output));
        Assert.True(Directory.Exists(Path.Combine(_root, ".bakes", "site")));
    }

    /// <summary>★ A memory it cannot write is a memory with nothing in it, never an exception.</summary>
    [Fact]
    public void An_unusable_root_recalls_nothing_and_does_not_throw()
    {
        var memory = new BakeMemory("");

        memory.Remember("k", "<p>x</p>");

        Assert.Null(memory.Recall("k"));
    }

    private string Output() => Path.Combine(_root, "site");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
