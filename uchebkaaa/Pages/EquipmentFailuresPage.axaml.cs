using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class EquipmentFailuresPage : UserControl
{
    public EquipmentFailuresPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => MainWindow.NavigateTo(new MasterScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        AddButton.Click += AddButton_Click;
        FinishButton.Click += FinishButton_Click;
        LoadFailures();
    }

    private void LoadFailures()
    {
        var list = App.DbContext.EquipmentFailures
            .OrderByDescending(f => f.StartTime)
            .ToList()
            .Select(f => new
            {
                f.Id,
                f.EquipmentMark,
                f.Reason,
                f.Description,
                f.StartTime,
                f.EndTime,
                StartTimeStr = f.StartTime.ToString("dd.MM.yyyy HH:mm"),
                EndTimeStr = f.EndTime?.ToString("dd.MM.yyyy HH:mm") ?? "—"
            }).ToList();
        FailuresDataGrid.ItemsSource = list;
    }

    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new EquipmentFailureDialog(null);
        await dialog.ShowDialog((Window)this.VisualRoot!);
        LoadFailures();
    }

    private async void FinishButton_Click(object? sender, RoutedEventArgs e)
    {
        if (FailuresDataGrid.SelectedItem == null) return;
        dynamic item = FailuresDataGrid.SelectedItem;
        int id = item.Id;
        var failure = App.DbContext.EquipmentFailures.Find(id);
        if (failure == null || failure.EndTime != null) return;
        var dialog = new EquipmentFailureDialog(failure, true);
        await dialog.ShowDialog((Window)this.VisualRoot!);
        LoadFailures();
    }
}
