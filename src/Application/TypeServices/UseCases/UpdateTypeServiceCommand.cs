using MediatR;

namespace Application.TypeServices.UseCases;

public sealed record UpdateTypeServiceCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<bool>;
