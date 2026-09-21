using Imprint.Editor.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Imprint.Editor.Mcp;

/// <summary>
/// Wires the headless authoring MCP server (all editor capabilities as MCP tools) over the ASP.NET Core
/// HTTP transport and mounts it at <c>/mcp</c>, gated by the SAME <c>Imprint:Authoring:Token</c> bearer
/// secret as the authoring API. FAIL-CLOSED: the endpoint is mapped only when the token is configured, so
/// no token ⇒ no MCP surface (identical to <see cref="AuthoringApi.MapAuthoringApi"/>).
/// </summary>
public static class ImprintAuthoringMcpServer
{
    /// <summary>Register the MCP server + its authoring tools. Safe to call unconditionally — the surface
    /// is only mounted (and thus reachable) by <see cref="MapImprintAuthoringMcp"/> when the token is set.</summary>
    public static IServiceCollection AddImprintAuthoringMcp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<ImprintAuthoringMcpTools>();
        // A tool that throws must SAY what it threw: this surface is driven off-network, so the caller
        // cannot read the host journal the SDK's opaque string sends them to. See McpToolFailureFilter.
        services.AddOptions<McpServerOptions>().Configure<ILoggerFactory>(McpToolFailureFilter.Install);
        return services;
    }

    /// <summary>Mount the MCP endpoint at <c>/mcp</c> behind a wholesale bearer-token branch (even
    /// listing the tools needs a valid <c>Imprint:Authoring:Token</c>). Mapped only when the token is
    /// configured — fail-closed, like the authoring API.</summary>
    public static void MapImprintAuthoringMcp(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var token = app.Configuration[AuthoringApi.TokenKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            return; // fail closed: no token ⇒ no MCP surface
        }

        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/mcp"),
            branch => branch.Use(async (ctx, next) =>
            {
                if (!AuthoringToken.Matches(ctx.Request, token))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                await next().ConfigureAwait(false);
            }));

        app.MapMcp("/mcp");
    }
}
