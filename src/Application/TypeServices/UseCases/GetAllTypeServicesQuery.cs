using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed record GetAllTypeServicesQuery
    : IRequest<IReadOnlyCollection<TypeService>>;
