using Domain.Verification.Entities;
using Domain.Verification.Enums;

namespace Application.Verification.Abstractions;

public interface IAppointmentActionVerificationSessionRepository
{
    Task<AppointmentActionVerificationSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<AppointmentActionVerificationSession?> GetActiveByAppointmentAndActionAsync(
        Guid appointmentId,
        AppointmentVerificationAction action,
        CancellationToken cancellationToken);

    Task AddAsync(
        AppointmentActionVerificationSession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        AppointmentActionVerificationSession session,
        CancellationToken cancellationToken);
}
