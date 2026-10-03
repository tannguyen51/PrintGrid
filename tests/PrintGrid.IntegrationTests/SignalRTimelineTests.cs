using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PrintGrid.Api.Hubs;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.IntegrationTests;

public class SignalRTimelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRTimelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JobStarted_ShouldPushToCorrectCustomerGroup()
    {
        // Arrange
        var mockHubContext = Substitute.For<IHubContext<OrderHub>>();
        var mockClients = Substitute.For<IHubClients>();
        var mockGroup = Substitute.For<IClientProxy>();
        
        mockHubContext.Clients.Returns(mockClients);
        mockClients.Group(Arg.Any<string>()).Returns(mockGroup);

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(mockHubContext);
            });
        });

        using var scope = factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        // Simulate what Customer module does after receiving ProductionStatusChanged
        await publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(orderId, customerId));

        // Assert
        mockClients.Received(1).Group($"customer:{customerId}");
        await mockGroup.Received(1).SendCoreAsync("TimelineUpdated", Arg.Is<object[]>(args => (Guid)args[0] == orderId), Arg.Any<CancellationToken>());
    }
}
