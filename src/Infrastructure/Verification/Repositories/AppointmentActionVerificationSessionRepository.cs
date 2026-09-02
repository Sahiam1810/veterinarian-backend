using Application.Verification.Abstractions;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Verification.Repositories;

public sealed class AppointmentActionVerificationSessionRepository(VeterinaryDbContext context)
    : IAppointmentActionVerificationSessionRepository
{
    public Task<AppointmentActionVerificationSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        context.Set<AppointmentActionVerificationSession>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<AppointmentActionVerificationSession?> GetActiveByAppointmentAndActionAsync(
        Guid appointmentId,
        AppointmentVerificationAction action,
        CancellationToken cancellationToken) =>
        context.Set<AppointmentActionVerificationSession>()
            .Where(s =>
                s.AppointmentId == appointmentId &&
                s.Action == action &&
                s.Status == VerificationSessionStatus.AwaitingOtp)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        AppointmentActionVerificationSession session,
        CancellationToken cancellationToken) =>
        await context.Set<AppointmentActionVerificationSession>()
            .AddAsync(session, cancellationToken);

    public Task UpdateAsync(
        AppointmentActionVerificationSession session,
        CancellationToken cancellationToken)
    {
        context.Set<AppointmentActionVerificationSession>().Update(session);
        return Task.CompletedTask;
    }
}
