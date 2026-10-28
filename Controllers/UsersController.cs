using System.Security.Claims;
using System.Security.Cryptography;
using BookSalesCore.Data;
using BookSalesCore.Models;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Controllers;

[Authorize(Roles = "Administrator")]
public class UsersController : Controller
{
    private const string AdministratorRole = "Administrator";
    private const string SalesStaffRole = "SalesStaff";

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        IPasswordHasher<AppUser> passwordHasher,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.AppUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.UserName)
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.Name,
                IsLockedOut = user.LockoutEnd > DateTimeOffset.UtcNow,
                MustChangePassword = user.MustChangePassword
            })
            .ToListAsync();

        return View(new UserListViewModel { Users = users });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = await _context.AppRoles
            .FirstOrDefaultAsync(candidate =>
                candidate.Code == model.Role && candidate.IsActive);
        if (role is null)
        {
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
            return View(model);
        }

        var userName = model.UserName.Trim();
        var email = model.Email.Trim();
        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();
        if (await _context.AppUsers.AnyAsync(user =>
                user.NormalizedUserName == normalizedUserName))
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã tồn tại.");
            return View(model);
        }

        if (await _context.AppUsers.AnyAsync(user =>
                user.NormalizedEmail == normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View(model);
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            RoleId = role.Id,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);

        _context.AppUsers.Add(user);
        WriteAuditLog(
            "Created",
            "User",
            user.Id,
            null,
            $"UserName={userName}; Role={role.Code}",
            "Tạo tài khoản người dùng");

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to create user {UserName}.", userName);
            ModelState.AddModelError(
                string.Empty,
                "Không thể tạo tài khoản. Vui lòng kiểm tra tên đăng nhập/email và thử lại.");
            return View(model);
        }

        TempData["CreatedUserName"] = userName;
        TempData["TemporaryPassword"] = temporaryPassword;
        TempData["SuccessMessage"] = "Đã tạo tài khoản thành công.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role, string? reason)
    {
        if (role != AdministratorRole && role != SalesStaffRole)
        {
            TempData["ErrorMessage"] = "Vai trò được chọn không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _context.AppUsers
            .Include(candidate => candidate.Role)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        var newRole = await _context.AppRoles
            .FirstOrDefaultAsync(candidate => candidate.Code == role && candidate.IsActive);
        if (user is null || newRole is null)
        {
            return NotFound();
        }

        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["ErrorMessage"] = "Bạn không thể tự thay đổi vai trò của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var oldRole = user.Role.Code;
        if (oldRole == role)
        {
            TempData["SuccessMessage"] = "Tài khoản đã có vai trò được chọn.";
            return RedirectToAction(nameof(Index));
        }

        if (oldRole == AdministratorRole &&
            role != AdministratorRole &&
            await _context.AppUsers.CountAsync(candidate =>
                candidate.Role.Code == AdministratorRole && candidate.IsActive) <= 1)
        {
            TempData["ErrorMessage"] =
                "Không thể hạ quyền quản trị của tài khoản quản trị viên cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        var reasonText = string.IsNullOrWhiteSpace(reason)
            ? "Thay đổi vai trò bởi quản trị viên"
            : reason.Trim();
        if (reasonText.Length > 500)
        {
            TempData["ErrorMessage"] = "Lý do không được vượt quá 500 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        user.RoleId = newRole.Id;
        user.UpdatedAt = DateTime.UtcNow;
        WriteAuditLog(
            "RoleChanged",
            "User",
            user.Id,
            oldRole,
            role,
            reasonText);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thay đổi vai trò của tài khoản {user.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản đang sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        var isLocked = user.LockoutEnd > DateTimeOffset.UtcNow;
        user.LockoutEnd = isLocked ? null : DateTimeOffset.MaxValue;
        user.FailedAccessCount = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = isLocked
            ? $"Đã mở khóa tài khoản {user.UserName}."
            : $"Đã khóa tài khoản {user.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        user.FailedAccessCount = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["ResetUserName"] = user.UserName;
        TempData["TemporaryPassword"] = temporaryPassword;
        TempData["SuccessMessage"] =
            "Đã cấp mật khẩu tạm mới. Hãy chuyển mật khẩu này trực tiếp cho người dùng.";
        return RedirectToAction(nameof(Index));
    }

    private void WriteAuditLog(
        string action,
        string entityName,
        string entityId,
        string? oldValue,
        string? newValue,
        string? reason)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = LimitLength(oldValue, 1000),
            NewValue = LimitLength(newValue, 1000),
            Reason = LimitLength(reason, 500),
            CreatedAt = DateTime.UtcNow
        });

    }

    private static string? LimitLength(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@$%*-_";
        const string all = upper + lower + digits + special;

        var chars = new List<char>
        {
            Pick(upper),
            Pick(lower),
            Pick(digits),
            Pick(special)
        };
        while (chars.Count < 16)
        {
            chars.Add(Pick(all));
        }

        for (var index = chars.Count - 1; index > 0; index--)
        {
            var other = RandomNumberGenerator.GetInt32(index + 1);
            (chars[index], chars[other]) = (chars[other], chars[index]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }
}
