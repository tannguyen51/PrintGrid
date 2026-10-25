using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetJobsQuery(JobStatus Status, Guid? LabId = null) : IRequest<Result<IReadOnlyList<JobDto>>>;