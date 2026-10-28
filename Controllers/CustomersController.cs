using BookSalesCore.Data;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator,SalesStaff")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string? status,
        int page = 1,
        int pageSize = 10)
    {
        int[] allowedPageSizes = [10, 20, 50, 100];

        if (!allowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        page = Math.Max(1, page);

        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(c =>
                c.CustomerCode.Contains(term) ||
                c.CustomerName.Contains(term) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.Email != null && c.Email.Contains(term)));
        }

        if (status == "active")
        {
            query = query.Where(c => c.IsActive);
        }
        else if (status == "inactive")
        {
            query = query.Where(c => !c.IsActive);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var customers = await query
            .OrderBy(c => c.CustomerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new CustomerListViewModel
        {
            SearchTerm = searchTerm,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Customers = customers
        };

        ViewData["Pagination"] = new PaginationViewModel
        {
            Action = nameof(Index),
            Controller = "Customers",
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
    public async Task<IActionResult> Details(long id, int page = 1)
    {
        const int pageSize = 10;
        page = Math.Max(1, page);

        var customer = await _context.Customers
            .AsNoTracking()
            .Where(item => item.CustomerId == id)
            .Select(item => new
            {
                item.CustomerId,
                item.CustomerCode,
                item.CustomerName,
                item.Phone,
                item.Email,
                item.Address,
                item.IsActive
            })
            .FirstOrDefaultAsync();

        if (customer is null)
        {
            return NotFound();
        }

        var ordersQuery = _context.SalesOrders
            .AsNoTracking()
            .Where(order => order.CustomerId == id);

        if (!User.IsInRole("Administrator"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            ordersQuery = ordersQuery.Where(order => order.CreatedByUserId == userId);
        }

        var totalOrders = await ordersQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var confirmedSummary = await ordersQuery
            .Where(order => order.Status == "Confirmed")
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Total = group.Sum(order => order.TotalAmount)
            })
            .FirstOrDefaultAsync();

        var salesOrders = await ordersQuery
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.SalesOrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new CustomerSalesOrderListItemViewModel
            {
                SalesOrderId = order.SalesOrderId,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerName = order.CustomerNameSnapshot,
                TotalAmount = order.TotalAmount,
                Status = order.Status
            })
            .ToListAsync();

        var timeZone = GetVietnamTimeZone();
        foreach (var order in salesOrders)
        {
            order.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc),
                timeZone);
        }

        var model = new CustomerDetailsViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            IsActive = customer.IsActive,
            IsWalkInCustomer = customer.CustomerCode == "CUS-WALKIN",
            Page = page,
            PageSize = pageSize,
            TotalOrders = totalOrders,
            ConfirmedOrders = confirmedSummary?.Count ?? 0,
            ConfirmedSales = confirmedSummary?.Total ?? 0m,
            SalesOrders = salesOrders
        };

        ViewData["Pagination"] = new PaginationViewModel
        {
            Action = nameof(Details),
            Controller = "Customers",
            Page = page,
            PageSize = pageSize,
            TotalItems = totalOrders,
            RouteValues = new Dictionary<string, string?>
            {
                ["id"] = id.ToString()
            }
        };

        return View(model);
    }

    [Authorize(Roles = "Administrator,SalesStaff")]
    public IActionResult Create()
    {
        return View(new CustomerCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,SalesStaff")]
    public async Task<IActionResult> Create(CustomerCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isAdministrator = User.IsInRole("Administrator");
        var nextNumber = await _context.Database
            .SqlQuery<long>($"SELECT NEXT VALUE FOR dbo.CustomerCodeSequence AS Value")
            .SingleAsync();

        var customer = new Customer
        {
            CustomerCode = $"CUS-{nextNumber:D6}",
            CustomerName = model.CustomerName.Trim(),
            Phone = NormalizeOptional(model.Phone),
            Email = NormalizeOptional(model.Email),
            Address = NormalizeOptional(model.Address),
            IsActive = !isAdministrator || model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu khách hàng. Vui lòng kiểm tra dữ liệu và thử lại.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Đã thêm khách hàng thành công.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrator,SalesStaff")]
    public async Task<IActionResult> Edit(long id)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        if (customer.CustomerCode == "CUS-WALKIN")
        {
            return NotFound();
        }

        var model = new CustomerEditViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            IsActive = customer.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,SalesStaff")]
    public async Task<IActionResult> Edit(
        long id,
        CustomerEditViewModel model)
    {
        if (id != model.CustomerId)
        {
            return BadRequest();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        if (customer.CustomerCode == "CUS-WALKIN")
        {
            return NotFound();
        }

        var isAdministrator = User.IsInRole("Administrator");

        if (!isAdministrator)
        {
            ModelState.Remove(nameof(model.IsActive));
            model.IsActive = customer.IsActive;
        }

        if (!ModelState.IsValid)
        {
            model.CustomerCode = customer.CustomerCode;
            return View(model);
        }

        customer.CustomerName = model.CustomerName.Trim();
        if (isAdministrator)
        {
            customer.IsActive = model.IsActive;
        }

        customer.Phone = NormalizeOptional(model.Phone);
        customer.Email = NormalizeOptional(model.Email);
        customer.Address = NormalizeOptional(model.Address);
        customer.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật khách hàng. Vui lòng thử lại.");

            model.CustomerCode = customer.CustomerCode;
            return View(model);
        }

        TempData["SuccessMessage"] = "Đã cập nhật khách hàng thành công.";

        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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