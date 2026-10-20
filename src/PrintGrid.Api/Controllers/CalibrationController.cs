using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.Commands.Calibration;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-SCHED-008 calibration surface (ops/admin). The nightly worker calls the same two
/// commands; these endpoints exist for an on-demand run and for the MAPE/factor dashboard.
/// </summary>
[ApiController]
[Route("api/v1/calibration")]
[Authorize(Policy = Policies.RequireOps)]
public sealed class CalibrationController : ControllerBase
{
    private readonly ISender _sender;

    public CalibrationController(ISender sender) => _sender = sender;

    /// <summary>POST /calibration/samples/collect — sample newly completed jobs (idempotent).</summary>
    [HttpPost("samples/collect")]
    public async Task<IActionResult> Collect(CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new CollectCalibrationSamplesCommand(), cancellationToken));

    /// <summary>POST /calibration/recalibrate/{machineId} — recompute one machine's factor.</summary>
    [HttpPost("recalibrate/{machineId:guid}")]
    public async Task<IActionResult> Recalibrate(Guid machineId, CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new RecalibrateCommand(machineId), cancellationToken));

    /// <summary>POST /calibration/recalibrate — recompute every machine.</summary>
    [HttpPost("recalibrate")]
    public async Task<IActionResult> RecalibrateAll(CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new RecalibrateAllCommand(), cancellationToken));

    /// <summary>GET /calibration/status?labId= — current factor, sample count and MAPE.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] Guid? labId, CancellationToken cancellationToken) =>
        ToResult(await _sender.Send(new GetCalibrationStatusQuery(labId), cancellationToken));

    private IActionResult ToResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}
