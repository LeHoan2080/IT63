using System.Security.Cryptography;
using System.Security.Claims;
using BookSalesCore.Data;
using BookSalesCore.Models;
using BookSalesCore.Models.Entities;
using BookSalesCore.ViewModels;
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

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // GET: /Users
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.UserName)
            .ToListAsync();

        var model = new UserListViewModel();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            model.Users.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Chưa gán vai trò",
                IsLockedOut = isLockedOut,
                MustChangePassword = user.MustChangePassword
            });
        }

        return View(model);
    }

    // GET: /Users/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    // POST: /Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Role != AdministratorRole &&
            model.Role != SalesStaffRole)
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Vai trò không hợp lệ.");

            return View(model);
        }

        var userName = model.UserName.Trim();
        var email = model.Email.Trim();

        if (await _userManager.FindByNameAsync(userName) is not null)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "Tên đăng nhập đã tồn tại.");

            return View(model);
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email đã được sử dụng.");

            return View(model);
        }

        foreach (var roleName in new[]
                 {
                     AdministratorRole,
                     SalesStaffRole
                 })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }
        }

        var temporaryPassword = GenerateTemporaryPassword();

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = true,
            LockoutEnabled = true
        };

        var createResult = await _userManager.CreateAsync(
            user,
            temporaryPassword);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(
            user,
            model.Role);

        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            foreach (var error in addRoleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        try
        {
            await WriteAuditLogAsync(
                action: "Created",
                entityName: "User",
                entityId: user.Id,
                oldValue: null,
                newValue: $"UserName={userName}; Role={model.Role}",
                reason: "Tạo tài khoản người dùng");

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Không hiển thị chi tiết nội bộ hoặc dữ liệu nhạy cảm.
            // Tài khoản đã được tạo; báo rõ rằng nhật ký cần được kiểm tra.
            TempData["ErrorMessage"] =
                "Tài khoản đã được tạo nhưng chưa ghi được nhật ký kiểm toán. Vui lòng kiểm tra hệ thống.";

            return RedirectToAction(nameof(Index));
        }

        TempData["CreatedUserName"] = userName;
        TempData["TemporaryPassword"] = temporaryPassword;
        TempData["SuccessMessage"] = "Đã tạo tài khoản thành công.";

        return RedirectToAction(nameof(Create));
    }

    // POST: /Users/ChangeRole/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(
        string id,
        string role,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        if (role != AdministratorRole && role != SalesStaffRole)
        {
            TempData["ErrorMessage"] = "Vai trò được chọn không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] =
                "Bạn không thể tự thay đổi vai trò của chính mình.";

            return RedirectToAction(nameof(Index));
        }

        var oldRoles = await _userManager.GetRolesAsync(user);
        var oldRole = oldRoles.FirstOrDefault();

        if (oldRole == role)
        {
            TempData["SuccessMessage"] =
                "Tài khoản đã có vai trò được chọn; không có thay đổi nào được thực hiện.";

            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            TempData["ErrorMessage"] =
                "Vai trò chưa tồn tại trong hệ thống.";

            return RedirectToAction(nameof(Index));
        }

        // Không cho phép xóa quyền quản trị cuối cùng.
        if (oldRoles.Contains(AdministratorRole) &&
            role != AdministratorRole)
        {
            var administratorCount = await _userManager
                .GetUsersInRoleAsync(AdministratorRole);

            if (administratorCount.Count <= 1)
            {
                TempData["ErrorMessage"] =
                    "Không thể hạ quyền quản trị của tài khoản quản trị viên cuối cùng.";

                return RedirectToAction(nameof(Index));
            }
        }

        var reasonText = string.IsNullOrWhiteSpace(reason)
            ? "Thay đổi vai trò bởi quản trị viên"
            : reason.Trim();

        if (reasonText.Length > 500)
        {
            TempData["ErrorMessage"] =
                "Lý do thay đổi vai trò không được vượt quá 500 ký tự.";

            return RedirectToAction(nameof(Index));
        }

        // Thay vai trò bằng cách gỡ các vai trò cũ rồi gán vai trò mới.
        var removeResult = await _userManager.RemoveFromRolesAsync(
            user,
            oldRoles);

        if (!removeResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                removeResult.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);

        if (!addResult.Succeeded)
        {
            // Cố gắng khôi phục vai trò ban đầu nếu gán vai trò mới thất bại.
            if (oldRoles.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, oldRoles);
            }

            TempData["ErrorMessage"] = string.Join(
                " ",
                addResult.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        try
        {
            await WriteAuditLogAsync(
                action: "RoleChanged",
                entityName: "User",
                entityId: user.Id,
                oldValue: oldRole ?? "Chưa gán vai trò",
                newValue: role,
                reason: reasonText);

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] =
                "Vai trò đã được thay đổi nhưng chưa ghi được nhật ký kiểm toán. Vui lòng kiểm tra hệ thống.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] =
            $"Đã thay đổi vai trò của tài khoản {user.UserName}.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ToggleLock/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] =
                "Bạn không thể tự khóa tài khoản đang sử dụng.";

            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var unlockResult = await _userManager.SetLockoutEndDateAsync(
                user,
                null);

            if (unlockResult.Succeeded)
            {
                TempData["SuccessMessage"] =
                    $"Đã mở khóa tài khoản {user.UserName}.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(
                    " ",
                    unlockResult.Errors.Select(error => error.Description));
            }
        }
        else
        {
            var lockResult = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.MaxValue);

            if (lockResult.Succeeded)
            {
                TempData["SuccessMessage"] =
                    $"Đã khóa tài khoản {user.UserName}.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(
                    " ",
                    lockResult.Errors.Select(error => error.Description));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ResetPassword/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var temporaryPassword = GenerateTemporaryPassword();

        var resetToken =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetResult = await _userManager.ResetPasswordAsync(
            user,
            resetToken,
            temporaryPassword);

        if (!resetResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                resetResult.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        user.MustChangePassword = true;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            TempData["ErrorMessage"] =
                "Đã đặt lại mật khẩu nhưng không cập nhật được cờ bắt buộc đổi mật khẩu. Vui lòng kiểm tra tài khoản.";

            return RedirectToAction(nameof(Index));
        }

        TempData["ResetUserName"] = user.UserName;
        TempData["TemporaryPassword"] = temporaryPassword;
        TempData["SuccessMessage"] =
            "Đã cấp mật khẩu tạm mới. Hãy chuyển mật khẩu này trực tiếp cho người dùng.";

        return RedirectToAction(nameof(Index));
    }

    private async Task WriteAuditLogAsync(
        string action,
        string entityName,
        string entityId,
        string? oldValue,
        string? newValue,
        string? reason)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var log = new AuditLog
        {
            UserId = actorId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = LimitLength(oldValue, 1000),
            NewValue = LimitLength(newValue, 1000),
            Reason = LimitLength(reason, 500),
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(log);
    }

    private static string? LimitLength(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
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

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static char Pick(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }
}
