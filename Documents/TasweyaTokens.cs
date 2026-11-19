using System.Globalization;

namespace CentralPress.API.Documents;

public static class TasweyaTokens
{
    public static (string Template, Dictionary<string, object?> Model) Build(TasweyaPrintData d, IAmountInWords words)
    {
        var m = d.Moqaysa;
        if (d.Accounts.Count == 0)
            throw new PrintDataException("The job order has no debit account for the tasweya.");

        var accountsList = d.Accounts.Select(a => new AccountLine(a.Name, a.Amount)).ToList();
        var accountsTotal = accountsList.Sum(a => a.Amount);

        // Reconcile penny rounding differences (<= 0.05) if any
        if (Math.Abs(accountsTotal - m.Total) <= 0.05m && accountsTotal != m.Total && accountsList.Count > 0)
        {
            var diff = m.Total - accountsTotal;
            var last = accountsList[^1];
            accountsList[^1] = new AccountLine(last.Name, last.Amount + diff);
            accountsTotal = accountsList.Sum(a => a.Amount);
        }

        var revPlusVat = m.Revenue + m.Vat;
        var revenue = m.Revenue;
        if (Math.Abs(revPlusVat - m.Total) <= 0.05m && revPlusVat != m.Total)
        {
            revenue = m.Total - m.Vat;
        }

        if (accountsTotal != m.Total || revenue + m.Vat != m.Total)
        {
            throw new PrintDataException(
                $"Tasweya does not balance: accounts {accountsTotal:0.00}, revenue + VAT {revenue + m.Vat:0.00}, total {m.Total:0.00}.");
        }

        var model = new Dictionary<string, object?>
        {
            ["SettlementNo"] = d.SettlementNo,
            ["Date"] = d.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            ["JobOrderNo"] = m.JobOrderNo,
            ["Requester"] = m.Requester,
            ["ClaimNo"] = d.ClaimNo,
            ["Accounts"] = accountsList.Select(a => (object?)new Dictionary<string, object?>
            {
                ["Name"] = a.Name,
                ["Amount"] = a.Amount
            }).ToList(),
            ["Total"] = m.Total,
            ["Revenue"] = revenue,
            ["VAT"] = m.Vat,
            ["Materials"] = m.MaterialsTotal,
            ["Labour"] = m.LabourTotal,
            ["Machinery"] = m.MachineryTotal,
            ["Admin"] = m.Admin,
            ["Percentage"] = m.Percentage,
            ["Ministry"] = m.Ministry,
            ["WorkersFund"] = m.WorkersFund,
            ["Other"] = m.Other,
            ["CreditSum"] = revenue,
            ["AmountInWords"] = words.ToArabicCurrency(m.Total)
        };

        return (accountsList.Count == 1 ? "Tasweya_Single.docx" : "Tasweya_Multi.docx", model);
    }
}
