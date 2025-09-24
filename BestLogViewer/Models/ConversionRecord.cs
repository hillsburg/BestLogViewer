namespace BestLogViewer.Models;

public class ConversionRecord
{
    public string OriginalPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public DateTime ConvertedAt { get; set; } = DateTime.Now;
}
