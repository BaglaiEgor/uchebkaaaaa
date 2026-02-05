using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class DirectorScreen : UserControl
    {
        public DirectorScreen()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            EmployeesButton.Click += EmployeesButton_Click;
            MaterialsButton.Click += MaterialsButton_Click;
            AccessoriesButton.Click += AccessoriesButton_Click;
            OrdersButton.Click += OrdersButton_Click;
            WorkshopsButton.Click += WorkshopsButton_Click;
            ReportButton.Click += (_, _) => MainWindow.NavigateTo(new InventoryReportPage());
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }

        private void EmployeesButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new EmployeeManagementPage());
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

        private void WorkshopsButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new WorkshopPlanPage());
        }
    }
}
