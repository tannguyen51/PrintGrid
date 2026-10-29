using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Admin.Application.Pricing;
using PrintGrid.Modules.Admin.Application.Samples;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Two-tier pricing surface (FR-CUST-015):
///   GET /pricing/preview → anonymous, server-computed ranges only
///   GET /pricing          → authenticated customer, exact parameter table
/// Both are projections of the SAME active parameter set — the FE never re-implements
/// the formula, and no raw rate crosses the wire before login.
/// </summary>
[ApiController]
[Route("api/v1/pricing")]
public sealed class PricingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ISlicingService _slicing;

    public PricingController(ISender sender, ISlicingService slicing)
    {
        _sender = sender;
        _slicing = slicing;
    }

    [HttpGet("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPreview(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetPublicPricingQuery(ReferenceBaseMinutes()), cancellationToken));

    [HttpGet]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> GetPricing(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetPricingDetailQuery(ReferenceBaseMinutes()), cancellationToken));

    /// <summary>
    /// Print time of the documented reference part (50 cm³, 40 mm tall, 0.2 mm layer,
    /// 30% infill). Comes from the slicer so the page never invents its own heuristic.
    /// </summary>
    private int ReferenceBaseMinutes()
    {
        var geometry = new GeometryAnalysis(
            IsValid: true,
            WidthMm: 40m,
            DepthMm: 40m,
            HeightMm: PricingQueryHandlers.ReferenceHeightMm,
            VolumeCm3: PublicPricing.ReferenceVolumeCm3,
            VertexCount: 0,
            FaceCount: 0);

        return _slicing.Estimate(
            geometry,
            "PLA",
            PublicPricing.ReferenceLayerHeightMm,
            (int)PublicPricing.ReferenceInfillPercent,
            PrintTechnology.Fdm).PrintMinutes;
    }
}
