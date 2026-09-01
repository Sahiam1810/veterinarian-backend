using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record GetVeterinarianByIdQuery(Guid Id)
    : IRequest<Veterinarian>;
