using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BestLogViewer.Models;

namespace BestLogViewer.Services;

public static class ConverterService
{
    /// <summary>
    /// Convert a log file to HTML, writing to outputDir/[name]_[yyyyMMdd_HHmmss].html.
    /// The HTML uses a monospaced font and preserves whitespace.
    /// </summary>
    public static async Task<Models.ConversionRecord> ConvertAsync(
        string inputPath,
        string outputDir,
        List<KeywordRule> rules,
        bool wholeWord)
    {
        if (!File.Exists(inputPath)) throw new FileNotFoundException("Input not found", inputPath);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputFile = Path.Combine(outputDir, $"{baseName}_{stamp}.html");

        // Split rules into two buckets by scope. We will:
        // 1) Detect a line color (if any) using original text and the Line rules
        // 2) Apply word-level highlighting using the Word rules on the HTML-escaped text
        var wordRules = new List<(Regex regex, string color)>();
        var lineRules = new List<(Regex regex, string color)>();
        foreach (var r in rules)
        {
            if (string.IsNullOrWhiteSpace(r.Keyword)) continue;
            var escaped = Regex.Escape(r.Keyword);
            var pattern = wholeWord ? $"\\b{escaped}\\b" : escaped;
            var color = NormalizeColor(r.ColorHex);
            var options = RegexOptions.Compiled | (r.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            var rx = new Regex(pattern, options);
            if (r.Scope == HighlightScope.Line)
            {
                lineRules.Add((rx, color));
            }
            else
            {
                wordRules.Add((rx, color));
            }
        }

        using var input = new StreamReader(inputPath, detectEncodingFromByteOrderMarks: true);
        using var output = new StreamWriter(outputFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        // Theme colors for page background and default text are pulled from settings
        var bg = Services.SettingsService.Load().BackgroundColor;
        var fg = Services.SettingsService.Load().DefaultTextColor;

        await WriteHtmlHeaderAsync(output, inputPath, bg, fg);

        string? line;
        while ((line = await input.ReadLineAsync()) != null)
        {
            // Determine line color based on first matching line-scoped rule
            string? lineColor = null;
            foreach (var (rx, color) in lineRules)
            {
                if (rx.IsMatch(line)) { lineColor = color; break; }
            }

            // Escape and apply word rules
            var escaped = HtmlEscape(line);
            foreach (var (rx, color) in wordRules)
            {
                escaped = rx.Replace(escaped, m => $"<span style=\"color:{color}\">{m.Value}</span>");
            }

            if (!string.IsNullOrEmpty(lineColor))
                await output.WriteLineAsync($"<div class=\"l\" style=\"color:{lineColor}\">{escaped}</div>");
            else
                await output.WriteLineAsync($"<div class=\"l\">{escaped}</div>");
        }

        await WriteHtmlFooterAsync(output);

        return new Models.ConversionRecord
        {
            OriginalPath = inputPath,
            OutputPath = outputFile,
            ConvertedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Normalizes color strings by dropping the alpha channel if provided (#AARRGGBB -> #RRGGBB).
    /// Accepts named colors as-is and falls back to black on invalid input.
    /// </summary>
    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "#000000";
        color = color.Trim();
        if (!color.StartsWith('#')) return color; // allow named colors
        if (color.Length == 7 || color.Length == 4) return color; // #RRGGBB or #RGB
        if (color.Length == 9) return "#" + color.Substring(3); // drop alpha from #AARRGGBB
        return "#000000";
    }

    /// <summary>
    /// Writes a minimal HTML header with inline styles to apply the chosen theme.
    /// </summary>
    private static async Task WriteHtmlHeaderAsync(StreamWriter w, string title, string bg, string fg)
    {
        await w.WriteLineAsync("<!DOCTYPE html>");
        await w.WriteLineAsync("<html lang=\"en\">");
        await w.WriteLineAsync("<head>");
        await w.WriteLineAsync("<meta charset=\"utf-8\" />");
        await w.WriteLineAsync($"<title>{HtmlEscape(Path.GetFileName(title))} - Best Log Viewer</title>");
        await w.WriteLineAsync($"<style>body{{font-family:Consolas,monospace;background:{bg};color:{fg};margin:0}} .l{{white-space:pre; padding:0 8px;}}</style>");
        await w.WriteLineAsync("</head>");
        await w.WriteLineAsync("<body>");
        await w.WriteLineAsync($"<h3 style=\"margin:8px\">{HtmlEscape(Path.GetFileName(title))}</h3>");
    }

    /// <summary>
    /// Closes the HTML document.
    /// </summary>
    private static async Task WriteHtmlFooterAsync(StreamWriter w)
    {
        await w.WriteLineAsync("</body></html>");
    }

    /// <summary>
    /// HTML-escapes a string (minimal, fast implementation).
    /// </summary>
    private static string HtmlEscape(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length + 16);
        foreach (var ch in text)
        {
            sb.Append(ch switch
            {
                '<' => "&lt;",
                '>' => "&gt;",
                '&' => "&amp;",
                '"' => "&quot;",
                '\'' => "&#39;",
                _ => ch.ToString()
            });
        }
        return sb.ToString();
    }
}
