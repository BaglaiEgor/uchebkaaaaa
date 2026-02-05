using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;
using Microsoft.EntityFrameworkCore;

namespace uchebkaaa.Pages
{
    public partial class EmployeeEditDialog : Window
    {
        private readonly Employee? _employee;
        private readonly List<CheckBox> _operationCheckBoxes = new();

        public EmployeeEditDialog(Employee? employee)
        {
            InitializeComponent();
            _employee = employee;

            LoadOperations();
            if (_employee != null)
            {
                LoadEmployeeData();
            }

            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        private void LoadOperations()
        {
            var operations = App.DbContext.ProductionOperations.ToList();
            _operationCheckBoxes.Clear();
            OperationsStackPanel.Children.Clear();

            foreach (var operation in operations)
            {
                var checkBox = new CheckBox
                {
                    Content = operation.Name,
                    Tag = operation.Id,
                    Margin = new Avalonia.Thickness(0, 2, 0, 2)
                };
                _operationCheckBoxes.Add(checkBox);
                OperationsStackPanel.Children.Add(checkBox);
            }
        }

        private void LoadEmployeeData()
        {
            if (_employee == null) return;

            var employee = App.DbContext.Employees
                .Include(e => e.Operations)
                .FirstOrDefault(e => e.Id == _employee.Id);

            if (employee == null) return;

            LastNameTextBox.Text = employee.LastName;
            FirstNameTextBox.Text = employee.FirstName;
            MiddleNameTextBox.Text = employee.MiddleName;
            BirthDatePicker.SelectedDate = employee.BirthDate.ToDateTime(TimeOnly.MinValue);
            HomeAddressTextBox.Text = employee.HomeAddress;
            EducationTextBox.Text = employee.Education;
            QualificationTextBox.Text = employee.Qualification;

            foreach (var checkBox in _operationCheckBoxes)
            {
                if (checkBox.Tag is int opId)
                {
                    checkBox.IsChecked = employee.Operations.Any(eo => eo.Id == opId);
                }
            }
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || 
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                ShowError("Заполните обязательные поля: Фамилия и Имя");
                return;
            }

            if (BirthDatePicker.SelectedDate == null)
            {
                ShowError("Выберите дату рождения");
                return;
            }

            Employee employee;
            if (_employee == null)
            {
                employee = new Employee();
                App.DbContext.Employees.Add(employee);
            }
            else
            {
                employee = App.DbContext.Employees
                    .Include(e => e.Operations)
                    .FirstOrDefault(e => e.Id == _employee.Id) ?? _employee;
            }

            employee.LastName = LastNameTextBox.Text!;
            employee.FirstName = FirstNameTextBox.Text!;
            employee.MiddleName = MiddleNameTextBox.Text;
            employee.BirthDate = DateOnly.FromDateTime(BirthDatePicker.SelectedDate!.Value.DateTime);
            employee.HomeAddress = HomeAddressTextBox.Text;
            employee.Education = EducationTextBox.Text;
            employee.Qualification = QualificationTextBox.Text;

            employee.Operations.Clear();
            foreach (var checkBox in _operationCheckBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is int opId)
                {
                    var operation = App.DbContext.ProductionOperations.Find(opId);
                    if (operation != null)
                    {
                        employee.Operations.Add(operation);
                    }
                }
            }

            App.DbContext.SaveChanges();
            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.IsVisible = true;
        }
    }
}
