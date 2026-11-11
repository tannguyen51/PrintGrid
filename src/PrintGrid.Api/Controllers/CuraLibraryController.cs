using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/cura-library")]
[Authorize(Policy = Policies.RequireProduction)]
public sealed class CuraLibraryController(CuraResourceCatalog catalog, CuraLibrarySyncService synchronizer,
    PrintGridDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Status() => Ok(new { catalog.Version, catalog.IsAvailable,
        MachineCount = catalog.Machines.Count, MaterialCount = catalog.Materials.Count });

    [HttpGet("machines")]
    public IActionResult Machines([FromQuery] string? search = null)
    {
        var values = catalog.Machines.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) values = values.Where(x =>
            x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.Manufacturer.Contains(search, StringComparison.OrdinalIgnoreCase));
        return Ok(values.Take(250));
    }

    [HttpGet("materials")]
    public IActionResult Materials() => Ok(catalog.Materials);

    [HttpGet("machines/{definitionId}/options")]
    public IActionResult Options(string definitionId)
    {
        try { return Ok(catalog.GetOptions(definitionId)); }
        catch (KeyNotFoundException e) { return NotFound(new { error = new { code = "not_found", message = e.Message } }); }
    }

    [HttpPost("machines/{machineId:guid}/sync")]
    public async Task<IActionResult> Sync(Guid machineId, CancellationToken ct)
    {
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.OpsManager))
        {
            var actor = User.GetCustomerId();
            if (actor is null) return Forbid();
            var ownsMachine = await db.Set<Machine>().AsNoTracking().Where(x => x.Id == machineId)
                .Join(db.Set<Lab>().AsNoTracking(), machine => machine.LabId, lab => lab.Id,
                    (_, lab) => lab.OwnerUserId == actor).SingleOrDefaultAsync(ct);
            if (!ownsMachine) return Forbid();
        }
        try { return Ok(await synchronizer.SyncMachineAsync(machineId, ct)); }
        catch (KeyNotFoundException e) { return NotFound(new { error = new { code = "not_found", message = e.Message } }); }
        catch (ArgumentException e) { return BadRequest(new { error = new { code = "validation", message = e.Message } }); }
        catch (InvalidOperationException e) { return StatusCode(503, new { error = new { code = "cura_unavailable", message = e.Message } }); }
    }

    [HttpPost("sync")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SyncLibrary(CancellationToken ct)
    {
        try { return Ok(await synchronizer.SyncAllAsync(ct)); }
        catch (InvalidOperationException e)
        { return StatusCode(503, new { error = new { code = "cura_unavailable", message = e.Message } }); }
    }
}
