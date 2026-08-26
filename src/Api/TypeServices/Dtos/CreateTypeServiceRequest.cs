namespace Api.TypeServices.Dtos;

public sealed record CreateTypeServiceRequest(
    string Name,
    string? Description);
