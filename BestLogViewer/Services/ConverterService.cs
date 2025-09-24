using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BestLogViewer.Models;

namespace BestLogViewer.Services;

/// <summary>
/// Converts a plain-text log file to an HTML file with color highlighting.
/// - Supports two highlight scopes per keyword rule:
///   - Word: only the matched keyword text is colored.
///   - Line: if a keyword matches a line, the entire line is colored.
/// - Honors global flags: wholeWord (word boundaries) and ignoreCase.
/// </summary>
public static class ConverterService
{
    /// <summary>
    /// Convert a log file to HTML, writing to outputDir/[name].html.
    /// The HTML uses a monospaced font and preserves whitespace.
    /// </summary>
    public static async Task<Models.ConversionRecord> ConvertAsync(
        string inputPath,
        string outputDir,
        List<KeywordRule> rules,
        bool wholeWord,
        bool ignoreCase)
    {
        if (!File.Exists(inputPath)) throw new FileNotFoundException("Input not found", inputPath);

        var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(inputPath) + ".html");

        // Split rules into two buckets by scope. We will:
        // 1) Detect a line color (if any) using original text and the Line rules
        // 2) Apply word-level highlighting using the Word rules on the HTML-escaped text
        var wordRules = new List<(string pattern, string color)>();
        var lineRules = new List<(string pattern, string color)>();
        foreach (var r in rules)
        {
            if (string.IsNullOrWhiteSpace(r.Keyword)) continue;
            // Escape the literal keyword so it can be used in a regex safely
            var escaped = Regex.Escape(r.Keyword);
            // Apply word-boundaries if the "whole word" option is enabled
            var pattern = wholeWord ? $"\\b{escaped}\\b" : escaped;
            var color = NormalizeColor(r.ColorHex);
            if (r.Scope == HighlightScope.Line)
            {
                lineRules.Add((pattern, color));
            }
            else
            {
                wordRules.Add((pattern, color));
            }
        }

        // Helper to build a single combined regex with capturing groups for each rule.
        // We keep a map of group-index -> color so we can know which color to apply when a group matches.
        (Regex? regex, Dictionary<int, string>? groupToColor) BuildCombined(List<(string pattern, string color)> items)
        {
            if (items.Count == 0) return (null, null);
            var sb = new StringBuilder();
            var map = new Dictionary<int, string>();
            sb.Append('(').Append(items[0].pattern).Append(')');
            map[1] = items[0].color;
            for (int i = 1; i < items.Count; i++)
            {
                sb.Append("|(").Append(items[i].pattern).Append(')');
                map[i + 1] = items[i].color;
            }
            var options = RegexOptions.Compiled;
            if (ignoreCase) options |= RegexOptions.IgnoreCase;
            return (new Regex(sb.ToString(), options), map);
        }

        var (wordRegex, wordMap) = BuildCombined(wordRules);
        var (lineRegex, lineMap) = BuildCombined(lineRules);

        using var input = new StreamReader(inputPath, detectEncodingFromByteOrderMarks: true);
        using var output = new StreamWriter(outputFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        // Theme colors for page background and default text are pulled from settings
        var bg = Services.SettingsService.Load().BackgroundColor;
        var fg = Services.SettingsService.Load().DefaultTextColor;

        await WriteHtmlHeaderAsync(output, inputPath, bg, fg);

        string? line;
        while ((line = await input.ReadLineAsync()) != null)
        {
            // First pass: detect a line-level color using the raw (unescaped) text so regex indexes align with the source.
            string? lineColor = null;
            if (lineRegex != null && lineMap != null)
            {
                var m = lineRegex.Match(line);
                if (m.Success)
                {
                    // Pick the first successful group; this gives the color of the first matching rule.
                    for (int g = 1; g < m.Groups.Count; g++)
                    {
                        if (m.Groups[g].Success) { lineColor = lineMap[g]; break; }
                    }
                }
            }

            // Second pass: escape for HTML and then apply word-level highlighting with <span> wrappers.
            // Order matters: escape first, then replace, to avoid corrupting HTML markup.
            var escaped = HtmlEscape(line);
            if (wordRegex != null && wordMap != null)
            {
                escaped = wordRegex.Replace(escaped, m =>
                {
                    for (int g = 1; g < m.Groups.Count; g++)
                    {
                        if (m.Groups[g].Success)
                        {
                            var color = wordMap[g];
                            return $"<span style=\"color:{color}\">{m.Value}</span>";
                        }
                    }
                    return m.Value;
                });
            }

            // Finally, write the line container. If a line color is present, apply it to the whole line.
            if (!string.IsNullOrEmpty(lineColor))
            {
                await output.WriteLineAsync($"<div class=\"l\" style=\"color:{lineColor}\">{escaped}</div>");
            }
            else
            {
                await output.WriteLineAsync($"<div class=\"l\">{escaped}</div>");
            }
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
