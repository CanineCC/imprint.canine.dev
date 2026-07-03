using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageEditTextTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly HeadingNode _heading;
    private readonly RichTextNode _richText;
    private readonly ButtonNode _button;
    private readonly ImageNode _image;
    private readonly SvgNode _svg;
    private readonly DividerNode _divider;

    public PageEditTextTests()
    {
        _heading = Heading("Hello");
        _richText = RichText("<p>Hi</p>");
        _button = Button("Go");
        _image = Image();
        _svg = Svg();
        _divider = Divider();
    }

    private AggregateSpec<Page> Spec() => AggregateSpec.For<Page>().Given(
        _created,
        new NodeAdded(NodeId.Root, 0, Section(Stack(_heading, _richText, _button, _image, _svg, _divider))));

    [Fact]
    public void EditText_heading_text_raises_event_and_folds()
    {
        var outcome = Spec().When(p => p.EditText(_heading.Id, "text", En, "Welcome"));

        outcome.ThenRaised(new TextChanged(_heading.Id, "text", En, "Welcome"));
        var heading = Assert.IsType<HeadingNode>(outcome.Aggregate.Tree.Find(_heading.Id));
        Assert.Equal("Welcome", heading.Text.Get(En));
    }

    [Fact]
    public void EditText_in_another_locale_keeps_existing_translations()
    {
        var outcome = Spec().When(p => p.EditText(_heading.Id, "text", Da, "Velkommen"));

        outcome.ThenRaised(new TextChanged(_heading.Id, "text", Da, "Velkommen"));
        var heading = Assert.IsType<HeadingNode>(outcome.Aggregate.Tree.Find(_heading.Id));
        Assert.Equal("Hello", heading.Text.Get(En));
        Assert.Equal("Velkommen", heading.Text.Get(Da));
    }

    [Fact]
    public void EditText_button_label_is_editable()
    {
        Spec()
            .When(p => p.EditText(_button.Id, "label", En, "Read more"))
            .ThenRaised(new TextChanged(_button.Id, "label", En, "Read more"));
    }

    [Fact]
    public void EditText_image_alt_is_editable()
    {
        Spec()
            .When(p => p.EditText(_image.Id, "alt", En, "A lighthouse at dawn"))
            .ThenRaised(new TextChanged(_image.Id, "alt", En, "A lighthouse at dawn"));
    }

    [Fact]
    public void EditText_svg_alt_is_editable()
    {
        Spec()
            .When(p => p.EditText(_svg.Id, "alt", En, "The company logo"))
            .ThenRaised(new TextChanged(_svg.Id, "alt", En, "The company logo"));
    }

    [Fact]
    public void EditText_unknown_node_is_rejected()
    {
        Spec()
            .When(p => p.EditText(NodeId.New(), "text", En, "Hi"))
            .ThenFails("no longer exists");
    }

    [Fact]
    public void EditText_field_not_on_the_node_type_is_rejected()
    {
        Spec()
            .When(p => p.EditText(_heading.Id, "html", En, "<p>Hi</p>"))
            .ThenFails("no editable");
    }

    [Fact]
    public void EditText_on_a_node_without_text_is_rejected()
    {
        Spec()
            .When(p => p.EditText(_divider.Id, "text", En, "Hi"))
            .ThenFails("no editable");
    }

    [Fact]
    public void EditText_richtext_with_canonical_html_is_accepted()
    {
        const string html = "<p>Hello <strong>world</strong> and <a href=\"https://example.com\">more</a>.</p>";

        Spec()
            .When(p => p.EditText(_richText.Id, "html", En, html))
            .ThenRaised(new TextChanged(_richText.Id, "html", En, html));
    }

    [Fact]
    public void EditText_richtext_rejection_surfaces_the_validator_message()
    {
        Spec()
            .When(p => p.EditText(_richText.Id, "html", En, "<p><script>alert(1)</script></p>"))
            .ThenFails("Disallowed tag");

        Spec()
            .When(p => p.EditText(_richText.Id, "html", En, "plain text without a block"))
            .ThenFails("Expected <p>");

        Spec()
            .When(p => p.EditText(_richText.Id, "html", En, "<p><a href=\"javascript:alert(1)\">x</a></p>"))
            .ThenFails("https, http, mailto");
    }

    [Fact]
    public void EditText_plain_value_over_500_characters_is_rejected()
    {
        Spec()
            .When(p => p.EditText(_heading.Id, "text", En, new string('x', 501)))
            .ThenFails("500 characters");
    }

    [Fact]
    public void EditText_plain_value_of_exactly_500_characters_is_accepted()
    {
        Spec()
            .When(p => p.EditText(_heading.Id, "text", En, new string('x', 500)))
            .ThenRaised(new TextChanged(_heading.Id, "text", En, new string('x', 500)));
    }

    [Fact]
    public void EditText_unchanged_value_raises_nothing()
    {
        Spec()
            .When(p => p.EditText(_heading.Id, "text", En, "Hello"))
            .ThenNothing();
    }

    [Fact]
    public void EditText_clearing_an_absent_locale_raises_nothing()
    {
        Spec()
            .When(p => p.EditText(_heading.Id, "text", Da, ""))
            .ThenNothing();
    }

    [Fact]
    public void EditText_empty_value_clears_the_locale()
    {
        var outcome = Spec().When(p => p.EditText(_heading.Id, "text", En, ""));

        outcome.ThenRaised(new TextChanged(_heading.Id, "text", En, ""));
        var heading = Assert.IsType<HeadingNode>(outcome.Aggregate.Tree.Find(_heading.Id));
        Assert.False(heading.Text.Has(En));
    }
}
