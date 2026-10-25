namespace Imprint.Publishing.Tests;

/// <summary>
/// The product pricing templates (Assert, Assure, CCA) render the product's own prices from its <c>/api/public/pricing/{product}</c>
/// payload, print each price exactly as the product formatted it, and render nothing when nothing is on sale, so a page never reads as
/// free. The payloads are the shapes the product serves (Kennel's <c>PublicProductPricingEndpoint</c>).
/// </summary>
public sealed class ProductPricingTemplateTests
{
    private const string AssertJson = """
        {"product":"assert","currency":"EUR","vatIncluded":false,"plans":[
          {"key":"assert-standard","name":"Standard","tagline":"For the team doing the work.","pricePerAuthorEur":20,"price":"€20",
           "linesPerAuthorPerMonth":1000000,"fairUseCeilingPercent":150,"features":["Core survey","Agent-fix loop"]},
          {"key":"assert-pro","name":"Pro","tagline":"For a team that answers to someone outside it.","pricePerAuthorEur":30,"price":"€30",
           "linesPerAuthorPerMonth":1000000,"fairUseCeilingPercent":150,"features":["Core survey","Compliance"]}]}
        """;

    private const string AssureJson = """
        {"product":"assure","currency":"EUR","vatIncluded":true,"onSale":true,"pricePerRepositoryEur":10,"price":"€10","repositoryLineCap":500000}
        """;

    private const string CcaJson = """
        {"product":"cca","currency":"EUR","vatIncluded":false,"freeGrantLines":50000,"pricePerMillionEur":50,"pricePerMillion":"€50",
         "minimumPurchaseLines":1000000,"creditValidityMonths":12,"tiers":[
          {"key":"cca-5m","name":"CCA 5M","monthlyPriceEur":100,"price":"€100","linesPerMonth":5000000,"requestsPerMinute":60},
          {"key":"cca-25m","name":"CCA 25M","monthlyPriceEur":500,"price":"€500","linesPerMonth":25000000,"requestsPerMinute":120}]}
        """;

    private const string AssayJson = """
        {"product":"assay","currency":"EUR","vatIncluded":false,"flatFeeEur":1000,"flatFee":"€1,000","rateOfRebuildCost":0.0025,
         "ratePercent":"0.25 %","perReportDivisor":3}
        """;

    [Fact]
    public void Assay_renders_the_yearly_fee_per_repository_and_the_single_report_share()
    {
        var html = ProductPricingTemplate.RenderAssay(AssayJson);
        Assert.NotNull(html);
        Assert.Contains("€1,000", html);
        Assert.Contains("0.25 %", html);
        Assert.Contains("a third of the yearly fee", html);
        Assert.Contains("As many reports on that repository as you like that year", html);
        Assert.Null(ProductPricingTemplate.RenderAssay("""{"flatFee":null}"""));
        Assert.NotNull(PrerenderTemplates.Render("assay-pricing", AssayJson));
    }

    [Fact]
    public void Assert_renders_each_plan_per_author_with_its_fair_use_and_features()
    {
        var html = ProductPricingTemplate.RenderAssert(AssertJson);
        Assert.NotNull(html);
        Assert.Contains("<h3>Standard</h3>", html);
        Assert.Contains("€20<span class=\"ip-price-unit\">per author a month</span>", html);
        Assert.Contains("€30", html);
        Assert.Contains("1,000,000 lines scanned a month per paid author", html);
        Assert.Contains("<li>Agent-fix loop</li>", html);
    }

    [Fact]
    public void Assure_renders_its_one_price_with_vat_and_the_size_limit()
    {
        var html = ProductPricingTemplate.RenderAssure(AssureJson);
        Assert.NotNull(html);
        Assert.Contains("€10<span class=\"ip-price-unit\">a month for each repository we scan, VAT included</span>", html);
        Assert.Contains("Up to 500,000 lines of code per repository", html);
    }

    [Fact]
    public void Cca_renders_the_free_lines_pay_as_you_go_and_every_tier()
    {
        var html = ProductPricingTemplate.RenderCca(CcaJson);
        Assert.NotNull(html);
        Assert.Contains("The first 50,000 lines, once per account", html);
        Assert.Contains("€50 per 1M lines", html);
        Assert.Contains("Credit is valid for 12 months.", html);
        Assert.Contains("<th scope=\"row\">CCA 5M</th><td>€100 a month</td><td>5,000,000 a month</td>", html);
        Assert.Contains("CCA 25M", html);
    }

    [Fact]
    public void Nothing_on_sale_renders_nothing_so_the_placeholder_stays()
    {
        Assert.Null(ProductPricingTemplate.RenderAssert("""{"plans":[]}"""));
        Assert.Null(ProductPricingTemplate.RenderAssure("""{"onSale":false,"price":null}"""));
        Assert.Null(ProductPricingTemplate.RenderCca("""{"freeGrantLines":50000,"pricePerMillion":null,"tiers":[]}"""));
        Assert.Null(ProductPricingTemplate.RenderCca("not json"));
        Assert.Null(ProductPricingTemplate.RenderAssure(""));
    }

    [Fact]
    public void A_price_is_printed_as_the_product_wrote_it_and_escaped()
    {
        var html = ProductPricingTemplate.RenderAssure("""{"onSale":true,"price":"€9.99 <b>","vatIncluded":true}""");
        Assert.NotNull(html);
        Assert.Contains("€9.99 &lt;b&gt;", html);
    }

    [Fact]
    public void The_three_templates_are_reachable_by_the_names_their_widgets_select()
    {
        Assert.NotNull(PrerenderTemplates.Render("assert-pricing", AssertJson));
        Assert.NotNull(PrerenderTemplates.Render("assure-pricing", AssureJson));
        Assert.NotNull(PrerenderTemplates.Render("cca-pricing", CcaJson));
    }
}
