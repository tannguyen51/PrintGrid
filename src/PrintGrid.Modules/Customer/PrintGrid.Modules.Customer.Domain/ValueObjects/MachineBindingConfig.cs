using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.ValueObjects;

/// <summary>
/// Machine binding config per BR-01/BR-04/BR-05/BR-09: how a customer constrains machine selection.
/// Types: "không" (default), "theo_model" (pin model with surcharge), "như_đơn_cũ" (reorder).
/// </summary>
public enum MachineBindingType
{
    /// <summary>Không có ràng buộc — hệ thống tự phân bổ máy trong toàn mạng lưới.</summary>
    None = 0,

    /// <summary>Chỉ định model máy cụ thể, tính phụ phí h_chỉ_định (BR-05).</summary>
    ByModel = 1,

    /// <summary>Đặt lại như đơn cũ — cùng model là ưu tiên mềm, không bắt buộc (BR-09).</summary>
    LikeOldOrder = 2
}

/// <summary>
/// Represents the customer's machine selection mode and any related references.
/// Stores in DB as JSON string matching Section 6 schema requirements.
/// </summary>
public sealed class MachineBindingConfig : ValueObject
{
    public MachineBindingType Type { get; }
    public Guid? ModelId { get; }       // required when type == ByModel
    public bool HardRequirement { get; } // true = bắt buộc, false = ưu tiên (only for reorder)
    public Guid? ReferenceLineId { get; } // reference order line id (for reorder)

    private MachineBindingConfig(MachineBindingType type, Guid? modelId, bool hardRequirement, Guid? referenceLineId)
    {
        if (type == MachineBindingType.ByModel && !modelId.HasValue)
            throw new ArgumentException("Model ID is required when binding by model.", nameof(modelId));
        if (type == MachineBindingType.LikeOldOrder && !referenceLineId.HasValue)
            throw new ArgumentException("Reference line ID is required for reorder.", nameof(referenceLineId));

        Type = type;
        ModelId = modelId;
        HardRequirement = hardRequirement;
        ReferenceLineId = referenceLineId;
    }

    public static MachineBindingConfig None => new(MachineBindingType.None, null, false, null);

    /// <summary>Pin to a specific machine model (Section 4).</summary>
    public static MachineBindingConfig PinModel(Guid modelId)
        => new(MachineBindingType.ByModel, modelId, true, null);

    /// <summary>Reorder with same configuration (Section 5).</summary>
    public static MachineBindingConfig LikeOldOrder(Guid referenceLineId)
        => new(MachineBindingType.LikeOldOrder, null, false, referenceLineId);

    /// <summary>Deserialize from JSON stored in DB (Section 6 format).</summary>
    public static MachineBindingConfig FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return None;
        var obj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        var typeVal = obj.GetProperty("kiểu").GetInt32();
        var modelIdVal = obj.TryGetProperty("model_id", out var m) && m.ToString() != "" ? new Guid?(Guid.Parse(m.GetString()!)) : null;
        var mứcVal = obj.TryGetProperty("mức", out var l) && l.ToString() != "" ? l.GetString()?.ToLowerInvariant() == "ưu_tiên" : false;
        var refVal = obj.TryGetProperty("dòng_don_tham_chiếu", out var r) && r.ToString() != "" ? new Guid?(Guid.Parse(r.GetString()!)) : null;
        return (MachineBindingType)typeVal switch
        {
            MachineBindingType.ByModel => PinModel(modelIdVal!.Value),
            MachineBindingType.LikeOldOrder => LikeOldOrder(refVal!.Value),
            _ => None
        };
    }

    /// <summary>Serialize to JSON for DB storage (Section 6 format).</summary>
    public string ToJson()
    {
        var dict = new Dictionary<string, object?>
        {
            ["kiểu"] = (int)Type,
            ["model_id"] = ModelId?.ToString(),
            ["mức"] = HardRequirement ? "bắt_buộc" : "ưu_tiên",
            ["dòng_don_tham_chiếu"] = ReferenceLineId?.ToString()
        };
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return ModelId;
        yield return HardRequirement;
        yield return ReferenceLineId;
    }
}
