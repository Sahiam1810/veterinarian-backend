using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record RescheduleAppointmentByIdentificationCommand(
    Guid AppointmentId,
    string IdentificationNumber,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string RequesterPhoneNumber,
    string? Notes) : IRequest;

public sealed class RescheduleAppointmentByIdentificationCommandHandler(
    IUnitOfWork unitOfWork,
    ISender sender)
    : IRequestHandler<RescheduleAppointmentByIdentificationCommand>
{
    public async Task Handle(
        RescheduleAppointmentByIdentificationCommand request,
        CancellationToken cancellationToken)
    {
        var userAccountId = await ResolveUserAccountIdAsync(
            unitOfWork,
            request.IdentificationNumber,
            cancellationToken);

        await sender.Send(
            new RescheduleMyAppointmentCommand(
                request.AppointmentId,
                userAccountId,
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.RequesterPhoneNumber,
                request.Notes),
            cancellationToken);
    }

    internal static async Task<Guid> ResolveUserAccountIdAsync(
        IUnitOfWork unitOfWork,
        string identificationNumber,
        CancellationToken cancellationToken)
    {
        var client = await GetAppointmentsByIdentificationQueryHandler.ResolveClientOrNotFoundAsync(
            unitOfWork,
            identificationNumber,
            cancellationToken);

        var account = await unitOfWork.UserAccountsRepository.GetByUserIdAsync(
            client.UserId,
            cancellationToken)
            ?? throw new NotFoundException(
                "El cliente de esa cédula no tiene cuenta operativa para citas.");

        if (!string.Equals(account.Status, "Activo", StringComparison.Ordinal))
        {
            throw new ConflictException("La cuenta del cliente no está activa.");
        }

        return account.Id;
    }
}
