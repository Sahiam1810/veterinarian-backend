namespace Application.Reports;

internal static class ReportPercentage
{
    private const int Scale = 1;

    public static decimal OfTotal(int count, int total)
    {
        if (total <= 0 || count <= 0)
        {
            return 0m;
        }

        return Math.Round(
            (decimal)count / total * 100m,
            Scale,
            MidpointRounding.AwayFromZero);
    }
}
