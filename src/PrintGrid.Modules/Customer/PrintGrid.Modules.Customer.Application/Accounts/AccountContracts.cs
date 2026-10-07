namespace PrintGrid.Modules.Customer.Application.Accounts;

public record CustomerProfileDto(Guid Id, string Email, string FullName, string? PhoneNumber, bool IsEmailVerified);

public record CustomerAddressDto(Guid Id, string Label, string RecipientName, string PhoneNumber,
    string Street, string Ward, string District, string City, string PostalCode, string Country, bool IsDefault);

public record AddressInput(string Label, string RecipientName, string PhoneNumber, string Street,
    string Ward, string District, string City, string? PostalCode, string? Country, bool IsDefault);
