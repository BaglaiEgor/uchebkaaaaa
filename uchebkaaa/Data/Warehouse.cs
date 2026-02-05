using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Warehouse
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<ComponentWarehouse> ComponentWarehouses { get; set; } = new List<ComponentWarehouse>();

    public virtual ICollection<MaterialWarehouse> MaterialWarehouses { get; set; } = new List<MaterialWarehouse>();
}
