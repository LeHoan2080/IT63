
using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesCore.Data.Configurations;

public class CustomerConfiguration
    : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", table =>
        {
            table.HasCheckConstraint(
                "CK_Customers_WalkIn_Active",
                "[CustomerCode] <> 'CUS-WALKIN' OR [IsActive] = 1");
        });

        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.CustomerId)
            .UseIdentityColumn();

        builder.Property(c => c.CustomerCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(c => c.CustomerCode)
            .IsUnique();

        builder.Property(c => c.CustomerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnType("datetime2");
    }
}
