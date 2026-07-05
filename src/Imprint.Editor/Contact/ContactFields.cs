namespace Imprint.Editor.Contact;

/// <summary>
/// The raw wire fields of a <c>POST /api/contact</c> (form-encoded or JSON — exactly the
/// names the published <c>&lt;contact-form&gt;</c> island submits). <see cref="Website"/>
/// is the honeypot: humans never see the field, so any value marks the sender as a bot.
/// <see cref="Site"/> is the submitting site's hostname, stamped by the island so one
/// endpoint can triage canine/watchdog/assay/cai leads.
/// </summary>
public sealed record ContactFields(
    string? Topic,
    string? Name,
    string? Email,
    string? Org,
    string? Message,
    string? Website,
    string? Site)
{
    // Generous for a human note, a hard stop for a paste-bomb.
    private const int MaxMessageLength = 10_000;
    private const int MaxFieldLength = 200;

    public bool IsBot => !string.IsNullOrWhiteSpace(Website);

    /// <summary>Shape validation only — the errors are wire-facing strings the island
    /// can show verbatim. An empty list means the submission is deliverable.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("name is required");
        }

        // A light shape check, not RFC 5322 — the address only needs to be plausible
        // enough to Reply-To; a typo costs the sender their reply either way.
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            errors.Add("a reply email address is required");
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            errors.Add("message is required");
        }
        else if (Message.Length > MaxMessageLength)
        {
            errors.Add($"message is too long (max {MaxMessageLength} characters)");
        }

        if (Name?.Length > MaxFieldLength || Email?.Length > MaxFieldLength ||
            Org?.Length > MaxFieldLength || Topic?.Length > MaxFieldLength || Site?.Length > MaxFieldLength)
        {
            errors.Add($"a field exceeds {MaxFieldLength} characters");
        }

        return errors;
    }

    /// <summary>The validated, trimmed submission. Only meaningful after
    /// <see cref="Validate"/> returned no errors.</summary>
    public ContactSubmission Normalize() => new(
        string.IsNullOrWhiteSpace(Topic) ? "General" : Topic.Trim(),
        Name!.Trim(),
        Email!.Trim(),
        string.IsNullOrWhiteSpace(Org) ? null : Org.Trim(),
        Message!.Trim(),
        string.IsNullOrWhiteSpace(Site) ? null : Site.Trim());
}
