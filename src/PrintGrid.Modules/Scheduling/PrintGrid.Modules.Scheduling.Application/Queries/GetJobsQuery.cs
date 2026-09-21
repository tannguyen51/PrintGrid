using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Application.Mappings;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetJobsQuery(JobStatus Status) : IRequest<Result<IReadOnlyList<JobDto>>>;