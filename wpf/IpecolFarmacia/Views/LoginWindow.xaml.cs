using System.Windows;
using IpecolFarmacia.Services;

namespace IpecolFarmacia.Views;

public partial class LoginWindow : Window
{
    private readonly FarmaciaDbService _dbService = new();

    public LoginWindow() => InitializeComponent();

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        string user = UserBox.Text.Trim();
        string pass = PassBox.Password;

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ErrorText.Text = "Por favor ingresa tu usuario y contraseña.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Text = "Verificando en IUBIDB_FARMACIAS_PRUEBAS...";
        ErrorText.Visibility = Visibility.Visible;

        var (usuarioLogueado, mensajeError) = await _dbService.AutenticarUsuarioConValidacionAsync(user, pass);

        if (usuarioLogueado == null)
        {
            ErrorText.Text = mensajeError;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var mainWindow = new MainWindow(usuarioLogueado);
        mainWindow.Show();
        Close();
    }

    private void PowerOff_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
