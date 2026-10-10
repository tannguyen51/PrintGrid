using NSubstitute;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.UnitTests.Customer;

public class ModelQuotaTests
{
    [Fact]
    public async Task GetQuota_ReturnsCurrentUsageAndConfiguredLimits()
    {
        var customerId = Guid.NewGuid();
        var models = Substitute.For<IModelRepository>();
        models.GetStorageUsageAsync(customerId, Arg.Any<CancellationToken>())
            .Returns((19, 900L * 1024 * 1024));
        var handler = new GetModelQuotaQueryHandler(models);

        var result = await handler.Handle(
            new GetModelQuotaQuery(customerId, 20, 1024L * 1024 * 1024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UsedModels.Should().Be(19);
        result.Value.MaxModels.Should().Be(20);
        result.Value.UsedBytes.Should().Be(900L * 1024 * 1024);
        result.Value.MaxBytes.Should().Be(1024L * 1024 * 1024);
    }
}
