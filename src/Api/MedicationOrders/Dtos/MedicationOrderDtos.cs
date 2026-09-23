namespace Api.MedicationOrders.Dtos;

public record MedicationOrderItemDto(
    Guid Id,
    Guid MedicationOrderId,
    Guid MedicationId,
    string? MedicationName,
    string? Notes);

public record MedicationOrderDto(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<MedicationOrderItemDto> Items);

public record CreateMedicationOrderItemDto(
    Guid MedicationId,
    string? Notes);

public record CreateMedicationOrderDto(
    Guid ClientPetId,
    Guid AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<CreateMedicationOrderItemDto>? Items);
