using Application.Common.Abstractions;
using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class CreateTypeServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTypeServiceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateTypeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var typeService = new TypeService(
            request.Name,
            request.Description);

        await unitOfWork.TypeServicesRepository.AddAsync(
            typeService,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return typeService.Id;
    }
}
