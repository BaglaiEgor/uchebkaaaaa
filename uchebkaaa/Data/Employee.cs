using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Employee
{
    public int Id { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? HomeAddress { get; set; }

    public string? Education { get; set; }

    public string? Qualification { get; set; }

    public virtual ICollection<ProductionOperation> Operations { get; set; } = new List<ProductionOperation>();
}
