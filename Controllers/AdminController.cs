using BookSalesCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount = await _context.AppUsers.CountAsync();
        ViewBag.ActiveBookCount = await _context.Books.CountAsync(book => book.IsActive);
        ViewBag.CustomerCount = await _context.Customers.CountAsync(customer => customer.IsActive);
        ViewBag.OpenOrderCount = await _context.SalesOrders.CountAsync(
            order => order.Status == "Draft" || order.Status == "Sent");

        return View();
    }
}
