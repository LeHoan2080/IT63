namespace BookSalesCore.ViewModels.Sales;

public class SalesOrderAuditEventViewModel
{
    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Reason { get; set; }

    public DateTime LocalCreatedAt { get; set; }

    public string ActorName { get; set; } = string.Empty;
}
