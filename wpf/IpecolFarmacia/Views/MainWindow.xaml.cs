using System.Windows;
using System.Windows.Input;

namespace IpecolFarmacia.Views;

public partial class MainWindow : Window
{
    public MainWindow() : this(null)
    {
    }

    public MainWindow(Services.UsuarioLogueado? usuario)
    {
        InitializeComponent();
        if (usuario != null && DataContext is ViewModels.VentanillaViewModel vm)
        {
            vm.ConfigurarUsuario(usuario);
        }
    }

    private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Si el cursor está sobre un DataGrid, permitir que el DataGrid haga su propio scroll interno virtualizado
        var element = e.OriginalSource as DependencyObject;
        while (element != null && element != sender)
        {
            if (element is System.Windows.Controls.DataGrid)
            {
                return;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        var sv = sender as System.Windows.Controls.ScrollViewer;
        if (sv != null)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta / 2.5));
            e.Handled = true;
        }
    }

    private void TopLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnMinimizar_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaximizar_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnSalir_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Deseas salir del Sistema de Farmacia Institucional?", "Confirmar Salida · IPECOL", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        login.Show();
        Close();
    }

    private void BtnCambiarContrasenia_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.VentanillaViewModel vm && vm.PuedeVerGestionUsuarios)
        {
            vm.AbrirPestana("Usuarios");
        }
        else
        {
            MessageBox.Show("Para modificar contraseñas o permisos del personal, solicite el cambio al ADMINISTRADOR del sistema.", "Seguridad IPECOL", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
