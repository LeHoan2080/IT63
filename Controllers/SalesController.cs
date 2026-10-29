
using BookSalesCore.Data;
using BookSalesCore.Models.Entities;
using BookSalesCore.Services.Reports;
using BookSalesCore.ViewModels.Sales;
using BookSalesCore.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator,SalesStaff")]
public class SalesController : Controller
{
    private const decimal MaximumVndAmount = 999999999999999999m;

    private readonly ApplicationDbContext _context;
    private readonly IReportsPdfService _reportsPdfService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ApplicationDbContext context,
        IReportsPdfService reportsPdfService,
        ILogger<SalesController> logger)
    {
        _context = context;
        _reportsPdfService = reportsPdfService;
        _logger = logger;
    }

    // Danh sách chứng từ bán hàng
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        long? customerId,
        int page = 1,
        int pageSize = 10)
    {
        if (pageSize is not (10 or 20 or 50 or 100))
        {
            pageSize = 10;
        }

        page = Math.Max(page, 1);

        var query = _context.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        // Nhân viên chỉ xem chứng từ do mình tạo.
        if (User.IsInRole("SalesStaff"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            query = query.Where(x => x.CreatedByUserId == userId);
        }
        else if (!User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        if (fromDate.HasValue && toDate.HasValue &&
            fromDate.Value.Date > toDate.Value.Date)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            fromDate = null;
            toDate = null;
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(x =>
                x.OrderNumber.Contains(searchTerm) ||
                x.Customer.CustomerCode.Contains(searchTerm) ||
                x.CustomerNameSnapshot.Contains(searchTerm) ||
                x.Customer.CustomerName.Contains(searchTerm));
        }

        var vietnamTimeZone = GetVietnamTimeZone();
        if (fromDate.HasValue)
        {
            var startUtc = ConvertVietnamDateToUtc(
                fromDate.Value.Date,
                vietnamTimeZone);
            query = query.Where(order => order.OrderDate >= startUtc);
        }

        if (toDate.HasValue)
        {
            var endUtcExclusive = ConvertVietnamDateToUtc(
                toDate.Value.Date.AddDays(1),
                vietnamTimeZone);
            query = query.Where(order => order.OrderDate < endUtcExclusive);
        }

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        var allowedStatuses = new[]
        {
            "Draft",
            "Sent",
            "Confirmed",
            "Cancelled"
        };

        if (!string.IsNullOrWhiteSpace(status) &&
            allowedStatuses.Contains(status))
        {
            query = query.Where(x => x.Status == status);
        }
        else
        {
            status = null;
        }

        var totalItems = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            (double)totalItems / pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var orders = await query
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.SalesOrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        foreach (var order in orders)
        {
            order.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc),
                vietnamTimeZone);
        }

        var model = new SalesOrderListViewModel
        {
            SearchTerm = searchTerm,
            Status = status,
            CustomerId = customerId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            SalesOrders = orders
        };

        ViewData["Pagination"] = new PaginationViewModel
        {
            Action = nameof(Index),
            Controller = "Sales",
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            RouteValues = new Dictionary<string, string?>
            {
                ["searchTerm"] = searchTerm,
                ["status"] = status,
                ["fromDate"] = fromDate?.ToString("yyyy-MM-dd"),
                ["toDate"] = toDate?.ToString("yyyy-MM-dd"),
                ["customerId"] = customerId?.ToString()
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        string? searchTerm,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        long? customerId)
    {
        if (fromDate.HasValue && toDate.HasValue &&
            fromDate.Value.Date > toDate.Value.Date)
        {
            return BadRequest("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        var query = _context.SalesOrders
            .AsNoTracking()
            .Include(order => order.Customer)
            .AsQueryable();

        if (User.IsInRole("SalesStaff"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            query = query.Where(order => order.CreatedByUserId == userId);
        }
        else if (!User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(order =>
                order.OrderNumber.Contains(searchTerm) ||
                order.Customer.CustomerCode.Contains(searchTerm) ||
                order.CustomerNameSnapshot.Contains(searchTerm) ||
                order.Customer.CustomerName.Contains(searchTerm));
        }

        var vietnamTimeZone = GetVietnamTimeZone();
        if (fromDate.HasValue)
        {
            var startUtc = ConvertVietnamDateToUtc(
                fromDate.Value.Date,
                vietnamTimeZone);
            query = query.Where(order => order.OrderDate >= startUtc);
        }

        if (toDate.HasValue)
        {
            var endUtcExclusive = ConvertVietnamDateToUtc(
                toDate.Value.Date.AddDays(1),
                vietnamTimeZone);
            query = query.Where(order => order.OrderDate < endUtcExclusive);
        }

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        var allowedStatuses = new[]
        {
            "Draft",
            "Sent",
            "Confirmed",
            "Cancelled"
        };

        if (!string.IsNullOrWhiteSpace(status) &&
            allowedStatuses.Contains(status))
        {
            query = query.Where(order => order.Status == status);
        }
        else
        {
            status = null;
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.SalesOrderId)
            .ToListAsync();

        foreach (var order in orders)
        {
            order.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc),
                vietnamTimeZone);
        }

        var model = new SalesOrderListViewModel
        {
            SearchTerm = searchTerm,
            Status = status,
            CustomerId = customerId,
            FromDate = fromDate,
            ToDate = toDate,
            TotalItems = orders.Count,
            SalesOrders = orders
        };

        var pdf = _reportsPdfService.GenerateSalesOrdersPdf(model);
        var fileName = $"sales-orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    // Hiển thị biểu mẫu tạo chứng từ
    [HttpGet]
    public async Task<IActionResult> Create(long? customerId)
    {
        if (!CanCreateSalesOrder())
        {
            return Forbid();
        }

        var model = new SalesOrderCreateViewModel();
        if (customerId.HasValue &&
            await _context.Customers.AnyAsync(customer =>
                customer.CustomerId == customerId.Value &&
                customer.IsActive))
        {
            model.CustomerId = customerId.Value;
        }

        await PopulateCreateListsAsync(model);

        return View(model);
    }

    // Xử lý lưu chứng từ mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SalesOrderCreateViewModel model)
    {
        if (!CanCreateSalesOrder())
        {
            return Forbid();
        }

        // Kiểm tra dữ liệu các dòng sách.
        if (model.Lines == null ||
            model.Lines.Count == 0 ||
            !model.Lines.Any(x =>
                x.BookId.HasValue && x.Quantity > 0))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Chứng từ phải có ít nhất một dòng sách.");
        }

        var validLines = model.Lines?
            .Where(x => x.BookId.HasValue)
            .ToList()
            ?? new List<SalesOrderLineInputViewModel>();

        if (validLines.Any(x =>
            x.Quantity <= 0 || x.Quantity > 100000))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Số lượng mỗi dòng phải từ 1 đến 100.000.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateListsAsync(model);

            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        // Chỉ chấp nhận khách hàng đang hoạt động.
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CustomerId == model.CustomerId &&
                x.IsActive);

        if (customer == null)
        {
            ModelState.AddModelError(
                nameof(model.CustomerId),
                "Khách hàng không tồn tại hoặc đã ngừng hoạt động.");

            await PopulateCreateListsAsync(model);

            return View(model);
        }

        // Gộp các dòng trùng sách.
        var requestedLines = validLines
            .GroupBy(x => x.BookId!.Value)
            .Select(group => new
            {
                BookId = group.Key,
                Quantity = group.Sum(x => x.Quantity)
            })
            .ToList();

        if (requestedLines.Any(x => x.Quantity > 100000))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Tổng số lượng của cùng một sách không được vượt quá 100.000.");

            await PopulateCreateListsAsync(model);

            return View(model);
        }

        var bookIds = requestedLines
            .Select(x => x.BookId)
            .ToList();

        // Chỉ lấy sách đang kinh doanh.
        var books = await _context.Books
            .Where(x =>
                bookIds.Contains(x.BookId) &&
                x.IsActive)
            .ToListAsync();

        if (books.Count != bookIds.Count)
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Một hoặc nhiều sách không tồn tại hoặc đã ngừng kinh doanh.");

            await PopulateCreateListsAsync(model);

            return View(model);
        }

        var calculatedTotal = requestedLines.Sum(line =>
            books.Single(book => book.BookId == line.BookId).SalePrice *
            line.Quantity);

        if (calculatedTotal > MaximumVndAmount)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tổng tiền vượt quá giới hạn lưu trữ cho đơn vị VND.");
            await PopulateCreateListsAsync(model);
            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Cấp số chứng từ bằng SQL Server SEQUENCE.
            var nextNumber = await _context.Database
                .NextValueAsync(SqlServerSequence.SalesOrderNumber);

            var now = DateTime.UtcNow;

            var order = new SalesOrder
            {
                OrderNumber = $"SO-{nextNumber:D6}",
                OrderDate = now,
                CustomerId = customer.CustomerId,
                CustomerNameSnapshot = customer.CustomerName,
                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim(),
                CreatedByUserId = userId,
                CreatedAt = now,
                Status = "Draft",
                TotalAmount = 0
            };

            foreach (var requested in requestedLines)
            {
                var book = books.Single(
                    x => x.BookId == requested.BookId);

                // Đơn giá luôn lấy từ database, không tin giá từ trình duyệt.
                var unitPrice = book.SalePrice;
                var lineTotal = unitPrice * requested.Quantity;

                order.Lines.Add(new SalesOrderLine
                {
                    BookId = book.BookId,
                    SKUSnapshot = book.SKU,
                    BookTitleSnapshot = book.BookTitle,
                    Quantity = requested.Quantity,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });

                order.TotalAmount += lineTotal;
            }

            _context.SalesOrders.Add(order);

            // Ghi nhật ký tạo chứng từ.
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Created",
                EntityName = "SalesOrder",
                EntityId = order.OrderNumber,
                NewValue = "Draft",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã tạo chứng từ {order.OrderNumber} thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to create a sales order for user {UserId}.",
                userId);
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu chứng từ. Vui lòng kiểm tra cấu hình cơ sở dữ liệu và thử lại.");

            await PopulateCreateListsAsync(model);

            return View(model);
        }
    }

    private bool CanCreateSalesOrder()
    {
        return User.IsInRole("Administrator") ||
               User.IsInRole("SalesStaff");
    }


    // Xem chi tiết chứng từ
    public async Task<IActionResult> Details(long id)
    {
        var query = _context.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Lines)
                .ThenInclude(x => x.Book)
            .AsQueryable();

        if (User.IsInRole("SalesStaff"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            query = query.Where(x => x.CreatedByUserId == userId);
        }
        else if (!User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        var order = await query.FirstOrDefaultAsync(
            x => x.SalesOrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        ViewData["CanManageOrder"] =
            User.IsInRole("Administrator") ||
            (User.IsInRole("SalesStaff") &&
             order.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

        ViewData["CanCancelConfirmed"] = User.IsInRole("Administrator");
        ViewData["LocalOrderDate"] = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc),
            GetVietnamTimeZone());
        ViewData["CreatedByName"] = await _context.AppUsers
            .Where(user => user.Id == order.CreatedByUserId)
            .Select(user => user.UserName)
            .FirstOrDefaultAsync()
            ?? "Tài khoản không còn hoạt động";

        var localTimeZone = GetVietnamTimeZone();
        var history = await (
            from audit in _context.AuditLogs.AsNoTracking()
            join user in _context.AppUsers
                on audit.UserId equals user.Id into auditUsers
            from user in auditUsers.DefaultIfEmpty()
            where audit.EntityName == "SalesOrder" &&
                  audit.EntityId == order.OrderNumber
            orderby audit.CreatedAt
            select new
            {
                audit.Action,
                audit.OldValue,
                audit.NewValue,
                audit.Reason,
                audit.CreatedAt,
                ActorName = user == null ? "Tài khoản không còn hoạt động" : user.UserName
            })
            .ToListAsync();

        ViewData["OrderHistory"] = history.Select(item =>
            new SalesOrderAuditEventViewModel
        {
            Action = item.Action,
            OldValue = item.OldValue,
            NewValue = item.NewValue,
            Reason = item.Reason,
            LocalCreatedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc),
                localTimeZone),
            ActorName = item.ActorName ?? "Tài khoản không còn hoạt động"
        }).ToList();

        return View(order);
    }


    // Sửa chứng từ khi còn ở trạng thái Bản nháp.
    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var order = await GetEditableDraftAsync(id);

        if (order == null)
        {
            return NotFound(
                "Không tìm thấy chứng từ, hoặc chứng từ không còn ở trạng thái Bản nháp.");
        }

        var model = new SalesOrderEditViewModel
        {
            SalesOrderId = order.SalesOrderId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Note = order.Note,
            RowVersion = order.RowVersion,
            Lines = order.Lines
                .Select(line => new SalesOrderLineInputViewModel
                {
                    BookId = line.BookId,
                    Quantity = line.Quantity,
                    ExistingLineId = line.SalesOrderLineId,
                    OriginalBookId = line.BookId,
                    UnitPricePreview = line.UnitPrice
                })
                .ToList()
        };

        await PopulateEditListsAsync(model, order);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        SalesOrderEditViewModel model)
    {
        if (id != model.SalesOrderId)
        {
            return BadRequest();
        }

        var order = await GetEditableDraftAsync(id);

        if (order == null)
        {
            return NotFound(
                "Không tìm thấy chứng từ, hoặc chứng từ không còn ở trạng thái Bản nháp.");
        }

        if (model.RowVersion == null ||
            !order.RowVersion.SequenceEqual(model.RowVersion))
        {
            TempData["ErrorMessage"] =
                "Chứng từ đã được cập nhật ở nơi khác. Vui lòng tải lại trang trước khi sửa.";

            return RedirectToAction(nameof(Details), new { id });
        }

        if (model.Lines == null ||
            !model.Lines.Any(x => x.BookId.HasValue && x.Quantity > 0))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Chứng từ phải có ít nhất một dòng sách.");
        }

        var requestedLines = model.Lines?
            .Where(x => x.BookId.HasValue)
            .ToList()
            ?? new List<SalesOrderLineInputViewModel>();

        if (requestedLines.Any(x =>
                x.Quantity <= 0 || x.Quantity > 100000))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Số lượng mỗi dòng phải từ 1 đến 100.000.");
        }

        var existingLinesById = order.Lines.ToDictionary(
            line => line.SalesOrderLineId);

        var suppliedExistingIds = requestedLines
            .Where(line => line.ExistingLineId.HasValue)
            .Select(line => line.ExistingLineId!.Value)
            .ToList();

        if (suppliedExistingIds.Count != suppliedExistingIds.Distinct().Count() ||
            requestedLines.Any(line =>
                line.ExistingLineId.HasValue &&
                (!existingLinesById.TryGetValue(
                     line.ExistingLineId.Value,
                     out var existingLine) ||
                 existingLine.BookId != line.BookId!.Value)))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Một hoặc nhiều dòng sách không còn hợp lệ. Vui lòng tải lại chứng từ.");
        }

        var customer = model.CustomerId.HasValue
            ? await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == model.CustomerId.Value &&
                    (x.IsActive || x.CustomerId == order.CustomerId))
            : null;

        if (customer == null)
        {
            ModelState.AddModelError(
                nameof(model.CustomerId),
                "Vui lòng chọn khách hàng đang hoạt động.");
        }

        var requestedBookIds = requestedLines
            .Select(line => line.BookId!.Value)
            .Distinct()
            .ToList();

        var existingBookIds = order.Lines
            .Select(x => x.BookId)
            .ToHashSet();

        var books = await _context.Books
            .Where(x =>
                requestedBookIds.Contains(x.BookId) &&
                (x.IsActive || existingBookIds.Contains(x.BookId)))
            .ToListAsync();

        if (books.Count != requestedBookIds.Count)
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Có sách không tồn tại hoặc đã ngừng kinh doanh. Bạn chỉ có thể giữ sách ngừng kinh doanh nếu sách đó đã có trong chứng từ.");
        }

        if (requestedLines.Any(line =>
                !line.ExistingLineId.HasValue &&
                books.FirstOrDefault(book =>
                    book.BookId == line.BookId!.Value) is { IsActive: false }))
        {
            ModelState.AddModelError(
                nameof(model.Lines),
                "Chỉ có thể thêm sách đang kinh doanh vào chứng từ.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEditListsAsync(model, order);
            return View(model);
        }

        var booksById = books.ToDictionary(book => book.BookId);
        var calculatedTotal = requestedLines.Sum(line =>
        {
            var existingLine = line.ExistingLineId.HasValue
                ? existingLinesById[line.ExistingLineId.Value]
                : null;
            var unitPrice = existingLine?.UnitPrice ??
                booksById[line.BookId!.Value].SalePrice;
            return unitPrice * line.Quantity;
        });

        if (calculatedTotal > MaximumVndAmount)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tổng tiền vượt quá giới hạn lưu trữ cho đơn vị VND.");
            await PopulateEditListsAsync(model, order);
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            decimal totalAmount = 0;

            // Cập nhật UpdatedAt để EF Core kiểm tra RowVersion khi lưu.
            if (order.CustomerId != customer!.CustomerId)
            {
                order.CustomerNameSnapshot = customer.CustomerName;
            }

            order.CustomerId = customer!.CustomerId;
            order.Note = string.IsNullOrWhiteSpace(model.Note)
                ? null
                : model.Note.Trim();
            order.UpdatedAt = now;

            _context.SalesOrderLines.RemoveRange(order.Lines);

            foreach (var requested in requestedLines)
            {
                var book = books.Single(x =>
                    x.BookId == requested.BookId!.Value);

                var existingLine = requested.ExistingLineId.HasValue
                    ? existingLinesById[requested.ExistingLineId.Value]
                    : null;
                var unitPrice = existingLine?.UnitPrice ?? book.SalePrice;
                var lineTotal = unitPrice * requested.Quantity;

                totalAmount += lineTotal;

                order.Lines.Add(new SalesOrderLine
                {
                    BookId = book.BookId,
                    SKUSnapshot = existingLine?.SKUSnapshot ?? book.SKU,
                    BookTitleSnapshot =
                        existingLine?.BookTitleSnapshot ?? book.BookTitle,
                    Quantity = requested.Quantity,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });
            }

            order.TotalAmount = totalAmount;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Updated",
                EntityName = "SalesOrder",
                EntityId = order.OrderNumber,
                OldValue = "Draft",
                NewValue = "Draft",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã cập nhật chứng từ {order.OrderNumber} thành công.";

            return RedirectToAction(
                nameof(Details),
                new { id = order.SalesOrderId });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Chứng từ vừa được cập nhật ở nơi khác. Vui lòng tải lại trước khi sửa.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to update sales order {OrderNumber} by user {UserId}.",
                order.OrderNumber,
                userId);
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật chứng từ. Vui lòng kiểm tra dữ liệu và thử lại.");

            await PopulateEditListsAsync(model, order);
            return View(model);
        }
    }

    // Chỉ quản trị viên được sửa mọi bản nháp;
    // nhân viên chỉ được sửa bản nháp do mình tạo.
    private async Task<SalesOrder?> GetEditableDraftAsync(long id)
    {
        var query = _context.SalesOrders
            .Include(x => x.Lines)
            .AsQueryable();

        if (User.IsInRole("SalesStaff"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            query = query.Where(x => x.CreatedByUserId == userId);
        }
        else if (!User.IsInRole("Administrator"))
        {
            return null;
        }

        return await query.FirstOrDefaultAsync(x =>
            x.SalesOrderId == id && x.Status == "Draft");
    }

    private async Task PopulateEditListsAsync(
        SalesOrderEditViewModel model,
        SalesOrder order)
    {
        model.Customers = await _context.Customers
            .AsNoTracking()
            .Where(x =>
                x.IsActive || x.CustomerId == order.CustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = x.CustomerCode + " - " + x.CustomerName
            })
            .ToListAsync();

        if (!model.CustomerId.HasValue)
        {
            model.CustomerId = await _context.Customers
                .AsNoTracking()
                .Where(customer => customer.CustomerCode == "CUS-WALKIN")
                .Select(customer => (long?)customer.CustomerId)
                .SingleOrDefaultAsync();
        }

        var currentBookIds = order.Lines
            .Select(x => x.BookId)
            .ToList();

        model.Books = await _context.Books
            .AsNoTracking()
            .Where(x => x.IsActive || currentBookIds.Contains(x.BookId))
            .OrderBy(x => x.BookTitle)
            .Select(x => new SelectListItem
            {
                Value = x.BookId.ToString(),
                Text = x.SKU + " - " + x.BookTitle +
                       (x.IsActive ? "" : " (Ngừng kinh doanh)")
            })
            .ToListAsync();

        model.BookPrices = await _context.Books
            .AsNoTracking()
            .Where(book => book.IsActive || currentBookIds.Contains(book.BookId))
            .ToDictionaryAsync(book => book.BookId, book => book.SalePrice);

        if (model.Lines == null || model.Lines.Count == 0)
        {
            model.Lines = new List<SalesOrderLineInputViewModel>
            {
                new()
            };
        }

        foreach (var line in model.Lines)
        {
            if (line.ExistingLineId.HasValue &&
                order.Lines.FirstOrDefault(existing =>
                    existing.SalesOrderLineId == line.ExistingLineId.Value)
                is { } existingLine)
            {
                line.UnitPricePreview = existingLine.UnitPrice;
                line.OriginalBookId = existingLine.BookId;
            }
        }

    }


    // Nạp dữ liệu cho danh sách chọn trên biểu mẫu.
    private async Task PopulateCreateListsAsync(
        SalesOrderCreateViewModel model)
    {
        model.Customers = await _context.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CustomerName)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = x.CustomerCode + " - " + x.CustomerName
            })
            .ToListAsync();

        if (!model.CustomerId.HasValue)
        {
            model.CustomerId = await _context.Customers
                .AsNoTracking()
                .Where(customer => customer.CustomerCode == "CUS-WALKIN")
                .Select(customer => (long?)customer.CustomerId)
                .SingleOrDefaultAsync();
        }

        model.Books = await _context.Books
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.BookTitle)
            .Select(x => new SelectListItem
            {
                Value = x.BookId.ToString(),
                Text = x.SKU + " - " + x.BookTitle
            })
            .ToListAsync();

        model.BookPrices = await _context.Books
            .AsNoTracking()
            .Where(book => book.IsActive)
            .ToDictionaryAsync(book => book.BookId, book => book.SalePrice);

        if (model.Lines == null || model.Lines.Count == 0)
        {
            model.Lines = new List<SalesOrderLineInputViewModel>
            {
                new()
            };
        }
    }


    // Gửi báo giá: Draft -> Sent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var order = await _context.SalesOrders
            .FirstOrDefaultAsync(x => x.SalesOrderId == id);

        if (order == null)
            return NotFound();

        if (!User.IsInRole("Administrator") &&
            !User.IsInRole("SalesStaff"))
            return Forbid();

        if (User.IsInRole("SalesStaff") &&
            order.CreatedByUserId != userId)
            return Forbid();

        if (order.Status != "Draft")
        {
            TempData["ErrorMessage"] =
                "Chỉ có thể gửi chứng từ đang ở trạng thái bản nháp.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        var oldVersion = order.RowVersion.ToArray();

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var updated = await _context.SalesOrders
                .Where(x =>
                    x.SalesOrderId == id &&
                    x.Status == "Draft" &&
                    x.RowVersion == oldVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Sent")
                    .SetProperty(x => x.SentByUserId, userId)
                    .SetProperty(x => x.SentAt, now)
                    .SetProperty(x => x.UpdatedAt, now));

            if (updated != 1)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Chứng từ đã thay đổi. Vui lòng tải lại trang.";

                return RedirectToAction(nameof(Details), new { id });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Sent",
                EntityName = "SalesOrder",
                EntityId = order.OrderNumber,
                OldValue = "Draft",
                NewValue = "Sent",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã gửi chứng từ {order.OrderNumber}.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send sales order {OrderNumber} by user {UserId}.",
                order.OrderNumber,
                userId);
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] =
                "Không thể gửi chứng từ. Vui lòng thử lại.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Xác nhận bán hàng: Draft hoặc Sent -> Confirmed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var order = await _context.SalesOrders
            .FirstOrDefaultAsync(x => x.SalesOrderId == id);

        if (order == null)
            return NotFound();

        if (!User.IsInRole("Administrator") &&
            !User.IsInRole("SalesStaff"))
            return Forbid();

        if (User.IsInRole("SalesStaff") &&
            order.CreatedByUserId != userId)
            return Forbid();

        if (order.Status == "Confirmed")
        {
            TempData["SuccessMessage"] =
                $"Chứng từ {order.OrderNumber} đã được xác nhận trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (order.Status != "Draft" && order.Status != "Sent")
        {
            TempData["ErrorMessage"] =
                "Chỉ có thể xác nhận chứng từ ở trạng thái bản nháp hoặc đã gửi.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var oldStatus = order.Status;
        var oldVersion = order.RowVersion.ToArray();
        var now = DateTime.UtcNow;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var updated = await _context.SalesOrders
                .Where(x =>
                    x.SalesOrderId == id &&
                    x.Status == oldStatus &&
                    x.RowVersion == oldVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Confirmed")
                    .SetProperty(x => x.ConfirmedByUserId, userId)
                    .SetProperty(x => x.ConfirmedAt, now)
                    .SetProperty(x => x.UpdatedAt, now));

            if (updated != 1)
            {
                await transaction.RollbackAsync();

                var currentStatus = await _context.SalesOrders
                    .AsNoTracking()
                    .Where(current => current.SalesOrderId == id)
                    .Select(current => current.Status)
                    .SingleOrDefaultAsync();

                if (currentStatus == "Confirmed")
                {
                    TempData["SuccessMessage"] =
                        $"Chứng từ {order.OrderNumber} đã được xác nhận trước đó.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["ErrorMessage"] =
                    "Chứng từ đã thay đổi. Vui lòng tải lại trang.";

                return RedirectToAction(nameof(Details), new { id });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Confirmed",
                EntityName = "SalesOrder",
                EntityId = order.OrderNumber,
                OldValue = oldStatus,
                NewValue = "Confirmed",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã xác nhận chứng từ {order.OrderNumber}.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to confirm sales order {OrderNumber} by user {UserId}.",
                order.OrderNumber,
                userId);
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Không thể xác nhận chứng từ. Vui lòng thử lại.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Hiển thị biểu mẫu nhập lý do hủy
    [HttpGet]
    public async Task<IActionResult> Cancel(long id)
    {
        var order = await _context.SalesOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SalesOrderId == id);

        if (order == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        if (!CanCancelOrder(order, userId))
            return Forbid();

        return View(new SalesOrderCancelViewModel
        {
            SalesOrderId = id
        });
    }

    // Thực hiện hủy chứng từ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        SalesOrderCancelViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var order = await _context.SalesOrders
            .FirstOrDefaultAsync(x =>
                x.SalesOrderId == model.SalesOrderId);

        if (order == null)
            return NotFound();

        if (!CanCancelOrder(order, userId))
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var oldStatus = order.Status;
        var oldVersion = order.RowVersion.ToArray();
        var now = DateTime.UtcNow;
        var reason = model.Reason.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(
                nameof(model.Reason),
                "Vui lòng nhập lý do hủy.");

            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var updated = await _context.SalesOrders
                .Where(x =>
                    x.SalesOrderId == model.SalesOrderId &&
                    x.Status == oldStatus &&
                    x.RowVersion == oldVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Cancelled")
                    .SetProperty(x => x.CancelledByUserId, userId)
                    .SetProperty(x => x.CancelledAt, now)
                    .SetProperty(x => x.CancellationReason, reason)
                    .SetProperty(x => x.UpdatedAt, now));

            if (updated != 1)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Chứng từ đã thay đổi. Vui lòng tải lại trang.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.SalesOrderId });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Cancelled",
                EntityName = "SalesOrder",
                EntityId = order.OrderNumber,
                OldValue = oldStatus,
                NewValue = "Cancelled",
                Reason = reason,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã hủy chứng từ {order.OrderNumber}.";

            return RedirectToAction(
                nameof(Details),
                new { id = model.SalesOrderId });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to cancel sales order {OrderNumber} by user {UserId}.",
                order.OrderNumber,
                userId);
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Không thể hủy chứng từ. Vui lòng thử lại.";

            return RedirectToAction(
                nameof(Details),
                new { id = model.SalesOrderId });
        }
    }

    private bool CanCancelOrder(SalesOrder order, string userId)
    {
        if (User.IsInRole("Administrator"))
        {
            return order.Status is
                "Draft" or "Sent" or "Confirmed";
        }

        return User.IsInRole("SalesStaff") &&
               order.CreatedByUserId == userId &&
               (order.Status is "Draft" or "Sent");
    }

    private static DateTime ConvertVietnamDateToUtc(
        DateTime vietnamDate,
        TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(vietnamDate, DateTimeKind.Unspecified),
            timeZone);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
    }

}
