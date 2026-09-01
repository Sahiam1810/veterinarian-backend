using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetVaccinationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVaccinationByIdQuery, Vaccination?>
{
    public async Task<Vaccination?> Handle(
        GetVaccinationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var vaccination = await unitOfWork.VaccinationsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (vaccination is null)
        {
            return null;
        }

        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        var client = account is null
            ? null
            : await unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);

        // Sin perfil de Cliente (personal): ve el registro sin restricción.
        if (client is null)
        {
            return vaccination;
        }

        // Con perfil de Cliente: solo si la vacuna pertenece a una de sus
        // mascotas. Si no, se trata como inexistente (404), no como ajena.
        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken);
        var ownsVaccination = clientPets.Any(cp => cp.Id == vaccination.ClientPetId);

        return ownsVaccination ? vaccination : null;
    }
}
