using MediatR;
using PrintGrid.Modules.Scheduling.Application.Commands.Calibration;

namespace PrintGrid.Api.BackgroundJobs;

/// <summary>
/// FR-SCHED-008 recurring worker: collect newly completed (estimated, actual) pairs and
/// recalibrate every machine. Mirrors <see cref="EmailDeliveryJob"/>: a thin Hangfire entry
/// point, all rules live in the application handlers.
///
/// NOTE: registration in Program.cs / the Hangfire recurring-job list is still pending —
/// this task deliberately does not touch Program.cs. Until then the job can be triggered by
/// POST /api/v1/calibration/samples/collect followed by POST /api/v1/calibration/recalibrate.
/// </summary>
public class CalibrationNightlyJob
{
    private readonly ISender _sender;
    private readonly ILogger<CalibrationNightlyJob> _logger;

    public CalibrationNightlyJob(ISender sender, ILogger<CalibrationNightlyJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var collected = await _sender.Send(new CollectCalibrationSamplesCommand(), cancellationToken);
        if (collected.IsFailure)
        {
            _logger.LogWarning("Calibration sample collection failed: {Code} {Message}",
                collected.Error.Code, collected.Error.Message);
            return;
        }

        var recalibrated = await _sender.Send(new RecalibrateAllCommand(), cancellationToken);
        if (recalibrated.IsFailure)
        {
            _logger.LogWarning("Calibration recalibration failed: {Code} {Message}",
                recalibrated.Error.Code, recalibrated.Error.Message);
            return;
        }

        var applied = recalibrated.Value.Count(x => x.IsSufficient);
        _logger.LogInformation(
            "Calibration run: {Collected} new sample(s); {Applied}/{Total} machine(s) recalibrated",
            collected.Value, applied, recalibrated.Value.Count);
    }
}
