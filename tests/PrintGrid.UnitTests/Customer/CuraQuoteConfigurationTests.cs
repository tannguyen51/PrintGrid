using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrintGrid.Modules.Customer.Infrastructure;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Customer;

public sealed class CuraQuoteConfigurationTests
{
    [Fact]
    public void Customer_module_binds_the_deployed_engine_configuration_to_the_quote_slicer()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["QuoteSlicer:ExecutablePath"] = "/opt/cura/CuraEngine",
            ["QuoteSlicer:TimeoutSeconds"] = "90"
        }).Build();
        var services = new ServiceCollection();
        new CustomerModule().RegisterServices(services, configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CuraQuoteOptions>>().Value;

        options.ExecutablePath.Should().Be("/opt/cura/CuraEngine");
        options.TimeoutSeconds.Should().Be(90);
    }
}
