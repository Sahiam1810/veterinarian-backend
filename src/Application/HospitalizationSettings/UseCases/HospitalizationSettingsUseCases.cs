using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationSettings.Dtos;
using Domain.HospitalizationSettings.Entities;
using HospitalizationSettingsEntity = Domain.HospitalizationSettings.Entities.HospitalizationSettings;
using MediatR;

namespace Application.HospitalizationSettings.UseCases;

public sealed record GetHospitalizationSettingsQuery : IRequest<HospitalizationSettingsDto>;

public sealed record UpdateHospitalizationSettingsCommand(decimal DailyRate) : IRequest;

public sealed class GetHospitalizationSettingsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationSettingsQuery, HospitalizationSettingsDto>
{
    public async Task<HospitalizationSettingsDto> Handle(
        GetHospitalizationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await unitOfWork.HospitalizationSettingsRepository
            .GetAsync(cancellationToken);

        return new HospitalizationSettingsDto(settings?.DailyRate ?? 0m);
    }
}

public sealed class UpdateHospitalizationSettingsCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateHospitalizationSettingsCommand>
{
    public async Task Handle(
        UpdateHospitalizationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DailyRate <= 0m)
        {
            throw new BadRequestException(
                "La tarifa diaria de hospitalización debe ser mayor que cero.");
        }

        var settings = await unitOfWork.HospitalizationSettingsRepository
            .GetAsync(cancellationToken);

        if (settings is null)
        {
            await unitOfWork.HospitalizationSettingsRepository.AddAsync(
                new HospitalizationSettingsEntity(request.DailyRate),
                cancellationToken);
        }
        else
        {
            settings.SetDailyRate(request.DailyRate);
            await unitOfWork.HospitalizationSettingsRepository.UpdateAsync(
                settings,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
