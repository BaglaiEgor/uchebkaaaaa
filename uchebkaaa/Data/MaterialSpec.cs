using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class MaterialSpec
{
    public string MaterialId { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public int Count { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
