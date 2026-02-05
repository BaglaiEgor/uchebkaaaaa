using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class OrderStatusDialog : Window
{
    private readonly Order _order;
    private readonly string _oldStatus;

    public OrderStatusDialog(Order order)
    {
        InitializeComponent();
        _order = order;
        _oldStatus = order.Status;

        CurrentStatusTextBlock.Text = _oldStatus;
        StatusComboBox.ItemsSource = GetAvailableStatuses(_oldStatus);
        if (StatusComboBox.ItemCount > 0)
            StatusComboBox.SelectedIndex = 0;

        SaveButton.Click += SaveButton_Click;
        CancelButton.Click += (_, _) => Close();
    }

    private static IList<string> GetAvailableStatuses(string current)
    {
        return current switch
        {
            "Новый" => new[] { "Составление спецификации", "Отменен" },
            "Составление спецификации" => new[] { "Подтверждение" },
            "Подтверждение" => new[] { "Отклонен", "Закупка" },
            "Закупка" => new[] { "Производство" },
            "Производство" => new[] { "Контроль" },
            "Контроль" => new[] { "Готов" },
            "Готов" => new[] { "Закрыт" },
            _ => Array.Empty<string>()
        };
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (StatusComboBox.SelectedItem is not string newStatus)
        {
            ShowError("Выберите новый статус.");
            return;
        }

        var user = App.CurrentUser;
        if (user == null || user.Role != "Менеджер")
        {
            ShowError("Только менеджер может менять статус.");
            return;
        }

        var db = App.DbContext;

        // Получаем свежий экземпляр заказа
        var order = db.Orders.FirstOrDefault(o => o.Date == _order.Date && o.Number == _order.Number);
        if (order == null)
        {
            ShowError("Заказ не найден.");
            return;
        }

        // Проверка перехода статуса
        var allowed = GetAvailableStatuses(order.Status);
        if (!allowed.Contains(newStatus))
        {
            ShowError("Неверный переход статуса.");
            return;
        }

        // При переходе к составлению спецификации назначаем менеджера
        if (order.Status == "Новый" && newStatus == "Составление спецификации")
        {
            order.ManagerId = user.Login;
        }

        // При переходе Закупка -> Производство списываем материалы/комплектующие
        if (order.Status == "Закупка" && newStatus == "Производство")
        {
            if (!TryConsumeMaterialsForOrder(order, out var error))
            {
                ShowError(error ?? "Ошибка при списании материалов.");
                return;
            }
        }

        // Сохраняем историю
        var history = new OrderStatusHistory
        {
            OrderNumber = order.Number,
            OrderDate = order.Date,
            OldStatus = order.Status,
            NewStatus = newStatus,
            ChangedAt = DateTime.Now,
            ChangedBy = user.Login,
            Comment = CommentTextBox.Text
        };
        db.OrderStatusHistories.Add(history);

        order.Status = newStatus;
        db.SaveChanges();
        Close();
    }

    private static bool TryConsumeMaterialsForOrder(Order order, out string? error)
    {
        var db = App.DbContext;
        error = null;

        // Суммарные требования
        var materialCounts = new Dictionary<string, int>();
        var accessoryCounts = new Dictionary<string, int>();

        void AccumulateForProduct(string productId, int factor)
        {
            var matSpecs = db.MaterialSpecs.Where(ms => ms.ProductId == productId).ToList();
            foreach (var ms in matSpecs)
            {
                if (!materialCounts.ContainsKey(ms.MaterialId))
                    materialCounts[ms.MaterialId] = 0;
                materialCounts[ms.MaterialId] += ms.Count * factor;
            }

            var accSpecs = db.AccessoriesSpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var a in accSpecs)
            {
                if (!accessoryCounts.ContainsKey(a.AccessoriesId))
                    accessoryCounts[a.AccessoriesId] = 0;
                accessoryCounts[a.AccessoriesId] += a.Count * factor;
            }

            var assemblySpecs = db.AssemblySpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var a in assemblySpecs)
            {
                AccumulateForProduct(a.ItemId, factor * a.Count);
            }
        }

        AccumulateForProduct(order.ProductId, 1);

        // Проверяем наличие материалов
        foreach (var kv in materialCounts)
        {
            var material = db.Materials.FirstOrDefault(m => m.Article == kv.Key);
            if (material == null)
            {
                error = $"Материал с артикулом {kv.Key} не найден.";
                return false;
            }

            if (material.Count < kv.Value)
            {
                error = $"Недостаточно материала {material.Name} (требуется {kv.Value}, доступно {material.Count}).";
                return false;
            }
        }

        // Проверяем наличие комплектующих
        foreach (var kv in accessoryCounts)
        {
            var accessory = db.Accessories.FirstOrDefault(a => a.Article == kv.Key);
            if (accessory == null)
            {
                error = $"Комплектующее с артикулом {kv.Key} не найдено.";
                return false;
            }

            if (accessory.Count < kv.Value)
            {
                error = $"Недостаточно комплектующих {accessory.Name} (требуется {kv.Value}, доступно {accessory.Count}).";
                return false;
            }
        }

        // Если всего хватает — списываем
        foreach (var kv in materialCounts)
        {
            var material = db.Materials.First(m => m.Article == kv.Key);
            material.Count -= kv.Value;
        }

        foreach (var kv in accessoryCounts)
        {
            var accessory = db.Accessories.First(a => a.Article == kv.Key);
            accessory.Count -= kv.Value;
        }

        return true;
    }
}

