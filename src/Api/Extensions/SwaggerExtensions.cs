using Microsoft.OpenApi;

namespace Api.Extensions;

public static class SwaggerExtensions
{
    private const string SchemeName = "Bearer";

    public static IServiceCollection AddApiSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(SchemeName, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = SchemeName,
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token JWT de acceso. Ingresa únicamente el token: el prefijo \"Bearer \" se agrega automáticamente."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(SchemeName, document, null),
                    []
                }
            });
        });

        return services;
    }
}
