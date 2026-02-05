using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class ProductionOperation
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
