
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSalesCore.Models.Entities;

public class Book
{
    public long BookId { get; set; }

    [Required]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ISBN { get; set; }

    [Required]
    [StringLength(300)]
    [Display(Name = "Tên sách")]
    public string BookTitle { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Thể loại")]
    public string? CategoryName { get; set; }

    [Range(0, 999999999999999999)]
    [Column(TypeName = "decimal(18,0)")]
    [Display(Name = "Giá bán (VND)")]
    public decimal SalePrice { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
