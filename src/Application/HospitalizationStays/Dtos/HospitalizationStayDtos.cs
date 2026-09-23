using Domain.HospitalizationStays.Entities;

namespace Api.HospitalizationStays.Dtos;

public record ApiHospitalizationStayDto(
    Guid Id,
    Guid ClientPetId,
    string? PetName,
    string? OwnerName,
    Guid? AppointmentId,
    Guid AdmittedByUserId,
    string? AdmittedByUserName,
    DateTime FechaIngreso,
    DateTime? FechaAlta,
    HospitalizationStayStatus Estado,
    string Motivo);

public record ApiHospitalizationNoteDto(
    Guid Id,
    Guid StayId,
    Guid AutorUserId,
    string? AutorUserName,
    DateTime FechaHora,
    string Nota,
    Guid? EntregadoAUserId,
    string? EntregadoAUserName);

public record HospitalizationStaffUserDto(
    Guid Id,
    string Name,
    string RoleName);
