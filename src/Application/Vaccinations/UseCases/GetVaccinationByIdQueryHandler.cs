using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetVaccinationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVaccinationByIdQuery, Vaccination>
{
    public async Task<Vaccination> Handle(
        GetVaccinationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var vaccination = await unitOfWork.VaccinationsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Registro de vacunación no encontrado.");

        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);

        // Sin perfil de Cliente (personal): ve el registro sin restricción.
        if (client is null)
        {
            return vaccination;
        }

        // Con perfil de Cliente: solo si la vacuna pertenece a una de sus
        // mascotas. Si no, se trata como inexistente (404), no como ajena.
        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        var ownsVaccination = clientPets.Any(cp => cp.Id == vaccination.ClientPetId);

        return ownsVaccination
            ? vaccination
            : throw new NotFoundException("Registro de vacunación no encontrado.");
    }
}
