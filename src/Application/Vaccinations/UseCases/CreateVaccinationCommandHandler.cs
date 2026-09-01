using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class CreateVaccinationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVaccinationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateVaccinationCommand request,
        CancellationToken cancellationToken)
    {
        var medicalRecord = await unitOfWork.MedicalRecordsRepository.GetByIdAsync(
            request.RecordId,
            cancellationToken)
            ?? throw new NotFoundException("La historia clínica indicada no existe.");

        if (medicalRecord.ClientPetId != request.ClientPetId)
        {
            throw new BadRequestException("La historia clínica no corresponde a la relación cliente-mascota indicada.");
        }

        var vaccination = new Vaccination(
            request.ClientPetId,
            request.RecordId,
            request.VaccineName,
            request.DoseNumber,
            request.ApplicationDate,
            request.NextDoseDate);

        await unitOfWork.VaccinationsRepository.AddAsync(
            vaccination,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return vaccination.Id;
    }
}
