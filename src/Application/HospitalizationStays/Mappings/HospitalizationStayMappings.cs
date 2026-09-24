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
            admittedByName);
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
            note.FechaHora,
            note.Nota,
            note.EntregadoAUserId,
            handedToName);
    }
}
