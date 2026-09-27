using PrintGrid.Modules.Customer.Application.Auth;

namespace PrintGrid.Api.Auth;

/// <summary>BCrypt password hashing backed by BCrypt.Net-Next.</summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}