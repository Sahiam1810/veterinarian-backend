using Api.Agent;
using Api.Common.Errors;
using Api.Configuration;
using Api.Extensions;
using Api.Telegram;
using Api.Notifications;
using Api.Notifications.Hubs;

using Application;
using Infrastructure;
using Serilog;

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAgentApi();
builder.Services.AddTelegramApi();
builder.Services.AddNotificationsRealtime();
builder.Services.AddApiCors(builder.Configuration);

builder.Services.AddJwtAuthentication();
builder.Services.AddApiAuthorizationPolicies();

builder.Services.AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseApiCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseApiRateLimiting();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();
