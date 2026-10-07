using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PrintGrid.Api.Hubs;
using PrintGrid.SharedKernel.Events;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

/// <summary>
/// The API host boots Hangfire against PostgreSQL (Program.cs:84), so the test needs a real
/// database even though this particular flow never queries one. A throwaway container keeps
/// the test independent of whatever connection string is configured locally.
/// </summary>
public class SignalRTimelineTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // UseSetting (not only ConfigureAppConfiguration): Program.cs reads the connection
            // string from builder.Configuration while it registers Hangfire.
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                }));
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
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
