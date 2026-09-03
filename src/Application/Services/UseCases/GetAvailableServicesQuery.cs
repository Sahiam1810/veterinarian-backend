using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed record GetAvailableServicesQuery
    : IRequest<IReadOnlyCollection<Service>>;
