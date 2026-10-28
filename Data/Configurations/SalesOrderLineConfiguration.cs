using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesCore.Data.Configurations;

public class SalesOrderLineConfiguration
    : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines", table =>
        {
            table.HasCheckConstraint(
                "CK_SalesOrderLines_Quantity",
                "[Quantity] > 0");

            table.HasCheckConstraint(
                "CK_SalesOrderLines_UnitPrice",
                "[UnitPrice] >= 0");

            table.HasCheckConstraint(
                "CK_SalesOrderLines_LineTotal",
                "[LineTotal] = [Quantity] * [UnitPrice]");
        });

        builder.HasKey(x => x.SalesOrderLineId);

        builder.Property(x => x.SKUSnapshot)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BookTitleSnapshot)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,0)")
            .IsRequired();

        builder.Property(x => x.LineTotal)
            .HasColumnType("decimal(18,0)")
            .IsRequired();

        builder.HasOne(x => x.SalesOrder)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Book)
            .WithMany()
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.BookId);
    }
}