using System.Collections.Generic;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa
{
    /// <summary>
    /// Вспомогательный класс для автоматического списания материалов и комплектующих
    /// при переводе заказа в производство.
    /// Логика основана на иерархической спецификации изделий.
    /// </summary>
    public static class MaterialWriteOffHelper
    {
        private sealed class Requirement
        {
            public string Id { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        /// <summary>
        /// Выполнить списание материалов и комплектующих по спецификации изделия заказа.
        /// </summary>
        public static void WriteOffForOrder(AppDbContext db, Order order)
        {
            if (string.IsNullOrWhiteSpace(order.ProductId))
                return;

            var materialReqs = new Dictionary<string, int>();
            var accessoryReqs = new Dictionary<string, int>();

            // Строим требования для одного изделия (конвейера)
            BuildRequirementsRecursive(
                db,
                order.ProductId,
                1,
                materialReqs,
                accessoryReqs);

            // Списываем материалы
            foreach (var kv in materialReqs)
            {
                WriteOffMaterial(db, kv.Key, kv.Value);
            }

            // Списываем комплектующие
            foreach (var kv in accessoryReqs)
            {
                WriteOffAccessory(db, kv.Key, kv.Value);
            }

            db.SaveChanges();
        }

        private static void BuildRequirementsRecursive(
            AppDbContext db,
            string productId,
            int productCount,
            Dictionary<string, int> materialReqs,
            Dictionary<string, int> accessoryReqs)
        {
            // Материалы для текущего изделия
            var materialSpecs = db.MaterialSpecs.Where(ms => ms.ProductId == productId).ToList();
            foreach (var ms in materialSpecs)
            {
                var total = ms.Count * productCount;
                if (!materialReqs.ContainsKey(ms.MaterialId))
                    materialReqs[ms.MaterialId] = 0;
                materialReqs[ms.MaterialId] += total;
            }

            // Комплектующие для текущего изделия
            var accessoriesSpecs = db.AccessoriesSpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var ac in accessoriesSpecs)
            {
                var total = ac.Count * productCount;
                if (!accessoryReqs.ContainsKey(ac.AccessoriesId))
                    accessoryReqs[ac.AccessoriesId] = 0;
                accessoryReqs[ac.AccessoriesId] += total;
            }

            // Сборочные единицы / детали
            var assemblySpecs = db.AssemblySpecs.Where(a => a.ProductId == productId).ToList();
            foreach (var asm in assemblySpecs)
            {
                var requiredCount = asm.Count * productCount;
                BuildRequirementsRecursive(db, asm.ItemId, requiredCount, materialReqs, accessoryReqs);
            }
        }

        private static void WriteOffMaterial(AppDbContext db, string article, int required)
        {
            if (required <= 0) return;

            var material = db.Materials.FirstOrDefault(m => m.Article == article);
            if (material != null)
            {
                var newTotal = material.Count - required;
                material.Count = newTotal < 0 ? 0 : newTotal;
            }

            var warehouses = db.MaterialWarehouses
                .Where(mw => mw.MaterialArticle == article)
                .OrderBy(mw => mw.WarehouseId)
                .ToList();

            var remaining = required;
            foreach (var mw in warehouses)
            {
                if (remaining <= 0)
                    break;

                var delta = remaining > mw.Quantity ? mw.Quantity : remaining;
                mw.Quantity -= delta;
                remaining -= delta;
            }
        }

        private static void WriteOffAccessory(AppDbContext db, string article, int required)
        {
            if (required <= 0) return;

            var accessory = db.Accessories.FirstOrDefault(a => a.Article == article);
            if (accessory != null)
            {
                var newTotal = accessory.Count - required;
                accessory.Count = newTotal < 0 ? 0 : newTotal;
            }

            var warehouses = db.ComponentWarehouses
                .Where(cw => cw.ComponentArticle == article)
                .OrderBy(cw => cw.WarehouseId)
                .ToList();

            var remaining = required;
            foreach (var cw in warehouses)
            {
                if (remaining <= 0)
                    break;

                var delta = remaining > cw.Quantity ? cw.Quantity : remaining;
                cw.Quantity -= delta;
                remaining -= delta;
            }
        }
    }
}

