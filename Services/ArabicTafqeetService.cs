namespace CentralPress.API.Services;

public static class ArabicTafqeetService
{
    private static readonly ITafqeetService _instance = new TafqeetService();

    public static string ToWords(decimal amount) => _instance.ToWords(amount);

    public static string ToWords(decimal amount, string currencyName, string fractionalName) =>
        _instance.ToWords(amount, currencyName, fractionalName);

    public static string StripLeadingPrefix(string? words)
    {
        if (string.IsNullOrWhiteSpace(words)) return string.Empty;
        return System.Text.RegularExpressions.Regex
            .Replace(words.Trim(), @"^فقط\s+وقدره\s*:?\s*", string.Empty)
            .Trim();
    }
}
