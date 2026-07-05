using System.Text.Json;
using Imprint.Editor.Contact;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Editor.Tests;

/// <summary>
/// The /api/contact intake: the honeypot pretends success while dropping the payload,
/// shape validation names each missing/oversized field, and with no SMTP relay
/// configured a valid submission is appended to contact-submissions.jsonl — the
/// never-lose-a-lead fallback the endpoint promises.
/// </summary>
public sealed class ContactIntakeTests : IDisposable
{
    private readonly DirectoryInfo _dataDir = Directory.CreateTempSubdirectory("imprint-contact-");

    private string StorePath => Path.Combine(_dataDir.FullName, "contact-submissions.jsonl");

    private ContactIntake NewIntake() =>
        // No Contact:Smtp:Host / Contact:Recipients ⇒ the not-configured path under test.
        new(new ConfigurationBuilder().Build(), _dataDir.FullName, NullLogger<ContactIntake>.Instance);

    private static ContactFields ValidFields(
        string? name = "Ada", string? email = "ada@example.com", string? message = "We would like an appraisal.",
        string? website = null) =>
        new("Sales", name, email, "Analytical Engines ApS", message, website, "canine.dev");

    [Fact]
    public async Task Honeypot_filled_pretends_success_and_stores_nothing()
    {
        var errors = await NewIntake().Handle(ValidFields(website: "https://spam.example"), TestContext.Current.CancellationToken);

        Assert.Empty(errors); // the bot must not learn it was caught
        Assert.False(File.Exists(StorePath));
    }

    [Theory]
    [InlineData(null, "ada@example.com", "hello there")]
    [InlineData("Ada", null, "hello there")]
    [InlineData("Ada", "not-an-email", "hello there")]
    [InlineData("Ada", "ada@example.com", null)]
    [InlineData("Ada", "ada@example.com", "   ")]
    public async Task Missing_or_malformed_required_fields_are_rejected(string? name, string? email, string? message)
    {
        var errors = await NewIntake().Handle(ValidFields(name, email, message), TestContext.Current.CancellationToken);

        Assert.NotEmpty(errors);
        Assert.False(File.Exists(StorePath)); // rejected submissions are never stored
    }

    [Fact]
    public async Task Message_over_ten_thousand_characters_is_rejected()
    {
        var errors = await NewIntake().Handle(ValidFields(message: new string('x', 10_001)), TestContext.Current.CancellationToken);

        Assert.Contains(errors, e => e.Contains("too long"));
    }

    [Fact]
    public async Task Valid_submission_without_a_relay_is_appended_to_the_jsonl_store()
    {
        var intake = NewIntake();
        Assert.Empty(await intake.Handle(ValidFields(), TestContext.Current.CancellationToken));
        Assert.Empty(await intake.Handle(ValidFields(name: "Grace"), TestContext.Current.CancellationToken));

        var lines = File.ReadAllLines(StorePath);
        Assert.Equal(2, lines.Length); // one JSON line per lead, appended, never overwritten

        var first = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("Ada", first.GetProperty("Name").GetString());
        Assert.Equal("ada@example.com", first.GetProperty("Email").GetString());
        Assert.Equal("Sales", first.GetProperty("Topic").GetString());
        Assert.Equal("canine.dev", first.GetProperty("Site").GetString());
        Assert.Equal("We would like an appraisal.", first.GetProperty("Message").GetString());

        var second = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal("Grace", second.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task Stored_fields_are_trimmed_and_a_blank_topic_defaults_to_general()
    {
        var fields = new ContactFields("  ", "  Ada  ", " ada@example.com ", "  ", "  hello  ", null, null);
        Assert.Empty(await NewIntake().Handle(fields, TestContext.Current.CancellationToken));

        var stored = JsonDocument.Parse(File.ReadAllLines(StorePath).Single()).RootElement;
        Assert.Equal("Ada", stored.GetProperty("Name").GetString());
        Assert.Equal("ada@example.com", stored.GetProperty("Email").GetString());
        Assert.Equal("General", stored.GetProperty("Topic").GetString());
        Assert.Equal(JsonValueKind.Null, stored.GetProperty("Organisation").ValueKind);
        Assert.Equal("hello", stored.GetProperty("Message").GetString());
    }

    public void Dispose() => _dataDir.Delete(recursive: true);
}
