using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed class UpdateVeterinarianAbsenceCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<UpdateVeterinarianAbsenceCommand>
{
    public async Task Handle(
        UpdateVeterinarianAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        var absence = await absences.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ausencia no encontrada.");

        absence.Update(
            request.StartAtUtc,
            request.EndAtUtc,
            request.Reason,
            request.IsFullDay);

        await absences.UpdateAsync(absence, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
