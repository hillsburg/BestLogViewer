using System.IO;
using Microsoft.Data.Sqlite;
using BestLogViewer.Models;
using BestLogViewer.Services.Data;

namespace BestLogViewer.Services;

public static class SettingsService
{
    public static AppSettings Load()
    {
        try
        {
            SqliteDatabase.EnsureCreated();
            using var conn = SqliteDatabase.OpenConnection();
            var dao = new SettingsDao(conn);

            var row = dao.LoadSettings();
            var settings = row is null ? CreateDefault() : new AppSettings
            {
                WholeWordOnly = row.Value.wholeWord,
                IgnoreCase = row.Value.ignoreLegacy,
                BackgroundColor = row.Value.bg,
                DefaultTextColor = row.Value.fg,
                Keywords = new(),
                PaletteColors = new(),
                Records = new()
            };

            settings.Keywords = dao.LoadKeywordRules();
            settings.PaletteColors = dao.LoadPalette();
            settings.Records = dao.LoadRecords();

            return settings;
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
            SqliteDatabase.EnsureCreated();
            using var conn = SqliteDatabase.OpenConnection();
            using var tx = conn.BeginTransaction();
            var dao = new SettingsDao(conn);

            dao.UpsertSettings(settings.WholeWordOnly, settings.IgnoreCase, settings.BackgroundColor ?? "#111111", settings.DefaultTextColor ?? "#DDDDDD");
            dao.ReplaceKeywordRules(settings.Keywords);
            dao.ReplacePalette(settings.PaletteColors);
            dao.ReplaceRecords(settings.Records);

            tx.Commit();
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
            Keywords = new List<KeywordRule>
            {
                new() { Keyword = "ERROR", ColorHex = "#FF0000" },
                new() { Keyword = "WARN", ColorHex = "#FFA500" },
                new() { Keyword = "INFO", ColorHex = "#008000" }
            },
            IgnoreCase = true,
            WholeWordOnly = false,
            PaletteColors = new List<string> { "#FF0000", "#FFA500", "#FFFF00", "#008000", "#00CED1", "#1E90FF", "#800080", "#FF1493", "#FFFFFF", "#C0C0C0", "#808080", "#000000", "#8B4513", "#00FF00", "#ADD8E6", "#FFD700" },
            BackgroundColor = "#111111",
            DefaultTextColor = "#DDDDDD",
            Records = new()
        };
    }
}
