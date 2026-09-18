using Application.Appointments;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
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

        // AGENDADA o CONFIRMADA (paciente ya hizo check-in en recepción) admiten
        // registrar historia clinica; No Asistio/Cancelada/Atendida quedan cerradas.
        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");
        if (!string.Equals(currentStatus.Name, AppointmentStatusNames.Agendada, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(currentStatus.Name, AppointmentStatusNames.Confirmada, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "No se puede registrar la historia clínica de una cita que no está agendada o confirmada.");
        }

        if (await unitOfWork.MedicalRecordsRepository.ExistsByAppointmentIdAsync(
                request.AppointmentId,
                cancellationToken))
        {
            throw new ConflictException("Ya existe una historia clínica para esta cita.");
        }

        var targetStatus = (await unitOfWork.StatusAppointmentsRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(item =>
                string.Equals(item.Name, "ATENDIDA", StringComparison.OrdinalIgnoreCase))
            ?? throw new ConflictException("No está configurado el estado ATENDIDA.");

        AppointmentStatusTransitionRules.EnsureValidTransition(
            currentStatus.Name,
            targetStatus.Name,
            null);

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

        var history = new AppointmentStatusHistory(
            appointment.Id,
            targetStatus.Id,
            appointment.ClientPetId,
            "Historia clínica registrada por el veterinario.");

        await unitOfWork.AppointmentStatusHistoriesRepository.AddAsync(
            history,
            cancellationToken);

        appointment.Update(
            appointment.ClientPetId,
            appointment.VeterinarianId,
            appointment.ServiceId,
            targetStatus.Id,
            appointment.AvailabilityId,
            appointment.ScheduledStart,
            appointment.ScheduledEnd,
            appointment.Notes,
            appointment.ConsultingRoom);

        await unitOfWork.AppointmentsRepository.UpdateAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAppointmentMedicalRecordResult(
            record.Id,
            appointment.Id,
            vaccinationIds);
    }
}
