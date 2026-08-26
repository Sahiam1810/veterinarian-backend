using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed record GetAllServicesQuery
    : IRequest<IReadOnlyCollection<Service>>;
