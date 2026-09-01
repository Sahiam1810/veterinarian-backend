using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class GetVeterinarianByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVeterinarianByIdQuery, Veterinarian>
{
    public async Task<Veterinarian> Handle(
        GetVeterinarianByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");
    }
}
