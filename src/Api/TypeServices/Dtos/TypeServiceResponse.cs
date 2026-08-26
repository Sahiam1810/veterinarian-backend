namespace Api.TypeServices.Dtos;

public sealed record TypeServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);
