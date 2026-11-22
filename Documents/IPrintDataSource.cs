namespace CentralPress.API.Documents;

public interface IPrintDataSource
{
    Task<MoqaysaPrintData> LoadMoqaysaAsync(int moqaysaId, CancellationToken ct);
    Task<TasweyaPrintData> LoadTasweyaAsync(int tasweyaOrMoqaysaId, CancellationToken ct);
    Task<IReadOnlyList<int>> LoadLinkedMoqaysaIdsAsync(int tasweyaId, CancellationToken ct);
    Task<IReadOnlyList<string>> LoadActiveMachineryNamesAsync(CancellationToken ct);
}
