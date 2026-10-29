using System.Globalization;
using System.Text;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

/// <summary>
/// A platform-owned model in the public sample library (FR-CUST-014).
///
/// Samples belong to the platform, not to a customer: guests can browse and preview
/// them anonymously, and ordering one copies the file + metadata into the customer's
/// own library. Deleting that copy therefore never touches the original, which is the
/// IP guarantee the feature was approved with.
/// </summary>
public sealed class SampleModel : Entity<Guid>
{
    private readonly List<string> _tags = new();

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;
    public string FileFormat { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string? Sha256 { get; private set; }

    /// <summary>Optional admin-supplied thumbnail; the detail page still uses the 3D viewer.</summary>
    public string? CoverImageUrl { get; private set; }

    // Geometry is analysed once at upload and copied to every customer copy, so a
    // sample quote costs exactly what the same geometry would cost after an upload.
    public decimal? BoundingWidthMm { get; private set; }
    public decimal? BoundingDepthMm { get; private set; }
    public decimal? BoundingHeightMm { get; private set; }
    public decimal? VolumeCm3 { get; private set; }
    public int? EstimatedPrintMinutes { get; private set; }
    public decimal? EstimatedMaterialGrams { get; private set; }

    /// <summary>Unpublished samples disappear from the public library but keep their row.</summary>
    public bool IsPublished { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>EF needs a writable primitive collection (same constraint as customer models).</summary>
    public List<string> Tags => _tags;

    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private SampleModel() { }

    public static SampleModel Create(
        string slug,
        string name,
        string? description,
        string category,
        string fileName,
        string fileFormat,
        long sizeBytes,
        string storageKey,
        string? sha256,
        string? coverImageUrl,
        IEnumerable<string>? tags,
        int sortOrder,
        bool isPublished,
        Guid createdBy,
        DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên mẫu là bắt buộc.", nameof(name));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("File mẫu là bắt buộc.", nameof(storageKey));

        var sample = new SampleModel
        {
            Id = Guid.NewGuid(),
            Slug = NormaliseSlug(slug, name),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? SampleCategories.Other : category.Trim(),
            FileName = fileName.Trim(),
            FileFormat = (fileFormat ?? string.Empty).Trim().TrimStart('.').ToUpperInvariant(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256.ToLowerInvariant(),
            CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim(),
            SortOrder = sortOrder,
            IsPublished = isPublished,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };

        sample.ReplaceTags(tags);
        return sample;
    }

    public void UpdateDetails(
        string slug,
        string name,
        string? description,
        string category,
        string? coverImageUrl,
        IEnumerable<string>? tags,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên mẫu là bắt buộc.", nameof(name));

        Slug = NormaliseSlug(slug, name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? SampleCategories.Other : category.Trim();
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        SortOrder = sortOrder;
        ReplaceTags(tags);
        Touch();
    }

    /// <summary>Replaces the stored binary when an admin uploads a new revision of the sample.</summary>
    public void ReplaceFile(string fileName, string fileFormat, long sizeBytes, string storageKey, string? sha256)
    {
        FileName = fileName.Trim();
        FileFormat = (fileFormat ?? string.Empty).Trim().TrimStart('.').ToUpperInvariant();
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256.ToLowerInvariant();
        Touch();
    }

    public void ApplyGeometry(
        decimal widthMm,
        decimal depthMm,
        decimal heightMm,
        decimal volumeCm3,
        int estimatedPrintMinutes,
        decimal estimatedMaterialGrams)
    {
        BoundingWidthMm = widthMm;
        BoundingDepthMm = depthMm;
        BoundingHeightMm = heightMm;
        VolumeCm3 = volumeCm3;
        EstimatedPrintMinutes = estimatedPrintMinutes;
        EstimatedMaterialGrams = estimatedMaterialGrams;
        Touch();
    }

    public void SetPublished(bool published)
    {
        IsPublished = published;
        Touch();
    }

    private void ReplaceTags(IEnumerable<string>? tags)
    {
        _tags.Clear();
        if (tags is not null)
            _tags.AddRange(tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Lowercase ASCII slug. Vietnamese diacritics are folded (ế → e) so URLs stay
    /// readable; a name without any usable Latin characters falls back to its id.
    /// </summary>
    public static string NormaliseSlug(string? candidate, string fallbackName)
    {
        var source = string.IsNullOrWhiteSpace(candidate) ? fallbackName : candidate;
        var folded = FoldDiacritics(source).ToLowerInvariant();
        var builder = new StringBuilder(folded.Length);
        var lastWasDash = false;

        foreach (var ch in folded)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > 120) slug = slug[..120].Trim('-');
        return slug.Length == 0 ? $"mau-{Guid.NewGuid():N}"[..12] : slug;
    }

    private static string FoldDiacritics(string value)
    {
        // Vietnamese "đ" has no Unicode decomposition, so map it explicitly first.
        var pre = value.Replace('đ', 'd').Replace('Đ', 'D');
        var decomposed = pre.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public static class SampleCategories
{
    public const string Other = "Khác";

    public static readonly IReadOnlyList<string> All =
    [
        "Trang trí", "Cơ khí", "Phụ kiện", "Mô hình", Other
    ];
}
