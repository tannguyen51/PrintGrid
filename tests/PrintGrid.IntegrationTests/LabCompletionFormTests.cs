using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Controllers;
using PrintGrid.Api.Middleware;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Application.Queries;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.IntegrationTests;

public sealed class LabCompletionFormTests
{
    [Fact]
    public async Task Multipart_completion_binds_the_report_metrics_and_photos_and_advances_to_qc()
    {
        using var fixture = new Fixture();
        using var form = Form("Đã kiểm tra kích thước, bề mặt và màu sắc.");

        var response = await fixture.Client.PostAsync($"/api/v1/jobs/{fixture.Job.Id}/complete", form);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        fixture.Job.Status.Should().Be(JobStatus.AwaitingInspection);
        fixture.Job.QcSelfReport.Should().Be("Đã kiểm tra kích thước, bề mặt và màu sắc.");
        fixture.Job.ActualPrintMinutes.Should().Be(726);
        fixture.Job.ActualMaterialGrams.Should().Be(71.61m);
        fixture.Job.GetQcProofPhotoKeys().Should().ContainSingle().Which.Should().StartWith($"qc/{fixture.Job.Id}/");
        await fixture.Files.Received(1).UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "726", "71.61")]
    [InlineData("   ", "726", "71.61")]
    [InlineData("QC passed", "0", "71.61")]
    [InlineData("QC passed", "726", "-1")]
    public async Task Invalid_report_or_metrics_are_rejected_before_upload(string report, string minutes, string grams)
    {
        using var fixture = new Fixture();
        using var form = Form(report, minutes, grams);

        var response = await fixture.Client.PostAsync($"/api/v1/jobs/{fixture.Job.Id}/complete", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fixture.Job.Status.Should().Be(JobStatus.InProgress);
        await fixture.Files.DidNotReceiveWithAnyArgs().UploadAsync(null!, null!, null!, null!);
    }

    [Fact]
    public async Task Missing_photos_are_rejected_without_completing_the_job()
    {
        using var fixture = new Fixture();
        using var form = Form("QC passed", photos: false);

        var response = await fixture.Client.PostAsync($"/api/v1/jobs/{fixture.Job.Id}/complete", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fixture.Job.Status.Should().Be(JobStatus.InProgress);
        await fixture.Files.DidNotReceiveWithAnyArgs().UploadAsync(null!, null!, null!, null!);
    }

    [Fact]
    public async Task Hub_inspection_photos_use_the_same_multipart_binding()
    {
        using var fixture = new Fixture();
        using var form = Form("QC passed");

        var response = await fixture.Client.PostAsync($"/api/v1/jobs/{fixture.Job.Id}/inspection-photos", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        await fixture.Files.Received(1).UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>());
    }

    private static MultipartFormDataContent Form(string report, string minutes = "726", string grams = "71.61", bool photos = true)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(minutes), "actualPrintMinutes" },
            { new StringContent(grams), "actualMaterialGrams" },
            { new StringContent(report), "selfReport" }
        };
        if (photos)
        {
            // In-process storage substitute only; no test photo is sent to a deployed job.
            var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/l9sAAAAASUVORK5CYII="));
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(image, "photos", "proof.png");
        }
        return form;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestServer _server;
        public HttpClient Client { get; }
        public Job Job { get; }
        public IFileStorage Files { get; } = Substitute.For<IFileStorage>();

        public Fixture()
        {
            var lab = Lab.Onboard("Completion form test", "HCM", 1);
            var spec = JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .1m, .2m, PrintTechnology.Fdm, 71.61m);
            Job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), spec, 726, new DateOnly(2026, 11, 25));
            var now = DateTime.UtcNow;
            Job.AssignTo(lab.Id, Guid.NewGuid(), now, now.AddHours(12), 1m);
            Job.Accept(now);
            Job.Start(now);
            var jobs = Substitute.For<IJobRepository>();
            jobs.GetByIdAsync(Job.Id, Arg.Any<CancellationToken>()).Returns(Job);
            var labs = Substitute.For<ILabRepository>();
            labs.GetByIdAsync(lab.Id, Arg.Any<CancellationToken>()).Returns(lab);
            var clock = Substitute.For<IDateTimeProvider>();
            clock.UtcNow.Returns(now.AddHours(12));
            var handler = new CompleteJobCommandHandler(jobs, labs, Substitute.For<IUnitOfWork>(), clock);
            var sender = Substitute.For<ISender>();
            sender.Send(Arg.Any<GetMyLabQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success<LabDto?>(LabMappings.ToDto(lab)));
            sender.Send(Arg.Any<GetJobQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(JobMappings.ToDto(Job)));
            sender.Send(Arg.Any<CompleteJobCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => handler.Handle(call.Arg<CompleteJobCommand>(), call.Arg<CancellationToken>()));
            Files.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => $"{call.ArgAt<string>(0)}/{call.ArgAt<string>(1)}");

            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddAuthentication();
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(Policies.RequireLab, policy => policy.RequireRole(Roles.LabManager));
                        options.AddPolicy(Policies.RequireHub, policy => policy.RequireRole(Roles.HubQc));
                    });
                    services.AddSingleton(sender);
                    services.AddSingleton(Files);
                    services.AddSingleton(Substitute.For<IOrderRepository>());
                    services.AddSingleton(Substitute.For<IModelRepository>());
                    services.AddSingleton(Substitute.For<IAuditLogWriter>());
                    services.AddSingleton<IOptions<MinioOptions>>(Options.Create(new MinioOptions()));
                    services.AddControllers().AddApplicationPart(typeof(JobsController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.UseRouting();
                    app.Use(async (context, next) =>
                    {
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                             new Claim(ClaimTypes.Role, Roles.LabManager), new Claim(ClaimTypes.Role, Roles.HubQc)], "Test"));
                        await next();
                    });
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }));
            Client = _server.CreateClient();
        }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
