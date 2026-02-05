using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages
{
    public partial class OrderHistoryDialog : Window
    {
        private readonly Order _order;

        public OrderHistoryDialog(Order order)
        {
            _order = order;
            InitializeComponent();

            CloseButton.Click += CloseButton_Click;

            var db = App.DbContext;
            var displayNumber = OrderStatusHelper.GetFormattedOrderNumber(db, order);
            HeaderTextBlock.Text = $"История статусов заказа {displayNumber}";

            var history = OrderStatusHelper
                .GetHistory(order)
                .OrderBy(h => h.ChangeTime)
                .ToList();

            HistoryDataGrid.ItemsSource = history;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

