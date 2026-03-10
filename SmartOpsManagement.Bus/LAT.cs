using SmartManagement.Repo;
using SmartManagement.Repo.Models;

namespace SmartOpsManagement.Bus
{
    /// <summary>
    /// Business logic for LAT (Labor Activity Tracking) operations.
    /// </summary>
    public partial class SmartOpsBusinessLogic
    {
        private LatDetailRepository? _latDetailRepository;

        private LatDetailRepository LatDetailRepository => _latDetailRepository ??= new LatDetailRepository(_context);

        /// <summary>
        /// Gets all LAT details.
        /// </summary>
        public async Task<List<Latdetail>> GetAllLatDetailsAsync()
        {
            return await LatDetailRepository.GetAllAsync();
        }

        /// <summary>
        /// Gets a LAT detail by ID.
        /// </summary>
        public async Task<Latdetail?> GetLatDetailByIdAsync(int id)
        {
            return await LatDetailRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets LAT details by client abbreviation.
        /// </summary>
        public async Task<List<Latdetail>> GetLatDetailsByClientAsync(string clientAbbr)
        {
            return await LatDetailRepository.GetByClientAsync(clientAbbr);
        }

        /// <summary>
        /// Gets LAT details for a specific date range.
        /// </summary>
        public async Task<List<Latdetail>> GetLatDetailsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await LatDetailRepository.GetByDateRangeAsync(startDate, endDate);
        }

        /// <summary>
        /// Gets LAT details by client and date range.
        /// </summary>
        public async Task<List<Latdetail>> GetLatDetailsByClientAndDateRangeAsync(string clientAbbr, DateOnly startDate, DateOnly endDate)
        {
            return await LatDetailRepository.GetByClientAndDateRangeAsync(clientAbbr, startDate, endDate);
        }

        /// <summary>
        /// Saves a LAT detail (creates new or updates existing).
        /// </summary>
        public async Task<Latdetail?> SaveLatDetailAsync(Latdetail latDetail)
        {
            if (latDetail.LatdetailId == 0)
            {
                return await LatDetailRepository.AddAsync(latDetail);
            }
            else
            {
                return await LatDetailRepository.UpdateAsync(latDetail);
            }
        }

        /// <summary>
        /// Deletes a LAT detail by ID.
        /// </summary>
        public async Task<bool> DeleteLatDetailAsync(int id)
        {
            return await LatDetailRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Saves multiple LAT details in a batch.
        /// </summary>
        public async Task<List<Latdetail>> SaveLatDetailsBatchAsync(List<Latdetail> latDetails)
        {
            return await LatDetailRepository.SaveBatchAsync(latDetails);
        }
    }
}
