using Api.Appointments.Dtos;
using Application.Appointments.UseCases;
using Application.Common.Models;
using Application.MedicalRecords.UseCases;
using Domain.Appointments.Entities;

namespace Api.Appointments.Mappings;

public static class AppointmentMappings
{
    public static CreateAppointmentCommand ToCommand(
        this CreateAppointmentRequest request)
    {
        return new CreateAppointmentCommand(
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes,
            request.RequesterPhoneNumber);
    }

    public static CreateAppointmentMedicalRecordCommand ToCommand(
        this CreateAppointmentMedicalRecordRequest request,
        Guid appointmentId,
        Guid actorUserAccountId,
        bool enforceVeterinarianOwnership)
    {
        IReadOnlyCollection<CreateAppointmentMedicalRecordVaccinationItem>? vaccinations = null;
        if (request.Vaccinations is not null)
        {
            vaccinations = request.Vaccinations
                .Select(v => new CreateAppointmentMedicalRecordVaccinationItem(
                    v.VaccineName,
                    v.DoseNumber,
                    v.ApplicationDate,
                    v.NextDoseDate))
                .ToArray();
        }

        return new CreateAppointmentMedicalRecordCommand(
            appointmentId,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature,
            vaccinations,
            actorUserAccountId,
            enforceVeterinarianOwnership);
    }

    public static CreateAppointmentMedicalRecordResponse ToResponse(
        this CreateAppointmentMedicalRecordResult result)
    {
        return new CreateAppointmentMedicalRecordResponse(
            result.MedicalRecordId,
            result.AppointmentId,
            result.VaccinationIds);
    }

    public static UpdateAppointmentCommand ToCommand(
        this UpdateAppointmentRequest request,
        Guid id,
        Guid actorUserAccountId,
        bool enforceVeterinarianOwnership)
    {
        return new UpdateAppointmentCommand(
            id,
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes,
            actorUserAccountId,
            enforceVeterinarianOwnership);
    }

    public static UpdateAppointmentStatusCommand ToCommand(
        this UpdateAppointmentStatusRequest request,
        Guid appointmentId,
        Guid actorUserAccountId,
        bool enforceVeterinarianOwnership)
    {
        return new UpdateAppointmentStatusCommand(
            appointmentId,
            request.StatusId,
            request.Comment,
            actorUserAccountId,
            enforceVeterinarianOwnership);
    }

    public static AppointmentResponse ToResponse(
        this Appointment entity)
    {
        return new AppointmentResponse(
            entity.Id,
            entity.ClientPetId,
            entity.ClientPet?.Pet?.Name?.Value,
            entity.VeterinarianId,
            entity.Veterinarian?.User?.FullName,
            entity.ServiceId,
            entity.Service?.Name,
            entity.StatusId,
            entity.Status?.Name,
            entity.AvailabilityId,
            AsUtc(entity.ScheduledStart),
            AsUtc(entity.ScheduledEnd),
            entity.Notes,
            entity.RequesterPhoneNumber?.Value,
            entity.CreatedAt);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static IReadOnlyCollection<AppointmentResponse> ToResponse(
        this IReadOnlyCollection<Appointment> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }

    public static PaginatedAppointmentResponse ToResponse(
        this PaginatedResult<Appointment> result)
    {
        return new PaginatedAppointmentResponse(
            result.Items.ToResponse(),
            result.Pagination);
    }
}
