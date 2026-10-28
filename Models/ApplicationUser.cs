
using Microsoft.AspNetCore.Identity;

namespace BookSalesCore.Models;

public class ApplicationUser : IdentityUser
{
    public bool MustChangePassword { get; set; } = true;
}
