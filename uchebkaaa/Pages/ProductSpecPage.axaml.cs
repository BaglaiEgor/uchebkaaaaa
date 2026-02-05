using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class ProductSpecPage : UserControl
{
    public ProductSpecPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => MainWindow.NavigateTo(new MasterScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        EditButton.Click += EditButton_Click;
        PrintButton.Click += (_, _) => { };
        LoadProducts();
    }

    private void LoadProducts()
    {
        var products = App.DbContext.Products.ToList();
        ProductsDataGrid.ItemsSource = products;
    }

    private async void EditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is not Product p) return;
        var dialog = new ProductSpecEditDialog(p);
        await dialog.ShowDialog((Window)this.VisualRoot!);
        LoadProducts();
    }
}
