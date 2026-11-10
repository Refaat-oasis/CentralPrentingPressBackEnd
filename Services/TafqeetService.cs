using System;
using System.Text;

namespace CentralPress.API.Services;

public class TafqeetService : ITafqeetService
{
    private static readonly string[] Ones = {
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    };

    private static readonly string[] Tens = {
        "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
    };

    private static readonly string[] Hundreds = {
        "", "مائة", "مئتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
    };

    public string ToWords(decimal amount, string currencyName = "جنيه", string fractionalName = "قرش")
    {
        if (amount == 0m)
            return "فقط وقدره صفر جنيه لا غير";

        bool isNegative = amount < 0m;
        decimal absAmount = Math.Abs(amount);

        // Safe piaster rounding and rollover
        decimal rounded = Math.Round(absAmount, 2, MidpointRounding.AwayFromZero);
        long totalPiasters = (long)Math.Round(rounded * 100m, MidpointRounding.AwayFromZero);
        long pounds = totalPiasters / 100L;
        int piasters = (int)(totalPiasters % 100L);

        var sb = new StringBuilder();
        sb.Append("فقط وقدره ");

        if (isNegative)
        {
            sb.Append("سالب ");
        }

        bool hasPounds = pounds > 0;
        bool hasPiasters = piasters > 0;

        if (hasPounds)
        {
            sb.Append(FormatCurrencyUnits(pounds, currencyName));
        }

        if (hasPiasters)
        {
            if (hasPounds)
                sb.Append(" و");

            sb.Append(FormatFractionalUnits(piasters, fractionalName));
        }

        if (!hasPounds && !hasPiasters)
        {
            sb.Append("صفر جنيه");
        }

        sb.Append(" لا غير");
        return sb.ToString().Replace("  ", " ").Trim();
    }

    private static string FormatCurrencyUnits(long number, string currency)
    {
        if (number == 1) return currency + " واحد";
        if (number == 2) return currency + "ان";

        string words = ConvertNumberToArabicWords(number);
        string suffix = GetPoundSuffix(number, currency);

        return $"{words} {suffix}".Trim();
    }

    private static string FormatFractionalUnits(int number, string fraction)
    {
        if (number == 1) return fraction + " واحد";
        if (number == 2) return fraction + "ان";

        string words = ConvertNumberToArabicWords(number);
        string suffix = GetPiasterSuffix(number, fraction);

        return $"{words} {suffix}".Trim();
    }

    private static string GetPoundSuffix(long number, string currency)
    {
        long lastTwoDigits = number % 100;
        if (lastTwoDigits >= 3 && lastTwoDigits <= 10) return currency + "ات";
        if (lastTwoDigits >= 11 && lastTwoDigits <= 99) return currency + "اً";
        return currency;
    }

    private static string GetPiasterSuffix(int number, string fraction)
    {
        if (number >= 3 && number <= 10) return "قروش";
        return fraction + "اً";
    }

    public static string ConvertNumberToArabicWords(long number)
    {
        if (number == 0) return "";
        if (number < 0) return "سالب " + ConvertNumberToArabicWords(Math.Abs(number));

        var parts = new StringBuilder();

        // Trillions (< 1000)
        long trillions = number / 1_000_000_000_000L;
        if (trillions > 0)
        {
            if (trillions == 1) parts.Append("تريليون");
            else if (trillions == 2) parts.Append("تريليونان");
            else if (trillions >= 3 && trillions <= 10) parts.Append(ConvertChunk(trillions) + " تريليونات");
            else parts.Append(ConvertChunk(trillions) + " تريليوناً");

            number %= 1_000_000_000_000L;
            if (number > 0) parts.Append(" و");
        }

        // Billions (< 1000)
        long billions = number / 1_000_000_000L;
        if (billions > 0)
        {
            if (billions == 1) parts.Append("مليار");
            else if (billions == 2) parts.Append("ملياران");
            else if (billions >= 3 && billions <= 10) parts.Append(ConvertChunk(billions) + " مليارات");
            else parts.Append(ConvertChunk(billions) + " ملياراً");

            number %= 1_000_000_000L;
            if (number > 0) parts.Append(" و");
        }

        // Millions (< 1000)
        long millions = number / 1_000_000L;
        if (millions > 0)
        {
            if (millions == 1) parts.Append("مليون");
            else if (millions == 2) parts.Append("مليونان");
            else if (millions >= 3 && millions <= 10) parts.Append(ConvertChunk(millions) + " ملايين");
            else parts.Append(ConvertChunk(millions) + " مليوناً");

            number %= 1_000_000L;
            if (number > 0) parts.Append(" و");
        }

        // Thousands (< 1000)
        long thousands = number / 1000L;
        if (thousands > 0)
        {
            if (thousands == 1) parts.Append("ألف");
            else if (thousands == 2) parts.Append("ألفان");
            else if (thousands >= 3 && thousands <= 10) parts.Append(ConvertChunk(thousands) + " آلاف");
            else parts.Append(ConvertChunk(thousands) + " ألفاً");

            number %= 1000L;
            if (number > 0) parts.Append(" و");
        }

        // Remainder (< 1000)
        if (number > 0)
        {
            parts.Append(ConvertChunk(number));
        }

        return parts.ToString();
    }

    private static string ConvertChunk(long number)
    {
        var sb = new StringBuilder();

        long hundredsDigit = (number % 1000) / 100;
        long remainder = number % 100;

        if (hundredsDigit > 0 && hundredsDigit < Hundreds.Length)
        {
            sb.Append(Hundreds[hundredsDigit]);
            if (remainder > 0) sb.Append(" و");
        }

        if (remainder > 0)
        {
            if (remainder < 20)
            {
                sb.Append(Ones[remainder]);
            }
            else
            {
                long onesDigit = remainder % 10;
                long tensDigit = remainder / 10;

                if (onesDigit > 0)
                {
                    sb.Append(Ones[onesDigit]);
                    sb.Append(" و");
                }
                if (tensDigit < Tens.Length)
                {
                    sb.Append(Tens[tensDigit]);
                }
            }
        }

        return sb.ToString();
    }
}
