
using BookSalesCore.Data;
using BookSalesCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string? actionFilter,
        string? entityName,
        int page = 1)
    {
        page = Math.Max(page, 1);

        const int pageSize = 20;

        var query = _context.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            var matchingUserIds = _context.Users
                .Where(user => user.UserName != null &&
                               user.UserName.Contains(searchTerm))
                .Select(user => user.Id);

            query = query.Where(x =>
                x.EntityId.Contains(searchTerm) ||
                (x.UserId != null &&
                 (x.UserId.Contains(searchTerm) ||
                  matchingUserIds.Contains(x.UserId))) ||
                (x.OldValue != null && x.OldValue.Contains(searchTerm)) ||
                (x.NewValue != null && x.NewValue.Contains(searchTerm)) ||
                (x.Reason != null && x.Reason.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(x => x.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var logs = await (
            from audit in query
            join user in _context.Users
                on audit.UserId equals user.Id into auditUsers
            from user in auditUsers.DefaultIfEmpty()
            orderby audit.CreatedAt descending, audit.AuditLogId descending
            select new AuditLogListItemViewModel
            {
                CreatedAt = audit.CreatedAt,
                ActorName = user == null
                    ? "Hệ thống"
                    : user.UserName ?? "Người dùng",
                Action = audit.Action,
                EntityName = audit.EntityName,
                EntityId = audit.EntityId,
                OldValue = audit.OldValue,
                NewValue = audit.NewValue,
                Reason = audit.Reason
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.ActionFilter = actionFilter;
        ViewBag.EntityName = entityName;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = totalPages;

        return View(logs);
    }
}
