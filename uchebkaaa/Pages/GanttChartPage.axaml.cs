using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class GanttChartPage : UserControl
{
    public GanttChartPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => MainWindow.NavigateTo(new ManagerScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        BuildButton.Click += BuildButton_Click;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var orders = App.DbContext.Orders.Include(o => o.Product)
            .Where(o => o.Status != "Новый" && o.Status != "Отменен")
            .ToList();
        OrderComboBox.ItemsSource = orders;
        if (orders.Count > 0) OrderComboBox.SelectedIndex = 0;
    }

    private async void BuildButton_Click(object? sender, RoutedEventArgs e)
    {
        if (OrderComboBox.SelectedItem is not Order order)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Ошибка", "Заказ не выбран", ButtonEnum.Ok, Icon.Error)
                .ShowAsync();
            return;
        }

        var db = App.DbContext;

        await MessageBoxManager
            .GetMessageBoxStandard(
                "Отладка",
                $"Заказ: {order.Name}\nProductId: {order.ProductId}",
                ButtonEnum.Ok,
                Icon.Info)
            .ShowAsync();

        var ops = new List<(string Equipment, string Product, string Operation, int Start, int Duration)>();

        var specOps = db.OperationSpecs
            .Where(o => o.ProductId == order.ProductId)
            .OrderBy(o => o.Number)
            .ToList();

        if (specOps.Count == 0)
        {
            await MessageBoxManager
                .GetMessageBoxStandard(
                    "Нет данных",
                    $"Для изделия \"{order.ProductId}\" нет операций в OperationSpec",
                    ButtonEnum.Ok,
                    Icon.Warning)
                .ShowAsync();
            return;
        }

        int t = 0;
        foreach (var so in specOps)
        {
            ops.Add((so.EquipmentType, so.ProductId, so.Operation, t, so.OperationTime));
            t += so.OperationTime;
        }

        var equipments = ops.Select(x => x.Equipment).Distinct().ToList();

        const int rowH = 40;
        const int colW = 4;
        var maxTime = ops.Max(x => x.Start + x.Duration);

        GanttCanvas.Children.Clear();
        GanttCanvas.Width = 60 + maxTime * colW;
        GanttCanvas.Height = 40 + equipments.Count * rowH;

        // Названия оборудования
        for (int i = 0; i < equipments.Count; i++)
        {
            var tb = new TextBlock
            {
                Text = equipments[i],
                FontSize = 12,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(tb, 5);
            Canvas.SetTop(tb, 40 + i * rowH);
            GanttCanvas.Children.Add(tb);
        }

        // Прямоугольники
        foreach (var (equip, product, operation, start, duration) in ops)
        {
            var row = equipments.IndexOf(equip);

            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = duration * colW,
                Height = rowH - 6,
                Fill = new SolidColorBrush(Color.FromRgb(72, 121, 172)),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(rect, 60 + start * colW);
            Canvas.SetTop(rect, 35 + row * rowH);
            GanttCanvas.Children.Add(rect);

            var label = new TextBlock
            {
                Text = operation,
                FontSize = 10,
                Foreground = Brushes.White
            };

            Canvas.SetLeft(label, 65 + start * colW);
            Canvas.SetTop(label, 45 + row * rowH);
            GanttCanvas.Children.Add(label);
        }

        await MessageBoxManager
            .GetMessageBoxStandard(
                "Готово",
                "Диаграмма Ганта построена",
                ButtonEnum.Ok,
                Icon.Success)
            .ShowAsync();
    }
}
