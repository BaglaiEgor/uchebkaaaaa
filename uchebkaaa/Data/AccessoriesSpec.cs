using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class AccessoriesSpec
{
    public string AccessoriesId { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public int Count { get; set; }

    public virtual Accessory Accessories { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
