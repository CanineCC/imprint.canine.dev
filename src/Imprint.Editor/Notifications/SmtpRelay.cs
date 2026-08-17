using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imprint.Editor.Notifications;

/// <summary>
/// The one place the editor talks to a mail relay. Two things send mail now — a visitor's
/// contact submission and a "please review this post" notice — and they must agree on the
/// relay, the credentials and the failure behaviour, or a working contact form will sit next
/// to a review notice that silently never arrives.
///
/// <para>Configured from the <c>Contact:Smtp:*</c> keys that are already deployed rather than a
/// new section: renaming live configuration to make a class name read better is a way to take an
/// estate down. <c>Contact:From</c> is the envelope sender for both.</para>
/// </summary>
public sealed class SmtpRelay(IConfiguration configuration, ILogger<SmtpRelay> logger)
{
    /// <summary>Whether a relay is configured at all. False means every send is a no-op.</summary>
    public bool Configured => !string.IsNullOrWhiteSpace(configuration["Contact:Smtp:Host"]);

    /// <summary>
    /// Sends one plain-text message. Returns false for "not delivered" — unconfigured relay,
    /// no recipients, or an active failure — and never throws: a mail server being down must not
    /// take a Blazor circuit or a background worker with it.
    /// </summary>
    public async Task<bool> Send(
        IReadOnlyList<string> recipients,
        string subject,
        string body,
        string? replyTo = null,
        CancellationToken ct = default)
    {
        var host = configuration["Contact:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) || recipients.Count == 0)
        {
            logger.LogWarning(
                "Mail not sent — no Contact:Smtp:Host, or no recipients. subject={Subject} recipients={Count}",
                subject, recipients.Count);
            return false;
        }

        try
        {
            var port = int.TryParse(configuration["Contact:Smtp:Port"], out var p) ? p : 587;
            using var client = new SmtpClient(host, port)
            {
                // SSL on by default; Contact:Smtp:UseSsl=false only for a plaintext relay on a
                // trusted internal network (same knob as the watchdog notifier).
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
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            foreach (var recipient in recipients)
            {
                mail.To.Add(new MailAddress(recipient));
            }

            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                mail.ReplyToList.Add(new MailAddress(replyTo));
            }

            await client.SendMailAsync(mail, ct);
            logger.LogInformation("Mail sent. subject={Subject} recipients={Count}", subject, recipients.Count);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or IOException)
        {
            logger.LogError(ex, "Mail delivery failed. subject={Subject}", subject);
            return false;
        }
    }
}
