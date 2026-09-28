using Application.HospitalizationStays.Dtos;
using Domain.HospitalizationStays.Entities;

namespace Application.HospitalizationStays.Mappings;

public static class HospitalizationStayMappings
{
    public const string ActiveStatusLabel = "Activa";
    public const string DischargedStatusLabel = "Dada de alta";

    public static string ToStatusLabel(this HospitalizationStayStatus status) =>
        status == HospitalizationStayStatus.Activa ? ActiveStatusLabel : DischargedStatusLabel;

    public static ApiHospitalizationStayDto ToDto(this HospitalizationStay stay, string? admittedByName = null)
    {
        return new ApiHospitalizationStayDto(
            stay.Id,
            stay.ClientPetId,
            stay.ClientPet?.Pet?.Name?.Value,
            stay.ClientPet?.Client?.FullName?.Value,
            stay.AppointmentId,
            stay.FechaIngreso,
            stay.FechaAlta,
            stay.Estado.ToStatusLabel(),
            stay.Motivo,
            stay.AdmittedByUserId,
            admittedByName,
            stay.IsPaid,
            stay.PaidAt);
    }

    public static ApiHospitalizationNoteDto ToDto(
        this HospitalizationNote note,
        string? authorName = null,
        string? handedToName = null)
    {
        return new ApiHospitalizationNoteDto(
            note.Id,
            note.StayId,
            note.AutorUserId,
            authorName,
            // Oracle TIMESTAMP no conserva DateTimeKind al leerlo. Las notas
            // se almacenan en UTC, por lo que debemos conservar esa semántica
            // para que el cliente las convierta a la zona horaria local.
            DateTime.SpecifyKind(note.FechaHora, DateTimeKind.Utc),
            note.Nota,
            note.EntregadoAUserId,
            handedToName);
    }
}
