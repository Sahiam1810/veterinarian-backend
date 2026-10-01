using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed class CreateVeterinarianAbsenceCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<CreateVeterinarianAbsenceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateVeterinarianAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        var absence = new VeterinarianAbsence(
            request.VeterinarianId,
            request.StartAtUtc,
            request.EndAtUtc,
            request.Reason,
            request.IsFullDay);

        await absences.AddAsync(absence, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return absence.Id;
    }
}
