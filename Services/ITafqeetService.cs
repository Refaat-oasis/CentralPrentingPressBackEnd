namespace CentralPress.API.Services;

public interface ITafqeetService
{
    string ToWords(decimal amount, string currencyName = "جنيه", string fractionalName = "قرش");
}
