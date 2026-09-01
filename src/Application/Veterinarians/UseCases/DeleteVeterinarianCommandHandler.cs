using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class DeleteVeterinarianCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVeterinarianCommand>
{
    public async Task Handle(
        DeleteVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");

        await unitOfWork.VeterinariansRepository.DeleteAsync(
            veterinarian,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
