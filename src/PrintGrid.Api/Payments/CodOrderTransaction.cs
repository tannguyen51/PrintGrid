using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PrintGrid.Infrastructure.Shared.Persistence;

namespace PrintGrid.Api.Payments;

/// <summary>Serialize COD decisions and deadline processing on the persisted order across API replicas.</summary>
public static class CodOrderTransaction
{
    public static async Task<IDbContextTransaction?> BeginAsync(PrintGridDbContext db, Guid orderId, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return null;
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM customer.orders WHERE \"Id\" = {orderId} FOR UPDATE", ct);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
