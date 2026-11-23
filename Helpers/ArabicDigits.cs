using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CentralPress.API.Helpers;

/// <summary>
/// Central helper for Eastern Arabic (Hindi) numeral formatting (٠١٢٣٤٥٦٧٨٩)
/// across documents, PDFs, Word templates, emails, and Arabic UI messages,
/// while tolerating both Arabic and ASCII digits when parsing incoming input.
/// </summary>
public static partial class ArabicDigits
{
    private const char Zero = '٠'; // U+0660

    [GeneratedRegex(@"(?:[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}|https?://\S+|\bAU-[A-Z0-9\-]+\b|\bISO\s*\d+(?::\d+)?\b)", RegexOptions.Compiled)]
    private static partial Regex TechnicalTokenRegex();

    /// <summary>
    /// Replaces every ASCII digit (0-9) with its Eastern Arabic equivalent (٠-٩).
    /// </summary>
    public static string ToArabic(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        bool hasAsciiDigit = false;
        foreach (char ch in input)
        {
            if (ch is >= '0' and <= '9')
            {
                hasAsciiDigit = true;
                break;
            }
        }
        if (!hasAsciiDigit) return input;

        var sb = new StringBuilder(input.Length);
        foreach (char ch in input)
        {
            sb.Append(ch is >= '0' and <= '9' ? (char)(Zero + (ch - '0')) : ch);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Replaces ASCII digits with Eastern Arabic digits while preserving emails, URLs, and ISO/form codes (e.g. AU-GEA-CN-FRM-001, ISO 9001:2015).
    /// </summary>
    public static string ToArabicPreservingTechnical(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var matches = TechnicalTokenRegex().Matches(input);
        if (matches.Count == 0)
            return ToArabic(input);

        var sb = new StringBuilder(input.Length);
        int cursor = 0;
        foreach (Match match in matches)
        {
            if (match.Index > cursor)
                sb.Append(ToArabic(input[cursor..match.Index]));

            sb.Append(match.Value);
            cursor = match.Index + match.Length;
        }
        if (cursor < input.Length)
            sb.Append(ToArabic(input[cursor..]));

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes Eastern Arabic (٠-٩) and Persian (۰-۹) digits and Arabic decimal/thousands marks
    /// back into canonical ASCII digits for reliable math parsing and lookups.
    /// </summary>
    public static string NormalizeToAscii(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char ch in input)
        {
            if (ch is >= '٠' and <= '٩')
                sb.Append((char)('0' + (ch - '٠')));
            else if (ch is >= '۰' and <= '۹')
                sb.Append((char)('0' + (ch - '۰')));
            else if (ch == '٫')
                sb.Append('.');
            else if (ch == '٬' || ch == '،')
                sb.Append(',');
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }

    public static string FormatDecimal(decimal value, int decimals = 2, bool grouping = true)
    {
        string format = grouping ? $"N{decimals}" : $"F{decimals}";
        return ToArabic(value.ToString(format, CultureInfo.InvariantCulture));
    }

    public static string FormatInteger(long value, bool grouping = true)
    {
        string format = grouping ? "N0" : "0";
        return ToArabic(value.ToString(format, CultureInfo.InvariantCulture));
    }

    public static string FormatPounds(decimal value)
    {
        return ToArabic(value.ToString("#,##0", CultureInfo.InvariantCulture));
    }

    public static string FormatPiasters(int piasters)
    {
        return ToArabic(piasters.ToString("D2", CultureInfo.InvariantCulture));
    }

    public static string FormatCompactDecimal(decimal value)
    {
        return ToArabic(value.ToString("0.##", CultureInfo.InvariantCulture));
    }

    public static string FormatDate(DateOnly date, string pattern = "dd/MM/yyyy")
    {
        return ToArabic(date.ToString(pattern, CultureInfo.InvariantCulture));
    }

    public static string FormatDate(DateTime dateTime, string pattern = "yyyy/MM/dd")
    {
        return ToArabic(dateTime.ToString(pattern, CultureInfo.InvariantCulture));
    }
}
