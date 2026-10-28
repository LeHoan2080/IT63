using System.Diagnostics;
using System.Security.Claims;
using BookSalesCore.Data;
using Microsoft.AspNetCore.Mvc;
using BookSalesCore.Models;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly TimeZoneInfo _vietnamTimeZone;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
        _vietnamTimeZone = GetVietnamTimeZone();
    }

    public async Task<IActionResult> Index()
    {
        var isAdministrator = User.IsInRole("Administrator");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!isAdministrator &&
            (!User.IsInRole("SalesStaff") || string.IsNullOrWhiteSpace(userId)))
        {
            return Forbid();
        }

        var today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            _vietnamTimeZone).Date;
        var startDate = today.AddDays(-29);
        var utcStart = ConvertVietnamDateToUtc(startDate);
        var utcEndExclusive = ConvertVietnamDateToUtc(today.AddDays(1));

        IQueryable<SalesOrder> orders = _context.SalesOrders.AsNoTracking();
        if (!isAdministrator)
        {
            orders = orders.Where(order => order.CreatedByUserId == userId);
        }

        var confirmedOrders = orders.Where(order =>
            order.Status == "Confirmed" &&
            order.ConfirmedAt.HasValue &&
            order.ConfirmedAt.Value >= utcStart &&
            order.ConfirmedAt.Value < utcEndExclusive);

        var totalSales = await confirmedOrders
            .Select(order => (decimal?)order.TotalAmount)
            .SumAsync() ?? 0m;

        var confirmedOrderCount = await confirmedOrders.CountAsync();
        var pendingQuotationCount = await orders.CountAsync(order =>
            order.Status == "Draft" || order.Status == "Sent");

        var topBooks = await (
            from line in _context.SalesOrderLines.AsNoTracking()
            join order in confirmedOrders
                on line.SalesOrderId equals order.SalesOrderId
            group line by new
            {
                line.SKUSnapshot,
                line.BookTitleSnapshot
            }
            into bookGroup
            orderby bookGroup.Sum(line => line.Quantity) descending
            select new DashboardTopBookViewModel
            {
                SKU = bookGroup.Key.SKUSnapshot,
                BookTitle = bookGroup.Key.BookTitleSnapshot,
                Quantity = bookGroup.Sum(line => line.Quantity),
                TotalSales = bookGroup.Sum(line => line.LineTotal)
            })
            .Take(5)
            .ToListAsync();

        var recentOrders = await orders
            .OrderByDescending(order => order.UpdatedAt ?? order.CreatedAt)
            .ThenByDescending(order => order.SalesOrderId)
            .Take(8)
            .Select(order => new DashboardRecentOrderViewModel
            {
                SalesOrderId = order.SalesOrderId,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerNameSnapshot,
                Status = order.Status,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount
            })
            .ToListAsync();

        foreach (var recentOrder in recentOrders)
        {
            recentOrder.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(recentOrder.OrderDate, DateTimeKind.Utc),
                _vietnamTimeZone);
        }

        return View(new DashboardViewModel
        {
            FromDate = startDate,
            ToDate = today,
            TotalSales = totalSales,
            PendingQuotationCount = pendingQuotationCount,
            ConfirmedOrderCount = confirmedOrderCount,
            TopBooks = topBooks,
            RecentOrders = recentOrders
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private DateTime ConvertVietnamDateToUtc(DateTime vietnamDate)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(vietnamDate, DateTimeKind.Unspecified),
            _vietnamTimeZone);
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
