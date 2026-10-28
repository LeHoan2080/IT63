
using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.ViewModels;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3,
        ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Tên đăng nhập chỉ được chứa chữ, số, dấu chấm, gạch ngang và gạch dưới.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression("^(Administrator|SalesStaff)$",
        ErrorMessage = "Vai trò không hợp lệ.")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "SalesStaff";
}
