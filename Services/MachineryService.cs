using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class MachineryService : IMachineryService
{
    private readonly ApplicationDbContext _context;

    public MachineryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MachineryViewModel>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.Machineries.AsQueryable();
        if (activeOnly)
            query = query.Where(m => m.IsActive);

        var list = await query
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.MachineryId)
            .ToListAsync(ct);

        return list.Select(MapToViewModel).ToList();
    }

    public async Task<MachineryViewModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Machineries.FindAsync(new object[] { id }, ct);
        return item == null ? null : MapToViewModel(item);
    }

    public async Task<MachineryViewModel> CreateAsync(MachineryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.MachineryNameAr))
            throw new ArgumentException("يرجى كتابة اسم الآلة / العملية", nameof(dto.MachineryNameAr));

        // LOW-05: Non-negative validation
        ValidateNonNegative(dto.WorkerHourlyFee, nameof(dto.WorkerHourlyFee), "أجر العامل بالساعة يجب ألا يكون سالباً");
        ValidateNonNegative(dto.MachineOperatingHourlyFee, nameof(dto.MachineOperatingHourlyFee), "تكلفة تشغيل الآلة بالساعة يجب ألا تكون سالبة");
        ValidateNonNegative(dto.DepreciationHourlyRate, nameof(dto.DepreciationHourlyRate), "معدل إهلاك الآلة بالساعة يجب ألا يكون سالباً");
        ValidateNonNegative(dto.MaintenanceHourlyRate, nameof(dto.MaintenanceHourlyRate), "معدل صيانة الآلة بالساعة يجب ألا يكون سالباً");

        // LOW-04: Async MaxAsync with CancellationToken and .IgnoreQueryFilters()
        int displayOrder = dto.DisplayOrder > 0
            ? dto.DisplayOrder
            : (await _context.Machineries.IgnoreQueryFilters().MaxAsync(m => (int?)m.DisplayOrder, ct) ?? 0) + 1;

        var machinery = new Machinery
        {
            MachineryNameAr = dto.MachineryNameAr.Trim(),
            WorkerHourlyFee = dto.WorkerHourlyFee,
            MachineOperatingHourlyFee = dto.MachineOperatingHourlyFee,
            DepreciationHourlyRate = dto.DepreciationHourlyRate,
            MaintenanceHourlyRate = dto.MaintenanceHourlyRate,
            MaintenanceStatus = string.IsNullOrWhiteSpace(dto.MaintenanceStatus) ? "Operational" : dto.MaintenanceStatus.Trim(),
            MaintenanceNotes = dto.MaintenanceNotes?.Trim(),
            DisplayOrder = displayOrder,
            IsActive = true
        };

        _context.Machineries.Add(machinery);
        await _context.SaveChangesAsync(ct);

        return MapToViewModel(machinery);
    }

    public async Task<MachineryViewModel?> UpdateAsync(int id, MachineryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var item = await _context.Machineries.FindAsync(new object[] { id }, ct);
        if (item == null) return null;

        if (string.IsNullOrWhiteSpace(dto.MachineryNameAr))
            throw new ArgumentException("يرجى كتابة اسم الآلة / العملية", nameof(dto.MachineryNameAr));

        // LOW-05: Non-negative validation
        ValidateNonNegative(dto.WorkerHourlyFee, nameof(dto.WorkerHourlyFee), "أجر العامل بالساعة يجب ألا يكون سالباً");
        ValidateNonNegative(dto.MachineOperatingHourlyFee, nameof(dto.MachineOperatingHourlyFee), "تكلفة تشغيل الآلة بالساعة يجب ألا تكون سالبة");
        ValidateNonNegative(dto.DepreciationHourlyRate, nameof(dto.DepreciationHourlyRate), "معدل إهلاك الآلة بالساعة يجب ألا يكون سالباً");
        ValidateNonNegative(dto.MaintenanceHourlyRate, nameof(dto.MaintenanceHourlyRate), "معدل صيانة الآلة بالساعة يجب ألا يكون سالباً");

        item.MachineryNameAr = dto.MachineryNameAr.Trim();
        item.WorkerHourlyFee = dto.WorkerHourlyFee;
        item.MachineOperatingHourlyFee = dto.MachineOperatingHourlyFee;
        item.DepreciationHourlyRate = dto.DepreciationHourlyRate;
        item.MaintenanceHourlyRate = dto.MaintenanceHourlyRate;
        if (!string.IsNullOrWhiteSpace(dto.MaintenanceStatus))
            item.MaintenanceStatus = dto.MaintenanceStatus.Trim();
        if (dto.MaintenanceNotes != null)
            item.MaintenanceNotes = dto.MaintenanceNotes.Trim();
        if (dto.DisplayOrder > 0)
            item.DisplayOrder = dto.DisplayOrder;

        await _context.SaveChangesAsync(ct);
        return MapToViewModel(item);
    }

    public async Task<MachineryViewModel?> UpdateMaintenanceStatusAsync(int id, UpdateMachineryMaintenanceDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var item = await _context.Machineries.FindAsync(new object[] { id }, ct);
        if (item == null) return null;

        if (string.IsNullOrWhiteSpace(dto.MaintenanceStatus))
            throw new ArgumentException("يرجى تحديد حالة الصيانة", nameof(dto.MaintenanceStatus));

        item.MaintenanceStatus = dto.MaintenanceStatus.Trim();
        if (dto.MaintenanceNotes != null)
            item.MaintenanceNotes = dto.MaintenanceNotes.Trim();

        await _context.SaveChangesAsync(ct);
        return MapToViewModel(item);
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Machineries.FindAsync(new object[] { id }, ct);
        if (item == null) return false;

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync(ct);
        return item.IsActive;
    }

    private static void ValidateNonNegative(decimal value, string paramName, string errorMessage)
    {
        if (value < 0m)
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
    }

    private static MachineryViewModel MapToViewModel(Machinery m)
    {
        decimal totalMachineOperating = m.MachineOperatingHourlyFee + m.DepreciationHourlyRate + m.MaintenanceHourlyRate;
        return new MachineryViewModel
        {
            MachineryId = m.MachineryId,
            MachineryNameAr = m.MachineryNameAr,
            DisplayOrder = m.DisplayOrder,
            WorkerHourlyFee = m.WorkerHourlyFee,
            MachineOperatingHourlyFee = m.MachineOperatingHourlyFee,
            DepreciationHourlyRate = m.DepreciationHourlyRate,
            MaintenanceHourlyRate = m.MaintenanceHourlyRate,
            TotalMachineOperatingHourlyFee = totalMachineOperating,
            MaintenanceStatus = string.IsNullOrWhiteSpace(m.MaintenanceStatus) ? "Operational" : m.MaintenanceStatus,
            MaintenanceNotes = m.MaintenanceNotes,
            IsActive = m.IsActive
        };
    }
}
