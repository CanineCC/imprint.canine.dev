using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace Imprint.Editor.Contact;

/// <summary>
/// Handles a public contact-form submission end to end: honeypot drop, shape validation,
/// then delivery. Delivery mirrors the estate's existing contact idiom (watchdog's
/// <c>SmtpContactNotifier</c>): the BCL <see cref="SmtpClient"/> against the
/// <c>Contact:Smtp:*</c> relay config, a plain-text body, Reply-To the submitter — no
/// third-party mail SaaS. Recipients come from <c>Contact:Recipients</c> (comma-separated)
/// and live ONLY in server config: the whole point of the endpoint is that no inbox
/// address ever appears in published page markup. Where watchdog merely logs a lead when
/// no relay is configured, imprint also appends it to
/// <c>&lt;ImprintData&gt;/contact-submissions.jsonl</c> — an unconfigured or failing relay
/// stores the lead instead of losing it, and the visitor still gets a thank-you.
/// </summary>
public sealed class ContactIntake(IConfiguration configuration, string dataDirectory, ILogger<ContactIntake> logger)
{
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
        if (!await TrySend(submission, ct))
        {
            Store(submission);
        }

        return [];
    }

    /// <summary>Attempts SMTP delivery. False means "not emailed" — unconfigured relay or
    /// an active failure — and the caller stores the lead locally instead.</summary>
    private async Task<bool> TrySend(ContactSubmission submission, CancellationToken ct)
    {
        var host = configuration["Contact:Smtp:Host"];
        var recipients = (configuration["Contact:Recipients"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (string.IsNullOrWhiteSpace(host) || recipients.Length == 0)
        {
            logger.LogWarning(
                "Contact submission stored, not emailed — Contact:Smtp:Host / Contact:Recipients not configured. site={Site} topic={Topic} email={Email}",
                submission.Site ?? "—", submission.Topic, submission.Email);
            return false;
        }

        try
        {
            var port = int.TryParse(configuration["Contact:Smtp:Port"], out var p) ? p : 587;
            using var client = new SmtpClient(host, port)
            {
                // SSL on by default; Contact:Smtp:UseSsl=false only for a plaintext relay
                // on a trusted internal network (same knob as the watchdog notifier).
                EnableSsl = !string.Equals(configuration["Contact:Smtp:UseSsl"], "false", StringComparison.OrdinalIgnoreCase),
            };

            var user = configuration["Contact:Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user))
            {
                client.Credentials = new NetworkCredential(user, configuration["Contact:Smtp:Password"]);
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(configuration["Contact:From"] ?? recipients[0]),
                Subject = $"[{submission.Site ?? "contact"} · {submission.Topic}] {submission.Name}",
                Body = BuildBody(submission),
                IsBodyHtml = false,
            };
            foreach (var recipient in recipients)
            {
                mail.To.Add(new MailAddress(recipient));
            }

            // Reply-To the submitter so a one-click reply reaches them, not the From mailbox.
            mail.ReplyToList.Add(new MailAddress(submission.Email));

            await client.SendMailAsync(mail, ct);
            logger.LogInformation("Contact submission emailed (site={Site} topic={Topic}).", submission.Site ?? "—", submission.Topic);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or IOException)
        {
            // A configured relay actively failed — the local store below keeps the lead.
            logger.LogError(ex,
                "Contact email delivery failed — storing locally. site={Site} topic={Topic} email={Email}",
                submission.Site ?? "—", submission.Topic, submission.Email);
            return false;
        }
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
