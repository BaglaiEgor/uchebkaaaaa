using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class LoginPage : UserControl
    {
        public LoginPage()
        {
            InitializeComponent();
            LoginButton.Click += LoginButton_Click;
            RegisterButton.Click += RegisterButton_Click;
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            var login = LoginTextBox.Text?.Trim() ?? "";
            var password = PasswordTextBox.Text ?? "";
            var rememberMe = RememberMeCheckBox.IsChecked ?? false;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            if (MainWindow.Login(login, password, rememberMe))
            {
                NavigateToRoleScreen();
            }
            else
            {
                ShowError("Неверный логин или пароль");
            }
        }

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new RegisterPage());
        }

        private void NavigateToRoleScreen()
        {
            var user = App.CurrentUser;
            if (user == null) return;

            UserControl? screen = user.Role switch
            {
                "Заказчик" => new CustomerScreen(),
                "Менеджер" => new ManagerScreen(),
                "Конструктор" => new ConstructorScreen(),
                "Мастер" => new MasterScreen(),
                "Директор" => new DirectorScreen(),
                _ => null
            };

            if (screen != null)
            {
                MainWindow.NavigateTo(screen);
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
        }
    }
}
