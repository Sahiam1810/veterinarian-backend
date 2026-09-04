using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record GetMyVaccinationsQuery(Guid UserAccountId)
    : IRequest<IReadOnlyCollection<Vaccination>>;
