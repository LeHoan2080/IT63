namespace BookSalesCore.ViewModels;

public class CustomerDetailsViewModel
{
    public long CustomerId { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public bool IsWalkInCustomer { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalOrders { get; set; }

    public int ConfirmedOrders { get; set; }

    public decimal ConfirmedSales { get; set; }

    public List<CustomerSalesOrderListItemViewModel> SalesOrders { get; set; } = [];
}

public class CustomerSalesOrderListItemViewModel
{
    public long SalesOrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}
