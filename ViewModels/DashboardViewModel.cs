namespace BookSalesCore.ViewModels;

public class DashboardViewModel
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public decimal TotalSales { get; set; }

    public int PendingQuotationCount { get; set; }

    public int ConfirmedOrderCount { get; set; }

    public List<DashboardTopBookViewModel> TopBooks { get; set; } = [];

    public List<DashboardRecentOrderViewModel> RecentOrders { get; set; } = [];
}

public class DashboardTopBookViewModel
{
    public string SKU { get; set; } = string.Empty;

    public string BookTitle { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal TotalSales { get; set; }
}

public class DashboardRecentOrderViewModel
{
    public long SalesOrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }
}
