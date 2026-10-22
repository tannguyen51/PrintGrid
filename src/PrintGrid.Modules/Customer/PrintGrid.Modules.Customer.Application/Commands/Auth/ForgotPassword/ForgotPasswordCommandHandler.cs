using System.Net;
using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    public ForgotPasswordCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService email,
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _email = email;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var cacheKey = $"password_reset:{normalizedEmail}";
        if (await _cache.GetStringAsync(cacheKey, cancellationToken) is not null)
            return Result.Success();

        var customer = await _customers.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (customer is not null && customer.IsActive)
        {
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            customer.SetPasswordResetToken(_passwordHasher.Hash(rawToken), TimeSpan.FromMinutes(30));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var frontendUrl = (_configuration["App:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var resetLink = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(customer.Email)}&token={Uri.EscapeDataString(rawToken)}";
            var safeName = WebUtility.HtmlEncode(customer.FullName);
            var safeLink = WebUtility.HtmlEncode(resetLink);
            var body = $"""
                <p>Xin chào {safeName},</p>
                <p>PrintGrid nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                <p><a href="{safeLink}">Đặt lại mật khẩu</a></p>
                <p>Liên kết có hiệu lực trong 30 phút và chỉ dùng được một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                """;
            await _email.SendEmailAsync(customer.Email, "Đặt lại mật khẩu PrintGrid", body, cancellationToken);
        }

        await _cache.SetStringAsync(
            cacheKey,
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
            cancellationToken);

        // Always succeed so callers cannot enumerate registered email addresses.
        return Result.Success();
    }
}
