namespace BookSalesCore.ViewModels;

public class BookDetailsViewModel
{
    public long BookId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? ISBN { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public decimal SalePrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
