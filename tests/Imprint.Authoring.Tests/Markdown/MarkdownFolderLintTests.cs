using Imprint.Authoring.Domain;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Tests.Markdown;

/// <summary>
/// A lint over a folder of markdown files, run only when IMPRINT_LINT_DIR names one: every file must
/// convert with no problem, or the test lists each file, line and message. It is how a batch of
/// articles is checked BEFORE it is written into imprint, where a refusal would only surface at review.
/// </summary>
public sealed class MarkdownFolderLintTests
{
    [Fact]
    public void Every_markdown_file_in_the_lint_folder_converts()
    {
        if (Environment.GetEnvironmentVariable("IMPRINT_LINT_DIR") is not { Length: > 0 } folder)
        {
            return;
        }

        var problems = Directory.EnumerateFiles(folder, "*.md")
            .Order(StringComparer.Ordinal)
            .SelectMany(file => MarkdownToNodes.Convert(File.ReadAllText(file), new Locale("en")).Problems
                .Select(p => $"{Path.GetFileName(file)} line {p.Line}: {p.Message}"))
            .ToList();

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
