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
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

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
        IEnumerable<string>? tags)
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
}