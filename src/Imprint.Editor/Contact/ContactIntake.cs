using System.Text.Json;
using Imprint.Editor.Notifications;

namespace Imprint.Editor.Contact;

/// <summary>
/// Handles a public contact-form submission end to end: honeypot drop, shape validation,
/// then delivery. Delivery goes through <see cref="SmtpRelay"/> — the estate's existing contact
/// idiom (watchdog's <c>SmtpContactNotifier</c>): the BCL SMTP client against the
/// <c>Contact:Smtp:*</c> relay config, a plain-text body, Reply-To the submitter — no
/// third-party mail SaaS. Recipients are resolved per submission by
/// <see cref="ContactRecipientResolver"/> — the submitting site's private contact-form
/// widget prop first, the <c>Contact:Recipients</c> config as fallback — and live ONLY
/// server-side: the whole point of the endpoint is that no inbox address ever appears in
/// published page markup. Where watchdog merely logs a lead when no relay is configured,
/// imprint also appends it to <c>&lt;ImprintData&gt;/contact-submissions.jsonl</c> — an
/// unconfigured or failing relay stores the lead instead of losing it, and the visitor
/// still gets a thank-you.
/// </summary>
public sealed class ContactIntake(
    IConfiguration configuration,
    string dataDirectory,
    ILogger<ContactIntake> logger,
    SmtpRelay relay,
    ContactRecipientResolver? recipientResolver = null)
{
    // Config-only resolution when no widget-prop lookup is wired (tests, minimal hosts).
    private readonly ContactRecipientResolver _recipients = recipientResolver ?? new(configuration);

    // Serializes appends from concurrent submissions; a JSONL line must land whole.
    private readonly Lock _storeGate = new();

    /// <summary>Processes one submission. An empty error list means the visitor should see
    /// success — including the honeypot case, where "success" is deliberate misdirection.</summary>
    public async Task<IReadOnlyList<string>> Handle(ContactFields fields, CancellationToken ct)
    {
        // A filled honeypot is a bot: accept silently and drop, so it learns nothing.
        if (fields.IsBot)
        {
            logger.LogInformation("Contact submission dropped: honeypot filled.");
            return [];
        }

        var errors = fields.Validate();
        if (errors.Count > 0)
        {
            return errors;
        }

        var submission = fields.Normalize();
        var (recipients, source) = _recipients.Resolve(submission.Site);
        if (source != ContactRecipientResolver.Source.None)
        {
            // Deliberately the addresses' COUNT, not the addresses: the resolution source
            // is the operational fact worth logging; the inboxes stay out of the logs.
            logger.LogInformation(
                "Contact recipients resolved from {Source} for site={Site} ({Count} recipient(s)).",
                source == ContactRecipientResolver.Source.WidgetProp
                    ? "the contact-form widget prop"
                    : "Contact:Recipients config",
                submission.Site ?? "—", recipients.Count);
        }

        if (!await TrySend(submission, recipients, ct))
        {
            Store(submission);
        }

        return [];
    }

    /// <summary>Attempts delivery through the shared relay. False means "not emailed" —
    /// unconfigured relay or an active failure — and the caller stores the lead instead.</summary>
    private async Task<bool> TrySend(ContactSubmission submission, IReadOnlyList<string> recipients, CancellationToken ct)
    {
        if (recipients.Count == 0 || !relay.Configured)
        {
            logger.LogWarning(
                "Contact submission stored, not emailed — no Contact:Smtp:Host, or no recipients (widget prop / Contact:Recipients). site={Site} topic={Topic} email={Email}",
                submission.Site ?? "—", submission.Topic, submission.Email);
            return false;
        }

        // Reply-To the submitter so a one-click reply reaches them, not the From mailbox.
        return await relay.Send(
            recipients,
            $"[{submission.Site ?? "contact"} · {submission.Topic}] {submission.Name}",
            BuildBody(submission),
            replyTo: submission.Email,
            ct);
    }

    /// <summary>Appends the lead as one JSON line to <c>contact-submissions.jsonl</c> in the
    /// data directory — the never-lose-a-lead fallback for every not-emailed outcome.</summary>
    private void Store(ContactSubmission submission)
    {
        var path = Path.Combine(dataDirectory, "contact-submissions.jsonl");
        var line = JsonSerializer.Serialize(new StoredLead(
            DateTimeOffset.UtcNow, submission.Site, submission.Topic, submission.Name,
            submission.Email, submission.Organisation, submission.Message));
        lock (_storeGate)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }

        logger.LogInformation("Contact submission appended to {Path}.", path);
    }

    private static string BuildBody(ContactSubmission s) => string.Join('\n',
        $"Site:         {s.Site ?? "—"}",
        $"Topic:        {s.Topic}",
        $"Name:         {s.Name}",
        $"Email:        {s.Email}",
        $"Organisation: {s.Organisation ?? "—"}",
        "",
        "Message:",
        s.Message);

    private sealed record StoredLead(
        DateTimeOffset At, string? Site, string Topic, string Name, string Email, string? Organisation, string Message);
}
