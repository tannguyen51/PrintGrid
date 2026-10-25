namespace PrintGrid.Modules.Customer.Application.Auth;

public interface IGoogleIdentityVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken cancellationToken = default);
}

public sealed record GoogleIdentity(string Subject, string Email, string FullName, bool EmailVerified);
