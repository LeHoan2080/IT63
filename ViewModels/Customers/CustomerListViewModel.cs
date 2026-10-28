using BookSalesCore.Models.Entities;

namespace BookSalesCore.ViewModels.Customers;

public class CustomerListViewModel
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public List<Customer> Customers { get; set; } = [];
}