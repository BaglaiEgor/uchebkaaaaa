using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class User
{
    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Name { get; set; } = null!;

    public byte[]? Image { get; set; }

    public virtual ICollection<Order> OrderCustomers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderManagers { get; set; } = new List<Order>();
}
