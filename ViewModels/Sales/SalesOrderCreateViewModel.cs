
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookSalesCore.ViewModels.Sales;

public class SalesOrderCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
    [Display(Name = "Khách hàng")]
    public long? CustomerId { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1.000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public List<SelectListItem> Customers { get; set; } = new();

    public List<SalesOrderLineInputViewModel> Lines { get; set; } =
        new() { new SalesOrderLineInputViewModel() };

    public List<SelectListItem> Books { get; set; } = new();

    public Dictionary<long, decimal> BookPrices { get; set; } = [];
}

public class SalesOrderLineInputViewModel
{
    public long? ExistingLineId { get; set; }

    public long? OriginalBookId { get; set; }

    public decimal? UnitPricePreview { get; set; }

    [Display(Name = "Sách")]
    public long? BookId { get; set; }

    [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;
}
