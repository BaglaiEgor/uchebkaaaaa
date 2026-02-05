using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa
{
    /// <summary>
    /// Вспомогательный класс для анализа заказа:
    /// расчет требований к материалам/комплектующим, времени доставки
    /// и минимального времени производства с учетом спецификаций.
    /// </summary>
    public static class OrderAnalysisHelper
    {
        public class RequiredItemInfo
        {
            public bool IsMaterial { get; set; }
            public string Article { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int RequiredQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public int MissingQuantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal CostPrice { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public string SupplierTimeRaw { get; set; } = string.Empty;
            public int SupplierTimeDays { get; set; }
        }

        public class MaterialsSummary
        {
            public List<RequiredItemInfo> Items { get; set; } = new();
            public decimal TotalMissingCost { get; set; }
            public int MinimalDeliveryDays { get; set; }
        }

        public class ScheduledOperation
        {
            public string ProductId { get; set; } = string.Empty;
            public string Operation { get; set; } = string.Empty;
            public string EquipmentType { get; set; } = string.Empty;
            public int Number { get; set; }
            public double StartMinute { get; set; }
            public double DurationMinutes { get; set; }
            public double FinishMinute { get; set; }
        }

        public class ProductionSummary
        {
            public List<ScheduledOperation> Operations { get; set; } = new();
            public double TotalMinutes { get; set; }
        }

        /// <summary>
        /// Расчет требований к материалам/комплектующим и сроков доставки.
        /// </summary>
        public static MaterialsSummary CalculateMaterialsSummary(AppDbContext db, Order order)
        {
            var materialReqs = new Dictionary<string, int>();
            var accessoryReqs = new Dictionary<string, int>();

            if (!string.IsNullOrWhiteSpace(order.ProductId))
            {
                BuildRequirementsRecursive(db, order.ProductId, 1, materialReqs, accessoryReqs);
            }

            var result = new MaterialsSummary();

            // Материалы
            foreach (var kv in materialReqs)
            {
                var article = kv.Key;
                var required = kv.Value;
                var material = db.Materials.FirstOrDefault(m => m.Article == article);
                if (material == null)
                    continue;

                var available = db.MaterialWarehouses
                    .Where(mw => mw.MaterialArticle == article)
                    .Sum(mw => mw.Quantity);

                var missing = Math.Max(0, required - available);
                var supplier = material.Supplier;

                var info = new RequiredItemInfo
                {
                    IsMaterial = true,
                    Article = material.Article,
                    Name = material.Name,
                    Unit = material.Unit,
                    Type = material.ProductType,
                    RequiredQuantity = required,
                    AvailableQuantity = available,
                    MissingQuantity = missing,
                    PurchasePrice = material.Price ?? 0m,
                    CostPrice = material.Price ?? 0m,
                    SupplierName = supplier?.Name ?? string.Empty,
                    SupplierTimeRaw = supplier?.SupplyTime ?? string.Empty,
                    SupplierTimeDays = ParseDays(supplier?.SupplyTime)
                };

                result.Items.Add(info);
            }

            // Комплектующие
            foreach (var kv in accessoryReqs)
            {
                var article = kv.Key;
                var required = kv.Value;
                var accessory = db.Accessories.FirstOrDefault(a => a.Article == article);
                if (accessory == null)
                    continue;

                var available = db.ComponentWarehouses
                    .Where(cw => cw.ComponentArticle == article)
                    .Sum(cw => cw.Quantity);

                var missing = Math.Max(0, required - available);
                var supplier = accessory.Supplier;

                var info = new RequiredItemInfo
                {
                    IsMaterial = false,
                    Article = accessory.Article,
                    Name = accessory.Name,
                    Unit = accessory.Unit ?? string.Empty,
                    Type = accessory.ProductType,
                    RequiredQuantity = required,
                    AvailableQuantity = available,
                    MissingQuantity = missing,
                    PurchasePrice = accessory.Price ?? 0m,
                    CostPrice = accessory.Price ?? 0m,
                    SupplierName = supplier?.Name ?? string.Empty,
                    SupplierTimeRaw = supplier?.SupplyTime ?? string.Empty,
                    SupplierTimeDays = ParseDays(supplier?.SupplyTime)
                };

                result.Items.Add(info);
            }

            // Общая стоимость недостающих позиций
            result.TotalMissingCost = result.Items
                .Where(i => i.MissingQuantity > 0)
                .Sum(i => i.MissingQuantity * i.PurchasePrice);

            // Минимальное время доставки – максимум времени среди недостающих
            result.MinimalDeliveryDays = result.Items
                .Where(i => i.MissingQuantity > 0)
                .Select(i => i.SupplierTimeDays)
                .DefaultIfEmpty(0)
                .Max();

            return result;
        }

        private static void BuildRequirementsRecursive(
            AppDbContext db,
            string productId,
            int productCount,
            Dictionary<string, int> materialReqs,
            Dictionary<string, int> accessoryReqs)
        {
            var materialSpecs = db.MaterialSpecs.Where(ms => ms.ProductId == productId).ToList();
            foreach (var ms in materialSpecs)
            {
                var total = ms.Count * productCount;
                if (!materialReqs.ContainsKey(ms.MaterialId))
                    materialReqs[ms.MaterialId] = 0;
                materialReqs[ms.MaterialId] += total;
            }

            var accessoriesSpecs = db.AccessoriesSpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var ac in accessoriesSpecs)
            {
                var total = ac.Count * productCount;
                if (!accessoryReqs.ContainsKey(ac.AccessoriesId))
                    accessoryReqs[ac.AccessoriesId] = 0;
                accessoryReqs[ac.AccessoriesId] += total;
            }

            var assemblySpecs = db.AssemblySpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var asm in assemblySpecs)
            {
                var requiredCount = asm.Count * productCount;
                BuildRequirementsRecursive(db, asm.ItemId, requiredCount, materialReqs, accessoryReqs);
            }
        }

        private static int ParseDays(string? supplyTime)
        {
            if (string.IsNullOrWhiteSpace(supplyTime))
                return 0;

            // Пытаемся извлечь первое число из строки
            var digits = new string(supplyTime.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                digits = new string(supplyTime.Where(char.IsDigit).ToArray());
            }

            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
                return days;

            return 0;
        }

        /// <summary>
        /// Расчет минимального времени производства заказа и расписания операций (для диаграммы Ганта).
        /// </summary>
        public static ProductionSummary CalculateProductionSummary(AppDbContext db, Order order)
        {
            var result = new ProductionSummary();
            if (string.IsNullOrWhiteSpace(order.ProductId))
                return result;

            // Считаем, сколько единиц каждого изделия требуется для заказа
            var productUnits = new Dictionary<string, int>();
            BuildProductUnitsRecursive(db, order.ProductId, 1, productUnits);

            // Строим операции
            var tasks = BuildOperationTasks(db, productUnits);

            // Планируем операции по оборудованию
            ScheduleTasks(tasks);

            result.Operations = tasks
                .OrderBy(t => t.EquipmentType)
                .ThenBy(t => t.Start)
                .Select(t => new ScheduledOperation
                {
                    ProductId = t.ProductId,
                    Operation = t.Operation,
                    EquipmentType = t.EquipmentType,
                    Number = t.Number,
                    StartMinute = t.Start,
                    DurationMinutes = t.Duration,
                    FinishMinute = t.Finish
                })
                .ToList();

            result.TotalMinutes = result.Operations
                .Select(o => o.FinishMinute)
                .DefaultIfEmpty(0)
                .Max();

            return result;
        }

        private static void BuildProductUnitsRecursive(
            AppDbContext db,
            string productId,
            int count,
            Dictionary<string, int> productUnits)
        {
            if (!productUnits.ContainsKey(productId))
                productUnits[productId] = 0;
            productUnits[productId] += count;

            var assemblySpecs = db.AssemblySpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var asm in assemblySpecs)
            {
                var childCount = asm.Count * count;
                BuildProductUnitsRecursive(db, asm.ItemId, childCount, productUnits);
            }
        }

        private sealed class TaskInfo
        {
            public int Id { get; set; }
            public string ProductId { get; set; } = string.Empty;
            public string Operation { get; set; } = string.Empty;
            public string EquipmentType { get; set; } = string.Empty;
            public int Number { get; set; }
            public double Duration { get; set; }
            public List<int> PrereqIds { get; set; } = new();
            public double Start { get; set; }
            public double Finish { get; set; }
        }

        private static List<TaskInfo> BuildOperationTasks(AppDbContext db, Dictionary<string, int> productUnits)
        {
            var tasks = new List<TaskInfo>();
            var nextId = 1;

            // Сначала создаем задачи для всех операций
            var productFirstTasks = new Dictionary<string, List<TaskInfo>>();
            var productLastTasks = new Dictionary<string, List<TaskInfo>>();

            foreach (var kv in productUnits)
            {
                var productId = kv.Key;
                var units = kv.Value;

                var ops = db.OperationSpecs
                    .Where(o => o.ProductId == productId)
                    .OrderBy(o => o.Number)
                    .ToList();

                if (ops.Count == 0)
                    continue;

                TaskInfo? prev = null;
                foreach (var op in ops)
                {
                    var task = new TaskInfo
                    {
                        Id = nextId++,
                        ProductId = productId,
                        Operation = op.Operation,
                        EquipmentType = op.EquipmentType,
                        Number = op.Number,
                        Duration = op.OperationTime * Math.Max(1, units)
                    };

                    if (prev != null)
                    {
                        task.PrereqIds.Add(prev.Id);
                    }

                    tasks.Add(task);
                    prev ??= task;

                    if (!productFirstTasks.ContainsKey(productId))
                        productFirstTasks[productId] = new List<TaskInfo>();
                    if (productFirstTasks[productId].Count == 0)
                    {
                        productFirstTasks[productId].Add(task);
                    }

                    productLastTasks[productId] = new List<TaskInfo> { task };
                }
            }

            // Теперь добавляем зависимости между изделиями по спецификации сборок
            foreach (var kv in productUnits.Keys)
            {
                var parentId = kv;
                var assemblies = db.AssemblySpecs.Where(a => a.ProductId == parentId).ToList();
                foreach (var asm in assemblies)
                {
                    var childId = asm.ItemId;

                    if (!productFirstTasks.TryGetValue(parentId, out var parentFirst) ||
                        !productLastTasks.TryGetValue(childId, out var childLast))
                    {
                        continue;
                    }

                    foreach (var pf in parentFirst)
                    {
                        foreach (var cl in childLast)
                        {
                            if (!pf.PrereqIds.Contains(cl.Id))
                                pf.PrereqIds.Add(cl.Id);
                        }
                    }
                }
            }

            return tasks;
        }

        private static void ScheduleTasks(List<TaskInfo> tasks)
        {
            var unscheduled = new HashSet<int>(tasks.Select(t => t.Id));
            var taskById = tasks.ToDictionary(t => t.Id);
            var equipmentAvailableAt = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var completionTime = new Dictionary<int, double>();

            while (unscheduled.Count > 0)
            {
                // Готовые задачи – все зависимости уже выполнены
                var ready = unscheduled
                    .Select(id => taskById[id])
                    .Where(t => t.PrereqIds.All(p => completionTime.ContainsKey(p)))
                    .OrderByDescending(t => t.Duration) // сначала более длительные
                    .ToList();

                if (ready.Count == 0)
                {
                    // На случай циклических зависимостей – выходим
                    break;
                }

                foreach (var task in ready)
                {
                    var prereqFinish = task.PrereqIds.Count == 0
                        ? 0
                        : task.PrereqIds.Max(id => completionTime[id]);

                    equipmentAvailableAt.TryGetValue(task.EquipmentType, out var eqTime);
                    var start = Math.Max(prereqFinish, eqTime);
                    var finish = start + task.Duration;

                    task.Start = start;
                    task.Finish = finish;

                    completionTime[task.Id] = finish;
                    equipmentAvailableAt[task.EquipmentType] = finish;

                    unscheduled.Remove(task.Id);
                }
            }
        }
    }
}

