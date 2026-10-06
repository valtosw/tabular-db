using System.Net.Http;
using System.Windows;
using TabularDB.Desktop.Services;
using TabularDB.Desktop.ViewModels;

namespace TabularDB.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs eventArguments)
        {
            base.OnStartup(eventArguments);
            var serverAddress = Environment.GetEnvironmentVariable("TABULARDB_API_URL") ?? "http://localhost:5194/";
            var normalizedServerAddress = serverAddress.EndsWith('/') ? serverAddress : $"{serverAddress}/";
            var httpClient = new HttpClient { BaseAddress = new Uri(normalizedServerAddress) };
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(new DatabaseApiClient(httpClient), new DatabaseFileDialogService())
            };
            window.Show();
        }
    }
}
