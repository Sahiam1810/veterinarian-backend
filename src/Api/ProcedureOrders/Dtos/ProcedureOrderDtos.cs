namespace Api.ProcedureOrders.Dtos;

public record ProcedureOrderItemDto(
    Guid Id,
    Guid ProcedureOrderId,
    Guid ProcedureId,
    string? ProcedureName,
    string? Notes);

public record ProcedureOrderDto(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid AppointmentId,
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
    Guid AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<CreateProcedureOrderItemDto>? Items);

public record CompleteProcedureOrderDto(
    string? ResultFileUrl = null);
