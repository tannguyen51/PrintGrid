using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.DesignRequests;

public sealed record DesignRequestDto(
    Guid Id,
    Guid CustomerId,
    string Title,
    string Description,
    string Status,
    Guid? QuoteId,
    IReadOnlyList<string> ReferenceImageKeys,
    DateTime CreatedAtUtc);

/// <summary>FR-CUST-013 / BR-IP-004: the customer submits a design brief plus reference image keys.</summary>
public sealed record CreateDesignRequestCommand(
    Guid CustomerId,
    string Title,
    string Description,
    IReadOnlyList<string> ReferenceImageKeys) : IRequest<Result<DesignRequestDto>>;

public sealed class CreateDesignRequestCommandHandler
    : IRequestHandler<CreateDesignRequestCommand, Result<DesignRequestDto>>
{
    private readonly IDesignRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDesignRequestCommandHandler(IDesignRequestRepository requests, IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DesignRequestDto>> Handle(
        CreateDesignRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = DesignRequest.Create(
            command.CustomerId,
            command.Title,
            command.Description,
            command.ReferenceImageKeys);

        await _requests.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DesignRequestMapping.ToDto(request));
    }
}

internal static class DesignRequestMapping
{
    internal static DesignRequestDto ToDto(DesignRequest request) => new(
        request.Id,
        request.CustomerId,
        request.Title,
        request.Description,
        request.Status,
        request.QuoteId,
        request.ReferenceImageKeys,
        request.CreatedAtUtc);
}
