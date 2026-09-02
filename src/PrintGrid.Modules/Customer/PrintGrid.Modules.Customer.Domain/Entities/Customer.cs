using PrintGrid.SharedKernel.Common;
using PrintGrid.Modules.Customer.Domain.Events;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Customer : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private Customer() { }

    public static Customer Register(string email, string passwordHash, string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required", nameof(passwordHash));

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        customer.AddDomainEvent(new CustomerRegisteredEvent(customer.Id, customer.Email));
        return customer;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
}
