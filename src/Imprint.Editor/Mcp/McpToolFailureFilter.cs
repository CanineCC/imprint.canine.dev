using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Imprint.Editor.Mcp;

/// <summary>
/// Makes a failed authoring MCP tool call AUDIBLE. Wraps the call-tool pipeline so that an exception a
/// tool handler lets escape is (1) logged on the server with a correlation id and the full stack, and
/// (2) returned to the caller as <c>isError: true</c> content naming the exception TYPE, its MESSAGE and
/// that same correlation id — instead of the SDK's own rendering, which is the bare string
/// <c>An error occurred invoking '&lt;tool&gt;'.</c> for anything that is not an <see cref="McpException"/>.
/// </summary>
/// <remarks>
/// <para>Why this matters here in particular: the authoring MCP is driven from OFF-NETWORK — that is the
/// reason it exists (<see cref="ImprintAuthoringMcpServer"/>). A caller who cannot reach the box cannot
/// read <c>journalctl -u imprint-green</c>, so with the opaque string a failure is indistinguishable
/// between a bad argument, a missing registration and a broken deploy — and those need completely
/// different responses. The known example is a host built without an <c>IWidgetCatalog</c>: every node
/// endpoint answers 500 with an empty body, which reads like an outage rather than a wiring mistake.</para>
/// <para>★ The seam is the SDK's <c>CallToolFilters</c>, and its position is the whole point: the SDK's
/// own catch sits OUTSIDE the filter chain (the composed handler is built from the filters, then wrapped
/// in the try/catch that produces the opaque string), so a filter is the last place the raw exception is
/// still visible. A <c>DelegatingMcpServerTool</c> wrapper is NOT that place — the per-tool
/// <c>InvokeAsync</c> has already returned by the time it sees anything.</para>
/// <para>What is deliberately NOT caught: cancellation the caller asked for (it is theirs to see as
/// cancellation), and <see cref="McpException"/> — the SDK already appends its message, and a tool that
/// throws one has chosen its own wording.</para>
/// <para>Ported from Kennel's <c>McpToolFailureFilter</c>, which was written after a reporter spent six
/// live calls against production — two of them writes — to establish only that a failure was not their
/// own argument.</para>
/// </remarks>
public static class McpToolFailureFilter
{
    /// <summary>Install the filter on a server's options. Used by the production wiring in
    /// <see cref="ImprintAuthoringMcpServer.AddImprintAuthoringMcp"/> and by the tests, so a red test is a
    /// test of the wiring the host runs rather than of a look-alike.</summary>
    public static void Install(McpServerOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        var logger = loggerFactory.CreateLogger("Imprint.Authoring.Mcp.ToolFailure");
        options.Filters.Request.CallToolFilters.Add(next => Wrap(next, logger));
    }

    /// <summary>The filter itself: <paramref name="next"/> wrapped so that an escaping exception becomes a
    /// logged, correlated, typed error result.</summary>
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Wrap(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        return async (context, ct) =>
        {
            try
            {
                return await next(context, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRender(ex, ct))
            {
                var tool = context.Params?.Name ?? "?";
                var correlationId = NewCorrelationId();
                logger.LogError(
                    ex,
                    "MCP tool {Tool} threw {ExceptionType} [correlation {CorrelationId}]: {Message}",
                    tool, ex.GetType().FullName, correlationId, ex.Message);
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = Describe(tool, ex, correlationId) }],
                };
            }
        };
    }

    /// <summary>The text the caller reads. Type, message and the correlation id that joins it to the
    /// server log — the three things the opaque string withheld.</summary>
    public static string Describe(string tool, Exception ex, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return $"'{tool}' failed: {ex.GetType().Name}: {ex.Message} [correlation {correlationId}; the imprint host log carries the stack under that id]";
    }

    /// <summary>Which escapes we render. Caller cancellation and <see cref="McpException"/> pass through
    /// untouched — see the type remarks.</summary>
    public static bool ShouldRender([NotNullWhen(true)] Exception? ex, CancellationToken ct) =>
        ex is not null
        && ex is not McpException
        && !(ex is OperationCanceledException && ct.IsCancellationRequested);

    private static string NewCorrelationId() => Guid.NewGuid().ToString("N")[..12];
}
