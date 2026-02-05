using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class AssemblySpec
{
    public string ProductId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public int Count { get; set; }

    public virtual Product Item { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
