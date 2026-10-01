using Domain.MedicalRecords.Entities;

namespace Application.MedicalRecords.Abstraction;

public interface IMedicalRecordRepository
{
    Task<IReadOnlyCollection<MedicalRecord>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<MedicalRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MedicalRecord>> GetByClientPetIdsAsync(
        IReadOnlyCollection<Guid> clientPetIds,
        CancellationToken cancellationToken);

    Task AddAsync(
        MedicalRecord medicalRecord,
        CancellationToken cancellationToken);

    Task<bool> ExistsByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken);

    // Borra expedientes de esos vínculos dueño-mascota sin Include
    Task DeleteByClientPetIdsAsync(
        IReadOnlyCollection<Guid> clientPetIds,
        CancellationToken cancellationToken);
}
