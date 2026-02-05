using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class OperationSpec
{
    public string ProductId { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public int Number { get; set; }

    public string EquipmentType { get; set; } = null!;

    public int OperationTime { get; set; }

    public virtual EquipmentType EquipmentTypeNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
