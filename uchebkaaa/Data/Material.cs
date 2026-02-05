using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Material
{
    public string Article { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int Count { get; set; }

    public string SupplierId { get; set; } = null!;

    public byte[]? Image { get; set; }

    public string ProductType { get; set; } = null!;

    public decimal? Price { get; set; }

    public string? Gost { get; set; }

    public double? Length { get; set; }

    public double? Characteristics { get; set; }

    public double? MassPerMeter { get; set; }

    public virtual ICollection<MaterialSpec> MaterialSpecs { get; set; } = new List<MaterialSpec>();

    public virtual Supplier Supplier { get; set; } = null!;
}
