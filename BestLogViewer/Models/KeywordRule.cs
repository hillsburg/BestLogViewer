namespace BestLogViewer.Models;

public enum HighlightScope
{
    Word,
    Line
}

public static class HighlightScopeValues
{
    public static HighlightScope[] All { get; } = new[] { HighlightScope.Word, HighlightScope.Line };
}

public class KeywordRule
{
    public string Keyword { get; set; } = string.Empty;
    // Expect formats like #RRGGBB or #AARRGGBB; we will normalize to #RRGGBB when used
    public string ColorHex { get; set; } = "#FF0000";
    public HighlightScope Scope { get; set; } = HighlightScope.Word;
}
