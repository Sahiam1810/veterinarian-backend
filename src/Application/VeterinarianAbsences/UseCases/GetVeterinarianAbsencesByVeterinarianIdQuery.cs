using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record GetVeterinarianAbsencesByVeterinarianIdQuery(Guid VeterinarianId)
    : IRequest<IReadOnlyCollection<VeterinarianAbsence>>;

public sealed class GetVeterinarianAbsencesByVeterinarianIdQueryHandler(
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<
        GetVeterinarianAbsencesByVeterinarianIdQuery,
        IReadOnlyCollection<VeterinarianAbsence>>
{
    public Task<IReadOnlyCollection<VeterinarianAbsence>> Handle(
        GetVeterinarianAbsencesByVeterinarianIdQuery request,
        CancellationToken cancellationToken)
    {
        return absences.GetAllByVeterinarianIdAsync(request.VeterinarianId, cancellationToken);
    }
}
