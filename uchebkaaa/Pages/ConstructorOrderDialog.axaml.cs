using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class ConstructorOrderDialog : Window
{
    private readonly Order _order;

    public ConstructorOrderDialog(Order order)
    {
        InitializeComponent();
        _order = order;
        CostTextBox.Text = order.Cost.ToString(CultureInfo.InvariantCulture);
        EndDatePicker.SelectedDate = order.EndDate.ToDateTime(TimeOnly.MinValue);

        SaveButton.Click += SaveButton_Click;
        CancelButton.Click += (_, _) => Close();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(CostTextBox.Text?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var cost))
            return;
        if (EndDatePicker.SelectedDate == null) return;

        var db = App.DbContext;
        var order = db.Orders.Find(_order.Date, _order.Number);
        if (order == null || order.Status != "Составление спецификации") return;

        order.Cost = cost;
        order.EndDate = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value.DateTime);
        order.Status = "Подтверждение";
        db.SaveChanges();
        Close();
    }
}
