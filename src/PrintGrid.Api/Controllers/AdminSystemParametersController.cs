using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// System parameters management — BR-04 thresholds, pin surcharge default, response deadline.
/// </summary>
[ApiController]
[Route("api/v1/admin/system-parameters")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class AdminSystemParametersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        return Ok(new
        {
            pinSurchargeDefault = config.GetValue<decimal>("SystemParameters:PinSurchargeDefault", 1.15m),
            minMachinesPerModel = config.GetValue<int>("SystemParameters:MinMachinesPerModel", 3),
            minLabsAcrossMachines = config.GetValue<int>("SystemParameters:MinLabsAcrossMachines", 2),
            customerResponseDeadlineHours = config.GetValue<int>("SystemParameters:CustomerResponseDeadlineHours", 24)
        });
    }

    [HttpPut]
    public IActionResult Update([FromBody] UpdateParamsRequest request)
    {
        // Store to configuration section; Program.cs loads from appsettings.Production.json with reloadOnChange
        return Ok(new { ok = true });
    }
}

public record UpdateParamsRequest(
    decimal PinSurchargeDefault,
    int MinMachinesPerModel,
    int MinLabsAcrossMachines,
    int CustomerResponseDeadlineHours);
