using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Queries;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-ANAL-002/003: 90-day network SLA report (BR-PERF-001/002) with a CSV export.
/// Export uses a UTF-8 BOM so Excel opens the Vietnamese lab names correctly, and an empty
/// network is written as "no data" rather than a table of zeroes (FR-ANAL-003 AC).
/// </summary>
[ApiController]
[Route("api/v1/sla")]
[Authorize(Policy = Policies.RequireOps)]
public sealed class SlaController : ControllerBase
{
    private const string CsvHeader =
        "lab_name,on_time_delivery_rate,first_pass_yield,acceptance_rate,utilization_rate,completed_jobs,assigned_jobs,late_jobs";

    private readonly ISender _sender;

    public SlaController(ISender sender) => _sender = sender;

    /// <summary>GET /sla/report?labId= — JSON report; omit labId for every lab.</summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] Guid? labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNetworkSlaReportQuery(labId), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }

    /// <summary>GET /sla/report/export?labId= — text/csv with BOM.</summary>
    [HttpGet("report/export")]
    public async Task<IActionResult> Export([FromQuery] Guid? labId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNetworkSlaReportQuery(labId), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "not_found"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        var report = result.Value;
        var csv = new StringBuilder();
        csv.Append(CsvHeader).Append("\r\n");

        if (report.Labs.Count == 0)
        {
            csv.Append("no data\r\n");
        }
        else
        {
            foreach (var row in report.Labs.Prepend(report.Network))
                csv.Append(Row(row)).Append("\r\n");
        }

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8",
            $"printgrid-sla-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static string Row(SlaLabRowDto row) =>
        string.Join(',',
            Quote(row.LabName),
            Rate(row.OnTimeDeliveryRate),
            Rate(row.FirstPassYield),
            Rate(row.AcceptanceRate),
            Rate(row.UtilizationRate),
            row.CompletedJobs.ToString(CultureInfo.InvariantCulture),
            row.AssignedJobs.ToString(CultureInfo.InvariantCulture),
            row.LateJobs.ToString(CultureInfo.InvariantCulture));

    private static string Rate(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Quote(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@') safe = $"'{safe}";
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
