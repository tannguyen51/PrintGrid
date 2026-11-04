using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public sealed record ModelReviewDto(
    Guid ModelId,
    Guid CustomerId,
    string CustomerEmail,
    string Name,
    string FileName,
    string FileFormat,
    long SizeBytes,
    string? Sha256,
    string GeometryStatus,
    bool? IsPrintable,
    DateTime CreatedAt,
    string ReviewStatus,
    string? ReviewReason,
    DateTime? ReviewedAtUtc);

public record GetModelReviewsQuery(string? Status = "Pending", string? Search = null)
    : IRequest<Result<IReadOnlyList<ModelReviewDto>>>;

public record GetModelReviewQuery(Guid ModelId) : IRequest<Result<ModelReviewDto>>;

public record ApproveModelCommand(Guid ModelId, Guid ReviewerId, string? Note = null) : IRequest<Result>;

public record RejectModelCommand(Guid ModelId, Guid ReviewerId, string Reason) : IRequest<Result>;

public class GetModelReviewsQueryHandler
    : IRequestHandler<GetModelReviewsQuery, Result<IReadOnlyList<ModelReviewDto>>>
{
    private readonly IModelRepository _models;
    private readonly ICustomerRepository _customers;

    public GetModelReviewsQueryHandler(IModelRepository models, ICustomerRepository customers)
    {
        _models = models;
        _customers = customers;
    }

    public async Task<Result<IReadOnlyList<ModelReviewDto>>> Handle(
        GetModelReviewsQuery query, CancellationToken cancellationToken)
    {
        var models = await _models.GetForReviewAsync(query.Status, query.Search, cancellationToken);
        var result = new List<ModelReviewDto>(models.Count);
        foreach (var model in models)
        {
            var customer = await _customers.GetByIdAsync(model.CustomerId, cancellationToken);
            result.Add(ToDto(model, customer?.Email ?? string.Empty));
        }

        return Result.Success<IReadOnlyList<ModelReviewDto>>(result);
    }

    internal static ModelReviewDto ToDto(Domain.Entities.Model model, string customerEmail) => new(
        model.Id,
        model.CustomerId,
        customerEmail,
        model.Name,
        model.FileName,
        model.FileFormat,
        model.SizeBytes,
        model.Sha256,
        model.GeometryStatus.ToString(),
        model.IsPrintable,
        model.CreatedAt,
        model.ReviewStatus.ToString(),
        model.ReviewReason,
        model.ReviewedAtUtc);
}

public class GetModelReviewQueryHandler : IRequestHandler<GetModelReviewQuery, Result<ModelReviewDto>>
{
    private readonly IModelRepository _models;
    private readonly ICustomerRepository _customers;

    public GetModelReviewQueryHandler(IModelRepository models, ICustomerRepository customers)
    {
        _models = models;
        _customers = customers;
    }

    public async Task<Result<ModelReviewDto>> Handle(GetModelReviewQuery query, CancellationToken cancellationToken)
    {
        var model = await _models.GetByIdAsync(query.ModelId, cancellationToken);
        if (model is null) return Result.Failure<ModelReviewDto>(Error.NotFound("Model", query.ModelId));

        var customer = await _customers.GetByIdAsync(model.CustomerId, cancellationToken);
        return Result.Success(GetModelReviewsQueryHandler.ToDto(model, customer?.Email ?? string.Empty));
    }
}

public class ApproveModelCommandHandler : IRequestHandler<ApproveModelCommand, Result>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveModelCommand command, CancellationToken cancellationToken)
    {
        var model = await _models.GetByIdAsync(command.ModelId, cancellationToken);
        if (model is null) return Result.Failure(Error.NotFound("Model", command.ModelId));

        // Only a model with stored bytes can be reviewed; metadata-only rows are not printable.
        if (model.StorageKey is null)
            return Result.Failure(Error.Conflict("Model chưa có file — không thể duyệt."));

        model.Approve(command.ReviewerId, DateTime.UtcNow, command.Note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class RejectModelCommandHandler : IRequestHandler<RejectModelCommand, Result>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public RejectModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectModelCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure(Error.Validation("Lý do từ chối là bắt buộc."));

        var model = await _models.GetByIdAsync(command.ModelId, cancellationToken);
        if (model is null) return Result.Failure(Error.NotFound("Model", command.ModelId));

        model.Reject(command.ReviewerId, DateTime.UtcNow, command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
