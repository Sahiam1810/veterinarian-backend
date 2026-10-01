namespace Application.HospitalizationSettings.Dtos;

public sealed record HospitalizationSettingsDto(decimal DailyRate);

public sealed record UpdateHospitalizationSettingsRequest(decimal DailyRate);
