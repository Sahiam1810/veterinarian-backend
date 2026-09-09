using System.Net.Http.Headers;
using System.Text;
using Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Tests.Security;

public sealed class SwaggerBasicAuthTests
{
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SWAGGER_USER"] = "admin",
            ["SWAGGER_PASSWORD"] = "admin123"
        })
        .Build();

    [Fact]
    public async Task InvokeAsync_NonSwaggerPath_CallsNextMiddlewareWithoutAuth()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SwaggerBasicAuthMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/Appointments";

        // Act
        await middleware.InvokeAsync(httpContext, _configuration);

        // Assert
        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SwaggerPathWithoutAuthorizationHeader_Returns401AndWWWAuthenticateHeader()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SwaggerBasicAuthMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/swagger/index.html";

        // Act
        await middleware.InvokeAsync(httpContext, _configuration);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.True(httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
        Assert.Contains("Basic realm=\"Swagger UI\"", httpContext.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public async Task InvokeAsync_SwaggerPathWithInvalidCredentials_Returns401()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SwaggerBasicAuthMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/swagger/v1/swagger.json";

        var invalidAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrongpass"));
        httpContext.Request.Headers.Authorization = $"Basic {invalidAuth}";

        // Act
        await middleware.InvokeAsync(httpContext, _configuration);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SwaggerPathWithValidCredentials_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SwaggerBasicAuthMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/swagger/index.html";

        var validAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:admin123"));
        httpContext.Request.Headers.Authorization = $"Basic {validAuth}";

        // Act
        await middleware.InvokeAsync(httpContext, _configuration);

        // Assert
        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }
}
