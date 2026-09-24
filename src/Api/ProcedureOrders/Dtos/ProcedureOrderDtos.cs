namespace Api.ProcedureOrders.Dtos;

public record ProcedureOrderItemDto(
    Guid Id,
    Guid ProcedureOrderId,
    Guid ProcedureId,
    string? ProcedureName,
    decimal UnitPrice,
    string? Notes);

public record ProcedureOrderDto(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid? AppointmentId,
    Guid? HospitalizationStayId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    string Status,
    string? ResultFileUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ProcedureOrderItemDto> Items);

public record CreateProcedureOrderItemDto(
    Guid ProcedureId,
    string? Notes);

public record CreateProcedureOrderDto(
    Guid ClientPetId,
    Guid? AppointmentId,
    Guid? HospitalizationStayId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<CreateProcedureOrderItemDto>? Items);

public record CompleteProcedureOrderDto(
    string? ResultFileUrl = null);

public record PendingProcedureOrderDto(
    Guid Id,
    string PetName,
    string OwnerName,
    Guid? AppointmentId,
    Guid? HospitalizationStayId,
    bool IsInHouse,
    string Status,
    string? ResultFileUrl,
    DateTime CreatedAt,
    List<ProcedureOrderItemDto> Items);
