using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages
{
    public partial class OrderMaterialsAndProductionDialog : Window
    {
        private readonly Order _order;

        private class MaterialRow
        {
            public string Kind { get; set; } = string.Empty;
            public string Article { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int RequiredQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public int MissingQuantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public string SupplierTimeRaw { get; set; } = string.Empty;
        }

        public OrderMaterialsAndProductionDialog(Order order)
        {
            _order = order;
            InitializeComponent();

            CloseButton.Click += CloseButton_Click;

            LoadData();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadData()
        {
            var db = App.DbContext;
            var displayNumber = OrderStatusHelper.GetFormattedOrderNumber(db, _order);
            HeaderTextBlock.Text = $"Анализ заказа {displayNumber}";

            // Материалы и доставка
            var materialsSummary = OrderAnalysisHelper.CalculateMaterialsSummary(db, _order);
            var rows = new List<MaterialRow>();
            foreach (var i in materialsSummary.Items)
            {
                rows.Add(new MaterialRow
                {
                    Kind = i.IsMaterial ? "Материал" : "Комплект.",
                    Article = i.Article,
                    Name = i.Name,
                    RequiredQuantity = i.RequiredQuantity,
                    AvailableQuantity = i.AvailableQuantity,
                    MissingQuantity = i.MissingQuantity,
                    PurchasePrice = i.PurchasePrice,
                    SupplierName = i.SupplierName,
                    SupplierTimeRaw = i.SupplierTimeRaw
                });
            }

            MaterialsDataGrid.ItemsSource = rows;

            var totalMissingCost = materialsSummary.TotalMissingCost;
            var days = materialsSummary.MinimalDeliveryDays;
            MaterialsSummaryTextBlock.Text =
                $"Общая стоимость недостающих позиций: {totalMissingCost:C}. " +
                $"Минимальное время доставки всего: {days} дн.";

            // Производство
            var productionSummary = OrderAnalysisHelper.CalculateProductionSummary(db, _order);
            ProductionDataGrid.ItemsSource = productionSummary.Operations;

            var totalMinutes = productionSummary.TotalMinutes;
            var totalHours = totalMinutes / 60.0;
            ProductionSummaryTextBlock.Text =
                $"Минимальное время производства заказа: примерно {totalMinutes:F0} мин (~{totalHours:F1} ч). " +
                $"Учитываются последовательность операций и занятость типов оборудования.";
        }
    }
}

