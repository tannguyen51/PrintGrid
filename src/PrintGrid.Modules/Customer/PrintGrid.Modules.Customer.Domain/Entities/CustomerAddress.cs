using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class CustomerAddress : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string Ward { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = "VN";
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CustomerAddress() { }

    public static CustomerAddress Create(Guid customerId, string label, string recipientName,
        string phoneNumber, string street, string ward, string district, string city,
        string? postalCode, string? country, bool isDefault)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required", nameof(customerId));
        if (string.IsNullOrWhiteSpace(recipientName)) throw new ArgumentException("Recipient name is required", nameof(recipientName));
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Street and city are required");

        return new CustomerAddress
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Label = label.Trim(),
            RecipientName = recipientName.Trim(), PhoneNumber = phoneNumber.Trim(),
            Street = street.Trim(), Ward = ward.Trim(), District = district.Trim(), City = city.Trim(),
            PostalCode = postalCode?.Trim() ?? string.Empty, Country = country?.Trim() ?? "VN",
            IsDefault = isDefault, CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string label, string recipientName, string phoneNumber, string street,
        string ward, string district, string city, string? postalCode, string? country)
    {
        Label = label.Trim(); RecipientName = recipientName.Trim(); PhoneNumber = phoneNumber.Trim();
        Street = street.Trim(); Ward = ward.Trim(); District = district.Trim(); City = city.Trim();
        PostalCode = postalCode?.Trim() ?? string.Empty; Country = country?.Trim() ?? "VN";
    }

    public void SetDefault(bool value) => IsDefault = value;
}
