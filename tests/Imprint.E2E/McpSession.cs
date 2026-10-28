using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Imprint.E2E;

/// <summary>The MCP streamable-HTTP handshake and tool calls, as an agent's client makes them.</summary>
internal sealed class McpSession(string baseUrl) : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };
    private string? _session;
    private int _id;

    public void Dispose() => _http.Dispose();

    public async Task<JsonObject> Call(string tool, JsonObject arguments) => (await CallNode(tool, arguments)).AsObject();

    public async Task<JsonArray> CallArray(string tool, JsonObject arguments) => (await CallNode(tool, arguments)).AsArray();

    private async Task<JsonNode> CallNode(string tool, JsonObject arguments)
    {
        if (_session is null)
        {
            await Send("initialize", new JsonObject
            {
                ["protocolVersion"] = "2025-06-18",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject { ["name"] = "imprint-e2e", ["version"] = "1" },
            });
            await Send("notifications/initialized", null, notification: true);
        }

        var result = await Send("tools/call", new JsonObject { ["name"] = tool, ["arguments"] = arguments });
        var content = result!["result"]!["content"]!.AsArray()[0]!["text"]!.GetValue<string>();
        return content.TrimStart() is ['{' or '[', ..]
            ? JsonNode.Parse(content)!
            : throw new InvalidOperationException($"{tool} answered: {content}");
    }

    private async Task<JsonNode?> Send(string method, JsonObject? parameters, bool notification = false)
    {
        var message = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method };
        if (!notification)
        {
            message["id"] = ++_id;
        }

        if (parameters is not null)
        {
            message["params"] = parameters;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(message.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", EditorFixture.AuthoringToken);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (_session is not null)
        {
            request.Headers.Add("Mcp-Session-Id", _session);
        }

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (response.Headers.TryGetValues("Mcp-Session-Id", out var ids))
        {
            _session = ids.First();
        }

        if (notification)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        // Streamable HTTP may answer as an event stream: the JSON-RPC answer is the last data line.
        var json = body.TrimStart().StartsWith('{')
            ? body
            : body.Split('\n').Where(l => l.StartsWith("data:", StringComparison.Ordinal)).Select(l => l[5..].Trim()).Last();
        return JsonNode.Parse(json);
    }
}
