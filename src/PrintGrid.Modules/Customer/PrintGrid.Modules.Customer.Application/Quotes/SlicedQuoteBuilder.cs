using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

internal sealed class SlicedQuoteBuilder(IQuoteRepository quotes, IUnitOfWork unitOfWork, IDateTimeProvider clock,
    IProductionCapacityProbe capacity, IMachineFilterService machines, IQuoteSlicer? slicer, ILogger? logger)
{
    public async Task<Result<QuoteDto>> BuildAsync(CreateQuoteCommand command, Model model, CommercialPolicy policy, CancellationToken ct)
    {
        if (slicer is null) return Fail("Dịch vụ báo giá tự động chưa sẵn sàng.");
        MachineSelectionCriteria criteria;
        QuoteModelEnvelope? envelope;
        try
        {
            envelope = await slicer.GetEnvelopeAsync(model.StorageKey!, model.FileFormat, ct);
            criteria = MachineSelection.Criteria(command, model, envelope);
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException) { return Fail(e.Message); }
        var configured = await slicer.GetConfiguredMachineIdsAsync(ct);
        var candidates = await machines.FilterCandidatesAsync(configured, criteria, ct);
        if (candidates.Count == 0) return Fail("Không có máy đáp ứng cấu hình và ràng buộc kỹ thuật đã chọn.");

        var rates = policy.Pricing;
        MachineModelOption? pinnedModel = null;
        IReadOnlyList<Guid> pinnedCandidates = [];
        if (command.BindingType == 1)
        {
            var options = await machines.GetOptionsAsync(criteria, rates.MinPinMachines, rates.MinPinLabs, rates.PinSurchargeCoefficient, ct);
            pinnedModel = options.Models.SingleOrDefault(m => m.Id == command.PinnedModelId);
            if (pinnedModel is null) return Fail("Model máy không còn đủ điều kiện hoặc không đáp ứng cấu hình. Vui lòng chọn lại model.");
            (pinnedCandidates, _) = await machines.FilterByPinModelAsync(pinnedModel.Id, candidates, ct);
            if (pinnedCandidates.Count == 0) return Fail("Không có máy khả thi thuộc model đã chỉ định. Hệ thống sẽ không tự đổi model.");
        }

        var requirements = new ProductionRequirements(command.MaxColors is { } count ? Math.Max(count, criteria.Colors.Count) : criteria.Colors.Count,
            command.EnclosedChamber, command.NozzleDiameterMm, command.MaxToleranceMm, command.MinLayerHeightMm);
        var referenceKey = $"{command.MaterialCode}:{command.LayerHeightMm.ToString("0.###", CultureInfo.InvariantCulture)}";
        Guid? referenceProfile = rates.ReferenceProfileIds is { } refs && refs.TryGetValue(referenceKey, out var referenceId)
            ? referenceId : null;
        var referenceMachine = await machines.GetReferenceMachineAsync(candidates, command.MaterialCode, referenceProfile, ct);
        if (referenceMachine is null) return Fail("Chưa có profile tham chiếu phù hợp. Vui lòng liên hệ PrintGrid.");

        string? calculationError = null;
        var unpinned = await CalculateAndPlace(referenceMachine, candidates, requirements,
            await machines.GetCalibrationFactorAsync(null, ct), 1m);
        if (unpinned is null) return Fail(calculationError ?? "Không thể báo giá và xếp lịch với profile tham chiếu. Vui lòng thử lại hoặc liên hệ PrintGrid.");

        var chosen = unpinned;
        if (pinnedModel is not null)
        {
            var pinnedReference = await machines.GetReferenceMachineAsync(pinnedCandidates, command.MaterialCode, null, ct);
            if (pinnedReference is null) return Fail("Model đã chọn chưa có profile được duyệt phù hợp.");
            chosen = await CalculateAndPlace(pinnedReference, pinnedCandidates,
                requirements with { PinnedModelId = pinnedModel.Id, PinnedModelName = pinnedModel.Name },
                await machines.GetCalibrationFactorAsync(pinnedModel.Id, ct), rates.PinSurchargeCoefficient);
            if (chosen is null) return Fail("Không còn lịch khả thi cho model đã chọn. Hệ thống sẽ không tự đổi model; hãy chọn ngày khác hoặc bỏ chỉ định để báo giá lại.");
        }

        var price = chosen.Price;
        var quote = Quote.CreatePending(command.CustomerId, TimeSpan.FromHours(policy.QuoteValidityHours));
        var config = PrintConfiguration.Create(command.MaterialCode, criteria.Colors[0], command.LayerHeightMm,
            command.InfillPercent, command.ToleranceMm);
        var item = quote.AddItem(model.Id, command.Quantity, config, Money.Of(price.TotalAmount / command.Quantity),
            (int)decimal.Ceiling(price.PrintMinutes / command.Quantity), price.Colors.Sum(c => c.Grams) / command.Quantity,
            price.MaterialAmount / command.Quantity, price.MachineAmount / command.Quantity,
            price.WidthMm, price.DepthMm, price.HeightMm);
        item.FreezeCalculation(JsonSerializer.Serialize(price), price.TotalAmount, chosen.MachineId);
        if (price.ProfileId.HasValue)
            item.FreezePinchData(price.ProfileId.Value, price.ProfileVersion ?? "", price.PinSurchargeCoefficient,
                (long)unpinned.Price.TotalAmount, unpinned.Placement.PromisedDeliveryDate);
        if (pinnedModel is not null)
            quote.SetPinnedModelContext(rates.PinSurchargeCoefficient, pinnedModel.Id,
                (long)unpinned.Price.TotalAmount, unpinned.Placement.PromisedDeliveryDate);
        var draft = quote.MarkDraft(chosen.Placement.PromisedDeliveryDate, rates.Version,
            chosen.Placement.Basis, chosen.Placement.CustomerBasis);
        if (draft.IsFailure) return Result.Failure<QuoteDto>(draft.Error);
        if (policy.AutoLaneThreshold > 0 && price.TotalAmount < policy.AutoLaneThreshold
            && !OrderRoutingPolicy.RequiresManualDispatch(command.Quantity))
        {
            var approval = quote.Approve(null, price.TotalAmount, chosen.Placement.PromisedDeliveryDate, null,
                clock.UtcNow, TimeSpan.FromHours(policy.QuoteValidityHours), true, 0);
            if (approval.IsFailure) return Result.Failure<QuoteDto>(approval.Error);
        }
        await quotes.AddAsync(quote, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(QuoteMappings.ToCustomerDto(quote));

        async Task<PlacedQuote?> CalculateAndPlace(QuoteProfileSource source, IReadOnlyList<Guid> targetMachines,
            ProductionRequirements requirementsForOption, decimal calibration, decimal coefficient)
        {
            try
            {
                var scale = criteria.WidthMm / (envelope?.WidthMm ?? model.BoundingWidthMm!.Value);
                var slice = await slicer.SliceAsync(new QuoteSliceRequest(model.StorageKey!, model.FileFormat, scale,
                    command.MaterialCode, criteria.Colors, rates.DensityGramsPerCm3[command.MaterialCode],
                    command.LayerHeightMm, command.InfillPercent, command.Quantity, source.MachineId, source.ProfileId,
                    command.ColorAssignments), ct);
                var productionRequirements = requirementsForOption with { MinimumBuildWidthMm = slice.MinimumBuildWidthMm };
                var calculation = SlicedQuotePricing.Calculate(rates, command.MaterialCode, command.Quantity, slice,
                    command.Polishing, criteria.WidthMm, criteria.DepthMm, criteria.HeightMm, calibration, coefficient)
                    with { Requirements = productionRequirements };
                PlacedQuote? earliest = null;
                foreach (var machineId in targetMachines)
                {
                    var lines = calculation.Colors.Select((c, i) => new TrialPlacementLine(criteria.WidthMm,
                        criteria.DepthMm, criteria.HeightMm, command.MaterialCode, c.ColorCode, command.LayerHeightMm,
                        command.ToleranceMm, i == 0 ? (int)decimal.Ceiling(calculation.PrintMinutes) : 0,
                        c.Grams, machineId, productionRequirements, true)).ToList();
                    var placement = await capacity.FindEarliestFeasibleDeliveryAsync(lines, clock.UtcNow, ct);
                    if (placement is not null && (earliest is null || placement.PromisedDeliveryDate < earliest.Placement.PromisedDeliveryDate))
                        earliest = new(calculation, machineId, placement);
                }
                return earliest;
            }
            catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                if (e is InvalidDataException) calculationError = e.Message;
                logger?.LogWarning(e, "Quote calculation failed for product {ProductId}, reference machine {MachineId}", model.Id, source.MachineId);
                return null;
            }
        }
    }

    private static Result<QuoteDto> Fail(string message) => Result.Failure<QuoteDto>(Error.Validation(message));
    private sealed record PlacedQuote(QuotePriceBreakdown Price, Guid MachineId, TrialPlacementResult Placement);
}
