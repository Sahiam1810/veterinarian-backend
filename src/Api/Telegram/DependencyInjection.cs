using Api.Telegram.Security;

namespace Api.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramApi(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramWebhookSecretValidator, TelegramWebhookSecretValidator>();
        return services;
    }
}
