using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.ViewModels.Reports;

public class ReportsIndexViewModel
{
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public decimal TotalSales { get; set; }

    public int ConfirmedOrderCount { get; set; }

    public int SalesConfirmedOrderCount { get; set; }

    public int DraftCount { get; set; }

    public int SentCount { get; set; }

    public int CancelledCount { get; set; }

    public int TotalOrderCount { get; set; }

    public List<DailySalesReportItem> DailySales { get; set; } = [];

    public List<TopBookReportItem> TopBooks { get; set; } = [];
}

public class DailySalesReportItem
{
    public DateTime Date { get; set; }

    public decimal TotalSales { get; set; }

    public int OrderCount { get; set; }
}

public class TopBookReportItem
{
    public string SKU { get; set; } = string.Empty;

    public string BookTitle { get; set; } = string.Empty;

    public int TotalQuantity { get; set; }

    public decimal TotalSales { get; set; }
}