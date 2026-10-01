using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record GetAllVeterinarianAbsencesQuery
    : IRequest<IReadOnlyCollection<VeterinarianAbsence>>;

public sealed class GetAllVeterinarianAbsencesQueryHandler(
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<GetAllVeterinarianAbsencesQuery, IReadOnlyCollection<VeterinarianAbsence>>
{
    public Task<IReadOnlyCollection<VeterinarianAbsence>> Handle(
        GetAllVeterinarianAbsencesQuery request,
        CancellationToken cancellationToken)
    {
        return absences.GetAllAsync(cancellationToken);
    }
}
