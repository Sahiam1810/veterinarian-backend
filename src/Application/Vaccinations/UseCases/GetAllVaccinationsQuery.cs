using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record GetAllVaccinationsQuery
    : IRequest<IReadOnlyCollection<Vaccination>>;
