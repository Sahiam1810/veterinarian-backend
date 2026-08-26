using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Api.Common.Security;
using Api.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateOnStart();

        var settings = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => CreatePartition(
                    GetPartitionKey(context),
                    settings.GlobalPermitLimit,
                    settings.GlobalWindowSeconds));
            options.AddPolicy(RateLimitPolicies.Login, context =>
                CreatePartition(GetPartitionKey(context), settings.LoginPermitLimit, settings.LoginWindowSeconds));
            options.AddPolicy(RateLimitPolicies.Refresh, context =>
                CreatePartition(GetPartitionKey(context), settings.RefreshPermitLimit, settings.RefreshWindowSeconds));
            options.AddPolicy(RateLimitPolicies.Register, context =>
                CreatePartition(GetPartitionKey(context), settings.RegisterPermitLimit, settings.RegisterWindowSeconds));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds)
                            .ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json";
                await JsonSerializer.SerializeAsync(
                    context.HttpContext.Response.Body,
                    new
                    {
                        type = "https://httpstatuses.com/429",
                        title = "Too Many Requests",
                        status = StatusCodes.Status429TooManyRequests,
                        code = "RateLimit.Exceeded"
                    },
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app) =>
        app.UseRateLimiter();

    private static RateLimitPartition<string> CreatePartition(
        string key,
        int permitLimit,
        int windowSeconds) =>
        RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });

    private static string GetPartitionKey(HttpContext context) =>
        context.User.FindFirst("sub")?.Value is { Length: > 0 } subject
            ? $"user:{subject}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}