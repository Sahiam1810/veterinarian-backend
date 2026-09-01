using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record GetAllVaccinationsQuery(Guid UserAccountId)
    : IRequest<IReadOnlyCollection<Vaccination>>;
