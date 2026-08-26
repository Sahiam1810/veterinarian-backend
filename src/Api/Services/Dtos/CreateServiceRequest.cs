namespace Api.Services.Dtos;

public sealed record CreateServiceRequest(
    Guid TypeServiceId,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive = true);
