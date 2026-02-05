using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class EquipmentFailureDialog : Window
{
    private readonly EquipmentFailure? _failure;
    private readonly bool _finishOnly;

    public EquipmentFailureDialog(EquipmentFailure? failure, bool finishOnly = false)
    {
        InitializeComponent();
        _failure = failure;
        _finishOnly = finishOnly;

        var equipments = App.DbContext.Equipments.ToList();
        EquipmentComboBox.ItemsSource = equipments;
        EquipmentComboBox.IsEnabled = !finishOnly;

        if (failure != null)
        {
            Title = finishOnly ? "Завершение сбоя" : "Редактирование сбоя";
            var eq = equipments.FirstOrDefault(e => e.Mark == failure.EquipmentMark);
            if (eq != null) EquipmentComboBox.SelectedItem = eq;
            ReasonTextBox.Text = failure.Reason;
            DescriptionTextBox.Text = failure.Description;
            StartDatePicker.SelectedDate = failure.StartTime.Date;
        }
        else
        {
            StartDatePicker.SelectedDate = DateTime.Today;
        }

        if (finishOnly)
        {
            StartDatePicker.IsEnabled = false;
            ReasonTextBox.IsReadOnly = true;
        }

        SaveButton.Click += SaveButton_Click;
        CancelButton.Click += (_, _) => Close();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var user = App.CurrentUser;
        if (user == null) return;

        if (_finishOnly && _failure != null)
        {
            _failure.EndTime = DateTime.Now;
            App.DbContext.SaveChanges();
            Close();
            return;
        }

        var reason = ReasonTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(reason))
        {
            ReasonTextBox.Focus();
            return;
        }

        var eq = EquipmentComboBox.SelectedItem as Equipment;
        if (eq == null) return;

        var startDate = StartDatePicker.SelectedDate?.DateTime ?? DateTime.Today;
        var start = new DateTime(startDate.Year, startDate.Month, startDate.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);

        if (_failure == null)
        {
            var f = new EquipmentFailure
            {
                EquipmentMark = eq.Mark,
                StartTime = start,
                Reason = reason,
                Description = DescriptionTextBox.Text?.Trim(),
                ReportedBy = user.Login
            };
            App.DbContext.EquipmentFailures.Add(f);
        }
        else
        {
            _failure.EquipmentMark = eq.Mark;
            _failure.StartTime = start;
            _failure.Reason = reason;
            _failure.Description = DescriptionTextBox.Text?.Trim();
        }

        App.DbContext.SaveChanges();
        Close();
    }
}
