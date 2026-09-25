namespace Application.HospitalizationStays.Dtos;

public record HospitalizationAdmissionOptionDto(
    Guid ClientPetId,
    string PetName,
    string OwnerName);

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
    string? AdmittedByName,
    bool IsPaid,
    DateTime? PaidAt);

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

public record BillableItemDto(
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
    string? Notes);

public record HospitalizationStayInvoiceDto(
    Guid StayId,
    string PetName,
    string OwnerName,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    string Status,
    decimal DailyRate,
    int BilledDays,
    bool IsPaid,
    DateTime? PaidAt,
    decimal HospitalizationTotal,
    decimal SuppliesTotal,
    decimal MedicationsTotal,
    decimal ProceduresTotal,
    decimal Total,
    IReadOnlyList<BillableItemDto> Supplies,
    IReadOnlyList<BillableItemDto> Medications,
    IReadOnlyList<BillableItemDto> Procedures);
