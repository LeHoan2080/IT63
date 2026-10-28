using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.Models.Entities;

public class SalesOrder
{
    public long SalesOrderId { get; set; }

    [Required]
    [StringLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public long CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(1000)]
    public string? Note { get; set; }

    public decimal TotalAmount { get; set; }

    public string? SentByUserId { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public string? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<SalesOrderLine> Lines { get; set; }
        = new List<SalesOrderLine>();
}