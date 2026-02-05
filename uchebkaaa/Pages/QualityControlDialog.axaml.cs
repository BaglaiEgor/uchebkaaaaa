using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages
{
    public partial class QualityControlDialog : Window
    {
        private readonly Order _order;

        private class QualityCheckViewModel
        {
            public int? Id { get; set; }
            public int? ParameterId { get; set; }
            public bool IsAcceptable { get; set; }
            public string? Comment { get; set; }
        }

        private List<QualityCheckViewModel> _items = new();
        private List<QualityParameter> _parameters = new();

        public QualityControlDialog(Order order)
        {
            _order = order;
            InitializeComponent();

            AddRowButton.Click += AddRowButton_Click;
            RemoveRowButton.Click += RemoveRowButton_Click;

            OkButton.Click += OkButton_Click;
            CancelButton.Click += CancelButton_Click;

            LoadData();
        }

        private void LoadData()
        {
            var db = App.DbContext;
            var displayNumber = OrderStatusHelper.GetFormattedOrderNumber(db, _order);
            HeaderTextBlock.Text = $"Контроль качества заказа {displayNumber}";

            _parameters = db.QualityParameters
                .OrderBy(p => p.Name)
                .ToList();

            var existing = db.QualityChecks
                .Where(q => q.OrderNumber == _order.Number)
                .ToList();

            _items = existing
                .Select(q => new QualityCheckViewModel
                {
                    Id = q.Id,
                    ParameterId = q.ParameterId,
                    IsAcceptable = q.IsAcceptable,
                    Comment = q.Comment
                })
                .ToList();

            ChecksDataGrid.ItemsSource = _items;
        }

        private void AddRowButton_Click(object? sender, RoutedEventArgs e)
        {
            var item = new QualityCheckViewModel
            {
                IsAcceptable = true
            };

            // Если есть справочник параметров качества, пытаемся подобрать первый ещё не использованный параметр
            if (_parameters.Count > 0)
            {
                var usedIds = new HashSet<int?>(_items.Select(i => i.ParameterId));
                var nextParam = _parameters.FirstOrDefault(p => !usedIds.Contains(p.Id));
                if (nextParam != null)
                {
                    item.ParameterId = nextParam.Id;
                }
            }

            _items.Add(item);

            ChecksDataGrid.ItemsSource = null;
            ChecksDataGrid.ItemsSource = _items;
        }

        private void RemoveRowButton_Click(object? sender, RoutedEventArgs e)
        {
            if (ChecksDataGrid.SelectedItem is QualityCheckViewModel vm)
            {
                _items.Remove(vm);
                ChecksDataGrid.ItemsSource = null;
                ChecksDataGrid.ItemsSource = _items;
            }
        }

        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            // Обновляем список из DataGrid
            ChecksDataGrid.CommitEdit();

            var currentItems = new List<QualityCheckViewModel>();
            foreach (var obj in ChecksDataGrid.ItemsSource as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (obj is QualityCheckViewModel vm)
                {
                    if (vm.ParameterId == null)
                        continue;
                    currentItems.Add(vm);
                }
            }

            // Проверяем, что по заказу нет дубликатов параметров качества
            var duplicateParam = currentItems
                .Where(i => i.ParameterId != null)
                .GroupBy(i => i.ParameterId)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateParam != null)
            {
                ErrorTextBlock.Text = "Каждый параметр качества может быть указан только один раз для заказа.";
                return;
            }

            // Необязательно, но можем подсказать, если нет записей
            if (currentItems.Count == 0)
            {
                ErrorTextBlock.Text = "Добавьте хотя бы один параметр контроля.";
                return;
            }

            var db = App.DbContext;

            // Загружаем существующие проверки
            var existing = db.QualityChecks
                .Where(q => q.OrderNumber == _order.Number)
                .ToList();

            // Удаляем те, которых нет в currentItems
            foreach (var ex in existing)
            {
                if (!currentItems.Any(i => i.Id == ex.Id))
                {
                    db.QualityChecks.Remove(ex);
                }
            }

            // Добавляем/обновляем
            foreach (var vm in currentItems)
            {
                if (vm.ParameterId == null)
                    continue;

                QualityCheck? entity = null;

                if (vm.Id.HasValue)
                {
                    entity = existing.FirstOrDefault(e => e.Id == vm.Id.Value);
                }

                if (entity == null)
                {
                    entity = new QualityCheck
                    {
                        OrderNumber = _order.Number,
                        ParameterId = vm.ParameterId,
                        CheckDate = DateTime.Now,
                        CheckedBy = App.CurrentUser?.Name ?? App.CurrentUser?.Login ?? "master"
                    };
                    db.QualityChecks.Add(entity);
                }

                entity.ParameterId = vm.ParameterId;
                entity.IsAcceptable = vm.IsAcceptable;
                entity.Comment = string.IsNullOrWhiteSpace(vm.Comment) ? null : vm.Comment.Trim();
                entity.CheckDate = DateTime.Now;
                entity.CheckedBy = App.CurrentUser?.Name ?? App.CurrentUser?.Login ?? "master";
            }

            await db.SaveChangesAsync();
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}

