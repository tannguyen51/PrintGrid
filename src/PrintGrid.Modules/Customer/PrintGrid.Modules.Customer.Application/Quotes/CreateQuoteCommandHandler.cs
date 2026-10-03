using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    public static readonly TimeSpan QuoteValidity = TimeSpan.FromHours(48); // BR-QUOTE-003 (cfg in FR-ADMIN-002 later)

    private readonly IModelRepository _models;
    private readonly IQuoteRepository _quotes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly IProductionCapacityProbe _capacity;

    public CreateQuoteCommandHandler(
        IModelRepository models,
        IQuoteRepository quotes,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IProductionCapacityProbe capacity)
    {
        _models = models;
        _quotes = quotes;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _capacity = capacity;
    }

    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        var set = PricingParameterSet.Active;
        if (!QuotePricing.IsSupported(set, command.MaterialCode))
            return Result.Failure<QuoteDto>(
                Error.Validation($"Không hỗ trợ vật liệu '{command.MaterialCode}'"));

        var model = await _models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Model", command.ModelId));

        if (model.GeometryStatus != Domain.Enums.GeometryStatus.Ready)
            return Result.Failure<QuoteDto>(Error.Validation("Model chưa được phân tích hình học xong"));

        if (model.IsPrintable != true)
            return Result.Failure<QuoteDto>(Error.Validation(
                model.GeometryMessage ?? "Model không thể in trên bất kỳ máy nào trong mạng lưới"));

        // Volume & reference print time come from the async geometry analysis; fall back to
        // a deterministic envelope if analysis hasn't run yet.
        var volumeCm3 = model.VolumeCm3!.Value;
        var baseMinutes = model.EstimatedPrintMinutes!.Value;

        var config = PrintConfiguration.Create(
            command.MaterialCode,
            command.ColorCode,
            command.LayerHeightMm,
            command.InfillPercent,
            command.ToleranceMm);

        var line = QuotePricing.PriceLine(
            set, volumeCm3, baseMinutes, command.InfillPercent, command.MaterialCode, command.LayerHeightMm);

        var quote = Quote.CreatePending(command.CustomerId, QuoteValidity);
        quote.AddItem(
            model.Id,
            command.Quantity,
            config,
            Money.Of(line.UnitPrice),
            line.EffectiveMinutes,
            line.Grams,
            line.MaterialCostAmount,
            line.MachineTimeCostAmount,
            model.BoundingWidthMm,
            model.BoundingDepthMm,
            model.BoundingHeightMm);

        // FR-SCHED-005: the promised date comes from a TRIAL placement against the real
        // machine timelines (transit + hub buffers included) — never from a padded table.
        var trial = await _capacity.FindEarliestFeasibleDeliveryAsync(
            new[]
            {
                new TrialPlacementLine(
                    model.BoundingWidthMm ?? 100m,
                    model.BoundingDepthMm ?? 100m,
                    model.BoundingHeightMm ?? 100m,
                    command.MaterialCode,
                    command.ColorCode,
                    command.LayerHeightMm,
                    command.ToleranceMm,
                    line.EffectiveMinutes * command.Quantity,
                    line.Grams * command.Quantity)
            },
            _clock.UtcNow,
            cancellationToken);

        if (trial is null)
        {
            // BR-QUOTE-006: never sell the impossible — record the failed quote for the audit trail.
            quote.MarkFailed("Không có máy khả thi trong mạng lưới cho cấu hình này");
            await _quotes.AddAsync(quote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<QuoteDto>(Error.Conflict(
                "Mạng lưới hiện không nhận được cấu hình này — hãy đổi vật liệu/kích thước hoặc thử lại sau"));
        }

        var markResult = quote.MarkReady(trial.PromisedDeliveryDate, set.Version, trial.Basis);
        if (markResult.IsFailure)
            return Result.Failure<QuoteDto>(markResult.Error);

        await _quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuoteMappings.ToDto(quote));
    }
}
