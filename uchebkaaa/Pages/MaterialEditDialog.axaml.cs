using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using uchebkaaa.Data;
using Microsoft.EntityFrameworkCore;

namespace uchebkaaa.Pages
{
    public partial class MaterialEditDialog : Window
    {
        private readonly Material? _material;

        public MaterialEditDialog(Material? material)
        {
            InitializeComponent();
            _material = material;

            LoadSuppliers();
            LoadWarehouses();
            if (_material != null)
            {
                LoadMaterialData();
                ArticleTextBox.IsReadOnly = true; // Артикул менять нельзя
            }

            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        private void LoadSuppliers()
        {
            var suppliers = App.DbContext.Suppliers.ToList();
            SupplierComboBox.ItemsSource = suppliers;

            if (_material != null && _material.Supplier != null)
            {
                SupplierComboBox.SelectedItem = _material.Supplier;
            }
            else
            {
                SupplierComboBox.SelectedIndex = 0;
            }
        }

        private void LoadWarehouses()
        {
            var warehouses = App.DbContext.Warehouses.ToList();
            WarehouseComboBox.ItemsSource = warehouses;

            if (_material != null)
            {
                var mw = App.DbContext.MaterialWarehouses
                    .Include(x => x.Warehouse)
                    .FirstOrDefault(x => x.MaterialArticle == _material.Article);

                if (mw != null)
                    WarehouseComboBox.SelectedItem = mw.Warehouse;
                else
                    WarehouseComboBox.SelectedIndex = 0;
            }
            else
            {
                WarehouseComboBox.SelectedIndex = 0;
            }
        }

        private void LoadMaterialData()
        {
            if (_material == null) return;

            ArticleTextBox.Text = _material.Article;
            NameTextBox.Text = _material.Name;
            CountTextBox.Text = _material.Count.ToString();
            UnitTextBox.Text = _material.Unit;
            PriceTextBox.Text = _material.Price?.ToString("F2") ?? "0";
            ProductTypeTextBox.Text = _material.ProductType;
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text) ||
                string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                !int.TryParse(CountTextBox.Text, out int count) ||
                string.IsNullOrWhiteSpace(UnitTextBox.Text) ||
                !decimal.TryParse(PriceTextBox.Text, out decimal price) ||
                string.IsNullOrWhiteSpace(ProductTypeTextBox.Text))
            {
                ShowError("Заполните корректно все поля: Артикул, Наименование, Количество, Ед. изм., Цена, Тип материала");
                return;
            }

            var supplier = SupplierComboBox.SelectedItem as Supplier;
            var warehouse = WarehouseComboBox.SelectedItem as Warehouse;

            if (supplier == null || warehouse == null)
            {
                ShowError("Выберите Поставщика и Склад");
                return;
            }

            Material material;
            if (_material == null)
            {
                material = new Material
                {
                    Article = ArticleTextBox.Text
                };
                App.DbContext.Materials.Add(material);
            }
            else
            {
                material = App.DbContext.Materials.FirstOrDefault(m => m.Article == _material.Article) ?? _material;
            }

            material.Name = NameTextBox.Text;
            material.Count = count;
            material.Unit = UnitTextBox.Text;
            material.Price = price;
            material.Supplier = supplier;
            material.ProductType = ProductTypeTextBox.Text;

            var mw = App.DbContext.MaterialWarehouses
                .FirstOrDefault(x => x.MaterialArticle == material.Article && x.WarehouseId == warehouse.Id);

            if (mw == null)
            {
                mw = new MaterialWarehouse
                {
                    MaterialArticle = material.Article,
                    WarehouseId = warehouse.Id,
                    Quantity = count
                };
                App.DbContext.MaterialWarehouses.Add(mw);
            }
            else
            {
                mw.Quantity = count;
            }

            App.DbContext.SaveChanges();
            Close();
        }


        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
        }
    }
}
