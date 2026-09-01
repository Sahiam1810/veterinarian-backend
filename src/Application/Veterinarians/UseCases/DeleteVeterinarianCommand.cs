using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record DeleteVeterinarianCommand(Guid Id) : IRequest;
