using System.Windows;

namespace CSYetiTools.BranchViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            var path = e.Args.Length > 0 ? e.Args[0] : "./data";
            new MainWindow(path).Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            AdonisUI.Controls.MessageBox.Show(e.ToString()!, "Error", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
