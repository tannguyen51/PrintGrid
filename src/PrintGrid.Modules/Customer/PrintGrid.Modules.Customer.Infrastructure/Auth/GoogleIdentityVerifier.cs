using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.Auth;

namespace PrintGrid.Modules.Customer.Infrastructure.Auth;

public sealed class GoogleIdentityVerifier : IGoogleIdentityVerifier
{
    private readonly string _clientId;

    public GoogleIdentityVerifier(IConfiguration configuration)
    {
        _clientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
    }

    public async Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(credential))
            return null;

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] });
            cancellationToken.ThrowIfCancellationRequested();

            if (!payload.EmailVerified ||
                string.IsNullOrWhiteSpace(payload.Subject) ||
                string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            return new GoogleIdentity(
                payload.Subject,
                payload.Email.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(payload.Name) ? payload.Email.Split('@')[0] : payload.Name.Trim(),
                true);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
