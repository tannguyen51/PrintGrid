using System.Security.Claims;
using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.OrderWorkflowTests;

public sealed class ModelUploadHashRegressionTests
{
    [Theory]
    [InlineData("glb", 3355744)]
    [InlineData("stl", 172)]
    [InlineData("obj", 8193)]
    [InlineData("3mf", 65536)]
    public async Task Upload_hashes_the_complete_file_and_passes_identical_bytes_to_length_limited_storage(string extension, int length)
    {
        var bytes = new byte[length];
        new Random(42).NextBytes(bytes);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetModelQuotaQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ModelQuotaDto(0, 20, 0, 1073741824)));
        UploadModelCommand? command = null;
        sender.Send(Arg.Any<UploadModelCommand>(), Arg.Any<CancellationToken>()).Returns(call => {
            command = call.Arg<UploadModelCommand>();
            // Stop after capture: Hangfire and persistence are exercised by the live upload check.
            return Result.Failure<ModelDto>(Error.Validation("captured_upload"));
        });
        var files = Substitute.For<IFileStorage>();
        byte[]? stored = null;
        Stream? borrowed = null;
        files.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>(), Arg.Any<long?>()).Returns(async call => {
                borrowed = call.Arg<Stream>();
                stored = new byte[checked((int)call.Arg<long?>()!.Value)];
                // MinIO consumes the declared length, without reading once more to signal EOF.
                await borrowed.ReadExactlyAsync(stored);
                return "models/captured";
            });
        var services = new ServiceCollection();
        services.AddSingleton(sender);
        services.AddSingleton(files);
        services.AddSingleton(Options.Create(new MinioOptions()));
        services.AddSingleton(Options.Create(new ModelUploadOptions()));
        services.AddSingleton(Substitute.For<IAuditLogWriter>());
        services.AddSingleton(Substitute.For<IModelRepository>());
        services.AddLogging();
        using var provider = services.BuildServiceProvider();
        var controller = ActivatorUtilities.CreateInstance<ModelsController>(provider);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"))
        } };
        using var source = new MemoryStream(bytes);
        var file = new FormFile(source, 0, bytes.Length, "File", "part." + extension);

        var result = await controller.Upload(new("Test", null, file, null), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        command.Should().NotBeNull();
        command!.Sha256.Should().Be(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        stored.Should().Equal(bytes);
        borrowed!.CanRead.Should().BeFalse("the temporary upload buffer must be disposed after the request");
        await files.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
