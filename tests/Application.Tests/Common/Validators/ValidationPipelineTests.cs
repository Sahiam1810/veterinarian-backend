using Application;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Login;
using Application.Security.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.Tests.Common.Validators;

public sealed class ValidationPipelineTests
{
    [Fact]
    public async Task Send_invalid_login_command_throws_validation_exception_without_calling_authentication_service()
    {
        var authenticationService = new ControlledAuthenticationService();
        using var serviceProvider = BuildServiceProvider(authenticationService);
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new LoginCommand("correo-invalido", "secret")));

        Assert.Equal(0, authenticationService.LoginCallCount);
    }

    [Fact]
    public async Task Send_valid_login_command_calls_authentication_service_once_and_returns_success()
    {
        var authenticationService = new ControlledAuthenticationService();
        using var serviceProvider = BuildServiceProvider(authenticationService);
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new LoginCommand("cliente@huellitas.test", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, authenticationService.LoginCallCount);
    }

    private static ServiceProvider BuildServiceProvider(ControlledAuthenticationService authenticationService)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddLogging();
        services.AddSingleton<IAuthenticationService>(authenticationService);

        return services.BuildServiceProvider();
    }

    private sealed class ControlledAuthenticationService : IAuthenticationService
    {
        public int LoginCallCount { get; private set; }

        public Task<Result<AuthenticationTokens>> RegisterAsync(
            string fullName,
            string email,
            string userName,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<AuthenticationTokens>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            LoginCallCount++;

            return Task.FromResult(Result<AuthenticationTokens>.Success(
                new AuthenticationTokens(
                    "access-token",
                    new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    "refresh-token",
                    new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero))));
        }

        public Task<Result<AuthenticationTokens>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<CurrentProfile>> GetCurrentProfileAsync(
            Guid userAccountId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
