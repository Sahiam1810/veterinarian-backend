using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class UpdateVeterinarianCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVeterinarianCommand>
{
    public async Task Handle(
        UpdateVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");

        veterinarian.Update(
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber);

        await unitOfWork.VeterinariansRepository.UpdateAsync(
            veterinarian,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
