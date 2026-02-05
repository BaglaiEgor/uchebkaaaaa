using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Product
{
    public string Name { get; set; } = null!;

    public string Size { get; set; } = null!;

    public virtual ICollection<AccessoriesSpec> AccessoriesSpecs { get; set; } = new List<AccessoriesSpec>();

    public virtual ICollection<AssemblySpec> AssemblySpecItems { get; set; } = new List<AssemblySpec>();

    public virtual ICollection<AssemblySpec> AssemblySpecProducts { get; set; } = new List<AssemblySpec>();

    public virtual ICollection<MaterialSpec> MaterialSpecs { get; set; } = new List<MaterialSpec>();

    public virtual ICollection<OperationSpec> OperationSpecs { get; set; } = new List<OperationSpec>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
