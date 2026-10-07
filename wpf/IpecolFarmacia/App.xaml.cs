using System;
using System.IO;
using System.Windows;
using IpecolFarmacia.Views;

namespace IpecolFarmacia;

public partial class App : Application
{
    public App()
    {
        string logPath = @"C:\Users\Ipecol\Documents\debug_app.txt";
        File.AppendAllText(logPath, $"[{DateTime.Now}] App() constructor called\n");

        DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] DISPATCHER EXCEPTION: {e.Exception}\n");
                MessageBox.Show($"Ocurrió un error en la aplicación:\n\n{e.Exception.Message}\n\nDetalle:\n{e.Exception.StackTrace}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // Previene que la aplicación se cierre abruptamente
            }
            catch { }
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] APPDOMAIN EXCEPTION: {e.ExceptionObject}\n");
            }
            catch { }
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        string logPath = @"C:\Users\Ipecol\Documents\debug_app.txt";
        File.AppendAllText(logPath, $"[{DateTime.Now}] OnStartup called\n");

        base.OnStartup(e);

        try
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] Creating LoginWindow\n");
            var login = new LoginWindow();
            login.Closing += (s, ev) => File.AppendAllText(logPath, $"[{DateTime.Now}] LoginWindow Closing, Cancel={ev.Cancel}\n");
            login.Closed += (s, ev) => File.AppendAllText(logPath, $"[{DateTime.Now}] LoginWindow Closed\n");
            MainWindow = login;
            login.Topmost = true;
            login.Show();
            login.Activate();
            login.Focus();
            login.Topmost = false; // allow normal window behavior after bringing to front
            File.AppendAllText(logPath, $"[{DateTime.Now}] LoginWindow shown successfully\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] EXCEPTION: {ex}\n");
            MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
