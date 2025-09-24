namespace BestLogViewer.Services;

public static class ColorPickerService
{
    public static string? PickColor(string? initialHex = null)
    {
        using var dlg = new ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true
        };

        if (TryParse(initialHex, out var initial))
        {
            dlg.Color = System.Drawing.Color.FromArgb(initial.A, initial.R, initial.G, initial.B);
        }

        return dlg.ShowDialog() == DialogResult.OK
            ? ToHex(dlg.Color)
            : null;
    }

    private static bool TryParse(string? hex, out System.Windows.Media.Color color)
    {
        color = System.Windows.Media.Colors.Transparent;
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                color = c; return true;
            }
        }
        catch { }
        return false;
    }

    private static string ToHex(System.Drawing.Color c)
    {
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
