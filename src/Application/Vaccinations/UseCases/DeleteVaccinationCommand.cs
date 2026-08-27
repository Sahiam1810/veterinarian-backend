using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record DeleteVaccinationCommand(Guid Id) : IRequest<bool>;
