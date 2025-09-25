using Microsoft.Data.Sqlite;
using BestLogViewer.Models;

namespace BestLogViewer.Services.Data;

public class SettingsDao
{
    private readonly SqliteConnection _conn;

    public SettingsDao(SqliteConnection conn)
    {
        _conn = conn;
    }

    public (bool wholeWord, bool ignoreLegacy, string bg, string fg)? LoadSettings()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT WholeWordOnly, IgnoreCaseLegacy, BackgroundColor, DefaultTextColor FROM Settings WHERE Id = 1";
        using var rdr = cmd.ExecuteReader();
        if (!rdr.Read()) return null;
        return (
            rdr.GetInt32(0) != 0,
            rdr.GetInt32(1) != 0,
            rdr.GetString(2),
            rdr.GetString(3)
        );
    }

    public void UpsertSettings(bool wholeWord, bool ignoreLegacy, string bg, string fg)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Settings (Id, WholeWordOnly, IgnoreCaseLegacy, BackgroundColor, DefaultTextColor)
VALUES (1, $whole, $ignore, $bg, $fg)
ON CONFLICT(Id) DO UPDATE SET
    WholeWordOnly = excluded.WholeWordOnly,
    IgnoreCaseLegacy = excluded.IgnoreCaseLegacy,
    BackgroundColor = excluded.BackgroundColor,
    DefaultTextColor = excluded.DefaultTextColor;";
        cmd.Parameters.AddWithValue("$whole", wholeWord ? 1 : 0);
        cmd.Parameters.AddWithValue("$ignore", ignoreLegacy ? 1 : 0);
        cmd.Parameters.AddWithValue("$bg", bg);
        cmd.Parameters.AddWithValue("$fg", fg);
        cmd.ExecuteNonQuery();
    }

    public List<KeywordRule> LoadKeywordRules()
    {
        var list = new List<KeywordRule>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Keyword, ColorHex, Scope, IgnoreCase FROM KeywordRule ORDER BY DisplayOrder, Id";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new KeywordRule
            {
                Keyword = rdr.GetString(0),
                ColorHex = rdr.GetString(1),
                Scope = (HighlightScope)rdr.GetInt32(2),
                IgnoreCase = rdr.GetInt32(3) != 0
            });
        }
        return list;
    }

    public void ReplaceKeywordRules(IReadOnlyList<KeywordRule> rules)
    {
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM KeywordRule";
            cmd.ExecuteNonQuery();
        }
        for (int i = 0; i < rules.Count; i++)
        {
            var r = rules[i];
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO KeywordRule(Keyword, ColorHex, Scope, IgnoreCase, DisplayOrder)
VALUES ($k, $c, $s, $i, $o)";
            cmd.Parameters.AddWithValue("$k", r.Keyword ?? string.Empty);
            cmd.Parameters.AddWithValue("$c", r.ColorHex ?? "#FF0000");
            cmd.Parameters.AddWithValue("$s", (int)r.Scope);
            cmd.Parameters.AddWithValue("$i", r.IgnoreCase ? 1 : 0);
            cmd.Parameters.AddWithValue("$o", i);
            cmd.ExecuteNonQuery();
        }
    }

    public List<string> LoadPalette()
    {
        var list = new List<string>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT ColorHex FROM PaletteColor ORDER BY Position, Id";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) list.Add(rdr.GetString(0));
        return list;
    }

    public void ReplacePalette(IReadOnlyList<string> colors)
    {
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM PaletteColor";
            cmd.ExecuteNonQuery();
        }
        for (int i = 0; i < colors.Count; i++)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO PaletteColor(ColorHex, Position) VALUES ($c, $p)";
            cmd.Parameters.AddWithValue("$c", colors[i] ?? "#FFFFFF");
            cmd.Parameters.AddWithValue("$p", i);
            cmd.ExecuteNonQuery();
        }
    }

    public List<ConversionRecord> LoadRecords()
    {
        var list = new List<ConversionRecord>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT OriginalPath, OutputPath, ConvertedAt FROM ConversionRecord ORDER BY ConvertedAt DESC, Id DESC";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            var at = DateTime.TryParse(rdr.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt
                : DateTime.Now;
            list.Add(new ConversionRecord
            {
                OriginalPath = rdr.GetString(0),
                OutputPath = rdr.GetString(1),
                ConvertedAt = at
            });
        }
        return list;
    }

    public void ReplaceRecords(IReadOnlyList<ConversionRecord> records)
    {
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM ConversionRecord";
            cmd.ExecuteNonQuery();
        }
        foreach (var rec in records)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO ConversionRecord(OriginalPath, OutputPath, ConvertedAt)
VALUES ($op, $out, $at)";
            cmd.Parameters.AddWithValue("$op", rec.OriginalPath ?? string.Empty);
            cmd.Parameters.AddWithValue("$out", rec.OutputPath ?? string.Empty);
            cmd.Parameters.AddWithValue("$at", rec.ConvertedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }
}
