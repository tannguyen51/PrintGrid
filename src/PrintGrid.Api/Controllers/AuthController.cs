using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Login;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;
using PrintGrid.Modules.Customer.Application.Commands.Auth.Register;
using PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;
using PrintGrid.Modules.Customer.Application.Commands.Auth.ForgotPassword;
using PrintGrid.Modules.Customer.Application.Commands.Auth.ResetPassword;

using PrintGrid.Modules.Customer.Application.Commands.Auth.VerifyEmail;
using PrintGrid.Modules.Customer.Application.Commands.Auth.ResendVerification;
using PrintGrid.Modules.Admin.Application.AuditLogs;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditLogWriter _audit;

    public AuthController(ISender sender, IAuditLogWriter audit)
    {
        _sender = sender;
        _audit = audit;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCustomerCommand(request.FullName, request.Email, request.Password, request.PhoneNumber),
            cancellationToken);

        return result.IsFailure
            ? Conflict(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Created("/api/v1/auth/login", result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCustomerCommand(request.Email, request.Password),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "unauthorized"
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        await _audit.WriteAsync(new AuditLogWriteRequest(
            result.Value.User.Id,
            result.Value.User.Email,
            string.Join(',', result.Value.User.Roles),
            AuditActionClasses.Authentication,
            "auth.login_succeeded",
            "Session",
            OldValues: null,
            NewValues: new { result.Value.User.Id },
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-ADMIN-004", "UC-025", "BR-CONFIG-004"]), cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshSessionCommand(request.RefreshToken),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "unauthorized"
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }

        return Ok(result.Value);
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GoogleLoginCommand(request.Credential), cancellationToken);
        if (result.IsFailure)
            return Unauthorized(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        await _audit.WriteAsync(new AuditLogWriteRequest(
            result.Value.User.Id,
            result.Value.User.Email,
            string.Join(',', result.Value.User.Roles),
            AuditActionClasses.Authentication,
            "auth.google_login_succeeded",
            "Session",
            OldValues: null,
            NewValues: new { result.Value.User.Id, Provider = "Google" },
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            TraceId: HttpContext.TraceIdentifier,
            RequirementCodes: ["FR-CUST-001", "UC-026"]), cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Accepted(new
        {
            message = "Nếu email tồn tại, PrintGrid đã gửi liên kết đặt lại mật khẩu."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

        return result.IsFailure
            ? BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } })
            : Ok(new { message = "Mật khẩu đã được cập nhật." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyEmailCommand(request.Email, request.Token), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        return Ok();
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ResendVerificationCommand(request.Email), cancellationToken);
        if (result.IsFailure)
        {
            var status = result.Error.Code == "rate_limit_exceeded" || result.Error.Code == "cooldown_active"
                ? StatusCodes.Status429TooManyRequests
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
        }
        return Ok(result.Value);
    }
}

public record RegisterRequest(string FullName, string Email, string Password, string? PhoneNumber);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record VerifyEmailRequest(string Email, string Token);
public record ResendVerificationRequest(string Email);
public record GoogleLoginRequest(string Credential);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
