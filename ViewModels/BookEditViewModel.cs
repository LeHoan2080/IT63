
using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.ViewModels;

public class BookEditViewModel
{
    public long BookId { get; set; }

    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string? ISBN { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sách.")]
    [StringLength(300)]
    [Display(Name = "Tên sách")]
    public string BookTitle { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Thể loại")]
    public string? CategoryName { get; set; }

    [Range(typeof(decimal), "0", "999999999999999999",
        ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá bán (VND)")]
    public decimal SalePrice { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; }
}
