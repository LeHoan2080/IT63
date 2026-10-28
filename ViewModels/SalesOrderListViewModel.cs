
using BookSalesCore.Models.Entities;

namespace BookSalesCore.ViewModels;

public class SalesOrderListViewModel
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public long? CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public List<SalesOrder> SalesOrders { get; set; } = new();
}
