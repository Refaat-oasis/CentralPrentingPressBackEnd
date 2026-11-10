namespace CentralPress.API.Services;

public static class ArabicTafqeetService
{
    private static readonly ITafqeetService _instance = new TafqeetService();

    public static string ToWords(decimal amount) => _instance.ToWords(amount);

    public static string ToWords(decimal amount, string currencyName, string fractionalName) =>
        _instance.ToWords(amount, currencyName, fractionalName);
}
