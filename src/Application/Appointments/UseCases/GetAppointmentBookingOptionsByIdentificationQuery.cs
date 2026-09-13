using Application.Common.Abstractions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetAppointmentBookingOptionsByIdentificationQuery(string IdentificationNumber)
    : IRequest<AppointmentBookingOptionsResult>;

public sealed class GetAppointmentBookingOptionsByIdentificationQueryHandler(
    IUnitOfWork unitOfWork,
    ISender sender)
    : IRequestHandler<GetAppointmentBookingOptionsByIdentificationQuery, AppointmentBookingOptionsResult>
{
    public async Task<AppointmentBookingOptionsResult> Handle(
        GetAppointmentBookingOptionsByIdentificationQuery request,
        CancellationToken cancellationToken)
    {
        var userAccountId = await RescheduleAppointmentByIdentificationCommandHandler
            .ResolveUserAccountIdAsync(
                unitOfWork,
                request.IdentificationNumber,
                cancellationToken);

        return await sender.Send(
            new GetAppointmentBookingOptionsQuery(userAccountId),
            cancellationToken);
    }
}
