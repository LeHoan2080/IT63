using BookSalesCore.Models;
using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookSalesCore.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AppRole>(entity =>
        {
            entity.ToTable("AppRoles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasMaxLength(450);
            entity.Property(role => role.Code).HasMaxLength(50).IsRequired();
            entity.Property(role => role.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(role => role.Code).IsUnique();
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasMaxLength(450);
            entity.Property(user => user.UserName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.NormalizedUserName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.RoleId).HasMaxLength(450).IsRequired();
            entity.HasIndex(user => user.NormalizedUserName).IsUnique();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.HasSequence<long>("CustomerCodeSequence")
            .StartsAt(1)
            .IncrementsBy(1);

        builder.HasSequence<long>("BookCodeSequence")
            .StartsAt(1)
            .IncrementsBy(1);

        builder.HasSequence<long>("SalesOrderNumberSequence")
            .StartsAt(1)
            .IncrementsBy(1);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}