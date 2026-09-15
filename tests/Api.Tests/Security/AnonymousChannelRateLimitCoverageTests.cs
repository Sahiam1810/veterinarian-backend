using System.Reflection;
using Api.Appointments.Controllers;
using Api.Auth.Controllers;
using Api.Clients.Controllers;
using Api.Common.Security;
using Api.ContactVerification.Controllers;
using Api.Owners.Controllers;
using Api.Telegram.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace Api.Tests.Security;

// Tarea 6.1: Anexo A del kickoff Etapa 6 cubierto al 100% (policy en atributo).
public sealed class AnonymousChannelRateLimitCoverageTests
{
    [Theory]
    [InlineData(typeof(AuthController), "Login", RateLimitPolicies.Login)]
    [InlineData(typeof(AuthController), "Refresh", RateLimitPolicies.Refresh)]
    [InlineData(typeof(ClientsController), "GetByIdentification", RateLimitPolicies.ClientIdentificationLookup)]
    [InlineData(typeof(ClientsController), "GetByPhone", RateLimitPolicies.ClientPhoneLookup)]
    [InlineData(typeof(ContactVerificationController), "RequestEmail", RateLimitPolicies.ContactEmailRequest)]
    [InlineData(typeof(ContactVerificationController), "ConfirmEmail", RateLimitPolicies.ContactEmailConfirm)]
    [InlineData(typeof(BotOwnersController), "Register", RateLimitPolicies.BotOwnerRegistration)]
    [InlineData(typeof(MyAppointmentsController), "RequestCode", RateLimitPolicies.AppointmentOtpRequest)]
    [InlineData(typeof(MyAppointmentsController), "ConfirmCode", RateLimitPolicies.AppointmentOtpConfirm)]
    [InlineData(typeof(TelegramWebhookController), "Receive", RateLimitPolicies.TelegramWebhook)]
    public void Endpoint_has_expected_rate_limit_policy(Type controller, string methodName, string policy)
    {
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            ?? controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == methodName);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>()
            ?? controller.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(policy, attribute!.PolicyName);
    }

    [Fact]
    public void TelegramRegistrationController_has_class_level_telegram_registration_policy()
    {
        var attribute = typeof(TelegramRegistrationController)
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(RateLimitPolicies.TelegramRegistration, attribute!.PolicyName);

        var methods = typeof(TelegramRegistrationController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Contains(methods, m => m.GetCustomAttribute<HttpGetAttribute>() is not null);
        Assert.Contains(methods, m => m.GetCustomAttribute<HttpPostAttribute>() is not null);
    }

    [Fact]
    public void RateLimitPolicies_declares_all_anexo_a_constants()
    {
        string[] expected =
        [
            RateLimitPolicies.Login,
            RateLimitPolicies.Refresh,
            RateLimitPolicies.ClientPhoneLookup,
            RateLimitPolicies.ClientIdentificationLookup,
            RateLimitPolicies.ContactEmailRequest,
            RateLimitPolicies.ContactEmailConfirm,
            RateLimitPolicies.BotOwnerRegistration,
            RateLimitPolicies.TelegramRegistration,
            RateLimitPolicies.TelegramWebhook,
            RateLimitPolicies.AppointmentOtpRequest,
            RateLimitPolicies.AppointmentOtpConfirm
        ];

        Assert.Equal(expected.Length, expected.Distinct(StringComparer.Ordinal).Count());
    }
}
