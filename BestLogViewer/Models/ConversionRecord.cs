using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

namespace BestLogViewer.Models;

public class ConversionRecord : INotifyPropertyChanged
{
    private string _originalPath = string.Empty;
    private string _outputPath = string.Empty;
    private DateTime _convertedAt = DateTime.Now;

    public string OriginalPath
    {
        get => _originalPath;
        set { if (_originalPath != value) { _originalPath = value; OnPropertyChanged(); } }
    }

    public string OutputPath
    {
        get => _outputPath;
        set
        {
            if (_outputPath != value)
            {
                _outputPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OutputFileName));
            }
        }
    }

    public string OutputFileName => Path.GetFileName(_outputPath);

    public DateTime ConvertedAt
    {
        get => _convertedAt;
        set { if (_convertedAt != value) { _convertedAt = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
