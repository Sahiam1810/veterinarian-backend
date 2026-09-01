using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class GetAppointmentStatusHistoryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentStatusHistoryByIdQuery, AppointmentStatusHistory>
{
    public async Task<AppointmentStatusHistory> Handle(
        GetAppointmentStatusHistoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.AppointmentStatusHistoriesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Historial de estado de cita no encontrado.");
    }
}
