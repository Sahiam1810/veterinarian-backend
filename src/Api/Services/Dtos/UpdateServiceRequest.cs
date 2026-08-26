namespace Api.Services.Dtos;

public sealed record UpdateServiceRequest(
    Guid TypeServiceId,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive);
