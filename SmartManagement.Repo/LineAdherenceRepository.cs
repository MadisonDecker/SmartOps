using Microsoft.EntityFrameworkCore;
using SmartManagement.Repo.Models;

namespace SmartManagement.Repo;

/// <summary>
/// Repository for LineAdherence data operations.
/// </summary>
public class LineAdherenceRepository
{
    private readonly SmartOpsContext _context;

    public LineAdherenceRepository(SmartOpsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all line adherence records.
    /// </summary>
    public async Task<List<LineAdherence>> GetAllAsync()
    {
        return await _context.LineAdherences.ToListAsync();
    }

    /// <summary>
    /// Gets a line adherence record by ID.
    /// </summary>
    public async Task<LineAdherence?> GetByIdAsync(int id)
    {
        return await _context.LineAdherences.FindAsync(id);
    }

    /// <summary>
    /// Gets line adherence records by client abbreviation.
    /// </summary>
    public async Task<List<LineAdherence>> GetByClientAsync(string clientAbbr)
    {
        return await _context.LineAdherences
            .Where(l => l.ClientAbbr == clientAbbr)
            .ToListAsync();
    }

    /// <summary>
    /// Gets line adherence records for a specific date range.
    /// </summary>
    public async Task<List<LineAdherence>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.LineAdherences
            .Where(l => l.RequiredDate >= startDate && l.RequiredDate <= endDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets line adherence records by client and date range.
    /// </summary>
    public async Task<List<LineAdherence>> GetByClientAndDateRangeAsync(string clientAbbr, DateOnly startDate, DateOnly endDate)
    {
        return await _context.LineAdherences
            .Where(l => l.ClientAbbr == clientAbbr && l.RequiredDate >= startDate && l.RequiredDate <= endDate)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new line adherence record.
    /// </summary>
    public async Task<LineAdherence> AddAsync(LineAdherence lineAdherence)
    {
        lineAdherence.InsertedUtcDate = DateTime.UtcNow;
        lineAdherence.LastUpdatedDate = DateTime.UtcNow;

        _context.LineAdherences.Add(lineAdherence);
        await _context.SaveChangesAsync();

        return lineAdherence;
    }

    /// <summary>
    /// Updates an existing line adherence record.
    /// </summary>
    public async Task<LineAdherence?> UpdateAsync(LineAdherence lineAdherence)
    {
        var existing = await _context.LineAdherences.FindAsync(lineAdherence.LineAdherenceId);
        if (existing == null)
        {
            return null;
        }

        existing.ClientAbbr = lineAdherence.ClientAbbr;
        existing.RequiredDate = lineAdherence.RequiredDate;
        existing.RequiredTime = lineAdherence.RequiredTime;
        existing.RequiredHours = lineAdherence.RequiredHours;
        existing.LastUpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Deletes a line adherence record by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var lineAdherence = await _context.LineAdherences.FindAsync(id);
        if (lineAdherence == null)
        {
            return false;
        }

        _context.LineAdherences.Remove(lineAdherence);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Saves multiple line adherence records in a batch.
    /// </summary>
    public async Task<List<LineAdherence>> SaveBatchAsync(List<LineAdherence> lineAdherences)
    {
        var now = DateTime.UtcNow;

        foreach (var detail in lineAdherences)
        {
            if (detail.LineAdherenceId == 0)
            {
                detail.InsertedUtcDate = now;
                detail.LastUpdatedDate = now;
                _context.LineAdherences.Add(detail);
            }
            else
            {
                var existing = await _context.LineAdherences.FindAsync(detail.LineAdherenceId);
                if (existing != null)
                {
                    existing.ClientAbbr = detail.ClientAbbr;
                    existing.RequiredDate = detail.RequiredDate;
                    existing.RequiredTime = detail.RequiredTime;
                    existing.RequiredHours = detail.RequiredHours;
                    existing.LastUpdatedDate = now;
                }
            }
        }

        await _context.SaveChangesAsync();

        return lineAdherences;
    }
}
