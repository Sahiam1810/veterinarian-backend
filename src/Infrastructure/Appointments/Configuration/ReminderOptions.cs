namespace Infrastructure.Appointments.Configuration;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    public bool Enabled { get; init; } = true;
    public double WindowHours { get; init; } = 24;
    public int PollIntervalMinutes { get; init; } = 15;
    public string[] ExcludedStatusNames { get; init; } = [];
}
