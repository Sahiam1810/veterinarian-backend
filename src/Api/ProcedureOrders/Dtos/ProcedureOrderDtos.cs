using Application.Common.Models;

namespace Api.ProcedureOrders.Dtos;

public record ProcedureOrderItemDto(
    Guid Id,
    Guid ProcedureOrderId,
    Guid ProcedureId,
    string? ProcedureName,
    string? Notes,
    decimal? UnitPrice);

public record ProcedureOrderDto(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid? AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    string Status,
    string? ResultFileUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ProcedureOrderItemDto> Items,
    Guid? HospitalizationStayId = null);

public record CreateProcedureOrderItemDto(
    Guid ProcedureId,
    string? Notes);

public record CreateProcedureOrderDto(
    Guid ClientPetId,
    Guid? AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<CreateProcedureOrderItemDto>? Items,
    Guid? HospitalizationStayId = null);

public record CompleteProcedureOrderDto(
    string? ResultFileUrl = null);

public record PendingProcedureOrderDto(
    Guid Id,
    string PetName,
    string OwnerName,
    Guid? AppointmentId,
    bool IsInHouse,
    string Status,
    string? ResultFileUrl,
    DateTime CreatedAt,
    List<ProcedureOrderItemDto> Items,
    Guid? HospitalizationStayId = null);

public record ClinicalResultItemDto(
    Guid Id,
    Guid ProcedureId,
    string? ProcedureName,
    string? Notes,
    decimal? UnitPrice);

public record ClinicalResultDto(
    Guid ProcedureOrderId,
    Guid ClientPetId,
    Guid? AppointmentId,
    Guid? HospitalizationStayId,
    string PetName,
    string? Species,
    string? Breed,
    string OwnerName,
    string? OwnerPhone,
    string? ProcedureName,
    string? Notes,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    Guid VeterinarianId,
    string? VeterinarianName,
    decimal? UnitPrice,
    string ResultFileUrl,
    IReadOnlyCollection<ClinicalResultItemDto> Items);

public record PaginatedClinicalResultResponse(
    IReadOnlyCollection<ClinicalResultDto> Items,
    PaginationMetadata Pagination);
