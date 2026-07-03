using Imprint.Authoring;
using Imprint.Editor.Components;
using Imprint.EventSourcing;

var builder = WebApplication.CreateBuilder(args);

// All editor state lives under one data directory: the event store (truth), media
// files (bytes) and the published output (a projection). Point ImprintData somewhere
// else to host multiple installations side by side.
var dataDirectory = Path.GetFullPath(builder.Configuration["ImprintData"] ?? Path.Combine("data"));
Directory.CreateDirectory(dataDirectory);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddImprintEventSourcing(
    connectionString: $"Data Source={Path.Combine(dataDirectory, "imprint.db")}",
    domainAssemblies: [typeof(AuthoringJson).Assembly],
    configureJson: AuthoringJson.Configure);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.Services.InitializeImprintEventSourcing();

app.Run();
