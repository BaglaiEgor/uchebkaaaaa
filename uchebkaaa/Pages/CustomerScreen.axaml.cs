using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class CustomerScreen : UserControl
    {
        public CustomerScreen()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            MyOrdersButton.Click += MyOrdersButton_Click;
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }

        private void MyOrdersButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new OrdersPage());
        }
    }
}
