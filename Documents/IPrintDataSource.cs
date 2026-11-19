namespace CentralPress.API.Documents;

public interface IPrintDataSource
{
    Task<MoqaysaPrintData> LoadMoqaysaAsync(int moqaysaId, CancellationToken ct);
    Task<TasweyaPrintData> LoadTasweyaAsync(int tasweyaOrMoqaysaId, CancellationToken ct);
}
