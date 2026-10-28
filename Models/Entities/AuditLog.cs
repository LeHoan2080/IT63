using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.Models.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [Required]
    [StringLength(30)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string EntityId { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? OldValue { get; set; }

    [StringLength(1000)]
    public string? NewValue { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}