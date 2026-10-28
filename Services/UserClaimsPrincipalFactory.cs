using System.Security.Claims;
using BookSalesCore.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BookSalesCore.Services;

public static class UserClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Code),
            new Claim("MustChangePassword", user.MustChangePassword.ToString())
        };

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
