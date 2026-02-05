using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class EquipmentFailure
{
    public int Id { get; set; }

    public string EquipmentMark { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public string ReportedBy { get; set; } = null!;
}
