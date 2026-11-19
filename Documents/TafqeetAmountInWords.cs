using CentralPress.API.Services;

namespace CentralPress.API.Documents;

public sealed class TafqeetAmountInWords(ITafqeetService tafqeet) : IAmountInWords
{
    public string ToArabicCurrency(decimal amount)
    {
        return tafqeet.ToWords(amount, "جنيه", "قرش");
    }
}
