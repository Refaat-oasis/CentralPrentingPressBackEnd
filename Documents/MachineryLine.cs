namespace CentralPress.API.Documents;

public sealed record MachineryLine(
    string Name,                 // MachineryNameSnapshot
    decimal Hours,               // Hours
    decimal WorkerRate,          // WorkerHourlyRateSnapshot
    decimal WorkerValue,         // TotalWorkerWages
    decimal MachineHours,        // MachineHours
    decimal MachineRate,         // MachineHourlyRateSnapshot
    decimal MachineValue);       // TotalMachineOperatingCost
