using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record GetVaccinationByIdQuery(Guid Id)
    : IRequest<Vaccination?>;
