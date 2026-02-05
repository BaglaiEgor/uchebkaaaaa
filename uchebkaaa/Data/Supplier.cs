using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Supplier
{
    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string SupplyTime { get; set; } = null!;

    public virtual ICollection<Accessory> Accessories { get; set; } = new List<Accessory>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}
