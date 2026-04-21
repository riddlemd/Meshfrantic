namespace Meshfrantic.Services;

public class ThemeService
{
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

    public void SetTheme(string theme)
    {
        if (Themes.Keys.Contains(theme))
        {
            CurrentTheme = theme;
        }
    }
}
