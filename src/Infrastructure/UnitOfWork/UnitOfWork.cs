using Application.Availabilities.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Diagnostics.Abstraction;
using Application.Medications.Abstraction;
using Application.Procedures.Abstraction;
using Application.MedicationOrders.Abstraction;
using Application.ProcedureOrders.Abstraction;
using Application.Supplies.Abstraction;
using Application.SupplyConsumptions.Abstraction;
using Application.HospitalizationStays.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Modules.Abstraction;
using Application.Roles.Abstraction;
using Application.RolePermissions.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;

using Application.Services.Abstraction;

using Application.Specialties.Abstraction;
using Application.ClientsPets.Abstraction;

using Application.Veterinarians.Abstraction;

using Application.SenderTypes.Abstraction;

using Application.EscalationStatuses.Abstraction;

using Application.Notifications.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public UnitOfWork(
        VeterinaryDbContext context,
        IRolesRepository rolesRepository,
        IModulesRepository modulesRepository,
        IRolePermissionsRepository rolePermissionsRepository,
        ISpeciesRepository speciesRepository,
        IRaceRepository racesRepository,
        IUsersRepository usersRepository,
        IStatusAppointmentRepository statusAppointmentsRepository,
        ITypeServiceRepository typeServicesRepository,
        IServiceRepository servicesRepository,
        IPetRepository petsRepository,
        IClientRepository clientsRepository,
        IUserTokensRepository userTokensRepository,
        ISpecialtyRepository specialtiesRepository,
        IClientPetRepository clientPetsRepository,
        ISenderTypeRepository senderTypesRepository,
        IVeterinarianRepository veterinariansRepository,
        IEscalationStatusRepository escalationStatusesRepository,
        IAvailabilityRepository availabilitiesRepository,
        IAppointmentRepository appointmentsRepository,
        IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository,
        IHospitalizationStayRepository hospitalizationStaysRepository,
        IHospitalizationNoteRepository hospitalizationNotesRepository,
        IMedicalRecordRepository medicalRecordsRepository,
        INotificationRepository notificationsRepository,
        IDiagnosticRepository diagnosticsRepository,
        IMedicationRepository medicationsRepository,
        IProcedureRepository proceduresRepository,
        IMedicationOrderRepository medicationOrdersRepository,
        IProcedureOrderRepository procedureOrdersRepository,
        ISupplyRepository suppliesRepository,
        ISupplyConsumptionRepository supplyConsumptionsRepository,
        IVaccinationRepository vaccinationsRepository,
        IAgentHumanRepository agentHumansRepository,
        IChatConversationRepository chatConversationsRepository,
        IChatParticipantRepository chatParticipantsRepository,
        IChatMessageRepository chatMessagesRepository,
        IChatEscalationRepository chatEscalationsRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        ModulesRepository = modulesRepository;
        RolePermissionsRepository = rolePermissionsRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
        PetsRepository = petsRepository;
        UsersRepository = usersRepository;
        StatusAppointmentsRepository = statusAppointmentsRepository;
        TypeServicesRepository = typeServicesRepository;
        ServicesRepository = servicesRepository;
        ClientsRepository = clientsRepository;
        UserTokensRepository = userTokensRepository;
        SpecialtiesRepository = specialtiesRepository;
        ClientPetsRepository = clientPetsRepository;
        VeterinariansRepository = veterinariansRepository;
        SenderTypesRepository = senderTypesRepository;
        EscalationStatusesRepository = escalationStatusesRepository;
        AvailabilitiesRepository = availabilitiesRepository;
        AppointmentsRepository = appointmentsRepository;
        AppointmentStatusHistoriesRepository = appointmentStatusHistoriesRepository;
        HospitalizationStaysRepository = hospitalizationStaysRepository;
        HospitalizationNotesRepository = hospitalizationNotesRepository;
        MedicalRecordsRepository = medicalRecordsRepository;
        NotificationsRepository = notificationsRepository;
        DiagnosticsRepository = diagnosticsRepository;
        MedicationsRepository = medicationsRepository;
        ProceduresRepository = proceduresRepository;
        MedicationOrdersRepository = medicationOrdersRepository;
        ProcedureOrdersRepository = procedureOrdersRepository;
        SuppliesRepository = suppliesRepository;
        SupplyConsumptionsRepository = supplyConsumptionsRepository;
        VaccinationsRepository = vaccinationsRepository;
        AgentHumansRepository = agentHumansRepository;
        ChatConversationsRepository = chatConversationsRepository;
        ChatParticipantsRepository = chatParticipantsRepository;
        ChatMessagesRepository = chatMessagesRepository;
        ChatEscalationsRepository = chatEscalationsRepository;
    }

    public IRolesRepository RolesRepository { get; }
    public IModulesRepository ModulesRepository { get; }
    public IRolePermissionsRepository RolePermissionsRepository { get; }
    public ISpeciesRepository SpeciesRepository { get; }
    public IRaceRepository RacesRepository { get; }
    public IPetRepository PetsRepository { get; }
    public IUsersRepository UsersRepository { get; }
    public IClientRepository ClientsRepository { get; }
    public IUserTokensRepository UserTokensRepository { get; }
    public IStatusAppointmentRepository StatusAppointmentsRepository { get; }
    public ITypeServiceRepository TypeServicesRepository { get; }
    public IServiceRepository ServicesRepository { get; }
    public ISpecialtyRepository SpecialtiesRepository { get; }
    public IClientPetRepository ClientPetsRepository { get; }
    public IVeterinarianRepository VeterinariansRepository { get; }
    public ISenderTypeRepository SenderTypesRepository { get; }
    public IEscalationStatusRepository EscalationStatusesRepository { get; }
    public IAvailabilityRepository AvailabilitiesRepository { get; }
    public IAppointmentRepository AppointmentsRepository { get; }
    public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository { get; }
    public IHospitalizationStayRepository HospitalizationStaysRepository { get; }
    public IHospitalizationNoteRepository HospitalizationNotesRepository { get; }
    public IMedicalRecordRepository MedicalRecordsRepository { get; }
    public IVaccinationRepository VaccinationsRepository { get; }
    public INotificationRepository NotificationsRepository { get; }
    public IDiagnosticRepository DiagnosticsRepository { get; }
    public IMedicationRepository MedicationsRepository { get; }
    public IProcedureRepository ProceduresRepository { get; }
    public IMedicationOrderRepository MedicationOrdersRepository { get; }
    public IProcedureOrderRepository ProcedureOrdersRepository { get; }
    public ISupplyRepository SuppliesRepository { get; }
    public ISupplyConsumptionRepository SupplyConsumptionsRepository { get; }
    public IAgentHumanRepository AgentHumansRepository { get; }
    public IChatConversationRepository ChatConversationsRepository { get; }
    public IChatParticipantRepository ChatParticipantsRepository { get; }
    public IChatMessageRepository ChatMessagesRepository { get; }
    public IChatEscalationRepository ChatEscalationsRepository { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (OracleClientPhoneConflictMapper.TryMapToConflict(exception, out var conflict)
                  && conflict is not null)
        {
            throw conflict;
        }
        catch (DbUpdateException exception)
            when (OracleHospitalizationStayConflictMapper.TryMapToConflict(exception, out var conflict)
                  && conflict is not null)
        {
            throw conflict;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            cancellationToken);
        try
        {
            await action(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
