using Application.Common.Abstractions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record CreateAppointmentByIdentificationCommand(
    string IdentificationNumber,
    Guid PetId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    string? Notes,
    string? RequesterPhoneNumber,
    string IdempotencyKey) : IRequest<Appointment>;

public sealed class CreateAppointmentByIdentificationCommandHandler(
    IUnitOfWork unitOfWork,
    ISender sender)
    : IRequestHandler<CreateAppointmentByIdentificationCommand, Appointment>
{
    public async Task<Appointment> Handle(
        CreateAppointmentByIdentificationCommand request,
        CancellationToken cancellationToken)
    {
        var userAccountId = await RescheduleAppointmentByIdentificationCommandHandler
            .ResolveUserAccountIdAsync(
                unitOfWork,
                request.IdentificationNumber,
                cancellationToken);

        return await sender.Send(
            new CreateMyAppointmentCommand(
                userAccountId,
                request.PetId,
                request.VeterinarianId,
                request.ServiceId,
                request.ScheduledStartUtc,
                request.Notes,
                request.RequesterPhoneNumber,
                request.IdempotencyKey),
            cancellationToken);
    }
}
