using Application.Owners.Abstractions;
using Application.Owners.Enums;
using MediatR;

namespace Application.Owners.UseCases;

// Alta transaccional de dueño: User Cliente sin hash + Client. Sin account/credentials.
public sealed record RegisterOwnerCommand(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    RegisterOwnerChannel Channel,
    string? Address = null,
    Guid? ContactProofSessionId = null,
    string? ContactProof = null) : IRequest<RegisterOwnerResult>;
