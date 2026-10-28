
using System.ComponentModel.DataAnnotations;

namespace BookSalesCore.Models.Entities;

public class Customer
{
    public long CustomerId { get; set; }

    [Required]
    [StringLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [StringLength(200)]
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
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
