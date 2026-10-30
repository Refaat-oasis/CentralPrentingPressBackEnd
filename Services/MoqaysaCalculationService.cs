using System;
using CentralPress.API.Entities;

namespace CentralPress.API.Services;

public class MoqaysaCalculationInput
{
    public decimal RawMaterials { get; set; }
    public decimal FilmsProcessingFee { get; set; } = 0.00m;
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }

    public bool ApplyAdminExpenses { get; set; }
    public bool ApplyMarkup { get; set; }
    public decimal SelectedMarkupPercent { get; set; } = 8.5m;
    public bool ApplyOther { get; set; }
    public bool ApplyVat { get; set; }

    public decimal MaterialProcessingPercent { get; set; } = 5.5m;
    public decimal AdminExpensesPercent { get; set; } = 8.5m;
    public decimal MinistryOfFinancePercent { get; set; } = 15.0m;
    public decimal WorkersFundPercent { get; set; } = 10.0m;
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials";
    public decimal OtherPercent { get; set; } = 10.0m;
    public decimal VatPercent { get; set; } = 14.0m;
}

public class MoqaysaCalculationResult
{
    public decimal RawMaterials { get; set; }              // 27. الخامات
    public decimal MaterialProcessingFee { get; set; }     // 28. م. تشغيل الخامات (5.5%)
    public decimal FilmsProcessingFee { get; set; }        // م. تشغيل الأفلام (0.00 default)
    public decimal TotalMaterials { get; set; }            // 29. جملة الخامات
    public decimal TotalWorkerWages { get; set; }          // 25. جملة أجور العمال
    public decimal TotalMachineCosts { get; set; }         // 25. جملة م. الآلات
    public decimal SubTotal1 { get; set; }                 // 30. الإجمالي
    public bool ApplyAdminExpenses { get; set; }           // 31. اختياري
    public decimal AdminExpenses { get; set; }             // 31. مصروفات إدارية (8.5%)
    public decimal TotalExpenses { get; set; }             // 32. إجمالي المصروفات
    public bool ApplyMarkup { get; set; }                  // 33. اختياري
    public decimal SelectedMarkupPercent { get; set; }     // 33. 8.5 أو 15 أو 30%
    public decimal MarkupAmount { get; set; }              // 33. النسبة
    public decimal TotalAfterMarkup { get; set; }          // 34. الجملة
    public decimal MinistryOfFinanceFee { get; set; }      // 35. وزارة المالية (15%)
    public decimal WorkersSyndicateFund { get; set; }      // 36. ص. ت. العاملين (10%)
    public decimal SubTotal2 { get; set; }                 // 37. الإجمالي
    public bool ApplyOther { get; set; }                   // 38. اختياري
    public decimal OtherAmount { get; set; }               // 38. أخرى (10%)
    public decimal SubTotal3 { get; set; }                 // 39. إجمالي
    public bool ApplyVat { get; set; }                     // 40. اختياري
    public decimal VatAmount { get; set; }                 // 40. ض. القيمة المضافة (14%)
    public decimal FinalGrandTotal { get; set; }           // 41. القيمة الإجمالية
}

public class MoqaysaCalculationService
{
    public static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public MoqaysaCalculationResult Calculate(MoqaysaCalculationInput input)
    {
        var res = new MoqaysaCalculationResult
        {
            RawMaterials = Round2(input.RawMaterials),
            FilmsProcessingFee = Round2(input.FilmsProcessingFee)
        };

        // 28. م. تشغيل الخامات (5.5%)
        res.MaterialProcessingFee = Round2(res.RawMaterials * (input.MaterialProcessingPercent / 100m));

        // 29. جملة الخامات
        res.TotalMaterials = Round2(res.RawMaterials + res.MaterialProcessingFee + res.FilmsProcessingFee);

        // 25. أجور ومصاريف الآلات
        res.TotalWorkerWages = Round2(input.TotalWorkerWages);
        res.TotalMachineCosts = Round2(input.TotalMachineCosts);

        // 30. الإجمالي
        res.SubTotal1 = Round2(res.TotalMaterials + res.TotalWorkerWages + res.TotalMachineCosts);

        // 31. مصروفات إدارية (اختياري 8.5%)
        res.ApplyAdminExpenses = input.ApplyAdminExpenses;
        res.AdminExpenses = input.ApplyAdminExpenses
            ? Round2(res.SubTotal1 * (input.AdminExpensesPercent / 100m))
            : 0m;

        // 32. إجمالي المصروفات
        res.TotalExpenses = Round2(res.SubTotal1 + res.AdminExpenses);

        // 33. النسبة (اختياري 8.5 أو 15 أو 30%)
        res.ApplyMarkup = input.ApplyMarkup;
        res.SelectedMarkupPercent = input.SelectedMarkupPercent;
        res.MarkupAmount = input.ApplyMarkup
            ? Round2(res.TotalExpenses * (input.SelectedMarkupPercent / 100m))
            : 0m;

        // 34. الجملة
        res.TotalAfterMarkup = Round2(res.TotalExpenses + res.MarkupAmount);

        // 35. وزارة المالية (15% * (الجملة - جملة الخامات))
        decimal diffForTaxes = Math.Max(0m, res.TotalAfterMarkup - res.TotalMaterials);
        res.MinistryOfFinanceFee = Round2(diffForTaxes * (input.MinistryOfFinancePercent / 100m));

        // 36. ص. ت. العاملين (10%)
        if (input.WorkersFundBaseMode == "MinistryOfFinanceValue")
        {
            res.WorkersSyndicateFund = Round2(res.MinistryOfFinanceFee * (input.WorkersFundPercent / 100m));
        }
        else
        {
            res.WorkersSyndicateFund = Round2(diffForTaxes * (input.WorkersFundPercent / 100m));
        }

        // 37. الإجمالي
        res.SubTotal2 = Round2(res.TotalAfterMarkup + res.MinistryOfFinanceFee + res.WorkersSyndicateFund);

        // 38. أخرى (اختياري 10%)
        res.ApplyOther = input.ApplyOther;
        res.OtherAmount = input.ApplyOther
            ? Round2(res.SubTotal2 * (input.OtherPercent / 100m))
            : 0m;

        // 39. إجمالي
        res.SubTotal3 = Round2(res.SubTotal2 + res.OtherAmount);

        // 40. ض. القيمة المضافة (اختياري 14%)
        res.ApplyVat = input.ApplyVat;
        res.VatAmount = input.ApplyVat
            ? Round2(res.SubTotal3 * (input.VatPercent / 100m))
            : 0m;

        // 41. القيمة الإجمالية
        res.FinalGrandTotal = Round2(res.SubTotal3 + res.VatAmount);

        return res;
    }
}
