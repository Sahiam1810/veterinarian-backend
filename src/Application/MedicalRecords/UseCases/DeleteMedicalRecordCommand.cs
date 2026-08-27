using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record DeleteMedicalRecordCommand(Guid Id) : IRequest<bool>;
