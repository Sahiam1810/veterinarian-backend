using MediatR;

namespace Application.Services.UseCases;

public sealed record DeleteServiceCommand(Guid Id) : IRequest<bool>;
