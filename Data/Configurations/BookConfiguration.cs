
using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesCore.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", table =>
        {
            table.HasCheckConstraint(
                "CK_Books_SalePrice_NonNegative",
                "[SalePrice] >= 0");
        });

        builder.HasKey(x => x.BookId);

        builder.Property(x => x.SKU)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.SKU)
            .IsUnique();

        builder.Property(x => x.ISBN)
            .HasMaxLength(20);

        builder.Property(x => x.BookTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.CategoryName)
            .HasMaxLength(150);

        builder.Property(x => x.SalePrice)
            .HasPrecision(18, 0)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
