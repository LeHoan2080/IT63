
using BookSalesCore.Data;
using BookSalesCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
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
});

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

await ProvisionBootstrapAdministratorAsync(app);

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
            var userManager = context.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.GetUserAsync(context.User);

            if (user?.MustChangePassword == true)
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

static async Task ProvisionBootstrapAdministratorAsync(WebApplication app)
{
    var configuration = app.Configuration;
    var userName = configuration["BootstrapAdmin:UserName"];
    var email = configuration["BootstrapAdmin:Email"];
    var password = configuration["BootstrapAdmin:Password"];
    var hasBootstrapConfiguration =
        !string.IsNullOrWhiteSpace(userName) ||
        !string.IsNullOrWhiteSpace(email) ||
        !string.IsNullOrWhiteSpace(password);

    if (!hasBootstrapConfiguration)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(userName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "BootstrapAdmin cần được cấu hình đầy đủ UserName, Email và Password.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var roleName in new[] { "Administrator", "SalesStaff" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể khởi tạo vai trò {roleName}: " +
                    string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }

    if ((await userManager.GetUsersInRoleAsync("Administrator")).Count > 0)
    {
        return;
    }

    if (await userManager.FindByNameAsync(userName) is not null)
    {
        throw new InvalidOperationException(
            "Tài khoản BootstrapAdmin đã tồn tại nhưng chưa có vai trò Administrator.");
    }

    var administrator = new ApplicationUser
    {
        UserName = userName.Trim(),
        Email = email.Trim(),
        EmailConfirmed = true,
        LockoutEnabled = true,
        MustChangePassword = true
    };

    var createResult = await userManager.CreateAsync(administrator, password);
    if (!createResult.Succeeded)
    {
        throw new InvalidOperationException(
            "Không thể tạo tài khoản quản trị ban đầu: " +
            string.Join("; ", createResult.Errors.Select(error => error.Description)));
    }

    var addRoleResult = await userManager.AddToRoleAsync(
        administrator,
        "Administrator");
    if (!addRoleResult.Succeeded)
    {
        await userManager.DeleteAsync(administrator);
        throw new InvalidOperationException(
            "Không thể gán vai trò quản trị ban đầu: " +
            string.Join("; ", addRoleResult.Errors.Select(error => error.Description)));
    }

    app.Logger.LogInformation(
        "Đã khởi tạo tài khoản quản trị đầu tiên. Hãy gỡ cấu hình BootstrapAdmin sau khi đăng nhập.");
}
