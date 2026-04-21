namespace Meshfrantic.Services;

public class ThemeService
{
    private readonly ILogger<ThemeService> _logger;

    public string CurrentTheme { get; private set; } = "terminal-green";

    public IReadOnlyDictionary<string, string> Themes { get; } = new Dictionary<string, string>
    {
        { "terminal-green", "Green" },
        { "terminal-amber", "Amber" },
        { "terminal-white", "White" },
        { "nostromo", "Nostromo" },
        { "terminator", "T-800" },
        { "cyberdyne", "Cyberdyne" }
    };

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
        _logger.LogInformation("ThemeService initialized with theme: {Theme}", CurrentTheme);
    }

    public void SetTheme(string theme)
    {
        if (Themes.Keys.Contains(theme))
        {
            _logger.LogInformation("Theme changed from {OldTheme} to {NewTheme}", CurrentTheme, theme);
            CurrentTheme = theme;
        }
        else
        {
            _logger.LogWarning("Attempted to set invalid theme: {Theme}", theme);
        }
    }
}
