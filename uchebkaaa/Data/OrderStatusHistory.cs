using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderNumber { get; set; }

    public DateOnly OrderDate { get; set; }

    public string OldStatus { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public string ChangedBy { get; set; } = null!;

    public string? Comment { get; set; }
}
