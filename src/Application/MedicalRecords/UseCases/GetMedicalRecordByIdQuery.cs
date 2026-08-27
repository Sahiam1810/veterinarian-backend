using Domain.MedicalRecords.Entities;
using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record GetMedicalRecordByIdQuery(Guid Id)
    : IRequest<MedicalRecord?>;
