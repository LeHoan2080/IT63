
using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.ViewModels.Sales;

public class SalesOrderCancelViewModel
{
    public long SalesOrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy.")]
    [StringLength(
        500,
        ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự.")]
    [Display(Name = "Lý do hủy")]
    public string Reason { get; set; } = string.Empty;
}
