using Api.Notifications.Hubs;
using Api.Notifications.Realtime;
using Application.Notifications.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsRealtime(
        this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, PersonIdUserIdProvider>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

        return services;
    }
}
