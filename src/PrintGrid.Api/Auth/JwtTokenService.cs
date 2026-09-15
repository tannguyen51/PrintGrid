using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrintGrid.Api.Configuration;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.Api.Auth;

/// <summary>
/// Issues and validates the PrintGrid JWT pair.
/// The access token carries the user identity + claims; the refresh token is a
/// stateless long-lived token whose subject is the customer id, minted to be
/// exchanged back for a fresh pair.
/// </summary>
public class JwtTokenService : ITokenService
{
    private const string RefreshTokenType = "refresh";

    private readonly JwtOptions _options;
    private readonly TokenValidationParameters _validationParameters;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    public AuthTokenPairDto CreateTokenPair(AuthUserDto user)
    {
        var key = Encoding.UTF8.GetBytes(_options.Key);

        var accessToken = CreateToken(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.FullName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, string.Join(',', user.Roles)),
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes));

        var refreshToken = CreateToken(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Typ, RefreshTokenType),
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(_options.RefreshTokenDays));

        return new AuthTokenPairDto(accessToken, refreshToken);
    }

    /// <summary>
    /// Verifies signature, issuer/audience and the refresh purpose, then returns
    /// the customer id encoded as the token's subject — or null when invalid.
    /// </summary>
    public Guid? ValidateRefreshToken(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler
            {
                // Keep raw JWT claim types ("sub", "typ"...) instead of mapping them
                // to ClaimTypes.* so our refresh-purpose check matches what we write.
                MapInboundClaims = false,
            };
            var principal = handler.ValidateToken(refreshToken, _validationParameters, out var validatedToken);

            var isRefresh = validatedToken is JwtSecurityToken jwt &&
                            jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Typ && c.Value == RefreshTokenType);
            if (!isRefresh) return null;

            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            // Invalid signature, malformed token, not a JWT, expired, etc.
            // — anything that prevents reading it as a valid refresh token.
            return null;
        }
    }

    private string CreateToken(
        IEnumerable<Claim> claims,
        DateTime notBefore,
        DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}