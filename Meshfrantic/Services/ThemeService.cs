namespace Meshfrantic.Services;

public class ThemeService
{
    public string CurrentTheme { get; private set; } = "terminal-green";

    public IReadOnlyList<string> AvailableThemes { get; } = new[]
    {
        "terminal-green",
        "terminal-amber",
        "terminal-white",
        "nostromo"
    };

    public void SetTheme(string theme)
    {
        if (AvailableThemes.Contains(theme))
        {
            CurrentTheme = theme;
        }
    }
}
