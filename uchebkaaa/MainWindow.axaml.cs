using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using uchebkaaa.Data;
using uchebkaaa.Pages;

namespace uchebkaaa
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        private const string CredentialsFile = "credentials.json";

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Try auto-login after window is fully initialized
            if (TryAutoLogin())
            {
                NavigateToRoleScreen();
            }
            else
            {
                NavigateTo(new LoginPage());
            }
        }

        public static void NavigateTo(UserControl page)
        {
            if (Instance != null)
            {
                Instance.mainContent.Content = page;
            }
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
                NavigateTo(screen);
            }
        }

        // Методы авторизации
        public static bool TryAutoLogin()
        {
            var credentials = LoadCredentials();
            if (credentials == null)
            {
                return false;
            }

            return Login(credentials.Value.Login, credentials.Value.Password, true);
        }

        public static bool Login(string login, string password, bool rememberMe)
        {
            var user = App.DbContext.Users.FirstOrDefault(u => u.Login == login);
            if (user == null || user.Password != password)
            {
                return false;
            }

            App.CurrentUser = user;

            if (rememberMe)
            {
                SaveCredentials(login, password);
            }
            else
            {
                ClearCredentials();
            }

            return true;
        }

        public static void Logout()
        {
            App.CurrentUser = null;
            ClearCredentials();
        }

        public static bool RegisterCustomer(string login, string password, string name)
        {
            if (App.DbContext.Users.Any(u => u.Login == login))
            {
                return false;
            }

            var user = new User
            {
                Login = login,
                Password = password,
                Name = name,
                Role = "Заказчик"
            };

            App.DbContext.Users.Add(user);
            App.DbContext.SaveChanges();

            return true;
        }

        public static bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < 4 || password.Length > 16)
                return false;

            if (password.Any(c => "*&{}|+".Contains(c)))
                return false;

            if (!password.Any(char.IsUpper))
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            return true;
        }

        private static void SaveCredentials(string login, string password)
        {
            try
            {
                var credentials = new { Login = login, Password = password };
                var json = JsonSerializer.Serialize(credentials);
                File.WriteAllText(CredentialsFile, json);
            }
            catch { }
        }

        private static (string Login, string Password)? LoadCredentials()
        {
            try
            {
                if (!File.Exists(CredentialsFile))
                    return null;

                var json = File.ReadAllText(CredentialsFile);
                var credentials = JsonSerializer.Deserialize<CredentialsData>(json);
                if (credentials != null)
                {
                    return (credentials.Login, credentials.Password);
                }
            }
            catch { }
            return null;
        }

        private static void ClearCredentials()
        {
            try
            {
                if (File.Exists(CredentialsFile))
                    File.Delete(CredentialsFile);
            }
            catch { }
        }

        private class CredentialsData
        {
            public string Login { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
    public static class PdfHelper
    {
        public static void SaveControlToPdf(Control control, string filePath)
        {
            if (control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
                return;

            var pixelSize = new PixelSize(
                (int)control.Bounds.Width,
                (int)control.Bounds.Height
            );

            var dpi = new Avalonia.Vector(96, 96);
            using var bitmap = new RenderTargetBitmap(pixelSize, dpi);
            bitmap.Render(control);

            using var ms = new MemoryStream();
            bitmap.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var img = XImage.FromStream(() => new MemoryStream(ms.ToArray()));

            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = img.PixelWidth * 72 / 96.0;
            page.Height = img.PixelHeight * 72 / 96.0;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(img, 0, 0, page.Width, page.Height);

            document.Save(filePath);
        }
    }
}
