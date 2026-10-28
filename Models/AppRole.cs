namespace BookSalesCore.Models;

public class AppRole
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
