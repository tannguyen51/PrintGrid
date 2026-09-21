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
    private readonly IModelRepository _models;
    private readonly IQuoteRepository _quotes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CreateQuoteCommandHandler(
        IModelRepository models,
        IQuoteRepository quotes,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _models = models;
        _quotes = quotes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        if (!QuotePricing.IsSupported(command.MaterialCode))
            return Result.Failure<QuoteDto>(
                Error.Validation($"Không hỗ trợ vật liệu '{command.MaterialCode}'"));

        var model = await _models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Model", command.ModelId));

        // Volume & reference print time come from the async geometry analysis; fall back to
        // a deterministic envelope if analysis hasn't run yet.
        var volumeCm3 = model.VolumeCm3 ?? 1m;
        var baseMinutes = model.EstimatedPrintMinutes ?? 60;

        var config = PrintConfiguration.Create(
            command.MaterialCode,
            command.ColorCode,
            command.LayerHeightMm,
            command.InfillPercent,
            command.ToleranceMm);

        var effectiveMinutes = QuotePricing.EffectiveMinutes(baseMinutes, command.LayerHeightMm);
        var grams = QuotePricing.MaterialGrams(volumeCm3, command.InfillPercent, command.MaterialCode);
        var unitPrice = decimal.Round(
            QuotePricing.MaterialCost(command.MaterialCode, grams) + QuotePricing.MachineTimeCost(effectiveMinutes), 0);

        var quote = Quote.CreatePending(command.CustomerId, TimeSpan.FromHours(48));
        quote.AddItem(
            model.Id,
            command.Quantity,
            config,
            Money.Of(unitPrice),
            effectiveMinutes,
            grams);

        // Speculative delivery: same-day + buffer; demo placeholder for the scheduler.
        var promised = _clock.Today.AddDays(3);
        var markResult = quote.MarkReady(promised);
        if (markResult.IsFailure)
            return Result.Failure<QuoteDto>(markResult.Error);

        await _quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuoteMappings.ToDto(quote));
    }
}