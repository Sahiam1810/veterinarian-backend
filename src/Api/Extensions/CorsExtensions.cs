using Api.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Extensions;
public static class CorsExtensions
{
    private const string PolicyName = "HelpDeskFrontend";

    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateOnStart();

        var origins = configuration
            .GetSection($"{CorsOptions.SectionName}:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(
            PolicyName,
            policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }

    public static IApplicationBuilder UseApiCors(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}