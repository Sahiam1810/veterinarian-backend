namespace Api.Services.Dtos;

public sealed record ServiceResponse(
    Guid Id,
    Guid TypeServiceId,
    string? TypeServiceName,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt);
