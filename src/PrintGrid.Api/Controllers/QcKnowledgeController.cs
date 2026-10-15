using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.QcKnowledge;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/qc-knowledge")]
[Authorize(Policy = Policies.RequireHub)]
public sealed class QcKnowledgeController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] decimal layerHeightMm, CancellationToken cancellationToken)
    {
        if (layerHeightMm <= 0) return BadRequest(ApiError("invalid_layer_height", "Chiều cao lớp in phải lớn hơn 0."));
        var result = await sender.Send(new GetQcKnowledgeQuery(layerHeightMm), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Conflict(ApiError(result.ErrorCode!, result.ErrorMessage!));
    }

    private static object ApiError(string code, string message) => new { error = new { code, message } };
}
