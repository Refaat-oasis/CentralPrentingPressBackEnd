namespace CentralPress.API.ViewModels;

public class MoqaysaMachineryItemViewModel
{
    public int ItemId { get; set; }
    public int MoqaysaId { get; set; }
    public int MachineryId { get; set; }
    public string MachineryNameSnapshot { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal? MachineHours { get; set; }
    public decimal WorkerHourlyRateSnapshot { get; set; }
    public decimal TotalWorkerWages { get; set; }
    public decimal MachineHourlyRateSnapshot { get; set; }
    public decimal TotalMachineOperatingCost { get; set; }
}
