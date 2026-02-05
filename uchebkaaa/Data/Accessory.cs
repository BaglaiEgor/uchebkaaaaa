using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Accessory
{
    public string Article { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Unit { get; set; }

    public int Count { get; set; }

    public string SupplierId { get; set; } = null!;

    public byte[]? Image { get; set; }

    public string ProductType { get; set; } = null!;

    public decimal? Price { get; set; }

    public double? Weight { get; set; }

    public virtual ICollection<AccessoriesSpec> AccessoriesSpecs { get; set; } = new List<AccessoriesSpec>();

    public virtual Supplier Supplier { get; set; } = null!;
}
