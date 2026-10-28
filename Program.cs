
using BookSalesCore.Data;
using BookSalesCore.Models;
using BookSalesCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection in user secrets or the ConnectionStrings__DefaultConnection environment variable.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "BookSales.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var authService = context.HttpContext.RequestServices
                .GetRequiredService<AuthService>();
            var user = await authService.GetActiveUserAsync(userId);
            if (user is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var refreshedPrincipal = UserClaimsPrincipalFactory.Create(user);
            var currentClaims = context.Principal!.Claims
                .Select(claim => (claim.Type, claim.Value))
                .OrderBy(claim => claim.Type)
                .ThenBy(claim => claim.Value);
            var refreshedClaims = refreshedPrincipal.Claims
                .Select(claim => (claim.Type, claim.Value))
                .OrderBy(claim => claim.Type)
                .ThenBy(claim => claim.Value);
            if (!currentClaims.SequenceEqual(refreshedClaims))
            {
                context.ReplacePrincipal(refreshedPrincipal);
                context.ShouldRenew = true;
            }
        };
    });

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<AppUser>,
    Microsoft.AspNetCore.Identity.PasswordHasher<AppUser>>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

// Buộc đổi mật khẩu nếu tài khoản đang có MustChangePassword = true.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path;

        var allowedPaths =
            path.StartsWithSegments("/Account/ChangePassword")
            || path.StartsWithSegments("/Account/Logout")
            || path.StartsWithSegments("/Account/AccessDenied");

        if (!allowedPaths)
        {
            if (bool.TryParse(
                    context.User.FindFirstValue("MustChangePassword"),
                    out var mustChangePassword) &&
                mustChangePassword)
            {
                context.Response.Redirect(
                    "/Account/ChangePassword");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
