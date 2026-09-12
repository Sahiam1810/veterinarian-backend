using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public record GetClientLookupQuery(
    string? IdentificationNumber,
    string? PhoneNumber) : IRequest<ClientEntity>;
