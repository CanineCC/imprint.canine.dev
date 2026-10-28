using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.ApprovePostReview;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.ChangePostMeta;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.SignPublishedPost;
using Imprint.Authoring.Features.Posts.SubmitPostForReview;
using Imprint.Authoring.Features.Posts.UnpublishPost;
using Imprint.Authoring.Features.Sites.SetByline;
using Imprint.Authoring.Features.Sites.SetSiteReviewer;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A resource center through the real publisher: the index lists every article grouped by kind, and
/// each article gets the resource center's header, document record and contents around its body,
/// all as server-rendered HTML. The signer is a NAME (the reviewer's), never the login that signed.
/// </summary>
public sealed class ResourceCenterPublishingTests
{
    private const string En = "en";
    private const string Base = "https://resourcecenter.example";
    private const string Login = "jimmy@example.com";
    private const string Body = "# Read-only by design\n\nIntro.\n\n## What it reads\n\nCode.\n\n## What it never does\n\nWrite.\n";

    private static PublishingTestHost NewHost(bool everyWidget = false)
    {
        if (everyWidget)
        {
            // The whole shipped manifest and every bundle, so real articles that place a widget publish
            // exactly as they will in production.
            var manifestPath = RepoFile("widgets/manifest.json");
            var full = new PublishingTestHost(Base, configure: s => s.AddSingleton<IWidgetCatalog>(new ManifestCatalog(manifestPath)));
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(manifestPath)!))
            {
                File.Copy(file, Path.Combine(full.WidgetsDirectory, Path.GetFileName(file)), overwrite: true);
            }

