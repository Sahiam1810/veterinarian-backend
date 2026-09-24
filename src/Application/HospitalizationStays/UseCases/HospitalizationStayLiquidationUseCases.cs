using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.HospitalizationStays.UseCases;

public sealed record HospitalizationStayLiquidation(
    Guid HospitalizationStayId,
    int BillableDays,
    decimal HospitalizationTotal,
    decimal SuppliesTotal,
    decimal MedicationsTotal,
    decimal ProceduresTotal,
    decimal Total);

public sealed record GetHospitalizationStayLiquidationQuery(Guid HospitalizationStayId)
    : IRequest<HospitalizationStayLiquidation>;

public sealed class GetHospitalizationStayLiquidationQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHospitalizationStayLiquidationQuery, HospitalizationStayLiquidation>
{
    public async Task<HospitalizationStayLiquidation> Handle(
        GetHospitalizationStayLiquidationQuery request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            request.HospitalizationStayId,
            cancellationToken);
        if (stay is null)
        {
            throw new NotFoundException($"No se encontró la estancia con ID '{request.HospitalizationStayId}'.");
        }

        var endDate = stay.FechaAlta ?? DateTime.UtcNow;
        var elapsedDays = Math.Max(1, (int)Math.Ceiling((endDate - stay.FechaIngreso).TotalDays));
        var hospitalizationTotal = elapsedDays * stay.DailyRate;
        var suppliesTotal = await unitOfWork.SupplyConsumptionsRepository
            .GetTotalByHospitalizationStayIdAsync(request.HospitalizationStayId, cancellationToken);

        var medicationOrders = await unitOfWork.MedicationOrdersRepository
            .GetByHospitalizationStayIdAsync(request.HospitalizationStayId, cancellationToken);
        var procedureOrders = await unitOfWork.ProcedureOrdersRepository
            .GetByHospitalizationStayIdAsync(request.HospitalizationStayId, cancellationToken);

        var medicationsTotal = medicationOrders
            .Where(order => order.Status == "Entregada")
            .SelectMany(order => order.Items)
            .Sum(item => item.UnitPrice);
        var proceduresTotal = procedureOrders
            .Where(order => order.Status == "Completada")
            .SelectMany(order => order.Items)
            .Sum(item => item.UnitPrice);

        return new HospitalizationStayLiquidation(
            request.HospitalizationStayId,
            elapsedDays,
            hospitalizationTotal,
            suppliesTotal,
            medicationsTotal,
            proceduresTotal,
            hospitalizationTotal + suppliesTotal + medicationsTotal + proceduresTotal);
    }
}
