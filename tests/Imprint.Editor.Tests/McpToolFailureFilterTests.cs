using Imprint.Editor.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Imprint.Editor.Tests;

/// <summary>
/// The authoring MCP is driven from off-network, so a caller who hits a failure cannot read the host
/// journal. These pin that a thrown exception reaches them by TYPE and MESSAGE rather than as the SDK's
/// bare "An error occurred invoking '&lt;tool&gt;'".
/// </summary>
public sealed class McpToolFailureFilterTests
{
    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static RequestContext<CallToolRequestParams> Context(string tool, McpServerOptions options) =>
        new(
            McpServer.Create(new StreamServerTransport(Stream.Null, Stream.Null), options),
            new JsonRpcRequest { Method = RequestMethods.ToolsCall },
            new CallToolRequestParams { Name = tool });

    [Fact]
    public async Task A_thrown_exception_is_returned_with_its_type_and_message()
    {
        var logger = new TestLogger();
        var handler = McpToolFailureFilter.Wrap(
            (_, _) => throw new InvalidOperationException("no IWidgetCatalog is registered"), logger);

        var result = await handler(Context("add_node", new McpServerOptions()), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("'add_node' failed: InvalidOperationException: no IWidgetCatalog is registered", Text(result), StringComparison.Ordinal);
        Assert.Contains("correlation ", Text(result), StringComparison.Ordinal);
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task A_healthy_tool_passes_straight_through()
    {
        var expected = new CallToolResult { Content = [new TextContentBlock { Text = "ok" }] };
        var handler = McpToolFailureFilter.Wrap((_, _) => ValueTask.FromResult(expected), new TestLogger());

        var result = await handler(Context("get_site", new McpServerOptions()), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public void An_McpException_is_left_for_the_SDK_and_cancellation_the_caller_asked_for_is_not_swallowed()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        // The SDK already appends an McpException's message, and a tool that throws one chose its wording.
        Assert.False(McpToolFailureFilter.ShouldRender(new McpException("bad argument"), CancellationToken.None));
        // Cancellation the caller asked for is theirs to see AS cancellation.
        Assert.False(McpToolFailureFilter.ShouldRender(new OperationCanceledException(), cancelled.Token));
        // A timeout the caller did NOT ask for is a real failure and must still be rendered.
        Assert.True(McpToolFailureFilter.ShouldRender(new OperationCanceledException(), CancellationToken.None));
        Assert.True(McpToolFailureFilter.ShouldRender(new InvalidOperationException("boom"), CancellationToken.None));
    }

    [Fact]
    public async Task The_filter_is_installed_by_the_production_registration()
    {
        // ★ SEMANTIC, not a count: the SDK registers call-tool filters of its own on these options, so a
        //   count would move whenever theirs did. Compose every filter the real registration produced over
        //   a handler that throws, and read what comes out.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImprintAuthoringMcp();
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        McpRequestHandler<CallToolRequestParams, CallToolResult> handler =
            (_, _) => throw new InvalidOperationException("the page stream is empty");
        foreach (var filter in options.Filters.Request.CallToolFilters.AsEnumerable().Reverse())
        {
            handler = filter(handler);
        }

        var result = await handler(Context("get_page_tree", options), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("'get_page_tree' failed: InvalidOperationException: the page stream is empty", Text(result), StringComparison.Ordinal);
    }

    private sealed class TestLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
