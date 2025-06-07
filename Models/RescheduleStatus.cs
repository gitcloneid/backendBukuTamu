namespace BukuTamuAPI.Models;

public static class RescheduleStatus
{
    public const string Tunggu = "Tunggu";
    public const string Batal = "Batal";

    public static readonly string[] ValidStatuses = { Tunggu, Batal };

    public static bool IsValid(string status)
    {
        return ValidStatuses.Contains(status);
    }
}