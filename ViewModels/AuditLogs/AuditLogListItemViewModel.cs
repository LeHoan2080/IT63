namespace BookSalesCore.ViewModels.AuditLogs;

public class AuditLogListItemViewModel
{
    public DateTime CreatedAt { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Reason { get; set; }
}
