using System;
using System.Text;

namespace CentralPress.API.Services;

public static class ArabicTafqeetService
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

    public static string ToWords(decimal amount)
    {
        if (amount == 0)
            return "فقط وقدره صفر جنيه لا غير";

        long pounds = (long)Math.Floor(amount);
        int piasters = (int)Math.Round((amount - pounds) * 100, MidpointRounding.AwayFromZero);

        var sb = new StringBuilder();
        sb.Append("فقط وقدره ");

        if (pounds > 0)
        {
            sb.Append(ConvertNumberToArabicWords(pounds));
            sb.Append(" ");
            sb.Append(GetPoundSuffix(pounds));
        }

        if (piasters > 0)
        {
            if (pounds > 0)
                sb.Append(" و");

            sb.Append(ConvertNumberToArabicWords(piasters));
            sb.Append(" ");
            sb.Append(GetPiasterSuffix(piasters));
        }

        sb.Append(" لا غير");
        return sb.ToString().Replace("  ", " ").Trim();
    }

    private static string GetPoundSuffix(long number)
    {
        long lastTwoDigits = number % 100;
        if (number == 1) return "جنيه واحد";
        if (number == 2) return "جنيهان";
        if (lastTwoDigits >= 3 && lastTwoDigits <= 10) return "جنيهات";
        if (lastTwoDigits >= 11 && lastTwoDigits <= 99) return "جنيهاً";
        return "جنيه";
    }

    private static string GetPiasterSuffix(int number)
    {
        if (number == 1) return "قرش واحد";
        if (number == 2) return "قرشان";
        if (number >= 3 && number <= 10) return "قروش";
        return "قرشاً";
    }

    private static string ConvertNumberToArabicWords(long number)
    {
        if (number == 0) return "";
        if (number < 0) return "سالب " + ConvertNumberToArabicWords(-number);

        var parts = new StringBuilder();

        // Billions
        if (number >= 1_000_000_000)
        {
            long billions = number / 1_000_000_000;
            if (billions == 1) parts.Append("مليار");
            else if (billions == 2) parts.Append("ملياران");
            else if (billions >= 3 && billions <= 10) parts.Append(ConvertChunk(billions) + " مليارات");
            else parts.Append(ConvertChunk(billions) + " ملياراً");

            number %= 1_000_000_000;
            if (number > 0) parts.Append(" و");
        }

        // Millions
        if (number >= 1_000_000)
        {
            long millions = number / 1_000_000;
            if (millions == 1) parts.Append("مليون");
            else if (millions == 2) parts.Append("مليونان");
            else if (millions >= 3 && millions <= 10) parts.Append(ConvertChunk(millions) + " ملايين");
            else parts.Append(ConvertChunk(millions) + " مليوناً");

            number %= 1_000_000;
            if (number > 0) parts.Append(" و");
        }

        // Thousands
        if (number >= 1000)
        {
            long thousands = number / 1000;
            if (thousands == 1) parts.Append("ألف");
            else if (thousands == 2) parts.Append("ألفان");
            else if (thousands >= 3 && thousands <= 10) parts.Append(ConvertChunk(thousands) + " آلاف");
            else parts.Append(ConvertChunk(thousands) + " ألفاً");

            number %= 1000;
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

        long hundredsDigit = number / 100;
        long remainder = number % 100;

        if (hundredsDigit > 0)
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
                sb.Append(Tens[tensDigit]);
            }
        }

        return sb.ToString();
    }
}
