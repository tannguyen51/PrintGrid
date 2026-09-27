using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Entities;

/// <summary>
/// A 3D model file owned by a customer in their personal library.
/// Currently metadata-only; the binary file (STL/OBJ/3MF) is future scope (MinIO).
/// </summary>
public class Model : AggregateRoot<Guid>
{
    private readonly List<string> _tags = new();

    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FileFormat { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Object key of the uploaded binary in MinIO (e.g. "printgrid-models/&lt;customerId&gt;/&lt;modelId&gt;.stl").
    /// Null when the model was registered as metadata-only (no file uploaded yet).
    /// </summary>
    public string? StorageKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // ── Geometry analysis (FR-SCHED-001) ─────────────────────────────
    // Populated asynchronously by the slicing/analysis pipeline after upload.
    public GeometryStatus GeometryStatus { get; private set; } = GeometryStatus.Pending;
    public decimal? BoundingWidthMm { get; private set; }
    public decimal? BoundingDepthMm { get; private set; }
    public decimal? BoundingHeightMm { get; private set; }
    public decimal? VolumeCm3 { get; private set; }
    public int? EstimatedPrintMinutes { get; private set; }
    public string? GeometryMessage { get; private set; }

    /// <summary>
    /// Mutable list for EF Core's primitive collection mapping; expose read-only
    /// through code. EF requires a writable collection type (List&lt;T&gt;), not
    /// IReadOnlyCollection.
    /// </summary>
    public List<string> Tags => _tags;

    private Model() { }

    public static Model Create(
        Guid customerId,
        string name,
        string? description,
        string fileName,
        string fileFormat,
        long sizeBytes,
        IEnumerable<string>? tags,
        string? storageKey = null)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required", nameof(customerId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required", nameof(fileName));

        var model = new Model
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Name = name.Trim(),
            Description = description?.Trim(),
            FileName = fileName.Trim(),
            FileFormat = (fileFormat ?? string.Empty).Trim().ToUpperInvariant(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (tags is not null)
        {
            model._tags.AddRange(tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct());
        }

        return model;
    }

    public void UpdateDetails(
        string name,
        string? description,
        string fileName,
        string fileFormat,
        long sizeBytes,
        IEnumerable<string>? tags)
    {
        Name = name.Trim();
        Description = description?.Trim();
        FileName = fileName.Trim();
        FileFormat = (fileFormat ?? string.Empty).Trim().ToUpperInvariant();
        SizeBytes = sizeBytes;
        UpdatedAt = DateTime.UtcNow;

        _tags.Clear();
        if (tags is not null)
        {
            _tags.AddRange(tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct());
        }
    }

    /// <summary>
    /// Records the result of the async geometry analysis run after upload.
    /// </summary>
    public void ApplyGeometry(
        decimal boundingWidthMm,
        decimal boundingDepthMm,
        decimal boundingHeightMm,
        decimal volumeCm3,
        int estimatedPrintMinutes)
    {
        BoundingWidthMm = boundingWidthMm;
        BoundingDepthMm = boundingDepthMm;
        BoundingHeightMm = boundingHeightMm;
        VolumeCm3 = volumeCm3;
        EstimatedPrintMinutes = estimatedPrintMinutes;
        GeometryStatus = GeometryStatus.Ready;
        GeometryMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks analysis as failed so the customer can see the reason (e.g. geometry
    /// issues on the mesh or an unsupported format).
    /// </summary>
    public void MarkGeometryFailed(string reason)
    {
        GeometryStatus = GeometryStatus.Failed;
        GeometryMessage = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}