
using BookSalesCore.Data;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator,SalesStaff")]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? status,
        int page = 1,
        int pageSize = 10)
    {
        // Giới hạn page và pageSize để tránh giá trị không hợp lệ.
        page = Math.Max(1, page);

        var allowedPageSizes = new[] { 10, 20, 50, 100 };

        if (!allowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        var query = _context.Books.AsNoTracking();

        // Tìm kiếm theo SKU, ISBN hoặc tên sách.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(b =>
                b.SKU.Contains(term) ||
                (b.ISBN != null && b.ISBN.Contains(term)) ||
                b.BookTitle.Contains(term));
        }

        // Lọc theo trạng thái sách.
        if (status == "active")
        {
            query = query.Where(b => b.IsActive);
        }
        else if (status == "inactive")
        {
            query = query.Where(b => !b.IsActive);
        }

        // Đếm số bản ghi sau khi áp dụng bộ lọc.
        var totalItems = await query.CountAsync();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling((double)totalItems / pageSize);

        // Nếu trang được yêu cầu vượt quá tổng số trang,
        // đưa người dùng về trang cuối cùng hợp lệ.
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        // Chỉ lấy dữ liệu của trang hiện tại từ database.
        var books = await query
            .OrderBy(b => b.BookId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookListItemViewModel
            {
                BookId = b.BookId,
                SKU = b.SKU,
                ISBN = b.ISBN,
                BookTitle = b.BookTitle,
                CategoryName = b.CategoryName,
                SalePrice = b.SalePrice,
                IsActive = b.IsActive
            })
            .ToListAsync();

        var model = new BookListViewModel
        {
            SearchTerm = searchTerm,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Books = books
        };

        // Chuẩn bị thông tin cho paging dùng chung.
        ViewData["Pagination"] = new PaginationViewModel
        {
            Action = nameof(Index),
            Controller = "Books",
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            RouteValues = new Dictionary<string, string?>
            {
                ["searchTerm"] = searchTerm,
                ["status"] = status
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var book = await _context.Books
            .AsNoTracking()
            .Where(item => item.BookId == id)
            .Select(item => new BookDetailsViewModel
            {
                BookId = item.BookId,
                SKU = item.SKU,
                ISBN = item.ISBN,
                BookTitle = item.BookTitle,
                CategoryName = item.CategoryName,
                SalePrice = item.SalePrice,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (book is null)
        {
            return NotFound();
        }

        var vietnamTimeZone = GetVietnamTimeZone();
        book.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(book.CreatedAt, DateTimeKind.Utc),
            vietnamTimeZone);
        if (book.UpdatedAt.HasValue)
        {
            book.UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(book.UpdatedAt.Value, DateTimeKind.Utc),
                vietnamTimeZone);
        }

        return View(book);
    }


    // Chỉ Administrator được thêm sách.
    [Authorize(Roles = "Administrator")]
    public IActionResult Create()
    {
        return View(new BookCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(BookCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var book = new Book
        {
            SKU = await GenerateNextSkuAsync(),
            ISBN = string.IsNullOrWhiteSpace(model.ISBN)
                ? null
                : model.ISBN.Trim(),
            BookTitle = model.BookTitle.Trim(),
            CategoryName = string.IsNullOrWhiteSpace(model.CategoryName)
                ? null
                : model.CategoryName.Trim(),
            SalePrice = model.SalePrice,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Books.Add(book);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu sách. Vui lòng kiểm tra dữ liệu và thử lại.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Đã thêm sách thành công.";

        return RedirectToAction(nameof(Index));
    }

    // Chỉ Administrator được sửa sách.
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(long id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound();
        }

        var model = new BookEditViewModel
        {
            BookId = book.BookId,
            ISBN = book.ISBN,
            BookTitle = book.BookTitle,
            CategoryName = book.CategoryName,
            SalePrice = book.SalePrice,
            IsActive = book.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(
        long id,
        BookEditViewModel model)
    {
        // Kiểm tra ID trên URL có khớp với ID trong dữ liệu gửi lên không.
        if (id != model.BookId)
        {
            return BadRequest();
        }

        // Tìm sách trong cơ sở dữ liệu.
        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound();
        }

        // Lưu SKU để hiển thị lại trên form nếu dữ liệu không hợp lệ.
        ViewData["SKU"] = book.SKU;

        // Kiểm tra dữ liệu nhập vào.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Cập nhật thông tin sách.
        // SKU không được thay đổi.
        book.ISBN = string.IsNullOrWhiteSpace(model.ISBN)
            ? null
            : model.ISBN.Trim();

        book.BookTitle = model.BookTitle.Trim();

        book.CategoryName = string.IsNullOrWhiteSpace(model.CategoryName)
            ? null
            : model.CategoryName.Trim();

        book.SalePrice = model.SalePrice;
        book.IsActive = model.IsActive;
        book.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật sách. Vui lòng kiểm tra dữ liệu và thử lại.");

            // Gán lại SKU trước khi hiển thị form lỗi.
            ViewData["SKU"] = book.SKU;

            return View(model);
        }

        // Thông báo thành công và quay về danh sách sách.
        TempData["SuccessMessage"] = "Đã cập nhật sách thành công.";

        return RedirectToAction(nameof(Index));
    }
    private async Task<string> GenerateNextSkuAsync()
    {
        var nextNumber = await _context.Database
            .SqlQuery<long>(
                $"SELECT NEXT VALUE FOR dbo.BookCodeSequence AS Value")
            .SingleAsync();

        return $"BK-{nextNumber:D6}";
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
