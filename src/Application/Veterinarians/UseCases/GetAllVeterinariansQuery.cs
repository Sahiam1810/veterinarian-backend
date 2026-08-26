using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record GetAllVeterinariansQuery
    : IRequest<IReadOnlyCollection<Veterinarian>>;
