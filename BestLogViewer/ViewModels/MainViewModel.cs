using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using BestLogViewer.Models;

namespace BestLogViewer.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly Services.AppSettings _settings;

    public ObservableCollection<KeywordRuleViewModel> Keywords { get; } = new();
    public ObservableCollection<ConversionRecord> Records { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    private bool _wholeWordOnly;
    public bool WholeWordOnly { get => _wholeWordOnly; set { if (SetProperty(ref _wholeWordOnly, value)) SaveSettings(); } }

    private bool _ignoreCase = true;
    public bool IgnoreCase { get => _ignoreCase; set { if (SetProperty(ref _ignoreCase, value)) SaveSettings(); } }

    private string _status = "Ready";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    private string _backgroundColor = "#111111";
    public string BackgroundColor { get => _backgroundColor; set { if (SetProperty(ref _backgroundColor, value)) SaveSettings(); OnPropertyChanged(nameof(BackgroundBrush)); } }

    private string _defaultTextColor = "#DDDDDD";
    public string DefaultTextColor { get => _defaultTextColor; set { if (SetProperty(ref _defaultTextColor, value)) SaveSettings(); OnPropertyChanged(nameof(DefaultTextBrush)); } }

    public System.Windows.Media.Brush BackgroundBrush => ToBrush(_backgroundColor);
    public System.Windows.Media.Brush DefaultTextBrush => ToBrush(_defaultTextColor);

    private static System.Windows.Media.Brush ToBrush(string color)
    {
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
            return new System.Windows.Media.SolidColorBrush(c);
        }
        catch
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }
    }

    private KeywordRuleViewModel? _selectedKeyword;
    public KeywordRuleViewModel? SelectedKeyword { get => _selectedKeyword; set => SetProperty(ref _selectedKeyword, value); }

    public ICommand AddKeywordCommand { get; }
    public ICommand RemoveKeywordCommand { get; }
    public ICommand OpenRecordCommand { get; }
    public ICommand DeleteRecordCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand CopyOriginalPathCommand { get; }
    public ICommand CopyOutputPathCommand { get; }
    public ICommand EditBackgroundColorCommand { get; }
    public ICommand EditDefaultTextColorCommand { get; }
    public ICommand EditKeywordColorCommand { get; }

    public MainViewModel()
    {
        _settings = Services.SettingsService.Load();

        foreach (var k in _settings.Keywords)
            Keywords.Add(new KeywordRuleViewModel(k.Keyword, k.ColorHex) { Scope = k.Scope });
        foreach (var r in _settings.Records.OrderByDescending(r => r.ConvertedAt))
            Records.Add(r);

        _wholeWordOnly = _settings.WholeWordOnly;
        _ignoreCase = _settings.IgnoreCase;
        _backgroundColor = _settings.BackgroundColor;
        _defaultTextColor = _settings.DefaultTextColor;

        AddKeywordCommand = new RelayCommand(_ => AddKeyword());
        RemoveKeywordCommand = new RelayCommand(obj =>
        {
            if (obj is KeywordRuleViewModel vm)
            {
                Keywords.Remove(vm);
                SaveSettings();
            }
        });
        OpenRecordCommand = new RelayCommand(obj =>
        {
            if (obj is ConversionRecord rec && File.Exists(rec.OutputPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rec.OutputPath) { UseShellExecute = true });
            }
        });
        DeleteRecordCommand = new RelayCommand(obj =>
        {
            if (obj is ConversionRecord rec)
            {
                try { if (File.Exists(rec.OutputPath)) File.Delete(rec.OutputPath); } catch { }
                Records.Remove(rec);
                SaveSettings();
            }
        });
        ClearLogsCommand = new RelayCommand(_ => Logs.Clear());
        CopyOriginalPathCommand = new RelayCommand(obj =>
        {
            if (obj is ConversionRecord rec && !string.IsNullOrEmpty(rec.OriginalPath))
            {
                try { System.Windows.Clipboard.SetText(rec.OriginalPath); } catch { }
            }
        });
        CopyOutputPathCommand = new RelayCommand(obj =>
        {
            if (obj is ConversionRecord rec && !string.IsNullOrEmpty(rec.OutputPath))
            {
                try { System.Windows.Clipboard.SetText(rec.OutputPath); } catch { }
            }
        });
        EditBackgroundColorCommand = new RelayCommand(_ =>
        {
            var picked = Services.ColorPickerService.PickColor(BackgroundColor);
            if (!string.IsNullOrWhiteSpace(picked)) BackgroundColor = picked!;
        });
        EditDefaultTextColorCommand = new RelayCommand(_ =>
        {
            var picked = Services.ColorPickerService.PickColor(DefaultTextColor);
            if (!string.IsNullOrWhiteSpace(picked)) DefaultTextColor = picked!;
        });
        EditKeywordColorCommand = new RelayCommand(obj =>
        {
            if (obj is KeywordRuleViewModel vm)
            {
                var picked = Services.ColorPickerService.PickColor(vm.ColorHex);
                if (!string.IsNullOrWhiteSpace(picked)) vm.ColorHex = picked!;
                SaveSettings();
            }
        });

        foreach (var vm in Keywords)
            vm.PropertyChanged += (_, __) => SaveSettings();
    }

    private void AddKeyword()
    {
        var vm = new KeywordRuleViewModel("ERROR", "#FF0000");
        vm.PropertyChanged += (_, __) => SaveSettings();
        Keywords.Add(vm);
        SaveSettings();
    }

    public void SaveSettings()
    {
        _settings.Keywords = Keywords.Select(k => new Models.KeywordRule { Keyword = k.Keyword, ColorHex = k.ColorHex, Scope = k.Scope }).ToList();
        _settings.Records = Records.ToList();
        _settings.WholeWordOnly = WholeWordOnly;
        _settings.IgnoreCase = IgnoreCase;
        _settings.BackgroundColor = BackgroundColor;
        _settings.DefaultTextColor = DefaultTextColor;
        Services.SettingsService.Save(_settings);
    }

    public void AddLog(string message)
    {
        Logs.Add(message);
        if (Logs.Count > 1000)
        {
            // prevent unbounded growth
            while (Logs.Count > 800) Logs.RemoveAt(0);
        }
    }
}
