namespace CentralPress.API.Documents;

public sealed record TasweyaPrintData(
    string SettlementNo,
    DateTime Date,
    string ClaimNo,
    MoqaysaPrintData Moqaysa,
    IReadOnlyList<AccountLine> Accounts);
