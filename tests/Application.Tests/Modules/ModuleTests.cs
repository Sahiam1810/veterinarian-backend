using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Modules.Abstraction;
using Application.Modules.UseCases;
using Application.RolePermissions.Abstraction;
using Domain.Modules.Entities;
using Domain.Modules.ValueObjects;
using Xunit;

namespace Application.Tests.Modules;

public sealed class ModuleTests
{
    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var module = new ModuleEntity("Citas", "Gestión de citas");

        Assert.Equal("Citas", module.Name.Value);
        Assert.Equal("Gestión de citas", module.Description);
        Assert.NotEqual(Guid.Empty, module.Id);
    }

    [Fact]
    public void Create_with_null_name_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ModuleEntity(null!, null));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_name_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ModuleEntity(string.Empty, null));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_with_whitespace_name_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ModuleEntity("   ", null));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_with_name_longer_than_max_throws_argument_exception()
    {
        var tooLong = new string('A', ModuleName.MaxLength + 1);

        var exception = Assert.Throws<ArgumentException>(
            () => new ModuleEntity(tooLong, null));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_with_null_description_is_valid()
    {
        var module = new ModuleEntity("Inventario", null);

        Assert.Null(module.Description);
    }

    [Fact]
    public void Update_changes_name_and_description()
    {
        var module = new ModuleEntity("Citas", "Antigua");

        module.Update("Facturación", "Nueva descripción");

        Assert.Equal("Facturación", module.Name.Value);
        Assert.Equal("Nueva descripción", module.Description);
    }

    [Fact]
    public void Update_sets_updated_at()
    {
        var module = new ModuleEntity("Citas", null);
        Assert.Null(module.UpdatedAt);

        module.Update("Citas", "Actualizado");

        Assert.NotNull(module.UpdatedAt);
    }

    [Fact]
    public async Task Create_adds_and_saves()
    {
        var context = new ModuleTestContext();
        var handler = new CreateModuleCommandHandler(context.UnitOfWork);

        var id = await handler.Handle(
            new CreateModuleCommand("Citas", "Desc"),
            CancellationToken.None);

        Assert.Contains(id, context.Modules.Keys);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Create_with_duplicate_name_throws_conflict()
    {
        var context = new ModuleTestContext();
        var existing = new ModuleEntity("Citas", null);
        context.Modules[existing.Id] = existing;
        var handler = new CreateModuleCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateModuleCommand("Citas", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_by_id_missing_throws_not_found()
    {
        var context = new ModuleTestContext();
        var handler = new GetModuleByIdQueryHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetModuleByIdQuery(Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_all_returns_ordered_by_name()
    {
        var context = new ModuleTestContext();
        var zebra = new ModuleEntity("Zebra", null);
        var alpha = new ModuleEntity("Alpha", null);
        context.Modules[zebra.Id] = zebra;
        context.Modules[alpha.Id] = alpha;
        var handler = new GetAllModulesQueryHandler(context.UnitOfWork);

        var results = await handler.Handle(new GetAllModulesQuery(), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha", results.First().Name.Value);
        Assert.Equal("Zebra", results.Last().Name.Value);
    }

    [Fact]
    public async Task Update_existing_saves()
    {
        var context = new ModuleTestContext();
        var module = new ModuleEntity("Citas", "Antigua");
        context.Modules[module.Id] = module;
        var handler = new UpdateModuleCommandHandler(context.UnitOfWork);

        await handler.Handle(
            new UpdateModuleCommand(module.Id, "Facturación", "Nueva"),
            CancellationToken.None);

        Assert.Equal("Facturación", context.Modules[module.Id].Name.Value);
        Assert.Equal("Nueva", context.Modules[module.Id].Description);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var context = new ModuleTestContext();
        var handler = new UpdateModuleCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateModuleCommand(Guid.NewGuid(), "Citas", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_duplicate_name_throws_conflict()
    {
        var context = new ModuleTestContext();
        var first = new ModuleEntity("Citas", null);
        var second = new ModuleEntity("Inventario", null);
        context.Modules[first.Id] = first;
        context.Modules[second.Id] = second;
        var handler = new UpdateModuleCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new UpdateModuleCommand(second.Id, "Citas", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Delete_existing_removes_and_saves()
    {
        var context = new ModuleTestContext();
        var module = new ModuleEntity("Citas", null);
        context.Modules[module.Id] = module;
        var handler = new DeleteModuleCommandHandler(context.UnitOfWork);

        await handler.Handle(new DeleteModuleCommand(module.Id), CancellationToken.None);

        Assert.DoesNotContain(module.Id, context.Modules.Keys);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Delete_missing_throws_not_found()
    {
        var context = new ModuleTestContext();
        var handler = new DeleteModuleCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new DeleteModuleCommand(Guid.NewGuid()),
                CancellationToken.None));
    }

    private sealed class ModuleTestContext
    {
        public Dictionary<Guid, ModuleEntity> Modules { get; } = new();

        public int SaveChangesCount { get; set; }

        public IUnitOfWork UnitOfWork { get; }

        public ModuleTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ModuleTestContext _context;

        public FakeUnitOfWork(ModuleTestContext context)
        {
            _context = context;
            ModulesRepository = new FakeModuleRepository(context);
        }

        public IModulesRepository ModulesRepository { get; }

        public IRolePermissionsRepository RolePermissionsRepository => null!;
        public Application.Roles.Abstraction.IRolesRepository RolesRepository => null!;
        public Application.Species.Abstraction.ISpeciesRepository SpeciesRepository => null!;
        public Application.Races.Abstraction.IRaceRepository RacesRepository => null!;
        public Application.Pets.Abstraction.IPetRepository PetsRepository => null!;
        public Application.Users.Abstraction.IUsersRepository UsersRepository => null!;
        public Application.StatusAppointments.Abstraction.IStatusAppointmentRepository StatusAppointmentsRepository => null!;
        public Application.TypeServices.Abstraction.ITypeServiceRepository TypeServicesRepository => null!;
        public Application.Services.Abstraction.IServiceRepository ServicesRepository => null!;
        public Application.Specialties.Abstraction.ISpecialtyRepository SpecialtiesRepository => null!;
        public Application.ClientsPets.Abstraction.IClientPetRepository ClientPetsRepository => null!;
        public Application.Veterinarians.Abstraction.IVeterinarianRepository VeterinariansRepository => null!;
        public Application.Priorities.Abstraction.IPriorityRepository PrioritiesRepository => null!;
        public Application.SenderTypes.Abstraction.ISenderTypeRepository SenderTypesRepository => null!;
        public Application.AiRunStatuses.Abstraction.IAiRunStatusRepository AiRunStatusesRepository => null!;
        public Application.ConversationStatuses.Abstraction.IConversationStatusRepository ConversationStatusesRepository => null!;
        public Application.MessageTypes.Abstraction.IMessageTypeRepository MessageTypesRepository => null!;
        public Application.EscalationStatuses.Abstraction.IEscalationStatusRepository EscalationStatusesRepository => null!;
        public Application.Appointments.Abstraction.IAppointmentRepository AppointmentsRepository => null!;
        public Application.AppointmentStatusHistories.Abstraction.IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public Application.MedicalRecords.Abstraction.IMedicalRecordRepository MedicalRecordsRepository => null!;
        public Application.Vaccinations.Abstraction.IVaccinationRepository VaccinationsRepository => null!;
        public Application.Notifications.Abstraction.INotificationRepository NotificationsRepository => null!;
        public Application.Diagnostics.Abstraction.IDiagnosticRepository DiagnosticsRepository => null!;
        public Application.AgentHumans.Abstraction.IAgentHumanRepository AgentHumansRepository => null!;
        public Application.AiModels.Abstraction.IAiModelRepository AiModelsRepository => null!;
        public Application.ChatUserProfiles.Abstraction.IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public Application.ChatConversations.Abstraction.IChatConversationRepository ChatConversationsRepository => null!;
        public Application.ChatConversationAssignments.Abstraction.IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public Application.ChatConversationAiSettings.Abstraction.IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
        public Application.ChatParticipants.Abstraction.IChatParticipantRepository ChatParticipantsRepository => null!;
        public Application.ChatMessages.Abstraction.IChatMessageRepository ChatMessagesRepository => null!;
        public Application.ChatAttachments.Abstraction.IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public Application.ChatEscalations.Abstraction.IChatEscalationRepository ChatEscalationsRepository => null!;
        public Application.ChatEscalationStatusHistories.Abstraction.IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository => null!;
        public Application.ChatEscalationResolutions.Abstraction.IChatEscalationResolutionRepository ChatEscalationResolutionsRepository => null!;
        public Application.ChatEscalationAssignments.Abstraction.IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository => null!;
        public Application.ChatAiRuns.Abstraction.IChatAiRunRepository ChatAiRunsRepository => null!;
        public Application.ChatAiRunMetrics.Abstraction.IChatAiRunMetricsRepository ChatAiRunMetricsRepository => null!;
        public Application.ChatAiRunErrors.Abstraction.IChatAiRunErrorRepository ChatAiRunErrorsRepository => null!;
        public Application.ProviderModelsAi.Abstraction.IProviderModelAiRepository ProviderModelsAiRepository => null!;
        public Application.UserAccounts.Abstraction.IUserAccountsRepository UserAccountsRepository => null!;
        public Application.UserCredentials.Abstraction.IUserCredentialsRepository UserCredentialsRepository => null!;
        public Application.Clients.Abstraction.IClientRepository ClientsRepository => null!;
        public Application.UserTokens.Abstraction.IUserTokensRepository UserTokensRepository => null!;
        public Application.AccountStatements.Abstraction.IAccountStatementsRepository AccountStatementsRepository => null!;
        public Application.Availabilities.Abstraction.IAvailabilityRepository AvailabilitiesRepository => null!;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _context.SaveChangesCount++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }

    private sealed class FakeModuleRepository : IModulesRepository
    {
        private readonly ModuleTestContext _context;

        public FakeModuleRepository(ModuleTestContext context) => _context = context;

        public Task AddAsync(ModuleEntity module, CancellationToken cancellationToken)
        {
            _context.Modules[module.Id] = module;
            return Task.CompletedTask;
        }

        public Task<ModuleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(
                _context.Modules.TryGetValue(id, out var module) ? module : null);

        public Task<ModuleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            var moduleName = ModuleName.Create(name);
            var match = _context.Modules.Values.FirstOrDefault(
                module => module.Name == moduleName);
            return Task.FromResult(match);
        }

        public Task<IReadOnlyCollection<ModuleEntity>> GetAllAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ModuleEntity>>(
                _context.Modules.Values
                    .OrderBy(module => module.Name.Value, StringComparer.Ordinal)
                    .ToArray());

        public Task UpdateAsync(ModuleEntity module, CancellationToken cancellationToken)
        {
            _context.Modules[module.Id] = module;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ModuleEntity module, CancellationToken cancellationToken)
        {
            _context.Modules.Remove(module.Id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
        {
            var moduleName = ModuleName.Create(name);
            var exists = _context.Modules.Values.Any(module => module.Name == moduleName);
            return Task.FromResult(exists);
        }
    }
}
