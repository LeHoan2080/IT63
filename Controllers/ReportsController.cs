using BookSalesCore.Data;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator,SalesStaff")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly TimeZoneInfo _vietnamTimeZone;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;

        try
        {
            _vietnamTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            _vietnamTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? fromDate,
        DateTime? toDate)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            _vietnamTimeZone).Date;

        var startDate = fromDate?.Date ?? today.AddDays(-29);
        var endDate = toDate?.Date ?? today;

        if (startDate > endDate)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

            return View(new ReportsIndexViewModel
            {
                FromDate = startDate,
                ToDate = endDate
            });
        }

        // Khoảng ngày được chọn là ngày Việt Nam, chuyển sang UTC
        // để truy vấn các trường thời gian được lưu theo UTC.
        var utcStart = ConvertVietnamDateToUtc(startDate);
        var utcEndExclusive = ConvertVietnamDateToUtc(endDate.AddDays(1));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdministrator = User.IsInRole("Administrator");

        if (!isAdministrator && string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        IQueryable<SalesOrder> scopedOrders =
            _context.SalesOrders.AsNoTracking();

        if (!isAdministrator)
        {
            scopedOrders = scopedOrders.Where(
                order => order.CreatedByUserId == userId);
        }

        // Đếm chứng từ theo OrderDate và trạng thái hiện tại.
        var periodOrders = scopedOrders.Where(order =>
            order.OrderDate >= utcStart &&
            order.OrderDate < utcEndExclusive);

        var totalOrderCount = await periodOrders.CountAsync();

        var draftCount = await periodOrders.CountAsync(
            order => order.Status == "Draft");

        var sentCount = await periodOrders.CountAsync(
            order => order.Status == "Sent");

        var cancelledCount = await periodOrders.CountAsync(
            order => order.Status == "Cancelled");

        // Chỉ các đơn hiện vẫn Confirmed mới được tính doanh số.
        // Dùng ConfirmedAt để lọc, không dùng OrderDate.
        var confirmedOrdersQuery = scopedOrders.Where(order =>
            order.Status == "Confirmed" &&
            order.ConfirmedAt.HasValue &&
            order.ConfirmedAt.Value >= utcStart &&
            order.ConfirmedAt.Value < utcEndExclusive);

        var confirmedOrderCount = await periodOrders.CountAsync(
            order => order.Status == "Confirmed");

        var salesConfirmedOrderCount =
            await confirmedOrdersQuery.CountAsync();

        var totalSales = await confirmedOrdersQuery
            .Select(order => (decimal?)order.TotalAmount)
            .SumAsync() ?? 0m;

        // Lấy dữ liệu cần thiết trước, sau đó đổi múi giờ và nhóm theo
        // ngày Việt Nam trong bộ nhớ. Tránh nhóm theo ngày UTC.
        var confirmedOrderDates = await confirmedOrdersQuery
            .Select(order => new
            {
                order.ConfirmedAt,
                order.TotalAmount
            })
            .ToListAsync();

        var dailySales = confirmedOrderDates
            .Where(order => order.ConfirmedAt.HasValue)
            .Select(order => new
            {
                LocalDate = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(
                        order.ConfirmedAt!.Value,
                        DateTimeKind.Utc),
                    _vietnamTimeZone).Date,
                order.TotalAmount
            })
            .GroupBy(order => order.LocalDate)
            .Select(group => new DailySalesReportItem
            {
                Date = group.Key,
                TotalSales = group.Sum(item => item.TotalAmount),
                OrderCount = group.Count()
            })
            .OrderBy(item => item.Date)
            .ToList();

        // Sách bán chạy được tính từ các dòng của đơn đang Confirmed
        // trong khoảng ngày xác nhận đã chọn.
        var topBooks = await (
            from line in _context.SalesOrderLines.AsNoTracking()
            join order in confirmedOrdersQuery
                on line.SalesOrderId equals order.SalesOrderId
            group line by new
            {
                line.SKUSnapshot,
                line.BookTitleSnapshot
            }
            into bookGroup
            orderby bookGroup.Sum(line => line.Quantity) descending
            select new TopBookReportItem
            {
                SKU = bookGroup.Key.SKUSnapshot,
                BookTitle = bookGroup.Key.BookTitleSnapshot,
                TotalQuantity = bookGroup.Sum(line => line.Quantity),
                TotalSales = bookGroup.Sum(line => line.LineTotal)
            })
            .Take(10)
            .ToListAsync();

        var viewModel = new ReportsIndexViewModel
        {
            FromDate = startDate,
            ToDate = endDate,
            TotalSales = totalSales,
            ConfirmedOrderCount = confirmedOrderCount,
            SalesConfirmedOrderCount = salesConfirmedOrderCount,
            DraftCount = draftCount,
            SentCount = sentCount,
            CancelledCount = cancelledCount,
            TotalOrderCount = totalOrderCount,
            DailySales = dailySales,
            TopBooks = topBooks
        };

        return View(viewModel);
    }

    private DateTime ConvertVietnamDateToUtc(DateTime vietnamDate)
    {
        var localDate = DateTime.SpecifyKind(
            vietnamDate,
            DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            localDate,
            _vietnamTimeZone);
    }
}