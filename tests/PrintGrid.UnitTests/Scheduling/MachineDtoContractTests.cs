using System.Text.Json;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.UnitTests.Scheduling;

public class MachineDtoContractTests
{
    [Fact]
    public void Machine_response_includes_editable_identity_and_profile_review_context()
    {
        var machine = Machine.Register(Guid.NewGuid(), "Printer 1", "Test Model", PrintTechnology.Fdm,
            BuildVolume.Create(200, 200, 200), 0.1m, 0.2m, ["PLA"]);
        var profileId = Guid.NewGuid();
        machine.SetMatchedProfiles([profileId], true);
        machine.ReviewProfile(false, Guid.NewGuid(), "Check the nozzle size");

        using var response = JsonDocument.Parse(JsonSerializer.Serialize(LabMappings.ToDto(machine),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var root = response.RootElement;
        root.GetProperty("name").GetString().Should().Be("Printer 1");
        root.GetProperty("model").GetString().Should().Be("Test Model");
        root.GetProperty("curaProfileIds")[0].GetGuid().Should().Be(profileId);
        root.GetProperty("profileReviewReason").GetString().Should().Be("Check the nozzle size");
    }
}
