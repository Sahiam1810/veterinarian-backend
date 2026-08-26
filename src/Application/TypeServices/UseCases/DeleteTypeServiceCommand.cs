using MediatR;

namespace Application.TypeServices.UseCases;

public sealed record DeleteTypeServiceCommand(Guid Id) : IRequest<bool>;
