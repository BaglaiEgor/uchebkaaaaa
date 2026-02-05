using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class QualityParameter
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public virtual ICollection<QualityCheck> QualityChecks { get; set; } = new List<QualityCheck>();
}
