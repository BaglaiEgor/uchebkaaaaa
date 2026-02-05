using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using System;
using uchebkaaa.Data;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;

namespace uchebkaaa.Pages
{
    public partial class MaterialsPage : UserControl
    {
        private bool _canEdit;

        public MaterialsPage()
        {
            InitializeComponent();

            // Ограничение доступа
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
            LoadMaterials();

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

            var allMaterialsOption = new Warehouse { Id = -1, Name = "Все материалы" };
            warehouses.Insert(0, allMaterialsOption);

            var allOption = new Warehouse { Id = 0, Name = "Все склады" };
            warehouses.Insert(1, allOption);

            WarehouseComboBox.ItemsSource = warehouses;
            WarehouseComboBox.SelectedIndex = 0;
        }

        private void LoadMaterials()
        {
            var selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

            var materials = App.DbContext.Materials.Include(m => m.Supplier).ToList();
            var materialWarehouses = App.DbContext.MaterialWarehouses.Include(mw => mw.Warehouse).ToList();

            var items = new List<dynamic>();

            if (selectedWarehouse != null && selectedWarehouse.Id == -1)
            {
                foreach (var material in materials)
                {
                    items.Add(new
                    {
                        material.Article,
                        material.Name,
                        Quantity = material.Count,
                        material.Unit,
                        material.Price,
                        SupplierName = material.Supplier?.Name ?? "Не указан",
                        SupplyTime = material.Supplier?.SupplyTime ?? "Не указан",
                        WarehouseName = "Не указан"
                    });
                }
            }
            else
            {
                foreach (var mw in materialWarehouses)
                {
                    var material = materials.FirstOrDefault(m => m.Article == mw.MaterialArticle);
                    if (material == null) continue;

                    if (selectedWarehouse != null && selectedWarehouse.Name != "Все материалы" && selectedWarehouse.Id > 0)
                    {
                        if (mw.Warehouse.Name != selectedWarehouse.Name) continue;
                    }

                    items.Add(new
                    {
                        material.Article,
                        material.Name,
                        mw.Quantity,
                        material.Unit,
                        material.Price,
                        SupplierName = material.Supplier?.Name ?? "Не указан",
                        SupplyTime = material.Supplier?.SupplyTime ?? "Не указан",
                        WarehouseName = mw.Warehouse.Name
                    });
                }
            }

            MaterialsDataGrid.ItemsSource = items;

            int totalPositions = items.Count;
            int totalMaterials = materials.Sum(m => m.Count);
            decimal totalCost = 0m;
            foreach (var i in items)
            {
                decimal price = i.Price ?? 0m;
                int quantity = i.Quantity;
                totalCost += price * quantity;
            }

            StatusTextBlock.Text = $"Показано позиций: {totalPositions} | Всего материалов: {totalMaterials}";
            TotalCostTextBlock.Text = $"Общая стоимость: {totalCost:C}";
        }


        private void WarehouseComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadMaterials();
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new MaterialEditDialog(null);
            await dialog.ShowDialog((Window)this.VisualRoot!);
            LoadMaterials();
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem == null) return;
            var selected = MaterialsDataGrid.SelectedItem;

            string article = selected.GetType().GetProperty("Article")!.GetValue(selected)!.ToString()!;
            var material = App.DbContext.Materials.FirstOrDefault(m => m.Article == article);
            if (material != null)
            {
                var dialog = new MaterialEditDialog(material);
                await dialog.ShowDialog((Window)this.VisualRoot!);
                LoadMaterials();
            }
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem == null) return;
            var selected = MaterialsDataGrid.SelectedItem;

            string article = selected.GetType().GetProperty("Article")!.GetValue(selected)!.ToString()!;
            var material = App.DbContext.Materials.FirstOrDefault(m => m.Article == article);

            if (material != null && material.Count == 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Подтверждение",
                    "Вы уверены, что хотите удалить эту позицию?",
                    ButtonEnum.YesNo);
                var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot!);
                if (result == ButtonResult.Yes)
                {
                    App.DbContext.Materials.Remove(material);
                    App.DbContext.SaveChanges();
                    LoadMaterials();
                }
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
