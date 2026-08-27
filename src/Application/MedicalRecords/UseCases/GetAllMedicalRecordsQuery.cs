using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record GetAllMedicalRecordsQuery
    : IRequest<IReadOnlyCollection<MedicalRecord>>;
