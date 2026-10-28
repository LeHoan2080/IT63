
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookSalesCore.ViewModels.Sales;

public class SalesOrderEditViewModel
{
    public long SalesOrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

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

    // Dùng để phát hiện trường hợp chứng từ bị cập nhật đồng thời.
    public byte[] RowVersion { get; set; } = [];
}
