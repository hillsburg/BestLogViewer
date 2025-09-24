using System.IO;
using System.Reflection;
using System.Windows;
using BestLogViewer.ViewModels;

namespace BestLogViewer
{
    public partial class MainWindow : Window
    {
        private MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files.Length == 0) return;
            await ConvertFileAsync(files[0]);
        }

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                e.Effects = files.Length > 0 && File.Exists(files[0]) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async Task ConvertFileAsync(string inputPath)
        {
            try
            {
                VM.Status = "Converting...";
                // Pass Scope so converter can honor per-keyword Word/Line behavior
                var record = await Services.ConverterService.ConvertAsync(
                    inputPath,
                    Path.GetDirectoryName(inputPath)!, // output same folder as input
                    VM.Keywords.Select(k => new Models.KeywordRule { Keyword = k.Keyword, ColorHex = k.ColorHex, Scope = k.Scope }).ToList(),
                    VM.WholeWordOnly,
                    VM.IgnoreCase);
                VM.Records.Insert(0, record);
                VM.SaveSettings();
                VM.Status = $"Converted to {record.OutputPath}";
            }
            catch (Exception ex)
            {
                VM.Status = "Conversion failed";
                VM.AddLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                // Center on owner by passing 'this' as owner
                System.Windows.MessageBox.Show(this,
                    ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Log File",
                Filter = "Text files (*.txt;*.log)|*.txt;*.log|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) == true)
            {
                _ = ConvertFileAsync(dlg.FileName);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version?.ToString() ?? "1.0.0.0";
            var text = $"Best Log Viewer\nVersion: {version}\n\nDrag a log, set keyword colors, and convert to HTML for highlighting.";
            // Center on owner by passing 'this' as owner
            System.Windows.MessageBox.Show(this, text, "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}