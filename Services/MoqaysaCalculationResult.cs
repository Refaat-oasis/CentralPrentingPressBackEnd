namespace CentralPress.API.Services;

public class MoqaysaCalculationResult
{
    public decimal RawMaterials { get; set; }              // 27. الخامات
    public decimal MaterialProcessingPercent { get; set; }
    public decimal MaterialProcessingFee { get; set; }     // 28. م. تشغيل الخامات (5.5%)
    public decimal FilmsProcessingFee { get; set; }        // م. تشغيل الأفلام (0.00 default)
    public decimal TotalMaterials { get; set; }            // 29. جملة الخامات

    public decimal TotalWorkerWages { get; set; }          // 25. جملة أجور العمال
    public decimal TotalMachineCosts { get; set; }         // 25. جملة م. الآلات
    public decimal SubTotal1 { get; set; }                 // 30. الإجمالي

    public bool ApplyAdminExpenses { get; set; }           // 31. اختياري
    public decimal AdminExpensesPercent { get; set; }
    public decimal AdminExpenses { get; set; }             // 31. مصروفات إدارية (8.5%)
    public decimal TotalExpenses { get; set; }             // 32. إجمالي المصروفات

    public bool ApplyMarkup { get; set; }                  // 33. اختياري
    public decimal SelectedMarkupPercent { get; set; }     // 33. 8.5 أو 15 أو 30%
    public decimal MarkupAmount { get; set; }              // 33. النسبة
    public decimal TotalAfterMarkup { get; set; }          // 34. الجملة

    public decimal MinistryOfFinancePercent { get; set; }
    public decimal MinistryOfFinanceFee { get; set; }      // 35. وزارة المالية (15%)
    public decimal WorkersFundPercent { get; set; }
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials";
    public decimal WorkersSyndicateFund { get; set; }      // 36. ص. ت. العاملين (10%)

    public decimal SubTotal2 { get; set; }                 // 37. الإجمالي
    public bool ApplyOther { get; set; }                   // 38. اختياري
    public decimal OtherPercent { get; set; }
    public decimal OtherAmount { get; set; }               // 38. أخرى (10%)
    public decimal SubTotal3 { get; set; }                 // 39. إجمالي

    public bool ApplyVat { get; set; }                     // 40. اختياري
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }                 // 40. ض. القيمة المضافة (14%)
    public decimal FinalGrandTotal { get; set; }           // 41. القيمة الإجمالية

    public decimal ParsedQuantity { get; set; } = 1.0m;
    public decimal UnitPriceBeforeVat { get; set; }        // MED-07: SubTotal3 / ParsedQuantity
    public decimal FinalUnitPrice { get; set; }            // MED-07: FinalGrandTotal / ParsedQuantity
    public string? TafqeetArabicText { get; set; }
}
