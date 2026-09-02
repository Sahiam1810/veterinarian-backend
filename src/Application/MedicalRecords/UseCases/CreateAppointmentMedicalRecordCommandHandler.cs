using Application.Appointments;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MedicalRecords.Entities;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed class CreateAppointmentMedicalRecordCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentMedicalRecordCommand, CreateAppointmentMedicalRecordResult>
{
    public async Task<CreateAppointmentMedicalRecordResult> Handle(
        CreateAppointmentMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        await AppointmentVeterinarianOwnership.EnsureAsync(
            unitOfWork,
            appointment,
            request.ActorUserAccountId,
            request.EnforceVeterinarianOwnership,
            cancellationToken);

        if (await unitOfWork.MedicalRecordsRepository.ExistsByAppointmentIdAsync(
                request.AppointmentId,
                cancellationToken))
        {
            throw new ConflictException("Ya existe una historia clínica para esta cita.");
        }

        var diagnostic = await unitOfWork.DiagnosticsRepository.GetByIdAsync(
            request.DiagnosticId,
            cancellationToken)
            ?? throw new NotFoundException("Diagnóstico no encontrado.");

        if (!diagnostic.IsActive)
        {
            throw new BadRequestException("El diagnóstico indicado no está activo.");
        }

        var record = new MedicalRecord(
            appointment.ClientPetId,
            appointment.Id,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature);

        await unitOfWork.MedicalRecordsRepository.AddAsync(
            record,
            cancellationToken);

        var vaccinationIds = new List<Guid>();
        if (request.Vaccinations is { Count: > 0 })
        {
            foreach (var item in request.Vaccinations)
            {
                var vaccination = new Vaccination(
                    appointment.ClientPetId,
                    record.Id,
                    item.VaccineName,
                    item.DoseNumber,
                    item.ApplicationDate,
                    item.NextDoseDate);

                await unitOfWork.VaccinationsRepository.AddAsync(
                    vaccination,
                    cancellationToken);

                vaccinationIds.Add(vaccination.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAppointmentMedicalRecordResult(
            record.Id,
            appointment.Id,
            vaccinationIds);
    }
}
