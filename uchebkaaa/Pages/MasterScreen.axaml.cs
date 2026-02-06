using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class MasterScreen : UserControl
    {
        public MasterScreen()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            FailuresButton.Click += (_, _) => MainWindow.NavigateTo(new EquipmentFailuresPage());
            //QualityButton.Click += (_, _) => MainWindow.NavigateTo(new QualityControlPage());
            SpecsButton.Click += (_, _) => MainWindow.NavigateTo(new ProductSpecPage());
            OrderButton.Click += (_, _) => MainWindow.NavigateTo(new OrdersPage());
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }
    }
}
