using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Infrastructure.Auth;

namespace PrintGrid.Modules.Customer.Infrastructure;

internal static class CustomerModuleRegistration
{
    internal static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IReprintRequestRepository, ReprintRequestRepository>();
        services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();
        services.AddScoped<IDesignRequestRepository, DesignRequestRepository>();
        services.AddSingleton<IGoogleIdentityVerifier, GoogleIdentityVerifier>();
    }
}
