using MediatR;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public class CreateQuoteCommandHandler(
    IModelRepository models,
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IProductionCapacityProbe capacity,
    IMachineFilterService machineFilter,
    ICommercialPolicyProvider? commercialPolicy = null,
    IQuoteSlicer? quoteSlicer = null)
    : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    private static readonly TimeSpan _quoteValidity = TimeSpan.FromHours(48); // BR-QUOTE-003 (cfg in FR-ADMIN-002 later)

    private readonly ICommercialPolicyProvider _commercialPolicy = commercialPolicy ?? new DefaultCommercialPolicyProvider();

    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        var policy = await _commercialPolicy.GetActiveAsync(cancellationToken);
        var set = policy.Pricing;
        if (!QuotePricing.IsSupported(set, command.MaterialCode))
        {
            return Result.Failure<QuoteDto>(
                Error.Validation($"Không hỗ trợ vật liệu '{command.MaterialCode}'"));
        }

        var model = await models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Model", command.ModelId));

        if (model.GeometryStatus != Domain.Enums.GeometryStatus.Ready)
            return Result.Failure<QuoteDto>(Error.Validation("Model chưa được phân tích hình học xong"));

        // Only an explicit "false" blocks quoting. NULL means the model was analysed before the
// printability flags existed (or the file predates them) — treating unknown as unprintable
// rejected every such model outright. The trial placement below is the real feasibility gate.
        if (model.IsPrintable == false && (command.SizeCode is null || model.IsWatertight != true || model.IsManifold != true))
        {
            return Result.Failure<QuoteDto>(Error.Validation(
                model.GeometryMessage ?? "Model không thể in trên bất kỳ máy nào trong mạng lưới"));
        }

        // Content moderation gate (blocking): a model enters quoting only after Ops approval.
        // Metadata-only models have no file to review and can never be approved.
        if (model.StorageKey is null)
        {
            return Result.Failure<QuoteDto>(Error.Conflict(
                "Model chưa có file trên hệ thống — hãy tải file trước khi báo giá."));
        }

        if (model.ReviewStatus != Domain.Enums.ModelReviewStatus.Approved)
        {
            return Result.Failure<QuoteDto>(Error.Conflict(
                model.ReviewStatus == Domain.Enums.ModelReviewStatus.Rejected
                    ? $"Model đã bị từ chối nội dung: {model.ReviewReason ?? "không rõ lý do"}"
                    : "Model đang chờ duyệt nội dung — chưa thể báo giá."));
        }

        if (command.SizeCode is not null || command.ColorCodes is not null || command.Polishing)
            return await CreateSlicedQuote(command, model, policy, cancellationToken);

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

        var quote = Quote.CreatePending(command.CustomerId, _quoteValidity);
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
        var trial = await capacity.FindEarliestFeasibleDeliveryAsync(
            [
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
            ],
            clock.UtcNow,
            cancellationToken);

        if (trial is null)
        {
            // BR-QUOTE-006: never sell the impossible — record the failed quote for the audit trail.
            quote.MarkFailed("Không có máy khả thi trong mạng lưới cho cấu hình này");
            await quotes.AddAsync(quote, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<QuoteDto>(Error.Conflict(
                "Mạng lưới hiện không nhận được cấu hình này — hãy đổi vật liệu/kích thước hoặc thử lại sau"));
        }

        var markResult = quote.MarkDraft(trial.PromisedDeliveryDate, set.Version, trial.Basis, trial.CustomerBasis);
        if (markResult.IsFailure)
            return Result.Failure<QuoteDto>(markResult.Error);

        // BR-QUOTE-008: inexpensive, ordinary drafts may bypass the staff queue. The
        // threshold is hot configuration; zero disables the auto-lane.
        var autoLaneThreshold = policy.AutoLaneThreshold;
        if (autoLaneThreshold > 0 && quote.TotalPrice.Amount < autoLaneThreshold)
        {
            var validityHours = policy.QuoteValidityHours;
            var approved = quote.Approve(
                null, quote.TotalPrice.Amount, quote.PromisedDeliveryDate, null,
                clock.UtcNow, TimeSpan.FromHours(validityHours), true, 0m);
            if (approved.IsFailure)
                return Result.Failure<QuoteDto>(approved.Error);
        }

        await quotes.AddAsync(quote, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuoteMappings.ToCustomerDto(quote));
    }

    private async Task<Result<QuoteDto>> CreateSlicedQuote(CreateQuoteCommand command, Model model,
        CommercialPolicy policy, CancellationToken cancellationToken)
    {
        var machineIds = quoteSlicer is null ? [] : await quoteSlicer.GetConfiguredMachineIdsAsync(cancellationToken);
        if (quoteSlicer is null || machineIds.Count == 0)
            return Result.Failure<QuoteDto>(Error.Conflict("Chưa cấu hình CuraEngine và profile máy để báo giá tự động. Vui lòng liên hệ PrintGrid."));

        // BR-03: Filter by tech constraints (hard filter — no surcharge)
        var techConstraints = TechConstraints.From(command.MaxColors, command.EnclosedChamber,
            command.NozzleDiameterMm, command.MaxToleranceMm, command.MinLayerHeightMm);
        if (techConstraints.HasValue)
        {
            machineIds = await machineFilter.FilterByTechConstraintsAsync(machineIds, techConstraints.Value, cancellationToken);
            if (machineIds.Count == 0)
                return Result.Failure<QuoteDto>(Error.Validation("Không có máy nào đáp ứng các ràng buộc kỹ thuật đã chọn"));
        }

        // Section 4: Pinned model handling (BR-04, BR-05, BR-06)
        decimal surchargeCoefficient = 1.0m;
        IReadOnlyList<Guid> allCandidateIds = machineIds;
        IReadOnlyList<Guid> pinnedCandidateIds = machineIds;

        if (command.BindingType == 1 && command.PinnedModelId.HasValue)
        {
            var result = await machineFilter.FilterByPinModelAsync(command.PinnedModelId.Value, machineIds, cancellationToken);
            pinnedCandidateIds = result.pinnedIds;
            allCandidateIds = result.unpinnedFallback;
            surchargeCoefficient = 1.15m; // Default h_chỉ_định per design doc section 4.2
        }

        // Compute both pricing options for comparison display (BR-06)
        QuotePriceBreakdown? bestPinned = null;
        QuotePriceBreakdown? bestUnpinned = null;
        Guid? pinnedMachineId = null;
        Guid? unpinnedMachineId = null;
        TrialPlacementResult? pinnedPlacement = null;
        TrialPlacementResult? unpinnedPlacement = null;

        (bestUnpinned, unpinnedMachineId, unpinnedPlacement) = await SliceOnCandidates(policy, allCandidateIds, cancellationToken, command, model);

        if (surchargeCoefficient > 1.0m && !pinnedCandidateIds.SequenceEqual(allCandidateIds))
        {
            (bestPinned, pinnedMachineId, pinnedPlacement) = await SliceOnCandidates(policy, pinnedCandidateIds, cancellationToken, command, model);
        }

        // Determine which option to use as final quote
        QuotePriceBreakdown? best = bestPinned ?? bestUnpinned;
        Guid? placementMachineId = bestPinned is not null ? pinnedMachineId : unpinnedMachineId;
        TrialPlacementResult? placement = bestPinned is not null ? pinnedPlacement : unpinnedPlacement;

        long? alternativePriceVnd = null;
        DateOnly? alternativeDeliveryDate = null;
        if (bestPinned is not null && bestUnpinned is not null)
        {
            var pinnedAfterSurcharge = bestPinned.TotalAmount * surchargeCoefficient;
            if (pinnedAfterSurcharge <= bestUnpinned.TotalAmount * 1.1m)
            { best = bestPinned; placementMachineId = pinnedMachineId; placement = pinnedPlacement; }
            else
            { best = bestUnpinned; placementMachineId = unpinnedMachineId; placement = unpinnedPlacement; }
            alternativePriceVnd = (long)bestUnpinned.TotalAmount;
            alternativeDeliveryDate = unpinnedPlacement?.PromisedDeliveryDate;
        }
        else if (bestUnpinned is not null)
        {
            alternativePriceVnd = (long)bestUnpinned.TotalAmount;
            alternativeDeliveryDate = unpinnedPlacement?.PromisedDeliveryDate;
        }

        if (best is null || placement is null)
            return Result.Failure<QuoteDto>(Error.Validation(
                "Không tìm thấy máy khả thi với các điều kiện đã chọn. Hãy thử thay đổi vật liệu hoặc màu sắc."));

        var quote = Quote.CreatePending(command.CustomerId, _quoteValidity);
        var config = PrintConfiguration.Create(command.MaterialCode,
            command.ColorCodes?.FirstOrDefault() ?? command.ColorCode, command.LayerHeightMm,
            command.InfillPercent, command.ToleranceMm);
        var item = quote.AddItem(model.Id, command.Quantity, config, Money.Of(best.TotalAmount / command.Quantity),
            (int)decimal.Ceiling(best.PrintMinutes / command.Quantity),
            best.Colors.Sum(c => c.Grams) / command.Quantity,
            best.MaterialAmount / command.Quantity, best.MachineAmount / command.Quantity,
            best.WidthMm, best.DepthMm, best.HeightMm);

        if (surchargeCoefficient > 1.0m && bestPinned is not null && alternativePriceVnd.HasValue)
        {
            item.FreezeCalculation(System.Text.Json.JsonSerializer.Serialize(best), best.TotalAmount, placementMachineId);
            quote.SetPinnedModelContext(surchargeCoefficient, command.PinnedModelId, alternativePriceVnd.Value, alternativeDeliveryDate);
        }
        else
        {
            item.FreezeCalculation(System.Text.Json.JsonSerializer.Serialize(best), best.TotalAmount, placementMachineId);
        }

        quote.MarkDraft(placement.PromisedDeliveryDate, policy.Pricing.Version, placement.Basis, placement.CustomerBasis);
        if (policy.AutoLaneThreshold > 0 && best.TotalAmount < policy.AutoLaneThreshold)
        {
            quote.Approve(null, best.TotalAmount, placement.PromisedDeliveryDate, null, clock.UtcNow,
                TimeSpan.FromHours(policy.QuoteValidityHours), true, 0);
        }

        await quotes.AddAsync(quote, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(QuoteMappings.ToCustomerDto(quote));
    }

    private async Task<(QuotePriceBreakdown? Best, Guid? MachineId, TrialPlacementResult? Placement)> SliceOnCandidates(CommercialPolicy policy, IReadOnlyList<Guid> candidateIds,
        CancellationToken ct, CreateQuoteCommand command, Domain.Entities.Model model)
    {
        QuotePriceBreakdown? best = null;
        Guid? bestMachineId = null;
        TrialPlacementResult? bestPlacement = null;
        foreach (var machineId in candidateIds)
        {
            try
            {
                var width = model.BoundingWidthMm!.Value;
                var depth = model.BoundingDepthMm!.Value;
                var height = model.BoundingHeightMm!.Value;
                decimal scale = SlicedQuotePricing.ScaleFor(command.SizeCode ?? "ORIGINAL", width, depth, height);
                var density = policy.Pricing.DensityGramsPerCm3[command.MaterialCode];
                var slice = await quoteSlicer!.SliceAsync(new QuoteSliceRequest(model.StorageKey!, model.FileFormat, scale,
                    command.MaterialCode, command.ColorCodes ?? [command.ColorCode], density,
                    command.LayerHeightMm, command.InfillPercent, command.Quantity, machineId), ct);
                var price = SlicedQuotePricing.Calculate(policy.Pricing, command.MaterialCode, command.Quantity, slice,
                    command.Polishing, width * scale, depth * scale, height * scale);
                var trial = await capacity.FindEarliestFeasibleDeliveryAsync(
                    slice.Colors.Select((c, i) => new TrialPlacementLine(price.WidthMm, price.DepthMm, price.HeightMm,
                        command.MaterialCode, c.ColorCode, command.LayerHeightMm, command.ToleranceMm,
                        i == 0 ? (int)decimal.Ceiling(slice.PrintMinutes) : 0, c.Grams, machineId)).ToList(),
                    clock.UtcNow, ct);
                if (trial is not null && (best is null || price.TotalAmount < best.TotalAmount))
                { best = price; bestMachineId = machineId; bestPlacement = trial; }
            }
            catch { /* Skip machines that fail */ }
        }
        return (best, bestMachineId, bestPlacement);
    }
}
