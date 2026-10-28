
namespace BookSalesCore.ViewModels;

public class SalesReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalSales { get; set; }
    public int ConfirmedOrderCount { get; set; }
    public int TotalBooksSold { get; set; }

    public List<TopSellingBookViewModel> TopSellingBooks { get; set; } = new();
}

public class TopSellingBookViewModel
{
    public long BookId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
