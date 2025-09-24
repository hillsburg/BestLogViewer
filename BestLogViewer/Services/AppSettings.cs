using BestLogViewer.Models;

namespace BestLogViewer.Services;

public class AppSettings
{
    public List<KeywordRule> Keywords { get; set; } = new(); // includes per-keyword Scope and ColorHex
    public List<ConversionRecord> Records { get; set; } = new();
    public bool WholeWordOnly { get; set; } = false;
    public bool IgnoreCase { get; set; } = true;
    public List<string> PaletteColors { get; set; } = new();
    public string BackgroundColor { get; set; } = "#111111";
    public string DefaultTextColor { get; set; } = "#DDDDDD";
}