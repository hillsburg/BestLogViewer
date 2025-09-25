using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BestLogViewer.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        public string AppVersion { get; } = "1.0.0";

        public event PropertyChangedEventHandler? PropertyChanged;


        private string _latestVersion = "1.0.6";

        public AboutViewModel()
        {
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set => SetField(ref _latestVersion, value);
        }

        private bool _isUpdateNeeded = false;

        public bool IsUpdateNeeded
        {
            get => _isUpdateNeeded;
            set => SetField(ref _isUpdateNeeded, value);
        }

        public ICommand OpenUrlCommand => new RelayCommand(OpenUrl);

        private void OpenUrl(object obj)
        {
            string url = "https://github.com/hillsburg/BestLogViewer/releases";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
