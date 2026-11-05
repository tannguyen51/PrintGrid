using System.Text.Json;
using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.UnitTests.Customer;

public class CustomerTimelineDtoTests
{
    [Fact]
    public void CustomerTimelineDto_Serialization_ShouldNotContainLabOrMachine()
    {
        // Arrange
        var dto = new CustomerTimelineDto(
            Guid.NewGuid(),
            "ORD-123",
            "Printing",
            new DateOnly(2026, 10, 1),
            false,
            [new OrderStageDto("Printing", DateTime.UtcNow, true)],
            [new CustomerOrderItemDto(Guid.NewGuid(), "Model", 2, "Printing")]);

        // Act
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert
        Assert.DoesNotContain("lab", json.ToLower());
        Assert.DoesNotContain("machine", json.ToLower());
    }
}
