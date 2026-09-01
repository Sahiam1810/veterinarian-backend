using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class CreateMedicalRecordCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicalRecordCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        if (appointment.ClientPetId != request.ClientPetId)
        {
            throw new BadRequestException("La mascota no corresponde a la cita.");
        }

        if (request.UserAccountId.HasValue)
        {
            var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
                request.UserAccountId.Value, cancellationToken);

            if (account is not null)
            {
                var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(
                    account.UserId, cancellationToken);

                if (veterinarian is not null && appointment.VeterinarianId != veterinarian.Id)
                {
                    throw new UnauthorizedException("La cita no está asignada al veterinario autenticado.");
                }
            }
        }

        var diagnostic = await unitOfWork.DiagnosticsRepository.GetByIdAsync(
            request.DiagnosticId, cancellationToken)
            ?? throw new NotFoundException("Diagnóstico no encontrado.");

        var record = new MedicalRecord(
            request.ClientPetId,
            request.AppointmentId,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature);

        await unitOfWork.MedicalRecordsRepository.AddAsync(
            record,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return record.Id;
    }
}
