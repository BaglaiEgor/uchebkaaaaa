using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class QualityCheck
{
    public int Id { get; set; }

    public int OrderNumber { get; set; }

    public int? ParameterId { get; set; }

    public bool IsAcceptable { get; set; }

    public string? Comment { get; set; }

    public DateTime CheckDate { get; set; }

    public string CheckedBy { get; set; } = null!;

    public virtual QualityParameter? Parameter { get; set; }
}
