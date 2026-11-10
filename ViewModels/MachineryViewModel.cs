namespace CentralPress.API.ViewModels;

public class MachineryViewModel
{
    public int MachineryId { get; set; }
    public string MachineryNameAr { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal WorkerHourlyFee { get; set; }
    public decimal MachineOperatingHourlyFee { get; set; }
    public decimal DepreciationHourlyRate { get; set; }
    public decimal MaintenanceHourlyRate { get; set; }
    public decimal TotalMachineOperatingHourlyFee { get; set; }
    public string MaintenanceStatus { get; set; } = "Operational";
    public string? MaintenanceNotes { get; set; }
    public bool IsActive { get; set; }
}
