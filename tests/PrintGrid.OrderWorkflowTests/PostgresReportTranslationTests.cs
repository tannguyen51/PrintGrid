using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.Persistence;

namespace PrintGrid.OrderWorkflowTests;

/// <summary>Validate Npgsql translation without claiming to execute PostgreSQL SQL or constraints.</summary>
public sealed class PostgresReportTranslationTests
{
    [Theory]
    [InlineData("all")]
    [InlineData("incident")]
    [InlineData("open-incident")]
    [InlineData("partial-refund")]
    [InlineData("full-refund")]
    public async Task Report_summary_filters_and_pagination_translate_to_Postgres(string category)
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        var reader = new EmptyResults();
        await using var db = new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=translation_only;Username=test;Password=test")
            .AddInterceptors(new SuppressConnection(), reader).Options);
        var response = await new OpsOrdersController(db).List(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 19), category, "PG-", ct: default);
        response.Should().BeOfType<OkObjectResult>();
        reader.Sql.Should().HaveCount(3, "summary, count and page must compile before the database execution is intercepted");
        reader.Sql[0].Should().Contain("EXISTS").And.ContainEquivalentOf("COUNT");
        reader.Sql[2].Should().Contain("LIMIT").And.Contain("ORDER BY");
    }

    private sealed class SuppressConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class EmptyResults : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            var table = new DataTable();
            // The total-count query needs one scalar row; summary and page receive no rows.
            if (command.CommandText.TrimStart().StartsWith("SELECT count(*)", StringComparison.OrdinalIgnoreCase)
                && !command.CommandText.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase))
            { table.Columns.Add("count", typeof(int)); table.Rows.Add(0); }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}
