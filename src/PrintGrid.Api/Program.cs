using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Api.Extensions;
using PrintGrid.Api.Hubs;
using PrintGrid.Api.Middleware;
using PrintGrid.Infrastructure.Shared;

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

builder.Services.AddCors(options => options.AddPolicy("PrintGridSpa", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddApplicationModules(builder.Configuration);

builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

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

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");
app.MapHub<LabHub>("/hubs/labs");
app.MapHealthChecks("/health");
app.MapHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

app.Run();
