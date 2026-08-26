using Application.Common.Abstractions;
using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed class CreateServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateServiceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = new Service(
            request.TypeServiceId,
            request.Name,
            request.DurationMinutes,
            request.Price,
            request.IsActive);

        await unitOfWork.ServicesRepository.AddAsync(
            service,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}