            return full;
        }

        var host = new PublishingTestHost(Base, configure: s => s.AddSingleton<IWidgetCatalog>(new EmptyWidgetCatalog()));
        // The resource center's widgets, exactly as the repository's manifest declares them.
        var manifest = JsonSerializer.Deserialize<JsonElement[]>(File.ReadAllText(RepoFile("widgets/manifest.json")))!;
        File.WriteAllText(Path.Combine(host.WidgetsDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest.Where(w => w.GetProperty("tag").GetString()!.StartsWith("rc-", StringComparison.Ordinal))));
        foreach (var bundle in Directory.EnumerateFiles(Path.GetDirectoryName(RepoFile("widgets/manifest.json"))!, "rc-*.js"))
        {
            File.Copy(bundle, Path.Combine(host.WidgetsDirectory, Path.GetFileName(bundle)));
        }
        return host;
    }

    private static string RepoFile(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(relative);
    }

    private static async Task<(SiteId Site, PostId Post)> SignedGuide(PublishingTestHost host, string reviewerEmail = Login)
    {
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", reviewerEmail));
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Read-only by design", "read-only-by-design", En, "guide", ["assert"]));
        await host.Ok(new ChangePostBody(id, En, Body));
        await host.Ok(new ChangePostMeta(id, En, null, "What an agent may touch, and what it may not."));
        await host.Ok(new SubmitPostForReview(id, En, null, null));
        var metadata = host.Services.GetRequiredService<EventMetadataProvider>();
        var person = metadata.ActorSource;
        metadata.ActorSource = () => Login;
        try
        {
            await host.Ok(new ApprovePostReview(id, null));
        }
        finally
        {
            metadata.ActorSource = person;
        }

        await host.Ok(new PublishPost(id, En));
        await host.Publisher.Synchronize();
        return (site, id);
    }

    [Fact]
    public async Task The_index_lists_the_article_under_its_kind_with_its_product_version_and_status()
    {
        await using var host = NewHost();
        await SignedGuide(host);

        var html = host.ReadText("index.html");

        Assert.Contains("<div class=\"rc-hub\">", html, StringComparison.Ordinal);
        Assert.Contains("<section class=\"rc-group\" data-kind=\"guide\"><div class=\"rc-group-h\"><h2>Guides</h2>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/read-only-by-design/\"", html, StringComparison.Ordinal);
        Assert.Contains("<h3>Read-only by design</h3>", html, StringComparison.Ordinal);
        Assert.Contains("What an agent may touch, and what it may not.", html, StringComparison.Ordinal);
        Assert.Contains("data-products=\"assert\"", html, StringComparison.Ordinal);
        Assert.Contains("<span>v1</span>", html, StringComparison.Ordinal);
        Assert.Contains("rc-st signed", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_article_gets_its_header_record_and_contents_and_one_title()
    {
        await using var host = NewHost();
        var (_, id) = await SignedGuide(host);

        var html = host.ReadText("read-only-by-design/index.html");

        Assert.Contains("<header class=\"rc-art-head\">", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-kind\"><svg", html, StringComparison.Ordinal);
        Assert.Contains("</svg>Guide</span>", html, StringComparison.Ordinal);
        Assert.Contains("Signed by Jimmy Borch", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Document record\"", html, StringComparison.Ordinal);
        Assert.Contains(Post.BodySha256(LocalizedText.Of(new Locale(En), Body))[..32], html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"#what-it-reads\">What it reads</a>", html, StringComparison.Ordinal);
        Assert.Contains("id=\"what-it-reads\"", html, StringComparison.Ordinal);
        // The header carries the title; the body's own "# Read-only by design" is not a second h1.
        Assert.Equal(1, html.Split("<h1").Length - 1);
        _ = id;
    }

    // A whole small center, one article of each kind and two products, published end to end. With
    // RC_PREVIEW_DIR set, the output is copied there so a person can look at it in a browser.
    [Fact]
    public async Task A_center_with_every_kind_publishes_an_index_with_every_group()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        var essay = "# Does a maintainability score agree with experts?\n\n## Method\n\n| | |\n|---|---|\n| **Question** | Do the complexity signals rank Java classes the way experts rate them? |\n| **Units measured (n)** | 304 classes, 5 open-source Java projects |\n| **Fixed in advance** | Committed before any label was read |\n\n## The short answer\n\nTotal cognitive complexity agrees with the experts at **rho = 0.72**. File length alone agrees at **0.71**.\n\n> [!NOTE]\n> We said in advance that if this happened, it would go in the first paragraph.\n\n## What a score is for, then\n\nUse a complexity measure to find where and why.\n";
        await Article(host, site, "does-a-maintainability-score-agree-with-experts", "Does a maintainability score agree with experts?", "essay", ["cca"], essay,
            "We scored the 304 public classes that independent professionals rated. It agrees. File length agrees almost as well.", signed: true);
        await Article(host, site, "ten-questions-for-any-code-benchmark", "Ten questions for any code benchmark", "essay", ["canine"],
            "# Ten questions\n\n## How many units were measured?\n\nProse.\n\n## Is the sample the population?\n\nProse.\n",
            "Most published benchmarks can be judged in two minutes.", signed: true);
        await Article(host, site, "how-the-index-is-constructed", "How the Code Assurance Index is constructed", "whitepaper", ["cca"],
            "# How\n\n## Executive summary\n\nProse.\n\n## The ten-lens model\n\nProse.\n",
            "Lenses, dimensions, bands and the rubric that freezes them.", signed: true);
        await Article(host, site, "from-finding-to-fix", "From finding to fix, by hand or by agent", "guide", ["assert"],
            "# From finding to fix\n\n## Call audit\n\nProse.\n\n## Let the next scan decide\n\nProse.\n",
            "Hand a finding to a person or a coding agent with enough context to act on it.", signed: true);
        await Article(host, site, "answers-for-your-security-reviewer", "Answers for your security reviewer", "guide", ["assay"],
            "# Answers\n\n## Read-only\n\nProse.\n\n## Where the code runs\n\nProse.\n",
            "Read-only as a design principle: the questions, and a way to check each answer.", signed: false);
        await Article(host, site, "the-journey-of-a-survey", "The journey of a survey", "documentation", ["cca"],
            "# The journey\n\n## Six stages, in a fixed order\n\nProse.\n\n## When something goes wrong\n\n> [!WARNING]\n> A failed survey is a failure, not a low score.\n",
            "Six stages in a fixed order, and what each one records.", signed: true);
        await host.Publisher.Synchronize();

        var index = host.ReadText("index.html");
        foreach (var group in new[] { "Thought leadership", "Whitepapers", "Guides", "Documentation" })
        {
            Assert.Contains($"<h2>{group}</h2>", index, StringComparison.Ordinal);
        }

        Assert.Contains("<rc-filter", index, StringComparison.Ordinal);
        Assert.Contains(".ip-ap-article", host.ReadText(Assert.Single(host.FilesMatching("css/site.", ".css"))), StringComparison.Ordinal);
        Assert.Contains("ip-callout-note", host.ReadText("does-a-maintainability-score-agree-with-experts/index.html"), StringComparison.Ordinal);

        if (Environment.GetEnvironmentVariable("RC_PREVIEW_DIR") is { Length: > 0 } preview)
        {
            if (Directory.Exists(preview))
            {
                Directory.Delete(preview, recursive: true);
            }

            foreach (var relative in host.AllFiles())
            {
                var target = Path.Combine(preview, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(host.FullPath(relative), target);
            }
        }
    }

    // The real center, for a person to look at: with RC_CONTENT_JSON naming a file of {slug, title, kind,
    // products, dek, body} and RC_PREVIEW_DIR a folder, every article is published signed and copied there.
    [Fact]
    public async Task The_real_articles_publish_and_can_be_previewed()
    {
        if (Environment.GetEnvironmentVariable("RC_CONTENT_JSON") is not { Length: > 0 } content
            || Environment.GetEnvironmentVariable("RC_PREVIEW_DIR") is not { Length: > 0 } preview)
        {
            return;
        }

        await using var host = NewHost(everyWidget: true);
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        foreach (var article in JsonDocument.Parse(File.ReadAllText(content)).RootElement.EnumerateArray())
        {
            await Article(host, site, article.GetProperty("slug").GetString()!, article.GetProperty("title").GetString()!,
                article.GetProperty("kind").GetString()!, [.. article.GetProperty("products").EnumerateArray().Select(p => p.GetString()!)],
                article.GetProperty("body").GetString()!, article.GetProperty("dek").GetString()!, signed: true);
        }

        await host.Publisher.Synchronize();
        if (Directory.Exists(preview))
        {
            Directory.Delete(preview, recursive: true);
        }

        foreach (var relative in host.AllFiles())
        {
            var target = Path.Combine(preview, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(host.FullPath(relative), target);
        }

        // And as the editor's preview shows it, with one article still a draft and one in review.
        if (Environment.GetEnvironmentVariable("RC_PREVIEW_DRAFTS_DIR") is { Length: > 0 } drafts)
        {
            await Draft(host, site, "a-draft-article", "A draft, as the preview shows it", "essay", ["assert"], Body, "Not yet submitted.");
            await Draft(host, site, "an-article-in-review", "An article in review", "guide", ["assay"], Body, "Submitted.", submit: true);
            if (Directory.Exists(drafts))
            {
                Directory.Delete(drafts, recursive: true);
            }

            var aggregate = host.Services.GetRequiredService<SiteOverview>().Get(site)!;
            await host.Publisher.Synchronize(new PublishTarget(aggregate, drafts, BaseUrl: null, IncludeDrafts: true));
        }
    }

    private static async Task Article(
        PublishingTestHost host, SiteId site, string slug, string title, string kind, string[] products,
        string body, string dek, bool signed)
    {
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, title, slug, En, kind, products));
        await host.Ok(new ChangePostBody(id, En, body));
        await host.Ok(new ChangePostMeta(id, En, null, dek));
        await host.Ok(new SubmitPostForReview(id, En, null, null));
        var metadata = host.Services.GetRequiredService<EventMetadataProvider>();
        var person = metadata.ActorSource;
        metadata.ActorSource = () => signed ? Login : "someone-else@example.com";
        try
        {
            await host.Ok(new ApprovePostReview(id, null));
        }
        finally
        {
            metadata.ActorSource = person;
        }

        await host.Ok(new PublishPost(id, En));
    }

    [Fact]
    public async Task A_signer_who_is_not_the_reviewer_is_a_named_person_and_the_login_never_appears()
    {
        await using var host = NewHost();
        await SignedGuide(host, reviewerEmail: "someone-else@example.com");

        var article = host.ReadText("read-only-by-design/index.html");
        var index = host.ReadText("index.html");

        Assert.Contains("A named person", article, StringComparison.Ordinal);
        Assert.DoesNotContain(Login, article, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Login, index, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("someone-else@example.com", article, StringComparison.OrdinalIgnoreCase);
    }
    // A signature given after the article went live changes no words, so nothing the page stamp used to watch moved
    // and the live page kept saying "Not signed" (2026-10-28, the 24 articles signed in one go). The signature is
    // part of the page: signing re-renders the article and the index.
    [Fact]
    public async Task Signing_a_live_article_rewrites_its_page_and_the_index()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Read-only by design", "read-only-by-design", En, "guide", ["assert"]));
        await host.Ok(new ChangePostBody(id, En, Body));
        await host.Ok(new PublishPost(id, En));
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await host.Publisher.Synchronize();
        Assert.Contains("Not signed", host.ReadText("read-only-by-design/index.html"), StringComparison.Ordinal);

        var metadata = host.Services.GetRequiredService<EventMetadataProvider>();
        var person = metadata.ActorSource;
        metadata.ActorSource = () => Login;
        try
        {
            await host.Ok(new SignPublishedPost(id));
        }
        finally
        {
            metadata.ActorSource = person;
        }

        await host.Publisher.Synchronize();
        var article = host.ReadText("read-only-by-design/index.html");
        Assert.Contains("Signed by Jimmy Borch", article, StringComparison.Ordinal);
        Assert.DoesNotContain("Not signed", article, StringComparison.Ordinal);
        Assert.Contains("rc-st signed", host.ReadText("index.html"), StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------ round 2 (owner smoke test, 2026-10-28)

    private static async Task<PostId> Draft(PublishingTestHost host, SiteId site, string slug, string title, string kind,
        string[] products, string body, string dek, bool submit = false)
    {
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, title, slug, En, kind, products));
        await host.Ok(new ChangePostBody(id, En, body));
        await host.Ok(new ChangePostMeta(id, En, null, dek));
        if (submit)
        {
            await host.Ok(new SubmitPostForReview(id, En, null, null));
        }

        return id;
    }

    private static async Task<string> Preview(PublishingTestHost host, SiteId site)
    {
        var folder = Path.Combine(host.Root, "preview-" + Guid.NewGuid().ToString("N"));
        var aggregate = host.Services.GetRequiredService<SiteOverview>().Get(site)!;
        await host.Publisher.Synchronize(new PublishTarget(aggregate, folder, BaseUrl: null, IncludeDrafts: true));
        return folder;
    }

    [Fact]
    public async Task A_previewed_draft_shows_its_record_as_a_draft_never_an_empty_frame()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await Draft(host, site, "a-draft", "A draft", "essay", ["assert"], Body, "Not yet sent.");
        await Draft(host, site, "in-review", "In review", "essay", ["assert"], Body, "Sent.", submit: true);

        var folder = await Preview(host, site);
        var draft = File.ReadAllText(Path.Combine(folder, "a-draft", "index.html"));
        var review = File.ReadAllText(Path.Combine(folder, "in-review", "index.html"));

        // The record is always baked: an unbaked widget is styled as a loading placeholder (the
        // dashed empty box the owner found on 2026-10-28).
        Assert.Contains("<rc-context class=\"ip-widget ip-widget-baked\"", draft, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-st draft\">Draft</span>", draft, StringComparison.Ordinal);
        Assert.Contains("Not yet submitted", draft, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-st review\">In review</span>", review, StringComparison.Ordinal);
        Assert.Contains("Awaiting Jimmy Borch", review, StringComparison.Ordinal);
        Assert.DoesNotContain(Login, review, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_decorative_bar_is_gone_and_the_contents_column_carries_a_reading_progress()
    {
        await using var host = NewHost();
        await SignedGuide(host);
        var html = host.ReadText("read-only-by-design/index.html");

        Assert.DoesNotContain("rc-bar", html, StringComparison.Ordinal);
        Assert.Contains("class=\"rc-prog\" role=\"progressbar\"", html, StringComparison.Ordinal);
        Assert.Contains("<a class=\"rc-back\" href=\"/\">All resources</a>", html, StringComparison.Ordinal);
        // The reader island (progress, the active section) rides the article.
        Assert.Contains("<rc-reader", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_record_lists_every_published_version_with_its_date()
    {
        await using var host = NewHost();
        var (_, id) = await SignedGuide(host);
        await host.Ok(new UnpublishPost(id));
        await host.Ok(new ChangePostBody(id, En, Body + "\n## A third part\n\nMore.\n"));
        await host.Ok(new SubmitPostForReview(id, En, null, null));
        var metadata = host.Services.GetRequiredService<EventMetadataProvider>();
        var person = metadata.ActorSource;
        metadata.ActorSource = () => Login;
        try
        {
            await host.Ok(new ApprovePostReview(id, null));
        }
        finally
        {
            metadata.ActorSource = person;
        }

        await host.Ok(new PublishPost(id, En));
        await host.Publisher.Synchronize();
        var html = host.ReadText("read-only-by-design/index.html");

        Assert.Contains("<ol class=\"rc-versions\"", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-v\">v2</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-v\">v1</span>", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf(">v2<", StringComparison.Ordinal) < html.IndexOf(">v1<", StringComparison.Ordinal), "newest first");
    }

    [Fact]
    public async Task Go_deeper_lists_the_articles_it_links_to_and_those_that_link_to_it()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await Article(host, site, "the-guide", "The guide", "guide", ["assert"],
            "# The guide\n\n## One\n\nSee [the reference](https://resourcecenter.example/the-reference/).\n\n## Two\n\nDone.\n", "A guide.", signed: true);
        await Article(host, site, "the-reference", "The reference", "documentation", ["assert"],
            "# The reference\n\n## One\n\nFacts.\n\n## Two\n\nMore facts.\n", "A reference.", signed: true);
        await Article(host, site, "an-essay", "An essay", "essay", ["assert"],
            "# An essay\n\n## One\n\nRead [the guide](https://resourcecenter.example/the-guide/).\n\n## Two\n\nEnd.\n", "An essay.", signed: true);
        await host.Publisher.Synchronize();

        var guide = host.ReadText("the-guide/index.html");
        var go = guide[guide.IndexOf("Go deeper", StringComparison.Ordinal)..];
        Assert.Contains("href=\"/the-reference/\"", go, StringComparison.Ordinal);
        Assert.Contains("Linked from this guide", go, StringComparison.Ordinal);
        Assert.Contains("href=\"/an-essay/\"", go, StringComparison.Ordinal);
        Assert.Contains("Links to this guide", go, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/the-guide/\"", go, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Documentation_gets_the_documentation_tree_and_an_on_this_page_list()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await Article(host, site, "what-a-survey-is", "What a survey is", "documentation", ["cca"],
            "# What\n\n## One\n\nA.\n\n## Two\n\nB.\n", "One repository, one commit.", signed: true);
        await Article(host, site, "how-assay-works", "How Assay works", "documentation", ["assay"],
            "# How\n\n## One\n\nA.\n\n## Two\n\nB.\n", "Assay.", signed: true);
        await host.Publisher.Synchronize();

        var html = host.ReadText("what-a-survey-is/index.html");
        Assert.Contains("<nav class=\"rc-docnav\" aria-label=\"Documentation\">", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/what-a-survey-is/\" aria-current=\"page\">What a survey is</a>", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/how-assay-works/\">How Assay works</a>", html, StringComparison.Ordinal);
        Assert.Contains("<nav class=\"rc-onpage\" aria-label=\"On this page\">", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_guide_numbers_its_steps_and_offers_a_tick_for_each()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await Article(host, site, "first-baseline", "Your first baseline", "guide", ["assert"],
            "# First\n\nIntro.\n\n## 1. Connect your code host\n\nA.\n\n## 2. Track a repository\n\nB.\n\n## Learn more\n\nC.\n", "Connect and read.", signed: true);
        await host.Publisher.Synchronize();

        var html = host.ReadText("first-baseline/index.html");
        Assert.Contains("<ol class=\"rc-steps-nav\"", html, StringComparison.Ordinal);
        Assert.Contains("data-step=\"connect-your-code-host\"", html, StringComparison.Ordinal);
        Assert.Contains("data-step=\"track-a-repository\"", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"rc-dot\">1</span><span>Connect your code host</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-step=\"learn-more\"", html, StringComparison.Ordinal);
        Assert.Contains("0 of 2 steps done", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_header_is_the_resource_centers_own_with_products_and_search()
    {
        await using var host = NewHost();
        var (site, _) = await SignedGuide(host);
        await host.Ok(new SetByline(site, new Byline(LocalizedText.Of(new Locale(En), "Jimmy Borch"), null)));
        await Article(host, site, "an-essay", "An essay", "essay", ["assert"], Body, "An essay.", signed: true);
        await host.Publisher.Synchronize();
        var essay = host.ReadText("an-essay/index.html");
        var article = host.ReadText("read-only-by-design/index.html");
        var index = host.ReadText("index.html");

        foreach (var html in new[] { article, index })
        {
            Assert.Contains("<small class=\"rc-by\">by Canine Development</small>", html, StringComparison.Ordinal);
            Assert.Contains("href=\"/?product=assert#rc-index\"", html, StringComparison.Ordinal);
            Assert.Contains("data-rc-search", html, StringComparison.Ordinal);
            Assert.Contains("<rc-search", html, StringComparison.Ordinal);
        }

        // An essay carries its author; a guide or a reference page does not.
        Assert.Contains("<span>By Jimmy Borch</span>", essay, StringComparison.Ordinal);
        Assert.DoesNotContain("By Jimmy Borch", article, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_search_index_lists_published_articles_only()
    {
        await using var host = NewHost();
        var (site, _) = await SignedGuide(host);
        await Draft(host, site, "not-yet", "Not yet", "essay", ["assert"], Body, "A draft.");
        await host.Publisher.Synchronize();

        using var index = JsonDocument.Parse(host.ReadText("search.json"));
        var entry = Assert.Single(index.RootElement.EnumerateArray());
        Assert.Equal("Read-only by design", entry.GetProperty("title").GetString());
        Assert.Equal("/read-only-by-design/", entry.GetProperty("href").GetString());
        Assert.Equal("guide", entry.GetProperty("kind").GetString());
        Assert.Equal("assert", entry.GetProperty("products")[0].GetString());
        Assert.Equal("What an agent may touch, and what it may not.", entry.GetProperty("dek").GetString());
    }

    [Fact]
    public async Task The_index_opens_with_a_bento_of_the_newest_and_a_latest_list()
    {
        await using var host = NewHost();
        var site = await host.CreateSite("Resource Center", kind: SiteKind.ResourceCenter);
        await host.Ok(new SetSiteReviewer(site, "Jimmy Borch", Login));
        await Article(host, site, "ten-questions", "Ten questions for any code benchmark", "essay", ["canine"],
            "# Ten\n\n## How many units were measured?\n\nA.\n\n## Is the sample the population?\n\nB.\n", "Two minutes.", signed: true);
        await Article(host, site, "a-paper", "A paper", "whitepaper", ["cca"], "# P\n\n## One\n\nA.\n\n## Two\n\nB.\n", "Paper.", signed: true);
        await Article(host, site, "a-guide", "A guide", "guide", ["assert"], "# G\n\n## 1. One\n\nA.\n\n## 2. Two\n\nB.\n", "Guide.", signed: true);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.Contains("<div class=\"rc-bento\">", html, StringComparison.Ordinal);
        // The feature card lists the essay's own sections (B21's are its ten questions).
        Assert.Contains("<li>How many units were measured?</li>", html, StringComparison.Ordinal);
        Assert.Contains("<nav class=\"rc-latest\" aria-label=\"Latest\">", html, StringComparison.Ordinal);
        Assert.Contains("class=\"rc-omni\"", html, StringComparison.Ordinal);
    }
}

/// <summary>The shipped widget manifest as a catalog: a tag exists when the manifest declares it.</summary>
internal sealed class ManifestCatalog(string manifestPath) : IWidgetCatalog
{
    private readonly IReadOnlyList<Imprint.Rendering.WidgetDescriptor> _widgets = Imprint.Rendering.WidgetManifest.Load(manifestPath);

    public bool Exists(string tag) => _widgets.Any(w => w.Tag == tag);

    public IReadOnlySet<string> PropNames(string tag) =>
        _widgets.FirstOrDefault(w => w.Tag == tag)?.Props.Select(p => p.Name).ToHashSet() ?? [];
}
