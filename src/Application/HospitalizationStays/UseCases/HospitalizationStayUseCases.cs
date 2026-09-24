using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Dtos;
using Application.HospitalizationStays.Mappings;
using Domain.HospitalizationStays.Entities;
using MediatR;

namespace Application.HospitalizationStays.UseCases;

public sealed record AdmitHospitalizationStayCommand(
    Guid ClientPetId,
    Guid? AppointmentId,
    string Motivo,
    Guid AdmittedByUserId) : IRequest<Guid>;

public sealed record DischargeHospitalizationStayCommand(Guid Id) : IRequest;

public sealed record RegisterHospitalizationStayPaymentCommand(Guid Id) : IRequest;

public sealed record AddHospitalizationNoteCommand(
    Guid StayId,
    string Nota,
    Guid? EntregadoAUserId,
    Guid AutorUserId) : IRequest<Guid>;

public sealed record GetHospitalizationStayByIdQuery(Guid Id) : IRequest<ApiHospitalizationStayDto>;

public sealed record GetAllActiveHospitalizationStaysQuery : IRequest<IReadOnlyCollection<ApiHospitalizationStayDto>>;

public sealed record GetHospitalizationStaysByPetQuery(Guid ClientPetId) : IRequest<IReadOnlyCollection<ApiHospitalizationStayDto>>;

public sealed record GetHospitalizationNotesByStayQuery(Guid StayId) : IRequest<IReadOnlyCollection<ApiHospitalizationNoteDto>>;

public sealed record GetHospitalizationStaffQuery : IRequest<IReadOnlyCollection<HospitalizationStaffUserDto>>;

public sealed record GetHospitalizationAdmissionOptionsQuery
    : IRequest<IReadOnlyCollection<HospitalizationAdmissionOptionDto>>;

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

public sealed class RegisterHospitalizationStayPaymentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterHospitalizationStayPaymentCommand>
{
    public async Task Handle(
        RegisterHospitalizationStayPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        stay.RegisterPayment();
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
    : IRequestHandler<GetHospitalizationStayByIdQuery, ApiHospitalizationStayDto>
{
    public async Task<ApiHospitalizationStayDto> Handle(
        GetHospitalizationStayByIdQuery request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        var user = await unitOfWork.UsersRepository.GetByIdAsync(stay.AdmittedByUserId, cancellationToken);
        return stay.ToDto(user?.FullName);
    }
}

public sealed class GetAllActiveHospitalizationStaysQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllActiveHospitalizationStaysQuery, IReadOnlyCollection<ApiHospitalizationStayDto>>
{
    public async Task<IReadOnlyCollection<ApiHospitalizationStayDto>> Handle(
        GetAllActiveHospitalizationStaysQuery request,
        CancellationToken cancellationToken)
    {
        var stays = await unitOfWork.HospitalizationStaysRepository.GetAllActiveAsync(cancellationToken);
        var userIds = stays.Select(x => x.AdmittedByUserId).Distinct().ToList();
        var users = await unitOfWork.UsersRepository.GetByIdsAsync(userIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

        return stays.Select(s => s.ToDto(userDict.GetValueOrDefault(s.AdmittedByUserId))).ToList();
    }
}

public sealed class GetHospitalizationStaysByPetQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStaysByPetQuery, IReadOnlyCollection<ApiHospitalizationStayDto>>
{
    public async Task<IReadOnlyCollection<ApiHospitalizationStayDto>> Handle(
        GetHospitalizationStaysByPetQuery request,
        CancellationToken cancellationToken)
    {
        var stays = await unitOfWork.HospitalizationStaysRepository.GetByClientPetIdAsync(request.ClientPetId, cancellationToken);
        var userIds = stays.Select(x => x.AdmittedByUserId).Distinct().ToList();
        var users = await unitOfWork.UsersRepository.GetByIdsAsync(userIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

        return stays.Select(s => s.ToDto(userDict.GetValueOrDefault(s.AdmittedByUserId))).ToList();
    }
}

public sealed class GetHospitalizationNotesByStayQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationNotesByStayQuery, IReadOnlyCollection<ApiHospitalizationNoteDto>>
{
    public async Task<IReadOnlyCollection<ApiHospitalizationNoteDto>> Handle(
        GetHospitalizationNotesByStayQuery request,
        CancellationToken cancellationToken)
    {
        _ = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(request.StayId, cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        var notes = await unitOfWork.HospitalizationNotesRepository.GetByStayIdAsync(request.StayId, cancellationToken);
        var orderedNotes = notes.OrderBy(x => x.FechaHora).ToList();

        var userIds = orderedNotes.Select(n => n.AutorUserId)
            .Concat(orderedNotes.Where(n => n.EntregadoAUserId.HasValue).Select(n => n.EntregadoAUserId!.Value))
            .Distinct().ToList();

        var users = await unitOfWork.UsersRepository.GetByIdsAsync(userIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

        return orderedNotes.Select(n => n.ToDto(
            userDict.GetValueOrDefault(n.AutorUserId),
            n.EntregadoAUserId.HasValue ? userDict.GetValueOrDefault(n.EntregadoAUserId.Value) : null
        )).ToList();
    }
}

public sealed class GetHospitalizationStaffQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStaffQuery, IReadOnlyCollection<HospitalizationStaffUserDto>>
{
    public async Task<IReadOnlyCollection<HospitalizationStaffUserDto>> Handle(
        GetHospitalizationStaffQuery request,
        CancellationToken cancellationToken)
    {
        var staffUsers = await unitOfWork.UsersRepository.GetStaffUsersAsync(cancellationToken);
        return staffUsers.Select(x => new HospitalizationStaffUserDto(x.User.Id, x.User.FullName, x.RoleName)).ToList();
    }
}

public sealed class GetHospitalizationAdmissionOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationAdmissionOptionsQuery, IReadOnlyCollection<HospitalizationAdmissionOptionDto>>
{
    public async Task<IReadOnlyCollection<HospitalizationAdmissionOptionDto>> Handle(
        GetHospitalizationAdmissionOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var relationships = await unitOfWork.ClientPetsRepository.GetAllWithDetailsAsync(cancellationToken);

        return relationships
            .Where(relationship =>
                relationship.Client is not null &&
                relationship.Client.IsActive &&
                relationship.Pet is not null)
            .Select(relationship => new HospitalizationAdmissionOptionDto(
                relationship.Id,
                relationship.Pet.Name.Value,
                relationship.Client.FullName.Value))
            .OrderBy(option => option.PetName)
            .ThenBy(option => option.OwnerName)
            .ToList();
    }
}
