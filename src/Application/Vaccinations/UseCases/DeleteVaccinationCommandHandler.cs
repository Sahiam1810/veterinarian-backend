using Application.Common.Abstractions;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class DeleteVaccinationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVaccinationCommand, bool>
{
    public async Task<bool> Handle(
        DeleteVaccinationCommand request,
        CancellationToken cancellationToken)
    {
        var vaccination = await unitOfWork.VaccinationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (vaccination is null)
        {
            return false;
        }

        await unitOfWork.VaccinationsRepository.DeleteAsync(
            vaccination,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
