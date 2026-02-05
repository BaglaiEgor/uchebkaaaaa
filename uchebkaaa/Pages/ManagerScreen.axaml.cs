using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class ManagerScreen : UserControl
    {
        public ManagerScreen()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            MaterialsButton.Click += MaterialsButton_Click;
            AccessoriesButton.Click += AccessoriesButton_Click;
            OrdersButton.Click += OrdersButton_Click;
            EstimateButton.Click += (_, _) => MainWindow.NavigateTo(new MaterialsEstimatePage());
            ReportButton.Click += (_, _) => MainWindow.NavigateTo(new InventoryReportPage());
            GanttButton.Click += (_, _) => MainWindow.NavigateTo(new GanttChartPage());
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }

        private void MaterialsButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new MaterialsPage());
        }

        private void AccessoriesButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new AccessoriesPage());
        }

        private void OrdersButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new OrdersPage());
        }
    }
}
