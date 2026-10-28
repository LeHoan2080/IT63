using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.ViewModels.Customers;

public class CustomerEditViewModel
{
    public long CustomerId { get; set; }

    // Mã khách hàng được tạo tự động, không cho sửa.
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [StringLength(200, ErrorMessage = "Tên khách hàng không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; }
}