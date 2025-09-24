using BestLogViewer.Models;
using System.Windows.Media;

namespace BestLogViewer.ViewModels;

public class KeywordRuleViewModel : ObservableObject
{
    private string _keyword;
    private string _colorHex;
    private HighlightScope _scope = HighlightScope.Word;

    public string Keyword { get => _keyword; set { if (SetProperty(ref _keyword, value)) OnPropertyChanged(nameof(Brush)); } }
    public string ColorHex { get => _colorHex; set { if (SetProperty(ref _colorHex, value)) OnPropertyChanged(nameof(Brush)); } }

    public HighlightScope Scope { get => _scope; set => SetProperty(ref _scope, value); }

    public SolidColorBrush Brush
    {
        get
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_colorHex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }
    }

    public KeywordRuleViewModel(string keyword, string colorHex)
    {
        _keyword = keyword;
        _colorHex = colorHex;
    }
}
