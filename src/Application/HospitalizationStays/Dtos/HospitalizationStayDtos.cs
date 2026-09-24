namespace Application.HospitalizationStays.Dtos;

// Contrato JSON acordado con el frontend (Ticket A). Nunca se expone la entidad de dominio.
public record ApiHospitalizationStayDto(
    Guid Id,
    Guid ClientPetId,
    string? PetName,
    string? OwnerName,
    Guid? AppointmentId,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    string Status,
    string Motivo,
    Guid AdmittedByUserId,
    string? AdmittedByName);

public record ApiHospitalizationNoteDto(
    Guid Id,
    Guid StayId,
    Guid AuthorUserId,
    string? AuthorName,
    DateTime CreatedAt,
    string Nota,
    Guid? HandedToUserId,
    string? HandedToName);

public record HospitalizationStaffUserDto(
    Guid Id,
    string Name,
    string RoleName);

// DTO para el selector de mascotas en la interfaz de admisión hospitalaria.
// Expone únicamente los datos necesarios; nunca la entidad de dominio subyacente.
public record HospitalizationAdmissionOptionDto(
    Guid ClientPetId,
    string PetName,
    string OwnerName);
