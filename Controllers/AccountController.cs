using System.Security.Claims;
using BookSalesCore.Services;
using BookSalesCore.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BookSalesCore.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        AuthService authService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        LoginResult result;
        try
        {
            result = await _authService.LoginAsync(
                model.UserNameOrEmail,
                model.Password);
        }
        catch (SqlException exception)
        {
            _logger.LogError(exception, "Database connection failed during sign-in.");
            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối cơ sở dữ liệu. Vui lòng kiểm tra SQL Server và thử lại.");
            return View(model);
        }
        if (result.Status == LoginStatus.LockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.");
            return View(model);
        }

        if (result.Status != LoginStatus.Success || result.User is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UserClaimsPrincipalFactory.Create(result.User),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            });

        if (result.User.MustChangePassword)
        {
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var result = await _authService.ChangePasswordAsync(
            userId,
            model.CurrentPassword,
            model.NewPassword);

        if (result == ChangePasswordStatus.InvalidCurrentPassword)
        {
            ModelState.AddModelError(
                nameof(model.CurrentPassword),
                "Mật khẩu hiện tại không chính xác.");
            return View(model);
        }

        if (result == ChangePasswordStatus.UserNotFound)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var user = await _authService.GetActiveUserAsync(userId);
        if (user is null)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UserClaimsPrincipalFactory.Create(user));

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
