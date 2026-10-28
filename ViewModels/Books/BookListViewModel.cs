
namespace BookSalesCore.ViewModels.Books;

public class BookListViewModel
{
    public string? SearchTerm { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalItems { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling(TotalItems / (double)PageSize);

    public List<BookListItemViewModel> Books { get; set; } = [];
}

public class BookListItemViewModel
{
    public long BookId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? ISBN { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public decimal SalePrice { get; set; }

    public bool IsActive { get; set; }
}
