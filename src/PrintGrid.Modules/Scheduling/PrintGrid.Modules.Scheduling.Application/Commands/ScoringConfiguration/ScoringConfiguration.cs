using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.ScoringConfiguration;

public sealed record ScoringConfigurationDto(
    Guid? Id, string Version, decimal DueDateSlack, decimal LabReliability,
    decimal LoadBalance, decimal Cost, decimal Transit, decimal Total,
    bool IsActive, string? Reason, Guid? ChangedBy, DateTime? CreatedAtUtc);

public sealed record GetScoringConfigurationsQuery : IRequest<Result<IReadOnlyList<ScoringConfigurationDto>>>;

public sealed record ActivateScoringConfigurationCommand(
    string Version, decimal DueDateSlack, decimal LabReliability,
    decimal LoadBalance, decimal Cost, decimal Transit,
    string Reason, Guid ChangedBy) : IRequest<Result<ScoringConfigurationDto>>;

public sealed class GetScoringConfigurationsQueryHandler(IScoringWeightConfigurationRepository repository)
    : IRequestHandler<GetScoringConfigurationsQuery, Result<IReadOnlyList<ScoringConfigurationDto>>>
{
    public async Task<Result<IReadOnlyList<ScoringConfigurationDto>>> Handle(GetScoringConfigurationsQuery request, CancellationToken ct)
    {
        var history = await repository.GetHistoryAsync(ct);
        IReadOnlyList<ScoringConfigurationDto> result = history.Count > 0
            ? history.Select(Map).ToList()
            : [Map(ScoringParameterSet.Active)];
        return Result.Success(result);
    }

    internal static ScoringConfigurationDto Map(ScoringWeightConfiguration x) => new(
        x.Id, x.Version, x.DueDateSlack, x.LabReliability, x.LoadBalance,
        x.Cost, x.Transit, x.ToWeights().Total, x.IsActive, x.Reason, x.ChangedBy, x.CreatedAtUtc);

    private static ScoringConfigurationDto Map(ScoringParameterSet x) => new(
        null, x.Version, x.Weights.DueDateSlack, x.Weights.LabReliability,
        x.Weights.LoadBalance, x.Weights.Cost, x.Weights.Transit,
        x.Weights.Total, true, "Built-in baseline", null, null);
}

public sealed class ActivateScoringConfigurationCommandHandler(
    IScoringWeightConfigurationRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateScoringConfigurationCommand, Result<ScoringConfigurationDto>>
{
    public async Task<Result<ScoringConfigurationDto>> Handle(ActivateScoringConfigurationCommand command, CancellationToken ct)
    {
        var version = command.Version?.Trim() ?? string.Empty;
        if (await repository.VersionExistsAsync(version, ct))
            return Result.Failure<ScoringConfigurationDto>(Error.Conflict($"Scoring configuration version '{version}' already exists"));

        var created = ScoringWeightConfiguration.Create(version, new ScoringWeights
        {
            DueDateSlack = command.DueDateSlack,
            LabReliability = command.LabReliability,
            LoadBalance = command.LoadBalance,
            Cost = command.Cost,
            Transit = command.Transit
        }, command.Reason, command.ChangedBy, clock.UtcNow);

        if (created.IsFailure)
            return Result.Failure<ScoringConfigurationDto>(created.Error);

        await repository.AddAndActivateAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(GetScoringConfigurationsQueryHandler.Map(created.Value));
    }
}
