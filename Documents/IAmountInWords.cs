namespace CentralPress.API.Documents;

public interface IAmountInWords
{
    string ToArabicCurrency(decimal amount);
}
