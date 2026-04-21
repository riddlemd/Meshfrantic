using Meshfrantic.Components;
using Meshfrantic.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "blazor", ".log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<MeshtasticService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<LogsService>();
builder.Services.AddHostedService<MeshtasticReaderService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Open browser automatically when not launched via dotnet run (which handles it via launchSettings.json)
if (!app.Environment.IsDevelopment())
{
    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5083";
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* best-effort */ }
    });
}

app.Run();
