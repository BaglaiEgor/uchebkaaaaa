using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using System.Collections.Generic;
using uchebkaaa.Data;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;

namespace uchebkaaa.Pages
{
    public partial class AccessoriesPage : UserControl
    {
        private bool _canEdit;

        public AccessoriesPage()
        {
            InitializeComponent();

            if (App.CurrentUser?.Role == "Заказчик")
            {
                MainWindow.NavigateTo(new CustomerScreen());
                return;
            }

            _canEdit = App.CurrentUser?.Role == "Менеджер" || App.CurrentUser?.Role == "Директор";

            AddButton.IsVisible = _canEdit;
            EditButton.IsVisible = _canEdit;
            DeleteButton.IsVisible = _canEdit;

            LoadWarehouses();
            LoadAccessories();

            WarehouseComboBox.SelectionChanged += WarehouseComboBox_SelectionChanged;
            BackButton.Click += BackButton_Click;
            AddButton.Click += AddButton_Click;
            EditButton.Click += EditButton_Click;
            DeleteButton.Click += DeleteButton_Click;
            LogoutButton.Click += LogoutButton_Click;
        }

        private void LoadWarehouses()
        {
            var warehouses = App.DbContext.Warehouses.ToList();
            var allAccessoriesOption = new Warehouse { Id = -1, Name = "Все комплектующие" };
            warehouses.Insert(0, allAccessoriesOption);
            var allOption = new Warehouse { Id = 0, Name = "Все склады" };
            warehouses.Insert(1, allOption);
            WarehouseComboBox.ItemsSource = warehouses;
            WarehouseComboBox.SelectedIndex = 0;
        }

        private void LoadAccessories()
        {
            var selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;
            var accessories = App.DbContext.Accessories.Include(a => a.Supplier).ToList();
            var componentWarehouses = App.DbContext.ComponentWarehouses.Include(cw => cw.Warehouse).ToList();
            var items = new List<dynamic>();

            if (selectedWarehouse != null && selectedWarehouse.Id == -1)
            {
                foreach (var a in accessories)
                {
                    items.Add(new
                    {
                        a.Article,
                        a.Name,
                        Quantity = a.Count,
                        a.Unit,
                        a.Price,
                        SupplierName = a.Supplier?.Name ?? "Не указан",
                        SupplyTime = a.Supplier?.SupplyTime ?? "Не указан",
                        WarehouseName = "Не указан"
                    });
                }
            }
            else
            {
                foreach (var cw in componentWarehouses)
                {
                    var accessory = accessories.FirstOrDefault(a => a.Article == cw.ComponentArticle);
                    if (accessory == null) continue;
                    if (selectedWarehouse != null && selectedWarehouse.Name != "Все комплектующие" && selectedWarehouse.Id > 0)
                    {
                        if (cw.Warehouse.Name != selectedWarehouse.Name) continue;
                    }

                    items.Add(new
                    {
                        accessory.Article,
                        accessory.Name,
                        cw.Quantity,
                        accessory.Unit,
                        accessory.Price,
                        SupplierName = accessory.Supplier?.Name ?? "Не указан",
                        SupplyTime = accessory.Supplier?.SupplyTime ?? "Не указан",
                        WarehouseName = cw.Warehouse.Name
                    });
                }
            }

            AccessoriesDataGrid.ItemsSource = items;
            int totalPositions = items.Count;
            int totalCount = accessories.Sum(a => a.Count);
            decimal totalCost = 0m;
            foreach (var i in items)
            {
                decimal price = i.Price ?? 0m;
                int quantity = i.Quantity;
                totalCost += price * quantity;
            }
            StatusTextBlock.Text = $"Показано позиций: {totalPositions} | Всего комплектующих: {totalCount}";
            TotalCostTextBlock.Text = $"Общая стоимость: {totalCost:C}";
        }

        private void WarehouseComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => LoadAccessories();

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (AccessoriesDataGrid.SelectedItem == null) return;
            dynamic selected = AccessoriesDataGrid.SelectedItem;
            string article = selected.Article;
            var accessory = App.DbContext.Accessories.FirstOrDefault(a => a.Article == article);

            if (accessory != null && accessory.Count == 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Подтверждение",
                    "Вы уверены, что хотите удалить эту позицию?",
                    ButtonEnum.YesNo);
                var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot!);

                if (result == ButtonResult.Yes)
                {
                    App.DbContext.Accessories.Remove(accessory);
                    var cwList = App.DbContext.ComponentWarehouses.Where(cw => cw.ComponentArticle == article).ToList();
                    App.DbContext.ComponentWarehouses.RemoveRange(cwList);
                    App.DbContext.SaveChanges();
                    LoadAccessories();
                }
            }
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new AccessoryEditDialog(null);
            await dialog.ShowDialog((Window)this.VisualRoot!);
            LoadAccessories();
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (AccessoriesDataGrid.SelectedItem == null) return;
            dynamic selected = AccessoriesDataGrid.SelectedItem;
            string article = selected.Article;
            var accessory = App.DbContext.Accessories.FirstOrDefault(a => a.Article == article);
            if (accessory != null)
            {
                var dialog = new AccessoryEditDialog(accessory);
                await dialog.ShowDialog((Window)this.VisualRoot!);
                LoadAccessories();
            }
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            UserControl? screen = App.CurrentUser?.Role switch
            {
                "Менеджер" => new ManagerScreen(),
                "Директор" => new DirectorScreen(),
                _ => null
            };
            if (screen != null) MainWindow.NavigateTo(screen);
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }
    }
}
