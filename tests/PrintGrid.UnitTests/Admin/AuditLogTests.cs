using FluentAssertions;
using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.UnitTests.Admin;

public sealed class AuditLogTests
{
    [Fact]
    public void Record_FreezesActorEvidenceAndRequirementCodes()
    {
        var actorId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 10, 12, 5, 0, 0, DateTimeKind.Utc);

        var entry = AuditLog.Record(
            actorId,
            "admin@printgrid.vn",
            "Admin",
            "configuration",
            "user.role_changed",
            "User",
            Guid.NewGuid().ToString(),
            "{\"roles\":[\"Customer\"]}",
            "{\"roles\":[\"OpsManager\"]}",
            "approved staff transfer",
            "127.0.0.1",
            "trace-1",
            "[\"BR-CONFIG-002\",\"BR-CONFIG-004\"]",
            createdAt);

        entry.Id.Should().NotBeEmpty();
        entry.ActorId.Should().Be(actorId);
        entry.ActorDisplay.Should().Be("admin@printgrid.vn");
        entry.OldValuesJson.Should().Contain("Customer");
        entry.NewValuesJson.Should().Contain("OpsManager");
        entry.RequirementCodesJson.Should().Contain("BR-CONFIG-004");
        entry.CreatedAtUtc.Should().Be(createdAt);
    }
}
