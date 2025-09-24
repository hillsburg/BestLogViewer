using System.IO;
using System.Text.Json;

namespace BestLogViewer.Services;

public static class SettingsService
{
    private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BestLogViewer");
    private static readonly string SettingsPath = Path.Combine(AppDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return CreateDefault();
            }
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return settings ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // ignore
        }
    }

    private static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            Keywords = new List<Models.KeywordRule>
            {
                new() { Keyword = "ERROR", ColorHex = "#FF0000" },
                new() { Keyword = "WARN", ColorHex = "#FFA500" },
                new() { Keyword = "INFO", ColorHex = "#008000" }
            },
            IgnoreCase = true,
            WholeWordOnly = false,
            PaletteColors = new List<string> { "#FF0000", "#FFA500", "#FFFF00", "#008000", "#00CED1", "#1E90FF", "#800080", "#FF1493", "#FFFFFF", "#C0C0C0", "#808080", "#000000", "#8B4513", "#00FF00", "#ADD8E6", "#FFD700" },
            BackgroundColor = "#111111",
            DefaultTextColor = "#DDDDDD"
        };
    }
}
