using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class ComponentWarehouse
{
    public string ComponentArticle { get; set; } = null!;

    public int WarehouseId { get; set; }

    public int Quantity { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
