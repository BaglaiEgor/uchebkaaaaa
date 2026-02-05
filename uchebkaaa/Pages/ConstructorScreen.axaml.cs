using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class ConstructorScreen : UserControl
    {
        public ConstructorScreen()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            OrdersButton.Click += (_, _) => MainWindow.NavigateTo(new OrdersPage());
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }
    }
}
