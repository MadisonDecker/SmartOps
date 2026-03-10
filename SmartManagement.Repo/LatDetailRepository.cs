using Microsoft.EntityFrameworkCore;
using SmartManagement.Repo.Models;

namespace SmartManagement.Repo;

/// <summary>
/// Repository for LATDetail data operations.
/// </summary>
public class LatDetailRepository
{
    private readonly SmartOpsContext _context;

    public LatDetailRepository(SmartOpsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all LAT details.
    /// </summary>
    public async Task<List<Latdetail>> GetAllAsync()
    {
        return await _context.Latdetails.ToListAsync();
    }

    /// <summary>
    /// Gets a LAT detail by ID.
    /// </summary>
    public async Task<Latdetail?> GetByIdAsync(int id)
    {
        return await _context.Latdetails.FindAsync(id);
    }

    /// <summary>
    /// Gets LAT details by client abbreviation.
    /// </summary>
    public async Task<List<Latdetail>> GetByClientAsync(string clientAbbr)
    {
        return await _context.Latdetails
            .Where(l => l.ClientAbbr == clientAbbr)
            .ToListAsync();
    }

    /// <summary>
    /// Gets LAT details for a specific date range.
    /// </summary>
    public async Task<List<Latdetail>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.Latdetails
            .Where(l => l.RequiredDate >= startDate && l.RequiredDate <= endDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets LAT details by client and date range.
    /// </summary>
    public async Task<List<Latdetail>> GetByClientAndDateRangeAsync(string clientAbbr, DateOnly startDate, DateOnly endDate)
    {
        return await _context.Latdetails
            .Where(l => l.ClientAbbr == clientAbbr && l.RequiredDate >= startDate && l.RequiredDate <= endDate)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new LAT detail.
    /// </summary>
    public async Task<Latdetail> AddAsync(Latdetail latDetail)
    {
        latDetail.InsertedUtcDate = DateTime.UtcNow;
        latDetail.LastUpdatedDate = DateTime.UtcNow;

        _context.Latdetails.Add(latDetail);
        await _context.SaveChangesAsync();

        return latDetail;
    }

    /// <summary>
    /// Updates an existing LAT detail.
    /// </summary>
    public async Task<Latdetail?> UpdateAsync(Latdetail latDetail)
    {
        var existing = await _context.Latdetails.FindAsync(latDetail.LatdetailId);
        if (existing == null)
        {
            return null;
        }

        existing.ClientAbbr = latDetail.ClientAbbr;
        existing.CampAbbr = latDetail.CampAbbr;
        existing.WorkGroup = latDetail.WorkGroup;
        existing.RequiredDate = latDetail.RequiredDate;
        existing.RequiredTime = latDetail.RequiredTime;
        existing.RequiredHours = latDetail.RequiredHours;
        existing.LastUpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Deletes a LAT detail by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var latDetail = await _context.Latdetails.FindAsync(id);
        if (latDetail == null)
        {
            return false;
        }

        _context.Latdetails.Remove(latDetail);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Saves multiple LAT details in a batch.
    /// </summary>
    public async Task<List<Latdetail>> SaveBatchAsync(List<Latdetail> latDetails)
    {
        var now = DateTime.UtcNow;

        foreach (var detail in latDetails)
        {
            if (detail.LatdetailId == 0)
            {
                detail.InsertedUtcDate = now;
                detail.LastUpdatedDate = now;
                _context.Latdetails.Add(detail);
            }
            else
            {
                var existing = await _context.Latdetails.FindAsync(detail.LatdetailId);
                if (existing != null)
                {
                    existing.ClientAbbr = detail.ClientAbbr;
                    existing.CampAbbr = detail.CampAbbr;
                    existing.WorkGroup = detail.WorkGroup;
                    existing.RequiredDate = detail.RequiredDate;
                    existing.RequiredTime = detail.RequiredTime;
                    existing.RequiredHours = detail.RequiredHours;
                    existing.LastUpdatedDate = now;
                }
            }
        }

        await _context.SaveChangesAsync();

        return latDetails;
    }
}
