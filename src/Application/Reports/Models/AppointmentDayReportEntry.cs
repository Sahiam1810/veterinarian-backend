namespace Application.Reports.Models;

// Proyección mínima del repositorio: el reporte por día solo agrega por fecha y estado,
// así que no hay razón para cargar Client/Pet (PII) como hace GetScheduledBetweenAsync.
public sealed record AppointmentDayReportEntry(
    DateTime ScheduledStart,
    string StatusName);
