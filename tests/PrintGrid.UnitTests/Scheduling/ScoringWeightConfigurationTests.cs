using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Commands.ScoringConfiguration;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class ScoringWeightConfigurationTests
{
    [Fact]
    public void Configuration_rejects_a_total_other_than_one()
    {
        var result = ScoringWeightConfiguration.Create("bad", new ScoringWeights
        {
            DueDateSlack = .4m, LabReliability = .3m, LoadBalance = .2m, Cost = .1m, Transit = .1m
        }, "Regression guard for invalid total", Guid.NewGuid(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("exactly 1.0");
    }

    [Fact]
    public async Task Valid_configuration_is_activated_and_committed()
    {
        var repository = Substitute.For<IScoringWeightConfigurationRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clock.UtcNow.Returns(new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc));
        var handler = new ActivateScoringConfigurationCommandHandler(repository, clock, unitOfWork);

        var result = await handler.Handle(new ActivateScoringConfigurationCommand(
            "2026.11", .2m, .2m, .4m, .1m, .1m, "Prioritize load balancing", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1m);
        await repository.Received(1).AddAndActivateAsync(
            Arg.Is<ScoringWeightConfiguration>(x => x.Version == "2026.11" && x.IsActive),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
