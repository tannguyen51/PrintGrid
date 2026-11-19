using System.Text.Json;
using System.Text.Json.Nodes;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.UnitTests.Scheduling;

public class JobQuotedMaterialTests
{
    private static Job Batch(bool legacy)
    {
        var quote = SlicedQuotePricing.Calculate(PricingParameterSet.Active, "PLA", 5,
            new QuoteSliceResult([new QuoteColorUsage("BLACK", 30), new QuoteColorUsage("WHITE", 20)], 100), false);
        var json = JsonNode.Parse(JsonSerializer.Serialize(quote))!.AsObject();
        if (legacy) json.Remove("Quantity");
        var spec = JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .2m, PrintTechnology.Fdm, 50);
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 100, new DateOnly(2026, 11, 25), 5);
        job.SetQuotedProduction(json.ToJsonString(), Guid.NewGuid());
        return job;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Batch_material_is_not_multiplied_by_quantity_again(bool legacy)
    {
        var demands = Batch(legacy).MaterialByColor();
        demands["BLACK"].Should().Be(30);
        demands["WHITE"].Should().Be(20);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Nested_splits_use_each_childs_share_of_the_original_batch(bool legacy)
    {
        var original = Batch(legacy);
        var children = original.Split([2, 3]).Value;
        children[0].MaterialByColor()["BLACK"].Should().Be(12);
        children[0].MaterialByColor()["WHITE"].Should().Be(8);
        children[1].MaterialByColor()["BLACK"].Should().Be(18);
        children[1].MaterialByColor()["WHITE"].Should().Be(12);
        var grandchildren = children[1].Split([1, 2]).Value;
        grandchildren[0].MaterialByColor()["BLACK"].Should().Be(6);
        grandchildren[0].MaterialByColor()["WHITE"].Should().Be(4);
        grandchildren[1].MaterialByColor()["BLACK"].Should().Be(12);
        grandchildren[1].MaterialByColor()["WHITE"].Should().Be(8);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Urgent_reprint_preserves_quantity_and_material_for_the_split_batch(bool legacy)
    {
        var original = Batch(legacy).Split([2, 3]).Value[0];
        var reprint = Job.CreateUrgentReprint(original, 1);
        reprint.Quantity.Should().Be(2);
        reprint.MaterialByColor()["BLACK"].Should().Be(12);
        reprint.MaterialByColor()["WHITE"].Should().Be(8);
    }

    [Fact]
    public void Unsliced_jobs_still_use_the_frozen_specification()
    {
        var spec = JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .2m, PrintTechnology.Fdm, 50);
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 60, new DateOnly(2026, 11, 25), 5);
        job.MaterialByColor()["BLACK"].Should().Be(50);
    }
}
