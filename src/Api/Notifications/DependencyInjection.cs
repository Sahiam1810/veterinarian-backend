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
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddScoped<IChatRealtimeNotifier, SignalRChatRealtimeNotifier>();

        return services;
    }
}
