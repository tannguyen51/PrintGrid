using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure;

internal static class CustomerModuleRegistration
{
    internal static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
    }
}
