using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using uchebkaaa.Data;

namespace uchebkaaa
{
    /// <summary>
    /// Вспомогательный класс для работы со статусами заказов и историей их изменений.
    /// Хранение выполняется во внешнем JSON-файле, чтобы не изменять структуру базы данных.
    /// </summary>
    public static class OrderStatusHelper
    {
        public const string StatusNew = "Новый";
        public const string StatusCancelled = "Отменен";
        public const string StatusSpecification = "Составление спецификации";
        public const string StatusConfirmation = "Подтверждение";
        public const string StatusProcurement = "Закупка";
        public const string StatusProduction = "Производство";
        public const string StatusControl = "Контроль";
        public const string StatusReady = "Готов";
        public const string StatusClosed = "Закрыт";

        public static readonly IReadOnlyList<string> AllStatuses = new[]
        {
            StatusNew,
            StatusCancelled,
            StatusSpecification,
            StatusConfirmation,
            StatusProcurement,
            StatusProduction,
            StatusControl,
            StatusReady,
            StatusClosed
        };

        public class OrderStatusHistoryRecord
        {
            public DateTime ChangeTime { get; set; }
            public string Status { get; set; } = string.Empty;
            public string ChangedBy { get; set; } = string.Empty;
            public string? Comment { get; set; }
        }

        public class OrderStatusInfo
        {
            public DateOnly Date { get; set; }
            public int Number { get; set; }
            public string CurrentStatus { get; set; } = StatusNew;
            public List<OrderStatusHistoryRecord> History { get; set; } = new();
        }

        private static readonly object _lock = new();
        private static List<OrderStatusInfo>? _cache;

        private static string GetStoragePath()
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "OrderData");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Path.Combine(folder, "order_statuses.json");
        }

        private static List<OrderStatusInfo> LoadAllInternal()
        {
            if (_cache != null)
                return _cache;

            var path = GetStoragePath();
            if (!File.Exists(path))
            {
                _cache = new List<OrderStatusInfo>();
                return _cache;
            }

            try
            {
                var json = File.ReadAllText(path);
                _cache = JsonSerializer.Deserialize<List<OrderStatusInfo>>(json) ?? new List<OrderStatusInfo>();
            }
            catch
            {
                _cache = new List<OrderStatusInfo>();
            }

            return _cache;
        }

        private static void SaveAllInternal(List<OrderStatusInfo> items)
        {
            _cache = items;
            var path = GetStoragePath();
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        private static string GetCustomerInitials(User? customer)
        {
            if (customer == null || string.IsNullOrWhiteSpace(customer.Name))
                return "__";

            var parts = customer.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0] : string.Empty;
            var second = parts.Length > 1 ? parts[1] : string.Empty;

            char GetFirstCharOrUnderscore(string s) =>
                string.IsNullOrEmpty(s) ? '_' : char.ToUpperInvariant(s[0]);

            return $"{GetFirstCharOrUnderscore(first)}{GetFirstCharOrUnderscore(second)}";
        }

        /// <summary>
        /// Форматированный номер заказа (12 символов) по правилам задания.
        /// Основан на дате заказа, клиенте и порядковом номере.
        /// </summary>
        public static string GetFormattedOrderNumber(AppDbContext db, Order order)
        {
            var customer = db.Users.FirstOrDefault(u => u.Login == order.CustomerId);
            var initials = GetCustomerInitials(customer);

            var year = order.Date.Year % 100;
            var month = order.Date.Month;
            var day = order.Date.Day;

            // Порядковый номер заказа для данного заказчика
            var ordersForCustomer = db.Orders
                .Where(o => o.CustomerId == order.CustomerId)
                .OrderBy(o => o.Date)
                .ThenBy(o => o.Number)
                .ToList();

            var index = ordersForCustomer
                .FindIndex(o => o.Date == order.Date && o.Number == order.Number);

            var seq = index >= 0 ? (index + 1) : order.Number;
            if (seq > 99) seq = (seq % 99) + 1;

            return string.Format(
                "{0}{1}{2:00}{3:00}{4:00}{5:00}",
                initials[0],
                initials[1],
                year,
                month,
                day,
                seq);
        }

        public static OrderStatusInfo GetOrCreateStatus(Order order, string? defaultStatus = null)
        {
            lock (_lock)
            {
                var all = LoadAllInternal();
                var info = all.FirstOrDefault(i => i.Date == order.Date && i.Number == order.Number);
                if (info == null)
                {
                    info = new OrderStatusInfo
                    {
                        Date = order.Date,
                        Number = order.Number,
                        CurrentStatus = defaultStatus ?? StatusNew
                    };

                    info.History.Add(new OrderStatusHistoryRecord
                    {
                        ChangeTime = DateTime.Now,
                        Status = info.CurrentStatus,
                        ChangedBy = App.CurrentUser?.Login ?? "system",
                        Comment = "Создание заказа"
                    });

                    all.Add(info);
                    SaveAllInternal(all);
                }

                return info;
            }
        }

        public static void ChangeStatus(Order order, string newStatus, string? comment = null)
        {
            lock (_lock)
            {
                var all = LoadAllInternal();
                var info = all.FirstOrDefault(i => i.Date == order.Date && i.Number == order.Number);
                if (info == null)
                {
                    info = new OrderStatusInfo
                    {
                        Date = order.Date,
                        Number = order.Number,
                        CurrentStatus = newStatus
                    };
                    all.Add(info);
                }

                info.CurrentStatus = newStatus;
                info.History.Add(new OrderStatusHistoryRecord
                {
                    ChangeTime = DateTime.Now,
                    Status = newStatus,
                    ChangedBy = App.CurrentUser?.Login ?? "system",
                    Comment = comment
                });

                SaveAllInternal(all);
            }
        }

        public static string GetCurrentStatus(Order order)
        {
            lock (_lock)
            {
                var all = LoadAllInternal();
                var info = all.FirstOrDefault(i => i.Date == order.Date && i.Number == order.Number);
                return info?.CurrentStatus ?? StatusNew;
            }
        }

        public static IReadOnlyList<OrderStatusHistoryRecord> GetHistory(Order order)
        {
            lock (_lock)
            {
                var all = LoadAllInternal();
                var info = all.FirstOrDefault(i => i.Date == order.Date && i.Number == order.Number);
                return info?.History?.OrderBy(h => h.ChangeTime).ToList() ?? new List<OrderStatusHistoryRecord>();
            }
        }
    }
}

