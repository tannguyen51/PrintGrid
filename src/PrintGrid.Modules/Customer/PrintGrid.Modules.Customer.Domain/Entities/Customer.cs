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
    public bool IsActive { get; private set; } = true;
    public string[] Roles { get; private set; } = ["Customer"];
    
    public bool IsEmailVerified { get; private set; } = false;
    public string? VerificationTokenHash { get; private set; }
    public DateTime? VerificationTokenExpiresAt { get; private set; }
    public string? GoogleSubject { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public int SessionVersion { get; private set; }

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

    public static Customer RegisterWithGoogle(string email, string passwordHash, string fullName, string googleSubject)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
            throw new ArgumentException("Google subject is required", nameof(googleSubject));

        var customer = Register(email, passwordHash, fullName, null);
        customer.GoogleSubject = googleSubject;
        customer.VerifyEmail();
        return customer;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required", nameof(fullName));

        FullName = fullName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void AssignRole(string role)
    {
        Roles = [role];
    }

    public void SetVerificationToken(string hash, TimeSpan expiresIn)
    {
        VerificationTokenHash = hash;
        VerificationTokenExpiresAt = DateTime.UtcNow.Add(expiresIn);
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        VerificationTokenHash = null;
        VerificationTokenExpiresAt = null;
    }

    public void LinkGoogleIdentity(string googleSubject)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
            throw new ArgumentException("Google subject is required", nameof(googleSubject));

        GoogleSubject = googleSubject;
        VerifyEmail();
    }

    public void SetPasswordResetToken(string hash, TimeSpan expiresIn)
    {
        PasswordResetTokenHash = hash;
        PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(expiresIn);
    }

    public bool IsPasswordResetTokenValid(DateTime utcNow) =>
        PasswordResetTokenHash is not null && PasswordResetTokenExpiresAt > utcNow;

    public void ResetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));

        PasswordHash = passwordHash;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
        SessionVersion++;
    }
}
