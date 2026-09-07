using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed class DeleteVeterinarianAbsenceCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<DeleteVeterinarianAbsenceCommand>
{
    public async Task Handle(
        DeleteVeterinarianAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        var absence = await absences.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ausencia no encontrada.");

        await absences.DeleteAsync(absence, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
