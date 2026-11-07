namespace PrintGrid.Modules.Customer.Domain.ValueObjects;

/// <summary>
/// Hard technical constraints for machine selection per design doc Section 3 (BR-03).
/// These are NOT surcharged — they simply filter out incompatible machines.
/// </summary>
public readonly partial struct TechConstraints
{
    public int? MaxColors { get; }
    public bool? EnclosedChamber { get; }
    public decimal? NozzleDiameterMm { get; }
    public decimal? MaxToleranceMm { get; }
    public decimal? MinLayerHeightMm { get; }

    private TechConstraints(int? maxColors, bool? enclosedChamber, decimal? nozzleDiameterMm,
        decimal? maxToleranceMm, decimal? minLayerHeightMm)
    {
        MaxColors = maxColors;
        EnclosedChamber = enclosedChamber;
        NozzleDiameterMm = nozzleDiameterMm;
        MaxToleranceMm = maxToleranceMm;
        MinLayerHeightMm = minLayerHeightMm;
    }

    /// <summary>Create from command parameters. Returns null when all values are null.</summary>
    public static TechConstraints? From(int? maxColors, bool? enclosedChamber, decimal? nozzleDiameterMm,
        decimal? maxToleranceMm, decimal? minLayerHeightMm)
    {
        if (maxColors is null && enclosedChamber is null && nozzleDiameterMm is null
            && maxToleranceMm is null && minLayerHeightMm is null)
            return null;
        return new TechConstraints(maxColors, enclosedChamber, nozzleDiameterMm, maxToleranceMm, minLayerHeightMm);
    }

    /// <summary>Check if this machine's specs satisfy all hard constraints.</summary>
    public bool MatchesMachine(int machineMaxColors, bool machineHeatedBed,
        decimal machineNozzleDiameterMm, decimal machineAchievableToleranceMm,
        decimal machineMinLayerHeightMm)
    {
        if (MaxColors is not null && machineMaxColors < MaxColors.Value) return false;
        if (EnclosedChamber is not null && machineHeatedBed != EnclosedChamber.Value) return false;
        if (NozzleDiameterMm is not null && Math.Abs(machineNozzleDiameterMm - NozzleDiameterMm.Value) > 0.001m) return false;
        if (MaxToleranceMm is not null && machineAchievableToleranceMm > MaxToleranceMm.Value) return false;
        if (MinLayerHeightMm is not null && machineMinLayerHeightMm < MinLayerHeightMm.Value) return false;
        return true;
    }

    /// <summary>Serialize to JSON-compatible dictionary.</summary>
    public System.Collections.Generic.Dictionary<string, object?> ToDictionary() =>
        new()
        {
            ["so_mau_to_da"] = MaxColors,
            ["buong_kin"] = EnclosedChamber,
            ["duong_kinh_dau_phun_mm"] = NozzleDiameterMm,
            ["sai_số_kich_thuoc_toi_da_mm"] = MaxToleranceMm,
            ["chieu_cao_lớp_toi_thieu_mm"] = MinLayerHeightMm,
        };
}
