using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using System;
using uchebkaaa.Data;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace uchebkaaa.Pages
{
    public partial class EmployeeManagementPage : UserControl
    {
        public EmployeeManagementPage()
        {
            InitializeComponent();
            LoadEmployees();
            BackButton.Click += BackButton_Click;
            AddButton.Click += AddButton_Click;
            EditButton.Click += EditButton_Click;
            DeleteButton.Click += DeleteButton_Click;
            LogoutButton.Click += LogoutButton_Click;
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo(new DirectorScreen());
        }

        private void LoadEmployees()
        {
            var employees = App.DbContext.Employees
                .Include(e => e.Operations)
                .Select(e => new
                {
                    e.Id,
                    e.LastName,
                    e.FirstName,
                    e.MiddleName,
                    Age = CalculateAge(e.BirthDate),
                    OperationsString = string.Join(", ", e.Operations.Select(o => o.Name))
                })
                .ToList();

            EmployeesDataGrid.ItemsSource = employees;
            StatusTextBlock.Text = $"Всего работников: {employees.Count}";
        }

        private static int CalculateAge(DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age;
        }

        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new EmployeeEditDialog(null);
            await dialog.ShowDialog((Window)this.VisualRoot!);
            LoadEmployees();
        }

        private async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem == null)
            {
                ShowMessage("Выберите работника для редактирования");
                return;
            }

            dynamic selected = EmployeesDataGrid.SelectedItem;
            int id = selected.Id;
            var employee = App.DbContext.Employees
                .Include(e => e.Operations)
                .FirstOrDefault(e => e.Id == id);

            if (employee != null)
            {
                var dialog = new EmployeeEditDialog(employee);
                await dialog.ShowDialog((Window)this.VisualRoot!);
                LoadEmployees();
            }
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem == null)
            {
                ShowMessage("Выберите работника для удаления");
                return;
            }

            dynamic selected = EmployeesDataGrid.SelectedItem;
            int id = selected.Id;
            var employee = App.DbContext.Employees.FirstOrDefault(e => e.Id == id);

            if (employee != null)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Подтверждение",
                    "Вы уверены, что хотите удалить этого работника?",
                    ButtonEnum.YesNo);
                var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot!);
                if (result == ButtonResult.Yes)
                {
                    App.DbContext.Employees.Remove(employee);
                    App.DbContext.SaveChanges();
                    LoadEmployees();
                }
            }
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.Logout();
            MainWindow.NavigateTo(new LoginPage());
        }

        private async void ShowMessage(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Информация", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot!);
        }
    }
}
