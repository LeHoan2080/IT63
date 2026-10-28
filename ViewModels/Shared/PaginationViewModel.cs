
namespace BookSalesCore.ViewModels.Shared;

public class PaginationViewModel
{
    public string Action { get; set; } = "Index";

    public string Controller { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalItems { get; set; }

    // Các tham số tìm kiếm và bộ lọc cần giữ lại khi chuyển trang.
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public int TotalPages =>
        TotalItems == 0
            ? 0
            : (int)Math.Ceiling((double)TotalItems / PageSize);

    public int StartItem =>
        TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int EndItem =>
        Math.Min(Page * PageSize, TotalItems);

    public int[] PageSizeOptions { get; } = [10, 20, 50, 100];
}
