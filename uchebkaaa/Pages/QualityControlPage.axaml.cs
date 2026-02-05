using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class QualityControlPage : UserControl
{
    private readonly List<QualityParamRow> _rows = new();
    private Order? _selectedOrder;

    public QualityControlPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => MainWindow.NavigateTo(new MasterScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        OrderComboBox.SelectionChanged += OrderComboBox_SelectionChanged;
        AddParamButton.Click += AddParamButton_Click;
        SaveButton.Click += SaveButton_Click;
        FinishButton.Click += FinishButton_Click;
        RateButton.Click += RateButton_Click;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var orders = App.DbContext.Orders
            .Include(o => o.Product)
            .Where(o => o.Status == "Производство" || o.Status == "Контроль")
            .ToList();
        OrderComboBox.ItemsSource = orders;
        if (orders.Count > 0) OrderComboBox.SelectedIndex = 0;
    }

    private void OrderComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (OrderComboBox.SelectedItem is Order o)
        {
            _selectedOrder = o;
            LoadParams();
        }
    }

    private void LoadParams()
    {
        _rows.Clear();
        if (_selectedOrder == null) return;

        var db = App.DbContext;
        var paramsAll = db.QualityParameters.ToList();
        var checks = db.QualityChecks
            .Where(c => c.OrderNumber == _selectedOrder.Number)
            .Include(c => c.Parameter)
            .ToList();

        foreach (var p in paramsAll)
        {
            var check = checks.FirstOrDefault(c => c.ParameterId == p.Id);
            _rows.Add(new QualityParamRow
            {
                ParamId = p.Id,
                ParamName = p.Name,
                Result = check == null ? "—" : (check.IsAcceptable ? "+" : "−"),
                Comment = check?.Comment ?? "",
                IsAcceptable = check?.IsAcceptable
            });
        }

        ParamsDataGrid.ItemsSource = _rows.ToList();
    }

    private async void RateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ParamsDataGrid.SelectedItem is not QualityParamRow row || _selectedOrder == null) return;
        var dialog = new QualityRateDialog(row.ParamName, row.IsAcceptable, row.Comment);
        await dialog.ShowDialog((Window)this.VisualRoot!);
        if (!dialog.ResultAcceptable.HasValue) return;
        row.IsAcceptable = dialog.ResultAcceptable;
        row.Result = dialog.ResultAcceptable.Value ? "+" : "−";
        row.Comment = dialog.ResultComment ?? "";
        ParamsDataGrid.ItemsSource = null;
        ParamsDataGrid.ItemsSource = _rows.ToList();
    }

    private async void AddParamButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new QualityParamAddDialog();
        await dialog.ShowDialog((Window)this.VisualRoot!);
        var name = dialog.ResultName;
        if (string.IsNullOrWhiteSpace(name)) return;

        var db = App.DbContext;
        if (db.QualityParameters.Any(q => q.Name == name))
        {
            await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Ошибка", "Параметр уже существует", ButtonEnum.Ok).ShowWindowDialogAsync((Window)this.VisualRoot!);
            return;
        }

        var p = new QualityParameter { Name = name };
        db.QualityParameters.Add(p);
        db.SaveChanges();
        LoadParams();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveChecks();
    }

    private void SaveChecks()
    {
        if (_selectedOrder == null) return;
        var user = App.CurrentUser;
        if (user == null) return;

        var db = App.DbContext;
        foreach (var r in _rows)
        {
            var existing = db.QualityChecks.FirstOrDefault(c =>
                c.OrderNumber == _selectedOrder.Number && c.ParameterId == r.ParamId);
            if (r.IsAcceptable.HasValue)
            {
                if (existing == null)
                {
                    db.QualityChecks.Add(new QualityCheck
                    {
                        OrderNumber = _selectedOrder.Number,
                        ParameterId = r.ParamId,
                        IsAcceptable = r.IsAcceptable.Value,
                        Comment = r.IsAcceptable.Value ? null : r.Comment,
                        CheckDate = DateTime.Now,
                        CheckedBy = user.Login
                    });
                }
                else
                {
                    existing.IsAcceptable = r.IsAcceptable.Value;
                    existing.Comment = r.IsAcceptable.Value ? null : r.Comment;
                    existing.CheckDate = DateTime.Now;
                }
            }
        }
        db.SaveChanges();
    }

    private async void FinishButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedOrder == null) return;
        SaveChecks();

        var db = App.DbContext;
        var paramsAll = db.QualityParameters.Select(q => q.Id).ToList();
        var checks = db.QualityChecks.Where(c => c.OrderNumber == _selectedOrder.Number).ToList();

        var allPassed = paramsAll.All(pid => checks.Any(c => c.ParameterId == pid && c.IsAcceptable));
        if (!allPassed)
        {
            await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Ошибка",
                "Не все параметры имеют положительную оценку. Мастер не может сменить статус заказа.",
                ButtonEnum.Ok).ShowWindowDialogAsync((Window)this.VisualRoot!);
            return;
        }

        _selectedOrder.Status = "Готов";
        db.SaveChanges();
        LoadOrders();
    }

    private class QualityParamRow
    {
        public int ParamId { get; set; }
        public string ParamName { get; set; } = "";
        public string Result { get; set; } = "";
        public string Comment { get; set; } = "";
        public bool? IsAcceptable { get; set; }
    }
}
