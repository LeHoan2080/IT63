using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.Models.Entities;

public class SalesOrderLine
{
    public long SalesOrderLineId { get; set; }

    public long SalesOrderId { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;

    public long BookId { get; set; }

    public Book Book { get; set; } = null!;

    [Required]
    [StringLength(30)]
    public string SKUSnapshot { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string BookTitleSnapshot { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}