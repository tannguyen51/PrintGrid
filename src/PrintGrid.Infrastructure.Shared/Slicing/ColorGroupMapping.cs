using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Validates complete source coverage before merging regions onto physical filament slots.</summary>
public static class ColorGroupMapping
{
    public sealed record MappedMesh(IReadOnlyList<List<QuoteMesh.Triangle>> Groups, IReadOnlyList<ColorGroupAssignment> Assignments);

    public static MappedMesh Apply(IReadOnlyList<ThreeMfPackage.ColorGroup> source,
        IReadOnlyList<ColorGroupAssignment> assignments, IReadOnlyList<string> colors)
    {
        var frozen = Resolve(source.ToDictionary(g => g.Id, g => g.OriginalColorHex, StringComparer.Ordinal), assignments, colors);
        var targets = colors.Select(c => c.Trim().ToUpperInvariant()).ToArray();
        var groups = targets.Select(_ => new List<QuoteMesh.Triangle>()).ToList();
        var byId = frozen.ToDictionary(a => a.GroupId, StringComparer.Ordinal);
        foreach (var group in source) groups[Array.IndexOf(targets, byId[group.Id].ColorCode)].AddRange(group.Triangles);
        return new(groups, frozen);
    }

    public static IReadOnlyList<ColorGroupAssignment> Resolve(IReadOnlyDictionary<string, string?> source,
        IReadOnlyList<ColorGroupAssignment> assignments, IReadOnlyList<string> colors)
    {
        var byId = new Dictionary<string, ColorGroupAssignment>(StringComparer.Ordinal);
        foreach (var assignment in assignments)
        {
            if (string.IsNullOrWhiteSpace(assignment.GroupId) || string.IsNullOrWhiteSpace(assignment.ColorCode)
                || !byId.TryAdd(assignment.GroupId, assignment))
                throw new InvalidDataException("Mỗi nhóm màu gốc phải có đúng một màu in.");
        }
        if (source.Count != byId.Count || source.Keys.Any(id => !byId.ContainsKey(id)))
            throw new InvalidDataException("Ánh xạ màu không khớp các nhóm trong file. Hãy tải lại model và chọn màu lại.");
        var targets = colors.Select(c => c.Trim().ToUpperInvariant()).ToArray();
        var selected = assignments.Select(a => a.ColorCode.Trim().ToUpperInvariant()).Distinct().ToArray();
        if (targets.Length is < 1 or > 4 || targets.Distinct().Count() != targets.Length
            || !targets.Order().SequenceEqual(selected.Order()))
            throw new InvalidDataException("Danh sách màu in không khớp ánh xạ nhóm màu.");
        var frozen = new List<ColorGroupAssignment>();
        foreach (var group in source)
        {
            var code = byId[group.Key].ColorCode.Trim().ToUpperInvariant();
            frozen.Add(new(group.Key, code, group.Value));
        }
        return frozen;
    }
}
