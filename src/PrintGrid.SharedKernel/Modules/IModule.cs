using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PrintGrid.SharedKernel.Modules;

public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
