namespace PrintGrid.SharedKernel.ValueObjects;

/// <summary>Stock color identities, including names used by older clients and ledgers.</summary>
public static class MaterialColorCodes
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Color is required", nameof(value));
        return value.Trim().ToUpperInvariant() switch
        {
            "SIGNALRED" => "RED",
            "GREY" => "GRAY",
            var code => code
        };
    }

    public static bool Equivalent(string left, string right) => Normalize(left) == Normalize(right);

    // Canonical configuration takes precedence when a legacy rate also exists.
    public static IReadOnlyList<string> Aliases(string value) => Normalize(value) switch
    {
        "RED" => ["RED", "SIGNALRED"],
        "GRAY" => ["GRAY", "GREY"],
        var code => [code]
    };
}
