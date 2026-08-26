using MediatR;

namespace Application.TypeServices.UseCases;

public sealed record CreateTypeServiceCommand(
    string Name,
    string? Description) : IRequest<Guid>;
