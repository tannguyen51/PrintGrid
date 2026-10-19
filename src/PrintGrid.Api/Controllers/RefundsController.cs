using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Refunds;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-ANAL-005 — refund processing and reconciliation. Ops approves/rejects the queue and can export
/// it as CSV for the finance reconciliation step. Approval executes the refund within the original
/// order's scope only (BR-LOG-002 / AC02).
/// </summary>
[ApiController]
[Route("api/v1/refunds")]
[Authorize(Policy = Policies.RequireOps)]
public sealed class RefundsController : ControllerBase
{
    private const string CsvHeader =
        "refund_id,order_id,order_item_id,date_change_request_id,amount,currency,payment_transaction_id,reason,status,created_at_utc";

    private readonly ISender _sender;

    public RefundsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRefundRequestsQuery(status), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> Process(
        Guid id,
        [FromBody] ProcessRefundRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetCustomerId();
        if (actorId is null) return Forbid();

        var result = await _sender.Send(
            new ProcessRefundCommand(id, request.Approve, request.Note, actorId.Value),
            cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRefundRequestsQuery(status), cancellationToken);
        if (result.IsFailure) return ToError(result.Error);

        var csv = new StringBuilder();
        csv.Append(CsvHeader).Append("\r\n");
        foreach (var row in result.Value)
        {
            csv.AppendJoin(',',
                Csv(row.Id.ToString()),
                Csv(row.OrderId.ToString()),
                Csv(row.OrderItemId.ToString()),
                Csv(row.DateChangeRequestId.ToString()),
                row.Amount.ToString("0.##", CultureInfo.InvariantCulture),
                Csv(row.Currency),
                Csv(row.PaymentTransactionId),
                Csv(row.Reason),
                Csv(row.Status),
                Csv(row.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            csv.Append("\r\n");
        }

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8",
            $"printgrid-refunds-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private IActionResult ToError(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = error.Code, message = error.Message } });
    }

    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        // Neutralise spreadsheet formula execution in exported free-text fields.
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@') safe = $"'{safe}";
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}

public sealed record ProcessRefundRequest(bool Approve, string? Note = null);
