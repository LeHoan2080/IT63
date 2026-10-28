using BookSalesCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesCore.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.AuditLogId);

        builder.Property(x => x.UserId).HasMaxLength(450);

        builder.Property(x => x.Action)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.OldValue).HasMaxLength(1000);
        builder.Property(x => x.NewValue).HasMaxLength(1000);
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new
        {
            x.EntityName,
            x.EntityId,
            x.CreatedAt
        });
    }
}