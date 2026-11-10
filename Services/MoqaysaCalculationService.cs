using System;

namespace CentralPress.API.Services;

public class MoqaysaCalculationService
{
    public static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal Round4(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public MoqaysaCalculationResult Calculate(MoqaysaCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Guard against negative costs (HIGH-10)
        if (input.RawMaterials < 0m)
            throw new ArgumentException("قيمة الخامات يجب ألا تكون سالبة", nameof(input.RawMaterials));
        if (input.FilmsProcessingFee < 0m)
            throw new ArgumentException("قيمة تشغيل الأفلام يجب ألا تكون سالبة", nameof(input.FilmsProcessingFee));
        if (input.TotalWorkerWages < 0m)
            throw new ArgumentException("أجور العمال يجب ألا تكون سالبة", nameof(input.TotalWorkerWages));
        if (input.TotalMachineCosts < 0m)
            throw new ArgumentException("تكلفة تشغيل الآلات يجب ألا تكون سالبة", nameof(input.TotalMachineCosts));

        // Percentage range validation (LOW-05)
        ValidatePercentage(input.MaterialProcessingPercent, nameof(input.MaterialProcessingPercent));
        ValidatePercentage(input.AdminExpensesPercent, nameof(input.AdminExpensesPercent));
        ValidatePercentage(input.SelectedMarkupPercent, nameof(input.SelectedMarkupPercent));
        ValidatePercentage(input.MinistryOfFinancePercent, nameof(input.MinistryOfFinancePercent));
        ValidatePercentage(input.WorkersFundPercent, nameof(input.WorkersFundPercent));
        ValidatePercentage(input.OtherPercent, nameof(input.OtherPercent));
        ValidatePercentage(input.VatPercent, nameof(input.VatPercent));

        var res = new MoqaysaCalculationResult
        {
            RawMaterials = Round2(input.RawMaterials),
            FilmsProcessingFee = Round2(input.FilmsProcessingFee),
            MaterialProcessingPercent = input.MaterialProcessingPercent,
            AdminExpensesPercent = input.AdminExpensesPercent,
            MinistryOfFinancePercent = input.MinistryOfFinancePercent,
            WorkersFundPercent = input.WorkersFundPercent,
            OtherPercent = input.OtherPercent,
            VatPercent = input.VatPercent
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
        res.AdminExpensesPercent = input.AdminExpensesPercent;
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
        res.MinistryOfFinancePercent = input.MinistryOfFinancePercent;
        res.MinistryOfFinanceFee = Round2(diffForTaxes * (input.MinistryOfFinancePercent / 100m));

        // 36. ص. ت. العاملين (10%)
        res.WorkersFundPercent = input.WorkersFundPercent;
        res.WorkersFundBaseMode = input.WorkersFundBaseMode ?? "DiffTotalMinusMaterials";
        if (res.WorkersFundBaseMode == "MinistryOfFinanceValue")
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
        res.OtherPercent = input.OtherPercent;
        res.OtherAmount = input.ApplyOther
            ? Round2(res.SubTotal2 * (input.OtherPercent / 100m))
            : 0m;

        // 39. إجمالي
        res.SubTotal3 = Round2(res.SubTotal2 + res.OtherAmount);

        // 40. ض. القيمة المضافة (اختياري 14%)
        res.ApplyVat = input.ApplyVat;
        res.VatPercent = input.VatPercent;
        res.VatAmount = input.ApplyVat
            ? Round2(res.SubTotal3 * (input.VatPercent / 100m))
            : 0m;

        // 41. القيمة الإجمالية
        res.FinalGrandTotal = Round2(res.SubTotal3 + res.VatAmount);

        // MED-07: Unified Unit Price Calculation
        decimal qty = input.Quantity > 0m ? input.Quantity : 1.0m;
        res.ParsedQuantity = qty;
        res.UnitPriceBeforeVat = Round2(res.SubTotal3 / qty);
        res.FinalUnitPrice = Round2(res.FinalGrandTotal / qty);

        res.TafqeetArabicText = ArabicTafqeetService.ToWords(res.FinalGrandTotal);

        return res;
    }

    private static void ValidatePercentage(decimal percent, string paramName)
    {
        if (percent < 0m || percent > 100m)
        {
            throw new ArgumentOutOfRangeException(paramName, $"النسبة المئوية ({percent}) يجب أن تكون بين 0 و 100");
        }
    }
}
