using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed record GetTypeServiceByIdQuery(Guid Id)
    : IRequest<TypeService>;
