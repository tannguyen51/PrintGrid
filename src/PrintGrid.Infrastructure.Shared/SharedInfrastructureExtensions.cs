using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Caching;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Time;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared;

public static class SharedInfrastructureExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PrintGridDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "shared")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        return services;
    }
}
