using System;
using System.Text.RegularExpressions;

namespace CentralPress.API.Helpers;

/// <summary>
/// Helper for calculating and formatting Egyptian University Central Printing Press financial years.
/// The financial year starts on May 1st and ends on April 30th of the following year.
/// </summary>
public static class FinancialYearHelper
{
    public record FinancialYearInfo(
        DateOnly StartDate,
        DateOnly EndDate,
        string Code,
        int StartYear,
        int EndYear
    );

    /// <summary>
    /// Gets the financial year bounds and label for a given date.
    /// </summary>
    public static FinancialYearInfo GetFinancialYear(DateOnly date)
    {
        int startYear = date.Month >= 5 ? date.Year : date.Year - 1;
        int endYear = startYear + 1;

        var startDate = new DateOnly(startYear, 5, 1);
        var endDate = new DateOnly(endYear, 4, 30);
        string code = $"{startYear}/{endYear}";

        return new FinancialYearInfo(startDate, endDate, code, startYear, endYear);
    }

    /// <summary>
    /// Ensures a settlement number is tagged with its financial year or timestamp if entered as a bare number.
    /// Example: "55" -> "55 / 2026"
    /// If user already wrote "55 / 2026" or "55 / 2026-2027", preserves user format.
    /// </summary>
    public static string FormatSettlementNumber(string? rawNumber, DateOnly settlementDate)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return string.Empty;

        string trimmed = rawNumber.Trim();

        // If it already contains a slash, dash with year, or letters, keep as specified by operator
        if (trimmed.Contains('/') || trimmed.Contains('-') || trimmed.Contains('\\'))
        {
            return trimmed;
        }

        // If it's a bare number (e.g. "55"), append the starting calendar year of the financial period
        var fy = GetFinancialYear(settlementDate);
        return $"{trimmed} / {fy.StartYear}";
    }
}

