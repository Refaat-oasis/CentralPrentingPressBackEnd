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

public class MaterialService : IMaterialService
{
    private readonly ApplicationDbContext _context;

    public MaterialService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MaterialViewModel>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.Materials.AsQueryable();
        if (activeOnly)
            query = query.Where(m => m.IsActive);

        var list = await query.ToListAsync(ct);

        // LOW-03: Culture-aware Arabic sorting (ar-EG)
        var culture = new CultureInfo("ar-EG");
        var comparer = StringComparer.Create(culture, false);

        return list
            .OrderBy(m => m.MaterialNameAr, comparer)
            .Select(MapToViewModel)
            .ToList();
    }

    public async Task<MaterialViewModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Materials.FindAsync(new object[] { id }, ct);
        return item == null ? null : MapToViewModel(item);
    }

    public async Task<MaterialViewModel> CreateAsync(MaterialDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.MaterialNameAr))
            throw new ArgumentException("يرجى كتابة اسم الخامة", nameof(dto.MaterialNameAr));

        var item = new Material
        {
            MaterialNameAr = dto.MaterialNameAr.Trim(),
            UnitNameAr = string.IsNullOrWhiteSpace(dto.UnitNameAr) ? "فرخ" : dto.UnitNameAr.Trim(),
            IsActive = true
        };

        _context.Materials.Add(item);
        await _context.SaveChangesAsync(ct);

        return MapToViewModel(item);
    }

    public async Task<MaterialViewModel?> UpdateAsync(int id, MaterialDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var item = await _context.Materials.FindAsync(new object[] { id }, ct);
        if (item == null) return null;

        if (string.IsNullOrWhiteSpace(dto.MaterialNameAr))
            throw new ArgumentException("يرجى كتابة اسم الخامة", nameof(dto.MaterialNameAr));

        item.MaterialNameAr = dto.MaterialNameAr.Trim();
        item.UnitNameAr = string.IsNullOrWhiteSpace(dto.UnitNameAr) ? "فرخ" : dto.UnitNameAr.Trim();

        await _context.SaveChangesAsync(ct);
        return MapToViewModel(item);
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Materials.FindAsync(new object[] { id }, ct);
        if (item == null) return false;

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync(ct);
        return item.IsActive;
    }

    private static MaterialViewModel MapToViewModel(Material m) => new()
    {
        MaterialId = m.MaterialId,
        MaterialNameAr = m.MaterialNameAr,
        UnitNameAr = m.UnitNameAr,
        StandardUnitPrice = 0m,
        CategoryAr = null,
        IsActive = m.IsActive
    };
}
