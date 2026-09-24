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


public sealed record GetHospitalizationStayInvoiceQuery(Guid StayId) : IRequest<HospitalizationStayInvoiceDto>;

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

        var availableServices = await unitOfWork.ServicesRepository.GetAvailableAsync(cancellationToken)
            ?? Array.Empty<Domain.Services.Entities.Service>();
        var dailyRate = availableServices
            .FirstOrDefault(service =>
                string.Equals(service.Name.Trim(), "Hospitalización", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(service.Name.Trim(), "Hospitalizacion", StringComparison.OrdinalIgnoreCase))
            ?.Price ?? 0m;

        var stay = new HospitalizationStay(
            request.ClientPetId,
            request.AppointmentId,
            request.AdmittedByUserId,
            request.Motivo,
            dailyRate);

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


public sealed class GetHospitalizationStayInvoiceQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStayInvoiceQuery, HospitalizationStayInvoiceDto>
{
    public async Task<HospitalizationStayInvoiceDto> Handle(
        GetHospitalizationStayInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(request.StayId, cancellationToken)
            ?? throw new NotFoundException("Estancia de hospitalización no encontrada.");

        var petName = stay.ClientPet?.Pet?.Name?.Value ?? "Mascota";
        var ownerName = stay.ClientPet?.Client?.FullName?.Value ?? "Cliente";

        var endDate = stay.FechaAlta ?? DateTime.UtcNow;
        var billedDays = Math.Max(1, (int)(endDate.Date - stay.FechaIngreso.Date).TotalDays);
        var dailyRate = stay.DailyRate;
        var hospitalizationTotal = dailyRate * billedDays;

        // Insumos registrados
        var supplyConsumptions = await unitOfWork.SupplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, cancellationToken);
        var supplyList = new List<BillableItemDto>();
        foreach (var sc in supplyConsumptions)
        {
            var supply = await unitOfWork.SuppliesRepository.GetByIdAsync(sc.SupplyId, cancellationToken);
            supplyList.Add(new BillableItemDto(
                supply?.Name ?? "Insumo",
                sc.Quantity,
                sc.UnitPrice,
                sc.Total,
                sc.Notes));
        }

        // Medicamentos con estado Entregada
        var medOrders = await unitOfWork.MedicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.Id,
            stay.AppointmentId,
            cancellationToken);

        var medList = new List<BillableItemDto>();
        foreach (var order in medOrders.Where(o => o.Status == "Entregada"))
        {
            foreach (var item in order.Items)
            {
                var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(item.MedicationId, cancellationToken);
                var unitPrice = medication?.Price ?? 0m;
                medList.Add(new BillableItemDto(
                    medication?.Name ?? item.Medication?.Name ?? "Medicamento",
                    1m,
                    unitPrice,
                    unitPrice,
                    item.Notes));
            }
        }

        // Procedimientos con estado Completada
        var procOrders = await unitOfWork.ProcedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.Id,
            stay.AppointmentId,
            cancellationToken);

        var procList = new List<BillableItemDto>();
        foreach (var order in procOrders.Where(o => o.Status == "Completada"))
        {
            foreach (var item in order.Items)
            {
                var procedure = await unitOfWork.ProceduresRepository.GetByIdAsync(item.ProcedureId, cancellationToken);
                var unitPrice = procedure?.Price ?? 0m;
                procList.Add(new BillableItemDto(
                    procedure?.Name ?? item.Procedure?.Name ?? "Procedimiento",
                    1m,
                    unitPrice,
                    unitPrice,
                    item.Notes));
            }
        }

        var suppliesTotal = supplyList.Sum(s => s.Total);
        var medicationsTotal = medList.Sum(m => m.Total);
        var proceduresTotal = procList.Sum(p => p.Total);
        var grandTotal = hospitalizationTotal + suppliesTotal + medicationsTotal + proceduresTotal;

        return new HospitalizationStayInvoiceDto(
            stay.Id,
            petName,
            ownerName,
            stay.FechaIngreso,
            stay.FechaAlta,
            stay.Estado.ToStatusLabel(),
            dailyRate,
            billedDays,
            stay.IsPaid,
            stay.PaidAt,
            hospitalizationTotal,
            suppliesTotal,
            medicationsTotal,
            proceduresTotal,
            grandTotal,
            supplyList,
            medList,
            procList);

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
