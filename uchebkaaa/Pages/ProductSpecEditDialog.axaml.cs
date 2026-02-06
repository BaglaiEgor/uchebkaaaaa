using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class ProductSpecEditDialog : Window
{
    private readonly Product _product;

    public ProductSpecEditDialog(Product product)
    {
        InitializeComponent();
        _product = product;
        ProductNameTextBlock.Text = $"Изделие: {product.Name}";

        LoadLookupData();
        LoadSpecs();

        MaterialsGrid.SelectionChanged += (_, _) => LoadSelectedMaterialSpec();
        AccessoriesGrid.SelectionChanged += (_, _) => LoadSelectedAccessorySpec();
        AssemblyGrid.SelectionChanged += (_, _) => LoadSelectedAssemblySpec();
        OperationsGrid.SelectionChanged += (_, _) => LoadSelectedOperationSpec();

        MaterialAddButton.Click += MaterialAddButton_Click;
        MaterialUpdateButton.Click += MaterialUpdateButton_Click;
        MaterialDeleteButton.Click += MaterialDeleteButton_Click;

        AccessoryAddButton.Click += AccessoryAddButton_Click;
        AccessoryUpdateButton.Click += AccessoryUpdateButton_Click;
        AccessoryDeleteButton.Click += AccessoryDeleteButton_Click;

        AssemblyAddButton.Click += AssemblyAddButton_Click;
        AssemblyUpdateButton.Click += AssemblyUpdateButton_Click;
        AssemblyDeleteButton.Click += AssemblyDeleteButton_Click;

        OperationAddButton.Click += OperationAddButton_Click;
        OperationUpdateButton.Click += OperationUpdateButton_Click;
        OperationDeleteButton.Click += OperationDeleteButton_Click;

        CloseButton.Click += (_, _) => Close();
    }

    private void LoadLookupData()
    {
        var db = App.DbContext;

        MaterialComboBox.ItemsSource = db.Materials.OrderBy(m => m.Name).ToList();
        AccessoryComboBox.ItemsSource = db.Accessories.OrderBy(a => a.Name).ToList();
        AssemblyComboBox.ItemsSource = db.Products
            .Where(p => p.Name != _product.Name)
            .OrderBy(p => p.Name)
            .ToList();
        EquipmentTypeComboBox.ItemsSource = db.EquipmentTypes.OrderBy(e => e.Name).ToList();
    }

    private void LoadSpecs()
    {
        var db = App.DbContext;
        MaterialsGrid.ItemsSource = db.MaterialSpecs.Include(ms => ms.Material)
             .Where(ms => ms.ProductId == _product.Name).ToList();
        AccessoriesGrid.ItemsSource = db.AccessoriesSpecs.Include(a => a.Accessories)
            .Where(a => a.ProductId == _product.Name).ToList();
        AssemblyGrid.ItemsSource = db.AssemblySpecs.Include(a => a.Item)
             .Where(a => a.ProductId == _product.Name).ToList();
        OperationsGrid.ItemsSource = db.OperationSpecs
            .Where(o => o.ProductId == _product.Name).ToList();
    }
    private void LoadSelectedMaterialSpec()
    {
        if (MaterialsGrid.SelectedItem is not MaterialSpec spec)
            return;

        MaterialComboBox.SelectedItem = spec.Material;
        MaterialCountTextBox.Text = spec.Count.ToString();
    }

    private void LoadSelectedAccessorySpec()
    {
        if (AccessoriesGrid.SelectedItem is not AccessoriesSpec spec)
            return;

        AccessoryComboBox.SelectedItem = spec.Accessories;
        AccessoryCountTextBox.Text = spec.Count.ToString();
    }

    private void LoadSelectedAssemblySpec()
    {
        if (AssemblyGrid.SelectedItem is not AssemblySpec spec)
            return;

        AssemblyComboBox.SelectedItem = spec.Item;
        AssemblyCountTextBox.Text = spec.Count.ToString();
    }

    private void LoadSelectedOperationSpec()
    {
        if (OperationsGrid.SelectedItem is not OperationSpec spec)
            return;

        OperationNameTextBox.Text = spec.Operation;
        EquipmentTypeComboBox.SelectedItem = App.DbContext.EquipmentTypes
            .FirstOrDefault(e => e.Name == spec.EquipmentType);
        OperationTimeTextBox.Text = spec.OperationTime.ToString();
        OperationNumberTextBox.Text = spec.Number.ToString();
    }

    private void MaterialAddButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (MaterialComboBox.SelectedItem is not Material material)
        {
            ShowError("Выберите материал.");
            return;
        }

        if (!int.TryParse(MaterialCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для материала.");
            return;
        }

        var db = App.DbContext;
        var existing = db.MaterialSpecs.FirstOrDefault(ms => ms.ProductId == _product.Name && ms.MaterialId == material.Article);
        if (existing != null)
        {
            ShowError("Материал уже добавлен. Используйте обновление количества.");
            return;
        }

        db.MaterialSpecs.Add(new MaterialSpec
        {
            ProductId = _product.Name,
            MaterialId = material.Article,
            Count = count
        });
        db.SaveChanges();
        LoadSpecs();
    }

    private void MaterialUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (MaterialsGrid.SelectedItem is not MaterialSpec spec)
        {
            ShowError("Выберите материал для обновления.");
            return;
        }

        if (!int.TryParse(MaterialCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для материала.");
            return;
        }

        var db = App.DbContext;
        var target = db.MaterialSpecs.FirstOrDefault(ms => ms.ProductId == _product.Name && ms.MaterialId == spec.MaterialId);
        if (target == null)
        {
            ShowError("Не удалось найти выбранный материал.");
            return;
        }

        target.Count = count;
        db.SaveChanges();
        LoadSpecs();
    }

    private void MaterialDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (MaterialsGrid.SelectedItem is not MaterialSpec spec)
        {
            ShowError("Выберите материал для удаления.");
            return;
        }

        var db = App.DbContext;
        db.MaterialSpecs.Remove(spec);
        db.SaveChanges();
        LoadSpecs();
    }

    private void AccessoryAddButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AccessoryComboBox.SelectedItem is not Accessory accessory)
        {
            ShowError("Выберите комплектующее.");
            return;
        }

        if (!int.TryParse(AccessoryCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для комплектующего.");
            return;
        }

        var db = App.DbContext;
        var existing = db.AccessoriesSpecs.FirstOrDefault(a => a.ProductId == _product.Name && a.AccessoriesId == accessory.Article);
        if (existing != null)
        {
            ShowError("Комплектующее уже добавлено. Используйте обновление количества.");
            return;
        }

        db.AccessoriesSpecs.Add(new AccessoriesSpec
        {
            ProductId = _product.Name,
            AccessoriesId = accessory.Article,
            Count = count
        });
        db.SaveChanges();
        LoadSpecs();
    }

    private void AccessoryUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AccessoriesGrid.SelectedItem is not AccessoriesSpec spec)
        {
            ShowError("Выберите комплектующее для обновления.");
            return;
        }

        if (!int.TryParse(AccessoryCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для комплектующего.");
            return;
        }

        var db = App.DbContext;
        var target = db.AccessoriesSpecs.FirstOrDefault(a => a.ProductId == _product.Name && a.AccessoriesId == spec.AccessoriesId);
        if (target == null)
        {
            ShowError("Не удалось найти выбранное комплектующее.");
            return;
        }

        target.Count = count;
        db.SaveChanges();
        LoadSpecs();
    }

    private void AccessoryDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AccessoriesGrid.SelectedItem is not AccessoriesSpec spec)
        {
            ShowError("Выберите комплектующее для удаления.");
            return;
        }

        var db = App.DbContext;
        db.AccessoriesSpecs.Remove(spec);
        db.SaveChanges();
        LoadSpecs();
    }

    private void AssemblyAddButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AssemblyComboBox.SelectedItem is not Product item)
        {
            ShowError("Выберите изделие для сборки.");
            return;
        }

        if (!int.TryParse(AssemblyCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для сборочной единицы.");
            return;
        }

        var db = App.DbContext;
        var existing = db.AssemblySpecs.FirstOrDefault(a => a.ProductId == _product.Name && a.ItemId == item.Name);
        if (existing != null)
        {
            ShowError("Сборочная единица уже добавлена. Используйте обновление количества.");
            return;
        }

        db.AssemblySpecs.Add(new AssemblySpec
        {
            ProductId = _product.Name,
            ItemId = item.Name,
            Count = count
        });
        db.SaveChanges();
        LoadSpecs();
    }

    private void AssemblyUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AssemblyGrid.SelectedItem is not AssemblySpec spec)
        {
            ShowError("Выберите сборочную единицу для обновления.");
            return;
        }

        if (!int.TryParse(AssemblyCountTextBox.Text, out var count) || count <= 0)
        {
            ShowError("Введите корректное количество для сборочной единицы.");
            return;
        }

        var db = App.DbContext;
        var target = db.AssemblySpecs.FirstOrDefault(a => a.ProductId == _product.Name && a.ItemId == spec.ItemId);
        if (target == null)
        {
            ShowError("Не удалось найти выбранную сборочную единицу.");
            return;
        }

        target.Count = count;
        db.SaveChanges();
        LoadSpecs();
    }

    private void AssemblyDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (AssemblyGrid.SelectedItem is not AssemblySpec spec)
        {
            ShowError("Выберите сборочную единицу для удаления.");
            return;
        }

        var db = App.DbContext;
        db.AssemblySpecs.Remove(spec);
        db.SaveChanges();
        LoadSpecs();
    }

    private void OperationAddButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (string.IsNullOrWhiteSpace(OperationNameTextBox.Text))
        {
            ShowError("Введите название операции.");
            return;
        }

        if (EquipmentTypeComboBox.SelectedItem is not EquipmentType equipmentType)
        {
            ShowError("Выберите тип оборудования.");
            return;
        }

        if (!int.TryParse(OperationTimeTextBox.Text, out var time) || time <= 0)
        {
            ShowError("Введите корректное время операции.");
            return;
        }

        if (!int.TryParse(OperationNumberTextBox.Text, out var number) || number <= 0)
        {
            ShowError("Введите корректный номер операции.");
            return;
        }

        var db = App.DbContext;
        var existing = db.OperationSpecs.FirstOrDefault(o => o.ProductId == _product.Name && o.Number == number);
        if (existing != null)
        {
            ShowError("Операция с таким номером уже существует. Используйте обновление.");
            return;
        }

        db.OperationSpecs.Add(new OperationSpec
        {
            ProductId = _product.Name,
            Operation = OperationNameTextBox.Text,
            EquipmentType = equipmentType.Name,
            OperationTime = time,
            Number = number
        });
        db.SaveChanges();
        LoadSpecs();
    }

    private void OperationUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (OperationsGrid.SelectedItem is not OperationSpec spec)
        {
            ShowError("Выберите операцию для обновления.");
            return;
        }

        if (string.IsNullOrWhiteSpace(OperationNameTextBox.Text))
        {
            ShowError("Введите название операции.");
            return;
        }

        if (EquipmentTypeComboBox.SelectedItem is not EquipmentType equipmentType)
        {
            ShowError("Выберите тип оборудования.");
            return;
        }

        if (!int.TryParse(OperationTimeTextBox.Text, out var time) || time <= 0)
        {
            ShowError("Введите корректное время операции.");
            return;
        }

        if (!int.TryParse(OperationNumberTextBox.Text, out var number) || number <= 0)
        {
            ShowError("Введите корректный номер операции.");
            return;
        }

        var db = App.DbContext;
        var target = db.OperationSpecs.FirstOrDefault(o => o.ProductId == _product.Name && o.Number == spec.Number);
        if (target == null)
        {
            ShowError("Не удалось найти выбранную операцию.");
            return;
        }

        if (spec.Number != number)
        {
            var conflict = db.OperationSpecs.Any(o => o.ProductId == _product.Name && o.Number == number);
            if (conflict)
            {
                ShowError("Операция с таким номером уже существует.");
                return;
            }
        }

        target.Operation = OperationNameTextBox.Text;
        target.EquipmentType = equipmentType.Name;
        target.OperationTime = time;
        target.Number = number;
        db.SaveChanges();
        LoadSpecs();
    }

    private void OperationDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();
        if (OperationsGrid.SelectedItem is not OperationSpec spec)
        {
            ShowError("Выберите операцию для удаления.");
            return;
        }

        var db = App.DbContext;
        db.OperationSpecs.Remove(spec);
        db.SaveChanges();
        LoadSpecs();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.IsVisible = true;
    }

    private void ClearError()
    {
        ErrorTextBlock.Text = string.Empty;
        ErrorTextBlock.IsVisible = false;
    }
}