using FluentValidation;
using NSubstitute;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.UnitTests.Admin;

public sealed class CommercialConfigurationTests
{
    [Fact]
    public void Configuration_FollowsDraftActiveArchivedLifecycle()
    {
        var actorId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 10, 13, 1, 0, 0, DateTimeKind.Utc);
        var activatedAt = createdAt.AddHours(1);
        var configuration = CommercialConfigurationVersion.Create(
            2, "  Phiên bản 2  ", "{\"pricePerPrintHour\":95000}", "  Điều chỉnh giá  ", actorId, createdAt);

        configuration.VersionNumber.Should().Be(2);
        configuration.Name.Should().Be("Phiên bản 2");
        configuration.ChangeSummary.Should().Be("Điều chỉnh giá");
        configuration.Status.Should().Be(ConfigurationStatuses.Draft);
        configuration.ActivatedAtUtc.Should().BeNull();

        configuration.Activate(activatedAt);

        configuration.Status.Should().Be(ConfigurationStatuses.Active);
        configuration.ActivatedAtUtc.Should().Be(activatedAt);
        var secondActivation = () => configuration.Activate(activatedAt.AddMinutes(1));
        secondActivation.Should().Throw<InvalidOperationException>();

        configuration.Archive();
        configuration.Status.Should().Be(ConfigurationStatuses.Archived);
    }

    [Fact]
    public void ParametersValidator_AcceptsACompleteValidConfiguration()
    {
        var result = new CommercialParametersValidator().Validate(ValidParameters());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ParametersValidator_RejectsIncompleteWeightsAndOutOfRangeValues()
    {
        var invalid = ValidParameters() with
        {
            PlatformMarginPercent = 101,
            QuoteValidityHours = 0,
            AssignmentWeights = new Dictionary<string, decimal>
            {
                ["time"] = 0.5m,
                ["cost"] = 0.5m
            }
        };

        var result = new CommercialParametersValidator().Validate(invalid);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(x => x.PropertyName).Should().Contain([
            nameof(CommercialParametersDto.PlatformMarginPercent),
            nameof(CommercialParametersDto.QuoteValidityHours),
            nameof(CommercialParametersDto.AssignmentWeights)
        ]);
    }

    [Fact]
    public async Task CreateHandler_RejectsMaterialTablesThatDoNotMatchTheActiveCatalog()
    {
        var repository = Substitute.For<IAdminConfigurationRepository>();
        repository.GetCatalogAsync(SharedCatalogTypes.Material, false, Arg.Any<CancellationToken>())
            .Returns([Material("PLA", 1, 120_000m), Material("ABS", 1, 135_000m)]);
        var parameters = ValidParameters() with
        {
            MaterialRates = new Dictionary<string, decimal> { ["PLA"] = 120_000m },
            MaterialDensities = new Dictionary<string, decimal> { ["PLA"] = 1.24m }
        };
        var handler = new CommercialConfigurationHandlers(repository);

        var action = () => handler.Handle(
            new CreateCommercialConfigurationCommand("v2", parameters, "Thiếu ABS", Guid.NewGuid()),
            CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("*bao phủ đúng các vật liệu*");
        await repository.DidNotReceive().AddConfigurationAsync(
            Arg.Any<CommercialConfigurationVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateHandler_CreatesTheNextDraftWhenMaterialRatesMatchTheCatalog()
    {
        var repository = Substitute.For<IAdminConfigurationRepository>();
        repository.GetCatalogAsync(SharedCatalogTypes.Material, false, Arg.Any<CancellationToken>())
            .Returns([Material("PLA", 1, 120_000m)]);
        repository.GetNextConfigurationVersionAsync(Arg.Any<CancellationToken>()).Returns(4);
        var parameters = ValidParameters();
        var handler = new CommercialConfigurationHandlers(repository);

        var result = await handler.Handle(
            new CreateCommercialConfigurationCommand("v4", parameters, "Giá mới", Guid.NewGuid()),
            CancellationToken.None);

        result.VersionNumber.Should().Be(4);
        result.Status.Should().Be(ConfigurationStatuses.Draft);
        result.Parameters.MaterialRates["PLA"].Should().Be(120_000m);
        await repository.Received(1).AddConfigurationAsync(
            Arg.Is<CommercialConfigurationVersion>(x => x.VersionNumber == 4),
            Arg.Any<CancellationToken>());
    }

    private static CommercialParametersDto ValidParameters() => new(
        new Dictionary<string, decimal> { ["PLA"] = 120_000m },
        new Dictionary<string, decimal> { ["PLA"] = 1.24m },
        94_500m,
        25_000m,
        12m,
        20m,
        500_000m,
        48,
        new Dictionary<string, decimal>
        {
            ["time"] = 0.25m,
            ["load"] = 0.20m,
            ["quality"] = 0.25m,
            ["cost"] = 0.20m,
            ["logistics"] = 0.10m
        },
        100,
        30,
        24);

    private static SharedCatalogItem Material(string code, int revision, decimal unitRate) =>
        SharedCatalogItem.Create(
            SharedCatalogTypes.Material, code, revision, code, null, unitRate, false, "{}", false, Guid.NewGuid());
}
