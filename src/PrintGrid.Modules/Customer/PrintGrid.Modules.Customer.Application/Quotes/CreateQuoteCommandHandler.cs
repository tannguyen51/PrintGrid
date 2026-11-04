using MediatR;
using Microsoft.Extensions.Configuration;
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
    private readonly ICommercialPolicyProvider _commercialPolicy;
    private readonly IQuoteSlicer? _quoteSlicer;

    public CreateQuoteCommandHandler(
        IModelRepository models,
        IQuoteRepository quotes,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IProductionCapacityProbe capacity,
        ICommercialPolicyProvider? commercialPolicy = null,
        IQuoteSlicer? quoteSlicer = null)
    {
        _models = models;
        _quotes = quotes;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _capacity = capacity;
        _commercialPolicy = commercialPolicy ?? new DefaultCommercialPolicyProvider();
        _quoteSlicer = quoteSlicer;
    }

    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        var policy = await _commercialPolicy.GetActiveAsync(cancellationToken);
        var set = policy.Pricing;
        if (!QuotePricing.IsSupported(set, command.MaterialCode))
            return Result.Failure<QuoteDto>(
                Error.Validation($"Không hỗ trợ vật liệu '{command.MaterialCode}'"));

        var model = await _models.GetByIdForCustomerAsync(command.CustomerId, command.ModelId, cancellationToken);
        if (model is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Model", command.ModelId));

        if (model.GeometryStatus != Domain.Enums.GeometryStatus.Ready)
            return Result.Failure<QuoteDto>(Error.Validation("Model chưa được phân tích hình học xong"));

        // Only an explicit "false" blocks quoting. NULL means the model was analysed before the
// printability flags existed (or the file predates them) — treating unknown as unprintable
// rejected every such model outright. The trial placement below is the real feasibility gate.
        if (model.IsPrintable == false && (command.SizeCode is null || model.IsWatertight != true || model.IsManifold != true))
            return Result.Failure<QuoteDto>(Error.Validation(
                model.GeometryMessage ?? "Model không thể in trên bất kỳ máy nào trong mạng lưới"));

        // Content moderation gate (blocking): a model enters quoting only after Ops approval.
        // Metadata-only models have no file to review and can never be approved.
        if (model.StorageKey is null)
            return Result.Failure<QuoteDto>(Error.Conflict(
                "Model chưa có file trên hệ thống — hãy tải file trước khi báo giá."));

        if (model.ReviewStatus != Domain.Enums.ModelReviewStatus.Approved)
            return Result.Failure<QuoteDto>(Error.Conflict(
                model.ReviewStatus == Domain.Enums.ModelReviewStatus.Rejected
                    ? $"Model đã bị từ chối nội dung: {model.ReviewReason ?? "không rõ lý do"}"
                    : "Model đang chờ duyệt nội dung — chưa thể báo giá."));

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
                _clock.UtcNow, TimeSpan.FromHours(validityHours), true, 0m);
            if (approved.IsFailure)
                return Result.Failure<QuoteDto>(approved.Error);
        }

        await _quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuoteMappings.ToCustomerDto(quote));
    }

    private async Task<Result<QuoteDto>> CreateSlicedQuote(CreateQuoteCommand command, Model model,
        CommercialPolicy policy, CancellationToken cancellationToken)
    {
        if (_quoteSlicer is null || _quoteSlicer.ConfiguredMachineIds.Count == 0)
            return Result.Failure<QuoteDto>(Error.Conflict("Chưa cấu hình CuraEngine và profile máy để báo giá tự động. Vui lòng liên hệ PrintGrid."));
        var colors = (command.ColorCodes ?? new[] { command.ColorCode })
            .Select(c => c.Trim().ToUpperInvariant()).ToList();
        if (colors.Count is < 1 or > 8 || colors.Distinct().Count() != colors.Count)
            return Result.Failure<QuoteDto>(Error.Validation("Chọn từ 1 đến 8 màu khác nhau."));
        if (command.Polishing && policy.Pricing.PolishingPerUnit <= 0)
            return Result.Failure<QuoteDto>(Error.Conflict("Chưa cấu hình đơn giá dịch vụ đánh bóng."));
        var size = command.SizeCode ?? "ORIGINAL";
        decimal scale;
        try { scale = SlicedQuotePricing.ScaleFor(size, model.BoundingWidthMm ?? 0, model.BoundingDepthMm ?? 0, model.BoundingHeightMm ?? 0); }
        catch (ArgumentException e) { return Result.Failure<QuoteDto>(Error.Validation(e.Message)); }
        var width = model.BoundingWidthMm!.Value;
        var depth = model.BoundingDepthMm!.Value;
        var height = model.BoundingHeightMm!.Value;
        QuotePriceBreakdown? best = null;
        TrialPlacementResult? placement = null;
        string? failure = null;
        foreach (var machineId in _quoteSlicer.ConfiguredMachineIds)
        {
            // Cheap dimensional/capacity check before starting an external engine process.
            var feasible = await _capacity.FindEarliestFeasibleDeliveryAsync(
                colors.Select((color, index) => new TrialPlacementLine(width * scale, depth * scale, height * scale,
                    command.MaterialCode, color, command.LayerHeightMm, command.ToleranceMm,
                    index == 0 ? 1 : 0, 0.1m, machineId)).ToList(), _clock.UtcNow, cancellationToken);
            if (feasible is null) continue;
            try
            {
                var density = policy.Pricing.DensityGramsPerCm3[command.MaterialCode];
                var slice = await _quoteSlicer.SliceAsync(new(model.StorageKey!, model.FileFormat, scale,
                    command.MaterialCode, colors, density, command.LayerHeightMm, command.InfillPercent,
                    command.Quantity, machineId), cancellationToken);
                if (!slice.Colors.Select(c => c.ColorCode).Order().SequenceEqual(colors.Order()))
                    throw new InvalidDataException("Kết quả slicer không khớp các màu đã chọn.");
                var price = SlicedQuotePricing.Calculate(policy.Pricing, command.MaterialCode, size,
                    width, depth, height, scale, command.Quantity, slice, command.Polishing);
                var trial = await _capacity.FindEarliestFeasibleDeliveryAsync(
                    slice.Colors.Select((color, index) => new TrialPlacementLine(price.WidthMm, price.DepthMm, price.HeightMm,
                        command.MaterialCode, color.ColorCode, command.LayerHeightMm, command.ToleranceMm,
                        index == 0 ? checked((int)decimal.Ceiling(slice.PrintMinutes)) : 0, color.Grams, machineId)).ToList(),
                    _clock.UtcNow, cancellationToken);
                if (trial is not null && (best is null || price.TotalAmount < best.TotalAmount ||
                    (price.TotalAmount == best.TotalAmount && trial.PromisedDeliveryDate < placement!.PromisedDeliveryDate)))
                { best = price; placement = trial; }
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or InvalidOperationException)
            { failure = e.Message; }
        }
        if (best is null || placement is null)
            return Result.Failure<QuoteDto>(Error.Conflict(failure ?? "Không có máy phù hợp với kích thước, vật liệu và màu đã chọn."));
        var quote = Quote.CreatePending(command.CustomerId, QuoteValidity);
        var config = PrintConfiguration.Create(command.MaterialCode, colors[0], command.LayerHeightMm,
            command.InfillPercent, command.ToleranceMm);
        var item = quote.AddItem(model.Id, command.Quantity, config, Money.Of(best.TotalAmount / command.Quantity),
            (int)decimal.Ceiling(best.PrintMinutes / command.Quantity),
            best.Colors.Sum(c => c.Grams) / command.Quantity,
            best.MaterialAmount / command.Quantity, best.MachineAmount / command.Quantity,
            best.WidthMm, best.DepthMm, best.HeightMm);
        item.FreezeCalculation(System.Text.Json.JsonSerializer.Serialize(best), best.TotalAmount, placement.MachineId);
        quote.MarkDraft(placement.PromisedDeliveryDate, policy.Pricing.Version, placement.Basis, placement.CustomerBasis);
        if (policy.AutoLaneThreshold > 0 && best.TotalAmount < policy.AutoLaneThreshold)
            quote.Approve(null, best.TotalAmount, placement.PromisedDeliveryDate, null, _clock.UtcNow,
                TimeSpan.FromHours(policy.QuoteValidityHours), true, 0);
        await _quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(QuoteMappings.ToCustomerDto(quote));
    }
}
