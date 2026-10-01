using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record DeleteVeterinarianAbsenceCommand(Guid Id) : IRequest;
