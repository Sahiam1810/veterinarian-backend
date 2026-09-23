using Domain.Clients.Entities;
using Domain.Diagnostics.Entities;
using Domain.Medications.Entities;
using Domain.Procedures.Entities;
using Domain.MedicationOrders.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Microsoft.EntityFrameworkCore;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

using Domain.TypeServices.Entities;

using Domain.Services.Entities;

using Domain.Specialties.Entities;
using Domain.ClientsPets.Entities;

using Domain.Veterinarians.Entities;

using Domain.SenderTypes.Entities;

using Domain.EscalationStatuses.Entities;
using Domain.Availabilities.Entities;
using Domain.VeterinarianAbsences.Entities;
using Domain.Appointments.Entities;
using Domain.AppointmentStatusHistories.Entities;
using Domain.MedicalRecords.Entities;
using Domain.Vaccinations.Entities;
using Domain.Notifications.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.Modules.Entities;
using Domain.Supplies.Entities;
using Domain.SupplyConsumptions.Entities;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;


namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<RaceEntity> Races => Set<RaceEntity>();

    public DbSet<SpeciesEntity> Species => Set<SpeciesEntity>();

    public DbSet<PetEntity> Pets => Set<PetEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<ClientEntity> Clients => Set<ClientEntity>();

    public DbSet<UserTokenEntity> UserTokens => Set<UserTokenEntity>();

    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();

    public DbSet<StatusAppointment> StatusAppointments => Set<StatusAppointment>();

    public DbSet<TypeService> TypeServices => Set<TypeService>();


    public DbSet<Service> Services => Set<Service>();

    public DbSet<SpecialtyEntity> Specialties => Set<SpecialtyEntity>();

    public DbSet<ClientPetEntity> ClientPets => Set<ClientPetEntity>();


    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();

    public DbSet<SenderTypeEntity> SenderTypes => Set<SenderTypeEntity>();

    public DbSet<EscalationStatusEntity> EscalationStatuses => Set<EscalationStatusEntity>();

    public DbSet<Availability> Availabilities => Set<Availability>();

    public DbSet<VeterinarianAbsence> VeterinarianAbsences => Set<VeterinarianAbsence>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();

    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();

    public DbSet<HospitalizationStay> HospitalizationStays => Set<HospitalizationStay>();

    public DbSet<HospitalizationNote> HospitalizationNotes => Set<HospitalizationNote>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<MedicationOrder> MedicationOrders => Set<MedicationOrder>();
    public DbSet<MedicationOrderItem> MedicationOrderItems => Set<MedicationOrderItem>();
    public DbSet<ProcedureOrder> ProcedureOrders => Set<ProcedureOrder>();
    public DbSet<ProcedureOrderItem> ProcedureOrderItems => Set<ProcedureOrderItem>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<SupplyConsumption> SupplyConsumptions => Set<SupplyConsumption>();

    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();

    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);
    }
}
