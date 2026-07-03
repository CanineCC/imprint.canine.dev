// Placeholder host — completed in the publishing milestone (docs/publishing.md §Imprint.Site).
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => "ok");
app.Run();
