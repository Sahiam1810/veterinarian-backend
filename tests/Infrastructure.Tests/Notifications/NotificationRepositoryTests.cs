using Domain.Appointments.Entities;
using Domain.Notifications.Entities;
using Infrastructure.Notifications.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Infrastructure.Tests.Notifications;

public sealed class NotificationRepositoryTests
{
    private static readonly DateTime SentAt = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Notification_mapping_allows_a_single_recipient_with_a_check_constraint()
    {
        using var context = CreateOracleModelContext();
        // Los CHECK viven en el modelo de diseño (el mismo que usa la migración).
        var entityType = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Notification))!;

        Assert.True(entityType.FindProperty(nameof(Notification.UserId))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(Notification.ClientId))!.IsNullable);
        Assert.Equal("CLIENT_ID", entityType.FindProperty(nameof(Notification.ClientId))!.GetColumnName());

        var check = Assert.Single(
            entityType.GetCheckConstraints(),
            constraint => constraint.Name == "CK_NOTIFICATIONS_ONE_RECIPIENT");
        Assert.Contains("\"USER_ID\" IS NOT NULL AND \"CLIENT_ID\" IS NULL", check.Sql);
        Assert.Contains("\"USER_ID\" IS NULL AND \"CLIENT_ID\" IS NOT NULL", check.Sql);
    }

    [Fact]
    public async Task Staff_queries_never_return_client_notifications()
    {
        await using var context = CreateContext();
        var appointmentId = await SeedAppointmentAsync(context);
        var userId = Guid.NewGuid();
        var staff = Notification.ForUser(userId, appointmentId, "Vet", SentAt, "Pendiente", "Recordatorio");
        var client = Notification.ForClient(Guid.NewGuid(), appointmentId, "Dueño", SentAt, "Enviado", "Recordatorio1h");
        context.Set<Notification>().AddRange(staff, client);
        await context.SaveChangesAsync();
        var repository = new NotificationRepository(context);

        var all = await repository.GetAllAsync();
        var byAppointment = await repository.GetByAppointmentIdAsync(appointmentId);
        var byUser = await repository.GetByUserIdAsync(userId);

        Assert.Equal(staff.Id, Assert.Single(all).Id);
        Assert.Equal(staff.Id, Assert.Single(byAppointment).Id);
        Assert.Equal(staff.Id, Assert.Single(byUser).Id);
        Assert.Null(await repository.GetByIdAsync(client.Id));
        Assert.NotNull(await repository.GetByIdAsync(staff.Id));
    }

    [Fact]
    public async Task Notified_appointments_include_client_notifications_so_reminders_are_not_repeated()
    {
        await using var context = CreateContext();
        var appointmentId = await SeedAppointmentAsync(context);
        context.Set<Notification>().Add(
            Notification.ForClient(Guid.NewGuid(), appointmentId, "Dueño", SentAt, "Enviado", "Recordatorio1h"));
        await context.SaveChangesAsync();
        var repository = new NotificationRepository(context);

        var notified = await repository.GetNotifiedAppointmentIdsAsync([appointmentId], "Recordatorio1h");

        Assert.Equal(appointmentId, Assert.Single(notified));
    }

    [Fact]
    public async Task DeleteByUserId_does_not_touch_client_notifications()
    {
        await using var context = CreateContext();
        var appointmentId = await SeedAppointmentAsync(context);
        var userId = Guid.NewGuid();
        var client = Notification.ForClient(Guid.NewGuid(), appointmentId, "Dueño", SentAt, "Enviado", "Recordatorio1h");
        context.Set<Notification>().AddRange(
            Notification.ForUser(userId, appointmentId, "Vet", SentAt, "Pendiente", "Recordatorio"),
            client);
        await context.SaveChangesAsync();
        var repository = new NotificationRepository(context);

        await repository.DeleteByUserIdAsync(userId);
        await context.SaveChangesAsync();

        var remaining = Assert.Single(await context.Set<Notification>().ToListAsync());
        Assert.Equal(client.Id, remaining.Id);
    }

    private static async Task<Guid> SeedAppointmentAsync(VeterinaryDbContext context)
    {
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            SentAt.AddHours(1), SentAt.AddHours(2), null);
        context.Set<Appointment>().Add(appointment);
        await context.SaveChangesAsync();
        return appointment.Id;
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
