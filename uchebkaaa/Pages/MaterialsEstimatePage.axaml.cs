using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class MaterialsEstimatePage : UserControl
{
    public MaterialsEstimatePage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => MainWindow.NavigateTo(new ManagerScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        OrderComboBox.SelectionChanged += (_, _) => { };
        CalcButton.Click += CalcButton_Click;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var orders = App.DbContext.Orders
            .Include(o => o.Product)
            .Where(o => o.Status != "Новый" && o.Status != "Отменен" && o.Status != "Отклонен")
            .ToList();
        OrderComboBox.ItemsSource = orders;
        if (orders.Count > 0) OrderComboBox.SelectedIndex = 0;
    }

    private void CalcButton_Click(object? sender, RoutedEventArgs e)
    {
        if (OrderComboBox.SelectedItem is not Order order) return;

        var db = App.DbContext;
        var materialCounts = new Dictionary<string, int>();
        var accessoryCounts = new Dictionary<string, int>();

        void Accumulate(string productId, int factor)
        {
            var materialSpecs = db.MaterialSpecs.Include(m => m.Material)
                                .Where(m => m.ProductId == productId)
                                .ToList();
            foreach (var ms in materialSpecs)
            {
                var key = "M:" + ms.MaterialId;
                if (!materialCounts.ContainsKey(key)) materialCounts[key] = 0;
                materialCounts[key] += ms.Count * factor;
            }

            var accessorySpecs = db.AccessoriesSpecs.Include(x => x.Accessories)
                                    .Where(a => a.ProductId == productId)
                                    .ToList();
            foreach (var a in accessorySpecs)
            {
                var key = "A:" + a.AccessoriesId;
                if (!accessoryCounts.ContainsKey(key)) accessoryCounts[key] = 0;
                accessoryCounts[key] += a.Count * factor;
            }

            var assemblyItems = db.AssemblySpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var asm in assemblyItems)
                Accumulate(asm.ItemId, factor * asm.Count);
        }

        Accumulate(order.ProductId, 1);

        var rows = new List<object>();
        decimal totalCost = 0;
        int maxDeliveryDays = 0;

        foreach (var kv in materialCounts)
        {
            var art = kv.Key.Substring(2);
            var m = db.Materials.Include(x => x.Supplier).FirstOrDefault(x => x.Article == art);
            if (m == null) continue;
            var inStock = m.Count;
            var shortage = Math.Max(0, kv.Value - inStock);
            var cost = (m.Price ?? 0) * kv.Value;
            totalCost += cost;
            var days = ParseDeliveryDays(m.Supplier?.SupplyTime ?? "0");
            if (shortage > 0 && days > maxDeliveryDays) maxDeliveryDays = days;
            rows.Add(new
            {
                ItemType = "Материал",
                Article = m.Article,
                Name = m.Name,
                Required = kv.Value,
                InStock = inStock,
                Shortage = shortage,
                PriceStr = (m.Price ?? 0).ToString("C", CultureInfo.GetCultureInfo("ru-RU")),
                CostStr = cost.ToString("C", CultureInfo.GetCultureInfo("ru-RU")),
                SupplyTime = m.Supplier?.SupplyTime ?? "—"
            });
        }

        foreach (var kv in accessoryCounts)
        {
            var art = kv.Key.Substring(2);
            var a = db.Accessories.Include(x => x.Supplier).FirstOrDefault(x => x.Article == art);
            if (a == null) continue;
            var inStock = a.Count;
            var shortage = Math.Max(0, kv.Value - inStock);
            var cost = (a.Price ?? 0) * kv.Value;
            totalCost += cost;
            var days = ParseDeliveryDays(a.Supplier?.SupplyTime ?? "0");
            if (shortage > 0 && days > maxDeliveryDays) maxDeliveryDays = days;
            rows.Add(new
            {
                ItemType = "Компл.",
                Article = a.Article,
                Name = a.Name,
                Required = kv.Value,
                InStock = inStock,
                Shortage = shortage,
                PriceStr = (a.Price ?? 0).ToString("C", CultureInfo.GetCultureInfo("ru-RU")),
                CostStr = cost.ToString("C", CultureInfo.GetCultureInfo("ru-RU")),
                SupplyTime = a.Supplier?.SupplyTime ?? "—"
            });
        }

        EstimateDataGrid.ItemsSource = rows;
        TotalCostTextBlock.Text = $"Общая себестоимость: {totalCost:C}";
        DeliveryTimeTextBlock.Text = $"Мин. время доставки: {maxDeliveryDays} дн.";
        MinProductionTextBlock.Text = ""; // Расчет мин. времени производства — отдельно
    }

    private static int ParseDeliveryDays(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            if (int.TryParse(parts[i], out var n)) return n;
        return 0;
    }
}
