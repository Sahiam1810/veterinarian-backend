using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.HospitalizationStays.Entities;
using MediatR;

namespace Application.HospitalizationStays.UseCases;

public sealed record AdmitHospitalizationStayCommand(
    Guid ClientPetId,
    Guid? AppointmentId,
    string Motivo,
    Guid AdmittedByUserId) : IRequest<Guid>;

public sealed record DischargeHospitalizationStayCommand(Guid Id) : IRequest;

public sealed record AddHospitalizationNoteCommand(
    Guid StayId,
    string Nota,
    Guid? EntregadoAUserId,
    Guid AutorUserId) : IRequest<Guid>;

public sealed record GetHospitalizationStayByIdQuery(Guid Id) : IRequest<HospitalizationStay>;

public sealed record GetAllActiveHospitalizationStaysQuery : IRequest<IReadOnlyCollection<HospitalizationStay>>;

public sealed record GetHospitalizationStaysByPetQuery(Guid ClientPetId) : IRequest<IReadOnlyCollection<HospitalizationStay>>;

public sealed record GetHospitalizationNotesByStayQuery(Guid StayId) : IRequest<IReadOnlyCollection<HospitalizationNote>>;

public sealed class AdmitHospitalizationStayCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AdmitHospitalizationStayCommand, Guid>
{
    public async Task<Guid> Handle(
        AdmitHospitalizationStayCommand request,
        CancellationToken cancellationToken)
    {
        var clientPet = await unitOfWork.ClientPetsRepository.GetByIdAsync(
            request.ClientPetId,
            cancellationToken)
            ?? throw new NotFoundException("Relación cliente-mascota no encontrada.");

        if (request.AppointmentId is Guid appointmentId)
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(appointmentId, cancellationToken)
                ?? throw new NotFoundException("Cita médica no encontrada.");

            if (appointment.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La cita no pertenece a la mascota indicada.");
            }
        }

        var activeStay = await unitOfWork.HospitalizationStaysRepository.GetActiveByPetIdAsync(
            request.ClientPetId,
            cancellationToken);

        if (activeStay is not null)
        {
            throw new ConflictException("La mascota ya tiene una estancia activa.");
        }

        var stay = new HospitalizationStay(
            request.ClientPetId,
            request.AppointmentId,
            request.AdmittedByUserId,
            request.Motivo);

        await unitOfWork.HospitalizationStaysRepository.AddAsync(stay, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return stay.Id;
    }
}

public sealed class DischargeHospitalizationStayCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DischargeHospitalizationStayCommand>
{
    public async Task Handle(
        DischargeHospitalizationStayCommand request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        if (stay.Estado == HospitalizationStayStatus.DadaDeAlta)
        {
            throw new InvalidOperationException("La estancia ya está dada de alta.");
        }

        stay.Discharge();
        await unitOfWork.HospitalizationStaysRepository.UpdateAsync(stay, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AddHospitalizationNoteCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AddHospitalizationNoteCommand, Guid>
{
    public async Task<Guid> Handle(
        AddHospitalizationNoteCommand request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            request.StayId,
            cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        if (request.EntregadoAUserId is Guid userId)
        {
            var recipient = await unitOfWork.UsersRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("Usuario destinatario no encontrado.");
            _ = recipient;
        }

        var note = new HospitalizationNote(
            request.StayId,
            request.AutorUserId,
            request.Nota,
            request.EntregadoAUserId);

        await unitOfWork.HospitalizationNotesRepository.AddAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}

public sealed class GetHospitalizationStayByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStayByIdQuery, HospitalizationStay>
{
    public async Task<HospitalizationStay> Handle(
        GetHospitalizationStayByIdQuery request,
        CancellationToken cancellationToken)
        => await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");
}

public sealed class GetAllActiveHospitalizationStaysQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllActiveHospitalizationStaysQuery, IReadOnlyCollection<HospitalizationStay>>
{
    public Task<IReadOnlyCollection<HospitalizationStay>> Handle(
        GetAllActiveHospitalizationStaysQuery request,
        CancellationToken cancellationToken)
        => unitOfWork.HospitalizationStaysRepository.GetAllActiveAsync(cancellationToken);
}

public sealed class GetHospitalizationStaysByPetQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStaysByPetQuery, IReadOnlyCollection<HospitalizationStay>>
{
    public Task<IReadOnlyCollection<HospitalizationStay>> Handle(
        GetHospitalizationStaysByPetQuery request,
        CancellationToken cancellationToken)
        => unitOfWork.HospitalizationStaysRepository.GetByClientPetIdAsync(request.ClientPetId, cancellationToken);
}

public sealed class GetHospitalizationNotesByStayQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationNotesByStayQuery, IReadOnlyCollection<HospitalizationNote>>
{
    public async Task<IReadOnlyCollection<HospitalizationNote>> Handle(
        GetHospitalizationNotesByStayQuery request,
        CancellationToken cancellationToken)
    {
        var notes = await unitOfWork.HospitalizationNotesRepository.GetByStayIdAsync(request.StayId, cancellationToken);
        return notes.OrderBy(x => x.FechaHora).ToArray();
    }
}
