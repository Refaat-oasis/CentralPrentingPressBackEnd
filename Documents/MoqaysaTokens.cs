using System.Globalization;

namespace CentralPress.API.Documents;

public static class MoqaysaTokens
{
    public const int MachinerySlots = 11;
    public const int MaterialSlots = 25;

    public static Dictionary<string, object?> Build(MoqaysaPrintData d)
    {
        static Dictionary<string, object?> Row(string name, decimal hours, decimal rate, decimal value) => new()
        {
            ["Name"] = name,
            ["Hours"] = hours == 0 ? "" : hours.ToString("0.##", CultureInfo.InvariantCulture),
            ["Rate"] = hours == 0 ? null : rate,
            ["Value"] = hours == 0 ? null : value
        };

        return new Dictionary<string, object?>
        {
            ["JobOrderNo"] = d.JobOrderNo,
            ["Requester"] = d.Requester,
            ["Quantity"] = d.Quantity,
            ["UnitPrice"] = string.IsNullOrWhiteSpace(d.UnitPrice)
                ? string.Empty
                : d.UnitPrice.Trim().TrimStart(':').Trim(),
            ["Specs"] = !string.IsNullOrEmpty(d.Specs) && d.Specs.Length > 150 ? d.Specs[..150] : (d.Specs ?? string.Empty),
            ["Labour"] = d.Machinery.Select(m => (object?)Row(m.Name, m.Hours, m.WorkerRate, m.WorkerValue)).ToList(),
            ["Machinery"] = d.Machinery.Select(m => (object?)Row(m.Name, m.MachineHours, m.MachineRate, m.MachineValue)).ToList(),
            ["Mat"] = d.Materials.Select(m => (object?)new Dictionary<string, object?>
            {
                ["Name"] = m.Name,
                ["Qty"] = m.Qty.ToString("0.##", CultureInfo.InvariantCulture),
                ["Price"] = m.Price,
                ["Total"] = m.Total
            }).ToList(),
            ["LabourTotal"] = d.LabourTotal,
            ["MachineryTotal"] = d.MachineryTotal,
            ["MatGross"] = d.MatGross,
            ["MatReturns"] = d.MatReturns == 0m ? null : d.MatReturns,
            ["MatNet"] = d.MatNet,
            ["OpsMaterials"] = d.OpsMaterials == 0m ? null : d.OpsMaterials,
            ["OpsFilms"] = d.OpsFilms == 0m ? null : d.OpsFilms,
            ["MaterialsTotal"] = d.MaterialsTotal,
            ["DirectCost"] = d.DirectCost,
            ["Admin"] = d.Admin == 0m ? null : d.Admin,
            ["ExpensesTotal"] = d.ExpensesTotal,
            ["Percentage"] = d.Percentage == 0m ? null : d.Percentage,
            ["Subtotal"] = d.Subtotal,
            ["Ministry"] = d.Ministry == 0m ? null : d.Ministry,
            ["WorkersFund"] = d.WorkersFund == 0m ? null : d.WorkersFund,
            ["TotalA"] = d.TotalA,
            ["Other"] = d.Other == 0m ? null : d.Other,
            ["Revenue"] = d.Revenue,
            ["VAT"] = d.Vat == 0m ? null : d.Vat,
            ["Total"] = d.Total
        };
    }
}
