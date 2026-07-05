namespace Imprint.Editor.Contact;

/// <summary>A validated, trimmed contact-form submission, ready to deliver. The shape the
/// estate's contact idiom already uses (watchdog's <c>ContactSubmission</c>), plus the
/// originating site so one endpoint serves every marketing brand.</summary>
public sealed record ContactSubmission(
    string Topic,
    string Name,
    string Email,
    string? Organisation,
    string Message,
    string? Site);
