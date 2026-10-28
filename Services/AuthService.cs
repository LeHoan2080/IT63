using BookSalesCore.Data;
using BookSalesCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Services;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut
}

public sealed record LoginResult(LoginStatus Status, AppUser? User = null);

public enum ChangePasswordStatus
{
    Success,
    UserNotFound,
    InvalidCurrentPassword
}

public class AuthService
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaximumFailedAttempts = 5;

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password)
    {
        var normalizedLogin = usernameOrEmail.Trim().ToUpperInvariant();
        var user = await _context.AppUsers
            .Include(candidate => candidate.Role)
            .FirstOrDefaultAsync(candidate =>
                candidate.NormalizedUserName == normalizedLogin ||
                candidate.NormalizedEmail == normalizedLogin);

        if (user is null || !user.IsActive || !user.Role.IsActive)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var now = DateTimeOffset.UtcNow;
        if (user.LockoutEnd > now)
        {
            return new LoginResult(LoginStatus.LockedOut);
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedAccessCount++;
            if (user.FailedAccessCount >= MaximumFailedAttempts)
            {
                user.LockoutEnd = now.Add(LockoutDuration);
                user.FailedAccessCount = 0;
            }

            await _context.SaveChangesAsync();

            return new LoginResult(
                user.LockoutEnd > now ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials);
        }

        user.FailedAccessCount = 0;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync();

        return new LoginResult(LoginStatus.Success, user);
    }

    public async Task<ChangePasswordStatus> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(candidate => candidate.Id == userId);

        if (user is null || !user.IsActive)
        {
            return ChangePasswordStatus.UserNotFound;
        }

        if (_passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword) == PasswordVerificationResult.Failed)
        {
            return ChangePasswordStatus.InvalidCurrentPassword;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ChangePasswordStatus.Success;
    }

    public Task<AppUser?> GetActiveUserAsync(string userId)
    {
        return _context.AppUsers
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.IsActive &&
                user.Role.IsActive &&
                (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow));
    }
}
