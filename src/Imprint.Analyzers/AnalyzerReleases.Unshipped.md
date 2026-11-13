; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
IMP0001 | Presentation | Error | CaiScoreTextAnalyzer — a CAI score becomes text only through Imprint.Publishing.CaiScoreText, one decimal always (ADR-0028)
IMP0002 | Presentation | Error | CaiScoreTextAnalyzer — a score keeps a name that says so, or is marked [CaiScore] / [NotACaiScore("why")] (ADR-0028)
