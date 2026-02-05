using System;
using System.Collections.Generic;

namespace uchebkaaa.Data;

public partial class Order
{
    public int Number { get; set; }

    public DateOnly Date { get; set; }

    public string Name { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string? ManagerId { get; set; }

    public decimal Cost { get; set; }

    public DateOnly EndDate { get; set; }

    public string Schemas { get; set; } = null!;

    public string? Status { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual User? Manager { get; set; }

    public virtual Product Product { get; set; } = null!;
}
