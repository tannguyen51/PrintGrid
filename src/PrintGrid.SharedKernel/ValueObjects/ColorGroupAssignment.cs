namespace PrintGrid.SharedKernel.ValueObjects;

/// <summary>A stable source region mapped to a stock color. The source hex is resolved from the file.</summary>
public sealed record ColorGroupAssignment(string GroupId, string ColorCode, string? OriginalColorHex = null);
