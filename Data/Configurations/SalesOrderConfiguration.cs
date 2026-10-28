using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesCore.Data.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", table =>
        {
            table.HasCheckConstraint(
                "CK_SalesOrders_Status",
                "[Status] IN ('Draft', 'Sent', 'Confirmed', 'Cancelled')");

            table.HasCheckConstraint(
                "CK_SalesOrders_TotalAmount",
                "[TotalAmount] >= 0");

            table.HasCheckConstraint(
                "CK_SalesOrders_StatusTimestamps",
                "([Status] <> 'Sent' OR [SentAt] IS NOT NULL) AND " +
                "([Status] <> 'Confirmed' OR [ConfirmedAt] IS NOT NULL) AND " +
                "([Status] <> 'Cancelled' OR ([CancelledAt] IS NOT NULL AND [CancellationReason] IS NOT NULL))");
        });

        builder.HasKey(x => x.SalesOrderId);

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerNameSnapshot)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.OrderNumber)
            .IsUnique();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,0)")
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.SentByUserId).HasMaxLength(450);
        builder.Property(x => x.ConfirmedByUserId).HasMaxLength(450);
        builder.Property(x => x.CancelledByUserId).HasMaxLength(450);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Status, x.OrderDate });
        builder.HasIndex(x => new { x.Status, x.ConfirmedAt });
        builder.HasIndex(x => new { x.CustomerId, x.OrderDate });
        builder.HasIndex(x => new { x.CreatedByUserId, x.Status, x.OrderDate });
        builder.HasIndex(x => new { x.CreatedByUserId, x.Status, x.ConfirmedAt });
    }
}