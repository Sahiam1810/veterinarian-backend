using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record GetVeterinarianAbsenceByIdQuery(Guid Id) : IRequest<VeterinarianAbsence>;

public sealed class GetVeterinarianAbsenceByIdQueryHandler(
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<GetVeterinarianAbsenceByIdQuery, VeterinarianAbsence>
{
    public async Task<VeterinarianAbsence> Handle(
        GetVeterinarianAbsenceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await absences.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ausencia no encontrada.");
    }
}
