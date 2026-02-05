using Avalonia.Controls;
using Avalonia.Interactivity;
using uchebkaaa.Data;
using System.Linq;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace uchebkaaa.Pages;

public partial class AccessoryEditDialog : Window
{
    private readonly Accessory? _accessory;

    public AccessoryEditDialog(Accessory? accessory)
    {
        InitializeComponent();
        _accessory = accessory;

        SupplierComboBox.ItemsSource = App.DbContext.Suppliers.ToList();
        WarehouseComboBox.ItemsSource = App.DbContext.Warehouses.ToList();

        if (_accessory != null)
        {
            ArticleTextBox.Text = _accessory.Article;
            NameTextBox.Text = _accessory.Name;
            CountTextBox.Text = _accessory.Count.ToString();
            UnitTextBox.Text = _accessory.Unit;
            PriceTextBox.Text = _accessory.Price.ToString();
            SupplierComboBox.SelectedItem = _accessory.Supplier;
            ProductTypeTextBox.Text = _accessory.ProductType ?? "Не указан";

            var mw = App.DbContext.ComponentWarehouses.FirstOrDefault(x => x.ComponentArticle == _accessory.Article);
            if (mw != null)
                WarehouseComboBox.SelectedItem = App.DbContext.Warehouses.FirstOrDefault(w => w.Id == mw.WarehouseId);
        }

        SaveButton.Click += SaveButton_Click;
        CancelButton.Click += (_, _) => Close();
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArticleTextBox.Text) ||
            string.IsNullOrWhiteSpace(NameTextBox.Text) ||
            !int.TryParse(CountTextBox.Text, out int count) ||
            string.IsNullOrWhiteSpace(UnitTextBox.Text) ||
            !decimal.TryParse(PriceTextBox.Text, out decimal price))
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                "Заполните корректно все поля: Артикул, Наименование, Количество, Ед. изм., Цена",
                ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        var supplier = SupplierComboBox.SelectedItem as Supplier;
        var warehouse = WarehouseComboBox.SelectedItem as Warehouse;

        if (supplier == null || warehouse == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                "Выберите Поставщика и Склад",
                ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        Accessory accessory;
        if (_accessory == null)
        {
            accessory = new Accessory
            {
                Article = ArticleTextBox.Text
            };
            App.DbContext.Accessories.Add(accessory);
        }
        else
        {
            accessory = App.DbContext.Accessories.FirstOrDefault(a => a.Article == _accessory.Article) ?? _accessory;
        }

        accessory.Name = NameTextBox.Text;
        accessory.Count = count;
        accessory.Unit = UnitTextBox.Text;
        accessory.Price = price;
        accessory.Supplier = supplier;
        accessory.ProductType = string.IsNullOrWhiteSpace(ProductTypeTextBox.Text) ? "Не указан" : ProductTypeTextBox.Text;

        var cw = App.DbContext.ComponentWarehouses.FirstOrDefault(x => x.ComponentArticle == accessory.Article);
        if (cw == null)
        {
            cw = new ComponentWarehouse
            {
                ComponentArticle = accessory.Article,
                WarehouseId = warehouse.Id,
                Quantity = count
            };
            App.DbContext.ComponentWarehouses.Add(cw);
        }
        else
        {
            cw.WarehouseId = warehouse.Id;
            cw.Quantity = count;
        }

        App.DbContext.SaveChanges();
        Close();
    }
}
