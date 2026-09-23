using Api.HospitalizationStays.Dtos;
using Domain.HospitalizationStays.Entities;

namespace Api.HospitalizationStays.Mappings;

public static class HospitalizationStayMappings
{
    public static ApiHospitalizationStayDto ToDto(this HospitalizationStay stay, string? admittedByUserName = null)
    {
        return new ApiHospitalizationStayDto(
            stay.Id,
            stay.ClientPetId,
            stay.ClientPet?.Pet?.Name?.Value,
            stay.ClientPet?.Client?.FullName?.Value,
            stay.AppointmentId,
            stay.AdmittedByUserId,
            admittedByUserName,
            stay.FechaIngreso,
            stay.FechaAlta,
            stay.Estado,
            stay.Motivo);
    }

    public static ApiHospitalizationNoteDto ToDto(
        this HospitalizationNote note,
        string? autorUserName = null,
        string? entregadoAUserName = null)
    {
        return new ApiHospitalizationNoteDto(
            note.Id,
            note.StayId,
            note.AutorUserId,
            autorUserName,
            note.FechaHora,
            note.Nota,
            note.EntregadoAUserId,
            entregadoAUserName);
    }
}
