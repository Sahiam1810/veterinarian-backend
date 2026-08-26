using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed record GetServiceByIdQuery(Guid Id)
    : IRequest<Service?>;
