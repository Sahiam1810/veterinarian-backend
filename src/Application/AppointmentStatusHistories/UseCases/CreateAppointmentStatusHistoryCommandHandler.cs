using Application.Common.Abstractions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class CreateAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentStatusHistoryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var history = new AppointmentStatusHistory(
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.AddAsync(
            history,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return history.Id;
    }
}
