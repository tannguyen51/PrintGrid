using PrintGrid.SharedKernel.Common;

namespace PrintGrid.SharedKernel.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string Ward { get; }
    public string District { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string ward, string district, string city, string postalCode, string country)
    {
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Address Create(
        string street,
        string ward,
        string district,
        string city,
        string postalCode,
        string country = "VN")
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required", nameof(street));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required", nameof(city));

        return new Address(
            street.Trim(),
            ward?.Trim() ?? string.Empty,
            district?.Trim() ?? string.Empty,
            city.Trim(),
            postalCode?.Trim() ?? string.Empty,
            country.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Ward;
        yield return District;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() =>
        string.Join(", ", new[] { Street, Ward, District, City, PostalCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p)));
}
