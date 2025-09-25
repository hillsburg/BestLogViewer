using Microsoft.Data.Sqlite;
using System.IO;

namespace BestLogViewer.Services.Data;

public static class SqliteDatabase
{
    public static string DbPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.db");

    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        return conn;
    }

    public static void EnsureCreated()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA foreign_keys=ON;
CREATE TABLE IF NOT EXISTS Settings (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    WholeWordOnly INTEGER NOT NULL DEFAULT 0,
    IgnoreCaseLegacy INTEGER NOT NULL DEFAULT 1,
    BackgroundColor TEXT NOT NULL,
    DefaultTextColor TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS KeywordRule (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Keyword TEXT NOT NULL,
    ColorHex TEXT NOT NULL,
    Scope INTEGER NOT NULL,
    IgnoreCase INTEGER NOT NULL DEFAULT 1,
    DisplayOrder INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_KeywordRule_Keyword ON KeywordRule(Keyword);
CREATE TABLE IF NOT EXISTS PaletteColor (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ColorHex TEXT NOT NULL,
    Position INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS ConversionRecord (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OriginalPath TEXT NOT NULL,
    OutputPath TEXT NOT NULL,
    ConvertedAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_ConversionRecord_ConvertedAt ON ConversionRecord(ConvertedAt);
INSERT INTO Settings(Id, WholeWordOnly, IgnoreCaseLegacy, BackgroundColor, DefaultTextColor)
    SELECT 1, 0, 1, '#111111', '#DDDDDD'
    WHERE NOT EXISTS(SELECT 1 FROM Settings WHERE Id = 1);
";
        cmd.ExecuteNonQuery();
    }
}
