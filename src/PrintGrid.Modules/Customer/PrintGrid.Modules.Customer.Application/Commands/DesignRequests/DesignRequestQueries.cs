using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.DesignRequests;

/// <summary>FR-CUST-013: the signed-in customer's own design requests.</summary>
public sealed record GetMyDesignRequestsQuery(Guid CustomerId)
    : IRequest<Result<IReadOnlyList<DesignRequestDto>>>;

public sealed class GetMyDesignRequestsQueryHandler
    : IRequestHandler<GetMyDesignRequestsQuery, Result<IReadOnlyList<DesignRequestDto>>>
{
    private readonly IDesignRequestRepository _requests;

    public GetMyDesignRequestsQueryHandler(IDesignRequestRepository requests) => _requests = requests;

    public async Task<Result<IReadOnlyList<DesignRequestDto>>> Handle(
        GetMyDesignRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _requests.GetByCustomerAsync(request.CustomerId, cancellationToken);
        return Result.Success<IReadOnlyList<DesignRequestDto>>(rows.Select(DesignRequestMapping.ToDto).ToList());
    }
}

/// <summary>FR-CUST-013: ops triage queue across all customers, optionally filtered by status.</summary>
public sealed record GetDesignRequestsQuery(string? Status = null)
    : IRequest<Result<IReadOnlyList<DesignRequestDto>>>;

public sealed class GetDesignRequestsQueryHandler
    : IRequestHandler<GetDesignRequestsQuery, Result<IReadOnlyList<DesignRequestDto>>>
{
    private readonly IDesignRequestRepository _requests;

    public GetDesignRequestsQueryHandler(IDesignRequestRepository requests) => _requests = requests;

    public async Task<Result<IReadOnlyList<DesignRequestDto>>> Handle(
        GetDesignRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _requests.GetAllAsync(request.Status, cancellationToken);
        return Result.Success<IReadOnlyList<DesignRequestDto>>(rows.Select(DesignRequestMapping.ToDto).ToList());
    }
}
