using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.Modules.Customer.Application.Auth;

public interface ITokenService
{
    /// <summary>Issues a fresh access-token/refresh-token pair for the given user.</summary>
    AuthTokenPairDto CreateTokenPair(AuthUserDto user);

    /// <summary>
    /// Validates a refresh token and returns the user id it was issued to,
    /// or <see langword="null"/> when the token is invalid or expired.
    /// </summary>
    Guid? ValidateRefreshToken(string refreshToken);
}