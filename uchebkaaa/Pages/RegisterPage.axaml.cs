using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages
{
    public partial class RegisterPage : UserControl
    {
        public RegisterPage()
        {
            InitializeComponent();
            RegisterButton.Click += RegisterButton_Click;
            BackButton.Click += BackButton_Click;
        }

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            var login = LoginTextBox.Text?.Trim() ?? "";
            var password = PasswordTextBox.Text ?? "";
            var name = NameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
            {
                ShowError("Заполните все поля");
                return;
            }

            if (!MainWindow.ValidatePassword(password))
            {
                ShowError("Пароль не соответствует требованиям");
                return;
            }

            if (MainWindow.RegisterCustomer(login, password, name))
            {
                MainWindow.NavigateTo(new LoginPage());
            }
            else
            {
                ShowError("Пользователь с таким логином уже существует");
            }
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new LoginPage());
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
        }
    }
}
