using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using uchebkaaa.Data;

namespace uchebkaaa.Pages
{
    public partial class OrderEditDialog : Window
    {
        private readonly Order? _order;

        private class SizeItem
        {
            public string Name { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private class OrderExtraData
        {
            public string Description { get; set; } = string.Empty;
            public List<SizeItem> Sizes { get; set; } = new();
            public List<string> SchemaFiles { get; set; } = new();
        }

        public OrderEditDialog(Order? order)
        {
            _order = order;
            InitializeComponent();

            OkButton.Click += OkButton_Click;
            CancelButton.Click += CancelButton_Click;
            AddSchemaButton.Click += AddSchemaButton_Click;

            LoadLookups();
            LoadOrderData();
        }

        private void LoadLookups()
        {
            var db = App.DbContext;

            var customers = db.Users
                .Where(u => u.Role == "Заказчик")
                .OrderBy(u => u.Name)
                .ToList();

            CustomerComboBox.ItemsSource = customers;

            var products = db.Products
                .OrderBy(p => p.Name)
                .ToList();
            ProductComboBox.ItemsSource = products;

            if (App.CurrentUser?.Role == "Заказчик")
            {
                CustomerComboBox.IsEnabled = false;
                var self = customers.FirstOrDefault(c => c.Login == App.CurrentUser.Login);
                if (self != null)
                {
                    CustomerComboBox.SelectedItem = self;
                }
            }
        }

        private string GetExtraDataPath(DateOnly date, int number)
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "OrderData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"order_{date:yyyyMMdd}_{number}.json");
        }

        private OrderExtraData LoadExtraData(DateOnly date, int number)
        {
            var path = GetExtraDataPath(date, number);
            if (!File.Exists(path))
                return new OrderExtraData();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<OrderExtraData>(json) ?? new OrderExtraData();
            }
            catch
            {
                return new OrderExtraData();
            }
        }

        private void SaveExtraData(DateOnly date, int number, OrderExtraData data)
        {
            var path = GetExtraDataPath(date, number);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        private void LoadOrderData()
        {
            var db = App.DbContext;

            if (_order == null)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                OrderDateTextBlock.Text = today.ToString("dd.MM.yyyy");
                OrderNumberTextBlock.Text = "Будет сгенерирован автоматически";
                EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);
                CostTextBox.Text = string.Empty;

                SizesDataGrid.ItemsSource = new List<SizeItem>();
                return;
            }

            var formatted = OrderStatusHelper.GetFormattedOrderNumber(db, _order);
            OrderNumberTextBlock.Text = formatted;
            OrderDateTextBlock.Text = _order.Date.ToString("dd.MM.yyyy");

            NameTextBox.Text = _order.Name;
            EndDatePicker.SelectedDate = _order.EndDate.ToDateTime(TimeOnly.Parse("00:00"));
            if (_order.Cost > 0)
            {
                CostTextBox.Text = _order.Cost.ToString("0.##");
            }
            else
            {
                CostTextBox.Text = string.Empty;
            }

            // Заказчик
            var customers = (List<User>)CustomerComboBox.ItemsSource!;
            var customer = customers.FirstOrDefault(c => c.Login == _order.CustomerId);
            if (customer != null)
                CustomerComboBox.SelectedItem = customer;

            // Изделие
            var products = (List<Product>)ProductComboBox.ItemsSource!;
            var product = products.FirstOrDefault(p => p.Name == _order.ProductId);
            if (product != null)
                ProductComboBox.SelectedItem = product;

            // Дополнительные данные
            var extra = LoadExtraData(_order.Date, _order.Number);
            DescriptionTextBox.Text = extra.Description;
            SizesDataGrid.ItemsSource = new List<SizeItem>(extra.Sizes);
            SchemasListBox.ItemsSource = new List<string>(extra.SchemaFiles);
        }

