using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class ProductSpecEditDialog : Window
{
    public ProductSpecEditDialog(Product product)
    {
        InitializeComponent();
        ProductNameTextBlock.Text = $"Изделие: {product.Name}";

        var db = App.DbContext;
        MaterialsGrid.ItemsSource = db.MaterialSpecs.Include(ms => ms.Material)
            .Where(ms => ms.ProductId == product.Name).ToList();
        AccessoriesGrid.ItemsSource = db.AccessoriesSpecs.Include(a => a.Accessories)
            .Where(a => a.ProductId == product.Name).ToList();
        AssemblyGrid.ItemsSource = db.AssemblySpecs.Include(a => a.Item)
            .Where(a => a.ProductId == product.Name).ToList();
        OperationsGrid.ItemsSource = db.OperationSpecs
            .Where(o => o.ProductId == product.Name).ToList();

        CloseButton.Click += (_, _) => Close();
    }
}
