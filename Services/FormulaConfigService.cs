using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class FormulaConfigService : IFormulaConfigService
{
    private static readonly HashSet<string> ValidWorkersFundModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DiffTotalMinusMaterials",
        "MinistryOfFinanceValue"
    };

    private readonly ApplicationDbContext _context;

    public FormulaConfigService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FormulaConfigViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.CostingFormulaConfigs
            .OrderBy(c => c.StepOrder)
            .ToListAsync(ct);

        return list.Select(MapToViewModel).ToList();
    }

    public async Task<FormulaConfigViewModel?> GetByFieldKeyAsync(string fieldKey, CancellationToken ct = default)
    {
        var config = await _context.CostingFormulaConfigs
            .FirstOrDefaultAsync(c => c.FieldKey == fieldKey, ct);

        return config == null ? null : MapToViewModel(config);
    }

    public async Task UpdateConfigsAsync(List<UpdateFormulaConfigDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            // LOW-05: Range validation [0, 100]
            if (item.DefaultPercentage < 0m || item.DefaultPercentage > 100m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(item.DefaultPercentage),
                    $"النسبة الافتراضية ({item.DefaultPercentage}) يجب أن تكون بين 0 و 100%");
            }

            // Validate WorkersFundBaseMode if provided
            if (!string.IsNullOrWhiteSpace(item.WorkersFundBaseMode))
            {
                var mode = item.WorkersFundBaseMode.Trim();
                if (!ValidWorkersFundModes.Contains(mode))
                {
                    throw new ArgumentException(
                        $"أسلوب حساب صندوق العاملين '{mode}' غير معتمد. الخيارات المتاحة: DiffTotalMinusMaterials أو MinistryOfFinanceValue",
                        nameof(item.WorkersFundBaseMode));
                }
            }

            // Validate AllowedOptionsCsv if provided
            if (!string.IsNullOrWhiteSpace(item.AllowedOptionsCsv))
            {
                var tokens = item.AllowedOptionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var token in tokens)
                {
                    if (!decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var opt) || opt < 0m || opt > 100m)
                    {
                        throw new ArgumentException(
                            $"القيمة '{token}' في الخيارات المسموحة غير صالحة. يجب أن تكون كل قيمة رقماً بين 0 و 100",
                            nameof(item.AllowedOptionsCsv));
                    }
                }
            }
        }

        foreach (var item in items)
        {
            var config = await _context.CostingFormulaConfigs.FindAsync(new object[] { item.FormulaConfigId }, ct);
            if (config != null)
            {
                config.DefaultPercentage = item.DefaultPercentage;
                if (!string.IsNullOrWhiteSpace(item.WorkersFundBaseMode))
                {
                    config.WorkersFundBaseMode = item.WorkersFundBaseMode.Trim();
                }
                if (item.AllowedOptionsCsv != null)
                {
                    config.AllowedOptionsCsv = item.AllowedOptionsCsv.Trim();
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private static FormulaConfigViewModel MapToViewModel(CostingFormulaConfig c) => new()
    {
        FormulaConfigId = c.FormulaConfigId,
        StepOrder = c.StepOrder,
        FieldKey = c.FieldKey,
        FieldLabelAr = c.FieldLabelAr,
        DefaultPercentage = c.DefaultPercentage,
        AllowedOptionsCsv = c.AllowedOptionsCsv,
        IsOptional = c.IsOptional,
        WorkersFundBaseMode = c.WorkersFundBaseMode,
        FormulaExpressionDescriptionAr = c.FormulaExpressionDescriptionAr,
        IsActive = c.IsActive
    };
}
