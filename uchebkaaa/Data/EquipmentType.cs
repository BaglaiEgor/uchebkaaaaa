using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class EquipmentType
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<OperationSpec> OperationSpecs { get; set; } = new List<OperationSpec>();
}
