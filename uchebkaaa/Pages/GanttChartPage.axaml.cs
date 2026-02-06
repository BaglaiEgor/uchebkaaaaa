using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
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
        LogoutButton.Click += (_, _) =>
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        };
        BuildButton.Click += BuildButton_Click;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var orders = App.DbContext.Orders
            .Include(o => o.Product)
            .Where(o => o.Status != "Новый" && o.Status != "Отменен")
            .ToList();

        OrderComboBox.ItemsSource = orders;
        if (orders.Count > 0)
            OrderComboBox.SelectedIndex = 0;
    }

    private async void BuildButton_Click(object? sender, RoutedEventArgs e)
    {
        if (OrderComboBox.SelectedItem is not Order order)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Ошибка", "Заказ не выбран", ButtonEnum.Ok, Icon.Error).ShowAsync();
            return;
        }

        var db = App.DbContext;

        var specOps = db.OperationSpecs
            .Where(o => o.ProductId == order.ProductId)
            .OrderBy(o => o.Number)
            .ToList();

        if (specOps.Count == 0)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Нет данных", "Для изделия нет операций",
                ButtonEnum.Ok, Icon.Warning).ShowAsync();
            return;
        }

        const int leftMargin = 140;
        const int topMargin = 40;
        const int rowH = 40;
        const int minBarWidth = 60;
        const int maxCanvasWidth = 1400;

        var ops = new List<(string Equip, string Op, int Start, int Dur)>();
        int time = 0;

        foreach (var so in specOps)
        {
            ops.Add((so.EquipmentType, so.Operation, time, so.OperationTime));
            time += so.OperationTime;
        }

        var equipments = ops.Select(o => o.Equip).Distinct().ToList();
        var maxTime = ops.Max(o => o.Start + o.Dur);
        var maxDur = ops.Max(o => o.Dur);

        double scale = (double)(maxCanvasWidth - leftMargin) / maxTime;
        scale = Math.Max(scale, (double)minBarWidth / maxDur);

        GanttCanvas.Children.Clear();
        GanttCanvas.Width = leftMargin + maxTime * scale;
        GanttCanvas.Height = topMargin + equipments.Count * rowH;

        for (int i = 0; i < equipments.Count; i++)
        {
            var tb = new TextBlock
            {
                Text = equipments[i],
                FontSize = 12,
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(tb, 10);
            Canvas.SetTop(tb, topMargin + i * rowH + 10);
            GanttCanvas.Children.Add(tb);
        }

        foreach (var (equip, op, start, dur) in ops)
        {
            int row = equipments.IndexOf(equip);

            var rect = new Rectangle
            {
                Width = dur * scale,
                Height = rowH - 6,
                Fill = new SolidColorBrush(Color.FromRgb(72, 121, 172)),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(rect, leftMargin + start * scale);
            Canvas.SetTop(rect, topMargin + row * rowH + 3);
            GanttCanvas.Children.Add(rect);

            var label = new TextBlock
            {
                Text = op,
                Width = rect.Width - 6,
                FontSize = 10,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(label, leftMargin + start * scale + 3);
            Canvas.SetTop(label, topMargin + row * rowH + 10);
            GanttCanvas.Children.Add(label);
        }

        await MessageBoxManager.GetMessageBoxStandard(
            "Готово", "Диаграмма Ганта построена",
            ButtonEnum.Ok, Icon.Success).ShowAsync();
    }
}