        private async void AddSchemaButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Title = "Выберите файлы схем/чертежей"
            };

            var result = await dialog.ShowAsync(this);
            if (result == null || result.Length == 0)
                return;

            var list = SchemasListBox.ItemsSource as List<string> ?? new List<string>();
            list.AddRange(result);
            SchemasListBox.ItemsSource = null;
            SchemasListBox.ItemsSource = list;
        }

        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ErrorTextBlock.Text = "Укажите наименование заказа.";
                return;
            }

            if (ProductComboBox.SelectedItem is not Product product)
            {
                ErrorTextBlock.Text = "Выберите изделие.";
                return;
            }

            if (App.CurrentUser?.Role == "Менеджер" &&
                CustomerComboBox.SelectedItem is not User)
            {
                ErrorTextBlock.Text = "Выберите заказчика.";
                return;
            }

            var db = App.DbContext;

            Order order;
            bool isNew = _order == null;

            if (isNew)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Определяем следующий номер для текущей даты
                var maxNumber = db.Orders
                    .Where(o => o.Date == today)
                    .Select(o => (int?)o.Number)
                    .Max() ?? 0;

                var newNumber = maxNumber + 1;

                order = new Order
                {
                    Date = today,
                    Number = newNumber
                };

                // Заказчик
                if (App.CurrentUser?.Role == "Заказчик")
                {
                    order.CustomerId = App.CurrentUser.Login;
                }
                else if (CustomerComboBox.SelectedItem is User selectedCustomer)
                {
                    order.CustomerId = selectedCustomer.Login;
                }
                else
                {
                    ErrorTextBlock.Text = "Не удалось определить заказчика.";
                    return;
                }

                // Менеджер
                if (App.CurrentUser?.Role == "Менеджер")
                {
                    order.ManagerId = App.CurrentUser.Login;
                }
                else
                {
                    // Для нового заказа, созданного не менеджером, необходимо указать существующего менеджера,
                    // иначе будет нарушение внешнего ключа FK_Orders_Users_ManagerId.
                    var defaultManager = db.Users.FirstOrDefault(u => u.Role == "Менеджер");
                    if (defaultManager == null)
                    {
                        ErrorTextBlock.Text = "В системе не найден ни один пользователь с ролью \"Менеджер\". Обратитесь к администратору.";
                        return;
                    }

                    order.ManagerId = defaultManager.Login;
                }

                db.Orders.Add(order);

                // Статус по умолчанию
                if (App.CurrentUser?.Role == "Менеджер")
                {
                    OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusSpecification, "Менеджер создал заказ");
                }
                else
                {
                    OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusNew, "Клиент создал заказ");
                }
            }
            else
            {
                order = _order!;
            }

            order.Name = NameTextBox.Text.Trim();
            order.ProductId = product.Name;

            // Стоимость: вносит конструктор (и при необходимости менеджер),
            // для остальных ролей поле можно считать только отображаемым.
            var role = App.CurrentUser?.Role;
            if (role == "Конструктор" || role == "Менеджер")
            {
                var costText = CostTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(costText))
                {
                    costText = costText.Replace(" ", string.Empty);
                    if (!decimal.TryParse(costText, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost) &&
                        !decimal.TryParse(costText, NumberStyles.Number, CultureInfo.InvariantCulture, out cost))
                    {
                        ErrorTextBlock.Text = "Некорректное значение стоимости.";
                        return;
                    }

                    order.Cost = cost;
                }
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                var dt = EndDatePicker.SelectedDate.Value.Date;
                order.EndDate = DateOnly.FromDateTime(dt);
            }

            // Дополнительные данные сохраняем во внешний файл
            var extra = new OrderExtraData
            {
                Description = DescriptionTextBox.Text ?? string.Empty,
                Sizes = (SizesDataGrid.ItemsSource as IEnumerable<SizeItem> ?? Array.Empty<SizeItem>())
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .Select(s => new SizeItem
                    {
                        Name = s.Name.Trim(),
                        Unit = s.Unit?.Trim() ?? string.Empty,
                        Value = s.Value?.Trim() ?? string.Empty
                    })
                    .ToList(),
                SchemaFiles = (SchemasListBox.ItemsSource as IEnumerable<string> ?? Array.Empty<string>()).ToList()
            };

            // Для совместимости с БД поле Schemas в заказе не должно быть NULL
            // Сохраняем в него сериализованный список файлов схем (может быть пустой JSON-массив).
            order.Schemas = JsonSerializer.Serialize(extra.SchemaFiles);

            SaveExtraData(order.Date, order.Number, extra);

            db.SaveChanges();
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}

