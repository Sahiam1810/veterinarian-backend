namespace Infrastructure.Appointments.Configuration;

public sealed class TelegramReminderOptions
{
    public const string SectionName = "TelegramReminders";

    public bool Enabled { get; init; } = true;
    public int LeadMinutes { get; init; } = 60;
    public int GraceMinutes { get; init; } = 10;
    public int PollIntervalMinutes { get; init; } = 5;
    public string[] AllowedStatusNames { get; init; } = ["AGENDADA", "CONFIRMADA"];
}
