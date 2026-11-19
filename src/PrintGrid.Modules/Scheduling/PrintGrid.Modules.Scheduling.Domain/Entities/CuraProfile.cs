using PrintGrid.SharedKernel.Common;
using System.Text.Json.Nodes;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>Immutable, resolved Cura settings for one hardware/material/quality combination.</summary>
public sealed class CuraProfile : Entity<Guid>
{
    public const string StatusApproved = "Approved";
    public const string StatusPendingReview = "PendingReview";
    public const string StatusRejected = "Rejected";

    public string Name { get; private set; } = "";
    public string Manufacturer { get; private set; } = "";
    public string MachineModel { get; private set; } = "";
    public string MaterialCode { get; private set; } = "";
    public string Version { get; private set; } = "";
    public string EngineVersion { get; private set; } = "";
    public decimal NozzleDiameterMm { get; private set; }
    public decimal FilamentDiameterMm { get; private set; }
    public int ExtruderCount { get; private set; }
    public int MaximumColorSlots { get; private set; }
    public decimal WidthMm { get; private set; }
    public decimal DepthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public bool HeatedBed { get; private set; }
    public decimal RequiredNozzleTemperatureC { get; private set; }
    public decimal RequiredBedTemperatureC { get; private set; }
    public string SettingsJson { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public bool IsVerified { get; private set; }
    public string ApprovalStatus { get; private set; } = StatusApproved;
    public Guid? SubmittedByLabId { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? TargetMachineId { get; private set; }
    public string? SubmissionNotes { get; private set; }
    public string? SourceFileName { get; private set; }
    public string? ReviewReason { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string CuraDefinitionId { get; private set; } = "";
    public string CuraExtruderDefinitionId { get; private set; } = "";
    public string CuraVariantId { get; private set; } = "";
    public string CuraQualityId { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private CuraProfile() { }

    public static CuraProfile Import(string name, string manufacturer, string model, string material,
        string version, string engineVersion, decimal nozzle, decimal filament, int extruders, int colors,
        decimal width, decimal depth, decimal height, bool heatedBed, decimal nozzleTemperature,
        decimal bedTemperature, string settingsJson, bool isActive = true, string approvalStatus = StatusApproved,
        Guid? submittedByLabId = null, Guid? submittedByUserId = null, Guid? targetMachineId = null,
        string? submissionNotes = null, string? sourceFileName = null)
    {
        var normalizedStatus = approvalStatus switch
        {
            StatusApproved or StatusPendingReview or StatusRejected => approvalStatus,
            _ => throw new ArgumentException("Trạng thái duyệt profile không hợp lệ.")
        };
        var now = DateTime.UtcNow;
        return new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Manufacturer = manufacturer.Trim().ToUpperInvariant(),
            MachineModel = model.Trim().ToUpperInvariant(),
            MaterialCode = material.Trim().ToUpperInvariant(),
            Version = version.Trim(),
            EngineVersion = engineVersion.Trim(),
            NozzleDiameterMm = nozzle,
            FilamentDiameterMm = filament,
            ExtruderCount = extruders,
            MaximumColorSlots = colors,
            WidthMm = width,
            DepthMm = depth,
            HeightMm = height,
            HeatedBed = heatedBed,
            RequiredNozzleTemperatureC = nozzleTemperature,
            RequiredBedTemperatureC = bedTemperature,
            SettingsJson = settingsJson,
            ApprovalStatus = normalizedStatus,
            IsActive = normalizedStatus == StatusApproved && isActive,
            SubmittedByLabId = submittedByLabId,
            SubmittedByUserId = submittedByUserId,
            TargetMachineId = targetMachineId,
            SubmissionNotes = string.IsNullOrWhiteSpace(submissionNotes) ? null : submissionNotes.Trim(),
            SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void MarkVerified(string definitionId, string extruderDefinitionId, string variantId, string qualityId)
    {
        if (new[] { definitionId, extruderDefinitionId, qualityId }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Profile được xác minh phải có đủ definition, extruder và quality ID.");
        IsVerified = true;
        CuraDefinitionId = definitionId;
        CuraExtruderDefinitionId = extruderDefinitionId;
        CuraVariantId = variantId;
        CuraQualityId = qualityId;
    }

    public void ApproveProposal(Guid reviewerId, string? note = null)
    {
        if (ApprovalStatus != StatusPendingReview)
            throw new ArgumentException("Chỉ có thể duyệt yêu cầu profile đang chờ duyệt.");
        if (note is not null && note.Trim().Length > 1000)
            throw new ArgumentException("Ghi chú duyệt tối đa 1000 ký tự.");

        ApprovalStatus = StatusApproved;
        IsActive = true;
        ReviewReason = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RejectProposal(Guid reviewerId, string reason)
    {
        if (ApprovalStatus != StatusPendingReview)
            throw new ArgumentException("Chỉ có thể từ chối yêu cầu profile đang chờ duyệt.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Vui lòng nhập lý do từ chối profile.");
        if (reason.Trim().Length > 1000)
            throw new ArgumentException("Lý do từ chối tối đa 1000 ký tự.");

        ApprovalStatus = StatusRejected;
        IsActive = false;
        ReviewReason = reason.Trim();
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ResubmitFrom(
        CuraProfile parsedReplacement,
        Guid submittedByUserId,
        Guid? targetMachineId = null,
        string? submissionNotes = null,
        string? sourceFileName = null)
    {
        if (ApprovalStatus == StatusApproved)
            throw new ArgumentException("Profile đã được duyệt không thể chỉnh sửa từ phía xưởng.");

        Name = parsedReplacement.Name;
        Manufacturer = parsedReplacement.Manufacturer;
        MachineModel = parsedReplacement.MachineModel;
        MaterialCode = parsedReplacement.MaterialCode;
        Version = parsedReplacement.Version;
        EngineVersion = parsedReplacement.EngineVersion;
        NozzleDiameterMm = parsedReplacement.NozzleDiameterMm;
        FilamentDiameterMm = parsedReplacement.FilamentDiameterMm;
        ExtruderCount = parsedReplacement.ExtruderCount;
        MaximumColorSlots = parsedReplacement.MaximumColorSlots;
        WidthMm = parsedReplacement.WidthMm;
        DepthMm = parsedReplacement.DepthMm;
        HeightMm = parsedReplacement.HeightMm;
        HeatedBed = parsedReplacement.HeatedBed;
        RequiredNozzleTemperatureC = parsedReplacement.RequiredNozzleTemperatureC;
        RequiredBedTemperatureC = parsedReplacement.RequiredBedTemperatureC;
        SettingsJson = parsedReplacement.SettingsJson;
        ApprovalStatus = StatusPendingReview;
        IsActive = false;
        ReviewReason = null;
        ReviewedByUserId = null;
        ReviewedAtUtc = null;
        SubmittedByUserId = submittedByUserId;
        TargetMachineId = targetMachineId ?? TargetMachineId;
        SubmissionNotes = string.IsNullOrWhiteSpace(submissionNotes) ? null : submissionNotes.Trim();
        if (!string.IsNullOrWhiteSpace(sourceFileName))
            SourceFileName = sourceFileName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (ApprovalStatus != StatusApproved)
            throw new ArgumentException("Chỉ profile đã duyệt mới được kích hoạt.");
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool HasSameSettings(string settingsJson) =>
        JsonNode.DeepEquals(JsonNode.Parse(SettingsJson), JsonNode.Parse(settingsJson));

    public bool Matches(Machine m) => IsActive && ApprovalStatus == StatusApproved &&
                                      m is { Technology: Enums.PrintTechnology.Fdm, PrinterConfiguration: { } c } &&
                                      (CuraDefinitionId.Length == 0 || string.IsNullOrWhiteSpace(c.CuraDefinitionId) || CuraDefinitionId.Equals(c.CuraDefinitionId.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                                      Manufacturer.Equals(c.Manufacturer.Trim(), StringComparison.InvariantCultureIgnoreCase) && MachineModel.Equals(m.Model.Trim(), StringComparison.InvariantCultureIgnoreCase) &&
                                      NozzleDiameterMm == c.NozzleDiameterMm && FilamentDiameterMm == c.FilamentDiameterMm &&
                                      ExtruderCount == c.ExtruderCount && MaximumColorSlots == c.MaximumColorSlots &&
                                      SameDimension(WidthMm, m.BuildVolume.WidthMm) &&
                                      SameDimension(DepthMm, m.BuildVolume.DepthMm) &&
                                      SameDimension(HeightMm, m.BuildVolume.HeightMm) &&
                                      HeatedBed == c.HeatedBed && RequiredNozzleTemperatureC <= c.MaxNozzleTemperatureC &&
                                      RequiredBedTemperatureC <= c.MaxBedTemperatureC && m.SupportedMaterials.Contains(MaterialCode);

    // PostgreSQL persists machine dimensions at scale 2 and rounds ties away from zero.
    private static bool SameDimension(decimal profile, decimal machine) =>
        Math.Round(profile, 2, MidpointRounding.AwayFromZero) == Math.Round(machine, 2, MidpointRounding.AwayFromZero);
}
