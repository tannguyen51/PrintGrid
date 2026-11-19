using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintGrid.Modules.Analytics.Domain.Entities;

namespace PrintGrid.Modules.Analytics.Infrastructure;

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> b)
    {
        b.ToTable("financial_transactions", "analytics"); b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(16).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Reference).HasMaxLength(128).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired(); b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => new { x.Type, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.OrderId, x.ConfirmedAtUtc });
    }
}

public sealed class LabPayableConfiguration : IEntityTypeConfiguration<LabPayable>
{
    public void Configure(EntityTypeBuilder<LabPayable> b)
    {
        b.ToTable("lab_payables", "analytics"); b.HasKey(x => x.Id);
        b.Property(x => x.GrossAmount).HasPrecision(18, 2); b.Property(x => x.DeductionAmount).HasPrecision(18, 2);
        b.Ignore(x => x.NetAmount); b.Property(x => x.Currency).HasMaxLength(3); b.Property(x => x.Status).HasMaxLength(16);
        b.Property(x => x.PaymentReference).HasMaxLength(128); b.HasIndex(x => x.JobId).IsUnique();
        b.HasIndex(x => new { x.LabId, x.Status }); b.HasIndex(x => x.OrderId);
    }
}
