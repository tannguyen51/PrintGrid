using FluentValidation;
using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.DesignRequests;

/// <summary>FR-CUST-013: ops moves a design request through the intake lifecycle.</summary>
public sealed record ChangeDesignRequestStatusCommand(Guid DesignRequestId, string Status)
    : IRequest<Result<DesignRequestDto>>;

public sealed class ChangeDesignRequestStatusCommandHandler
    : IRequestHandler<ChangeDesignRequestStatusCommand, Result<DesignRequestDto>>
{
    private readonly IDesignRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDesignRequestStatusCommandHandler(IDesignRequestRepository requests, IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DesignRequestDto>> Handle(
        ChangeDesignRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdAsync(command.DesignRequestId, cancellationToken);
        if (request is null)
            return Result.Failure<DesignRequestDto>(Error.NotFound("DesignRequest", command.DesignRequestId));

        var changed = request.ChangeStatus(command.Status);
        if (changed.IsFailure)
            return Result.Failure<DesignRequestDto>(changed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DesignRequestMapping.ToDto(request));
    }
}

public sealed class ChangeDesignRequestStatusCommandValidator : AbstractValidator<ChangeDesignRequestStatusCommand>
{
    public ChangeDesignRequestStatusCommandValidator()
    {
        RuleFor(x => x.DesignRequestId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}
