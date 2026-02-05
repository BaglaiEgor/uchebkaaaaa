using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages
{
    public partial class OrdersPage : UserControl
    {
        private class OrderViewModel
        {
            public DateOnly Date { get; set; }
            public int Number { get; set; }
            public string DisplayNumber { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal Cost { get; set; }
            public DateOnly EndDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string ManagerName { get; set; } = string.Empty;
        }

        private enum FilterType
        {
            All,
            New,
            Completed,
            Current,
            Cancelled
        }

        private FilterType _currentFilter = FilterType.All;

        public OrdersPage()
        {
            InitializeComponent();

            FilterComboBox.ItemsSource = new[]
            {
                "Все заказы",
                "Новые заказы",
                "Выполненные заказы",
                "Текущие заказы",
                "Отклоненные заказы"
            };
            FilterComboBox.SelectedIndex = 0;

            FilterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            AddButton.Click += AddButton_Click;
            EditButton.Click += EditButton_Click;
            DeleteButton.Click += DeleteButton_Click;
            CancelOrderButton.Click += CancelOrderButton_Click;
            TakeOrderButton.Click += TakeOrderButton_Click;
            ChangeStatusButton.Click += ChangeStatusButton_Click;
            HistoryButton.Click += HistoryButton_Click;
            QualityButton.Click += QualityButton_Click;
            AnalysisButton.Click += AnalysisButton_Click;
            BackButton.Click += BackButton_Click;
            LogoutButton.Click += LogoutButton_Click;

            ConfigureButtonsForRole();
            LoadOrders();
        }

        private string Role => App.CurrentUser?.Role ?? string.Empty;

        private void ConfigureButtonsForRole()
        {
            // Общие правила доступа
            AddButton.IsVisible = Role is "Заказчик" or "Менеджер";
            EditButton.IsVisible = Role is "Заказчик" or "Менеджер" or "Конструктор";
            DeleteButton.IsVisible = Role == "Заказчик";

            CancelOrderButton.IsVisible = Role is "Заказчик" or "Менеджер";
            TakeOrderButton.IsVisible = Role == "Менеджер";
            ChangeStatusButton.IsVisible = Role is "Менеджер" or "Конструктор" or "Мастер";
            HistoryButton.IsVisible = Role is "Менеджер" or "Директор";
            AnalysisButton.IsVisible = Role == "Менеджер";
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            UserControl? screen = Role switch
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

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }

        private void FilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _currentFilter = FilterComboBox.SelectedIndex switch
            {
                1 => FilterType.New,
                2 => FilterType.Completed,
                3 => FilterType.Current,
                4 => FilterType.Cancelled,
                _ => FilterType.All
            };
            LoadOrders();
        }

        private void LoadOrders()
        {
            var db = App.DbContext;
            var user = App.CurrentUser;
            if (user == null)
                return;

            var query = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Manager)
                .Include(o => o.Product)
                .AsQueryable();

            // Фильтрация по ролям, которую может выполнить БД
            if (Role == "Заказчик")
            {
                query = query.Where(o => o.CustomerId == user.Login);
            }

            // Полностью загружаем заказы в память, чтобы избежать параллельных запросов к БД
            // при вызовах OrderStatusHelper и GetFormattedOrderNumber, которые тоже используют DbContext.
            var orders = query.ToList().AsEnumerable();

            // Дополнительная фильтрация по ролям, зависящая от статуса
            switch (Role)
            {
                case "Менеджер":
                    orders = orders.Where(o =>
                        o.ManagerId == user.Login ||
                        OrderStatusHelper.GetCurrentStatus(o) == OrderStatusHelper.StatusNew);
                    break;
                case "Конструктор":
                    orders = orders.Where(o =>
                        OrderStatusHelper.GetCurrentStatus(o) == OrderStatusHelper.StatusSpecification);
                    break;
                case "Мастер":
                    orders = orders.Where(o =>
                        OrderStatusHelper.GetCurrentStatus(o) == OrderStatusHelper.StatusProduction ||
                        OrderStatusHelper.GetCurrentStatus(o) == OrderStatusHelper.StatusControl);
                    break;
                case "Директор":
                    // все заказы
                    break;
            }

            var list = orders
                .Select(o =>
                {
                    var status = OrderStatusHelper.GetCurrentStatus(o);
                    return new OrderViewModel
                    {
                        Date = o.Date,
                        Number = o.Number,
                        DisplayNumber = OrderStatusHelper.GetFormattedOrderNumber(db, o),
                        Name = o.Name,
                        Status = status,
                        Cost = o.Cost,
                        EndDate = o.EndDate,
                        CustomerName = o.Customer?.Name ?? o.CustomerId,
                        ManagerName = o.Manager?.Name ?? o.ManagerId
                    };
                })
                .ToList();

            // Фильтр по статусам
            list = _currentFilter switch
            {
                FilterType.New => list.Where(vm =>
                    vm.Status == OrderStatusHelper.StatusNew ||
                    vm.Status == OrderStatusHelper.StatusSpecification ||
                    vm.Status == OrderStatusHelper.StatusConfirmation).ToList(),
                FilterType.Completed => list.Where(vm =>
                    vm.Status == OrderStatusHelper.StatusReady ||
                    vm.Status == OrderStatusHelper.StatusClosed).ToList(),
                FilterType.Current => list.Where(vm =>
                    vm.Status == OrderStatusHelper.StatusProcurement ||
                    vm.Status == OrderStatusHelper.StatusProduction ||
                    vm.Status == OrderStatusHelper.StatusControl).ToList(),
                FilterType.Cancelled => list.Where(vm =>
                    vm.Status == OrderStatusHelper.StatusCancelled).ToList(),
                _ => list
            };

            OrdersDataGrid.ItemsSource = list;
            StatusTextBlock.Text = $"Всего заказов: {list.Count}";
        }

        private Order? GetSelectedOrder()
        {
            if (OrdersDataGrid.SelectedItem is not OrderViewModel vm)
                return null;

            return App.DbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Manager)
                .Include(o => o.Product)
                .FirstOrDefault(o => o.Date == vm.Date && o.Number == vm.Number);
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role is not ("Заказчик" or "Менеджер"))
                return;

            var dialog = new OrderEditDialog(null);
            await dialog.ShowDialog((Window)VisualRoot!);
            LoadOrders();
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для редактирования.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);

            if (Role == "Заказчик" && status != OrderStatusHelper.StatusNew)
            {
                await ShowInfo("Заказчик может редактировать только заказы в статусе \"Новый\".");
                return;
            }

            if (Role == "Менеджер" && status == OrderStatusHelper.StatusClosed)
            {
                await ShowInfo("Нельзя редактировать закрытый заказ.");
                return;
            }

            if (Role == "Конструктор" && status != OrderStatusHelper.StatusSpecification)
            {
                await ShowInfo("Конструктор может редактировать только заказы в статусе \"Составление спецификации\".");
                return;
            }

            var dialog = new OrderEditDialog(order);
            await dialog.ShowDialog((Window)VisualRoot!);
            LoadOrders();
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role != "Заказчик")
                return;

            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для удаления.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);
            if (status != OrderStatusHelper.StatusNew)
            {
                await ShowInfo("Удалить можно только заказы в статусе \"Новый\".");
                return;
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение",
                "Вы уверены, что хотите удалить этот заказ?",
                ButtonEnum.YesNo);
            var result = await box.ShowWindowDialogAsync((Window)VisualRoot!);
            if (result == ButtonResult.Yes)
            {
                App.DbContext.Orders.Remove(order);
                App.DbContext.SaveChanges();
                LoadOrders();
            }
        }

        private async void CancelOrderButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role is not ("Заказчик" or "Менеджер"))
                return;

            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для отмены.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);

            if (Role == "Заказчик")
            {
                // Клиент может отменить заявку до этапа «Закупка»
                if (status is OrderStatusHelper.StatusProcurement or
                    OrderStatusHelper.StatusProduction or
                    OrderStatusHelper.StatusControl or
                    OrderStatusHelper.StatusReady or
                    OrderStatusHelper.StatusClosed)
                {
                    await ShowInfo("Вы не можете отменить заказ на этой стадии.");
                    return;
                }

                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusCancelled, "Отмена заказчиком");
                LoadOrders();
                return;
            }

            if (Role == "Менеджер")
            {
                // Менеджер может отклонить «Новый» заказ клиента с указанием причины,
                // а также из статуса «Подтверждение» – если клиент отказался.
                if (status is not (OrderStatusHelper.StatusNew or OrderStatusHelper.StatusConfirmation))
                {
                    await ShowInfo("Менеджер может отменять только новые заказы или заказы в статусе \"Подтверждение\".");
                    return;
                }

                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusCancelled, "Отмена/отклонение менеджером");
                LoadOrders();
            }
        }

        private async void TakeOrderButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role != "Менеджер")
                return;

            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для принятия в работу.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);
            if (status != OrderStatusHelper.StatusNew)
            {
                await ShowInfo("Принять можно только заказы в статусе \"Новый\".");
                return;
            }

            order.ManagerId = App.CurrentUser!.Login;
            App.DbContext.SaveChanges();

            OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusSpecification, "Заказ принят менеджером");
            LoadOrders();
        }

        private async void ChangeStatusButton_Click(object? sender, RoutedEventArgs e)
        {
            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для смены статуса.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);

            if (Role == "Менеджер")
            {
                await HandleManagerStatusChange(order, status);
            }
            else if (Role == "Конструктор")
            {
                await HandleConstructorStatusChange(order, status);
            }
            else if (Role == "Мастер")
            {
                await HandleMasterStatusChange(order, status);
            }
        }

        private async System.Threading.Tasks.Task HandleManagerStatusChange(Order order, string status)
        {
            if (status == OrderStatusHelper.StatusConfirmation)
            {
                // Подтверждение -> Отклонен/Закупка, здесь реализуем переход только в «Закупка»
                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusProcurement, "Клиент подтвердил заказ, переходим к закупке");
                LoadOrders();
                return;
            }

            if (status == OrderStatusHelper.StatusProcurement)
            {
                // Закупка -> Производство
                MaterialWriteOffHelper.WriteOffForOrder(App.DbContext, order);
                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusProduction, "Материалы поступили, заказ передан в производство");
                LoadOrders();
                return;
            }

            if (status == OrderStatusHelper.StatusReady)
            {
                // Готов -> Закрыт
                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusClosed, "Заказ отгружен и полностью оплачен");
                LoadOrders();
                return;
            }

            await ShowInfo("На этом этапе менеджер не может изменить статус автоматически. Используйте соответствующие формы (спецификация, контроль качества и т.п.).");
        }

        private async System.Threading.Tasks.Task HandleConstructorStatusChange(Order order, string status)
        {
            if (status != OrderStatusHelper.StatusSpecification)
            {
                await ShowInfo("Конструктор может завершить только стадию \"Составление спецификации\".");
                return;
            }

            if (order.Cost <= 0 || order.EndDate == default)
            {
                await ShowInfo("Перед переводом в статус \"Подтверждение\" необходимо указать стоимость и плановую дату завершения заказа.");
                return;
            }

            OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusConfirmation, "Спецификация составлена, ожидается подтверждение клиента");
            LoadOrders();
        }

        private async System.Threading.Tasks.Task HandleMasterStatusChange(Order order, string status)
        {
            if (status == OrderStatusHelper.StatusProduction)
            {
                // Производство -> Контроль
                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusControl, "Работы по заказу завершены, переходим к контролю качества");
                LoadOrders();
                return;
            }

            if (status == OrderStatusHelper.StatusControl)
            {
                // Проверяем, что все параметры качества по заказу положительные
                var checks = App.DbContext.QualityChecks.Where(q => q.OrderNumber == order.Number).ToList();
                if (checks.Count == 0 || checks.Any(c => !c.IsAcceptable))
                {
                    await ShowInfo("Нельзя завершить контроль качества: есть параметры без оценки или с отрицательной оценкой.");
                    return;
                }

                OrderStatusHelper.ChangeStatus(order, OrderStatusHelper.StatusReady, "Контроль качества пройден, заказ готов");
                LoadOrders();
                return;
            }

            await ShowInfo("На этом этапе мастер не может изменить статус.");
        }

        private async void HistoryButton_Click(object? sender, RoutedEventArgs e)
        {
            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для просмотра истории.");
                return;
            }

            var dialog = new OrderHistoryDialog(order);
            await dialog.ShowDialog((Window)VisualRoot!);
        }

        private async void QualityButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role != "Мастер")
            {
                await ShowInfo("Форма контроля качества доступна только мастеру.");
                return;
            }

            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для контроля качества.");
                return;
            }

            var status = OrderStatusHelper.GetCurrentStatus(order);
            if (status != OrderStatusHelper.StatusControl)
            {
                await ShowInfo("Контроль качества выполняется только для заказов в статусе \"Контроль\".");
                return;
            }

            var dialog = new QualityControlDialog(order);
            await dialog.ShowDialog((Window)VisualRoot!);
            // после изменения оценок статус сам не меняется, но мастер сможет сменить его через кнопку «Изменить статус»
        }

        private async void AnalysisButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Role != "Менеджер")
            {
                await ShowInfo("Анализ материалов и производства доступен только менеджеру.");
                return;
            }

            var order = GetSelectedOrder();
            if (order == null)
            {
                await ShowInfo("Выберите заказ для анализа.");
                return;
            }

            var dialog = new OrderMaterialsAndProductionDialog(order);
            await dialog.ShowDialog((Window)VisualRoot!);
        }

        private async System.Threading.Tasks.Task ShowInfo(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Информация", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)VisualRoot!);
        }
    }
}

