using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Serilog;
using PrintGrid.Api.BackgroundJobs;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Api.Extensions;
using PrintGrid.Api.Hubs;
using PrintGrid.Api.Middleware;
using PrintGrid.Infrastructure.Shared;
using PrintGrid.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PrintGrid"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var redisConnection = builder.Configuration.GetConnectionString("Redis")!;

builder.Services.AddControllers();
builder.Services.AddApiDocumentation();
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.Configure<ModelUploadOptions>(builder.Configuration.GetSection(ModelUploadOptions.SectionName));

builder.Services.AddCors(options => options.AddPolicy("PrintGridSpa", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddApplicationModules(builder.Configuration);

// Handlers that depend on API-layer infrastructure (SignalR hub contexts) live here,
// not in a module — register the composition-root assembly too or they silently never run.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<AnalyzeModelJob>();
builder.Services.AddScoped<EmailDeliveryJob>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRedis(redisConnection, name: "redis");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("PrintGridSpa");
app.UseAuthentication();
app.UseAuthorization();

// Demo seeding removed (26/09): the product runs on real data only.
// A dedicated, explicit dev/demo data tool is scheduled as the W1 "Seed lại"
// task in planning/Plan-ToanDu-An.xlsx — never auto-run at startup.

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");
app.MapHub<LabHub>("/hubs/labs");
app.MapHealthChecks("/health");
app.MapHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

RecurringJob.AddOrUpdate<EmailDeliveryJob>(
    "email-outbox-delivery",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Minutely());

app.Run();

/// <summary>Exposed for WebApplicationFactory in PrintGrid.IntegrationTests.</summary>
public partial class Program { }
