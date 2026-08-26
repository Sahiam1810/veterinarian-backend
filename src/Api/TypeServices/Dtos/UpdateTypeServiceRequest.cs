namespace Api.TypeServices.Dtos;

public sealed record UpdateTypeServiceRequest(
    string Name,
    string? Description);
