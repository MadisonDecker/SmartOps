using ClosedXML.Excel;
using SmartManagement.Repo;
using SmartManagement.Repo.Models;

namespace SmartOpsManagement.Bus
{
    /// <summary>
    /// Business logic for Line Adherence operations.
    /// </summary>
    public partial class SmartOpsBusinessLogic
    {
        private LineAdherenceRepository? _lineAdherenceRepository;

        private LineAdherenceRepository LineAdherenceRepository => _lineAdherenceRepository ??= new LineAdherenceRepository(_context);

        /// <summary>
        /// Gets all line adherence records.
        /// </summary>
        public async Task<List<LineAdherence>> GetAllLineAdherencesAsync()
        {
            return await LineAdherenceRepository.GetAllAsync();
        }

        /// <summary>
        /// Gets a line adherence record by ID.
        /// </summary>
        public async Task<LineAdherence?> GetLineAdherenceByIdAsync(int id)
        {
            return await LineAdherenceRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets line adherence records by client abbreviation.
        /// </summary>
        public async Task<List<LineAdherence>> GetLineAdherencesByClientAsync(string clientAbbr)
        {
            return await LineAdherenceRepository.GetByClientAsync(clientAbbr);
        }

        /// <summary>
        /// Gets line adherence records for a specific date range.
        /// </summary>
        public async Task<List<LineAdherence>> GetLineAdherencesByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await LineAdherenceRepository.GetByDateRangeAsync(startDate, endDate);
        }

        /// <summary>
        /// Gets line adherence records by client and date range.
        /// </summary>
        public async Task<List<LineAdherence>> GetLineAdherencesByClientAndDateRangeAsync(string clientAbbr, DateOnly startDate, DateOnly endDate)
        {
            return await LineAdherenceRepository.GetByClientAndDateRangeAsync(clientAbbr, startDate, endDate);
        }

        /// <summary>
        /// Saves a line adherence record (creates new or updates existing).
        /// </summary>
        public async Task<LineAdherence?> SaveLineAdherenceAsync(LineAdherence lineAdherence)
        {
            if (lineAdherence.LineAdherenceId == 0)
            {
                return await LineAdherenceRepository.AddAsync(lineAdherence);
            }
            else
            {
                return await LineAdherenceRepository.UpdateAsync(lineAdherence);
            }
        }

        /// <summary>
        /// Deletes a line adherence record by ID.
        /// </summary>
        public async Task<bool> DeleteLineAdherenceAsync(int id)
        {
            return await LineAdherenceRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Saves multiple line adherence records in a batch.
        /// </summary>
        public async Task<List<LineAdherence>> SaveLineAdherencesBatchAsync(List<LineAdherence> lineAdherences)
        {
            return await LineAdherenceRepository.SaveBatchAsync(lineAdherences);
        }

        /// <summary>
        /// Imports line adherence data from an Excel spreadsheet.
        /// Row 1 contains dates (columns B onwards), Column A contains times.
        /// Data values are at the intersection - zero values are skipped.
        /// </summary>
        /// <param name="filePath">Full path to the Excel file.</param>
        /// <param name="sheetName">Name of the worksheet to import from.</param>
        /// <param name="clientAbbr">Client abbreviation for the imported records.</param>
        /// <returns>List of imported LineAdherence records.</returns>
        public async Task<List<LineAdherence>> ImportLineAdherenceFromExcelAsync(string filePath, string sheetName, string clientAbbr)
        {
            var lineAdherences = new List<LineAdherence>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(sheetName);

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
            {
                return lineAdherences;
            }

            int lastColumn = usedRange.LastColumn().ColumnNumber();
            int lastRow = usedRange.LastRow().RowNumber();

            // Parse dates from row 1 (columns B onwards)
            var dates = new Dictionary<int, DateOnly>();
            for (int col = 2; col <= lastColumn; col++)
            {
                var cell = worksheet.Cell(1, col);
                if (cell.TryGetValue<DateTime>(out var dateValue))
                {
                    dates[col] = DateOnly.FromDateTime(dateValue);
                }
                else if (DateOnly.TryParse(cell.GetString(), out var parsedDate))
                {
                    dates[col] = parsedDate;
                }
            }

            // Parse times from column A (rows 2 onwards) and cross-reference with data
            for (int row = 2; row <= lastRow; row++)
            {
                var timeCell = worksheet.Cell(row, 1);
                TimeOnly? time = null;

                if (timeCell.TryGetValue<DateTime>(out var timeValue))
                {
                    time = TimeOnly.FromDateTime(timeValue);
                }
                else if (TimeOnly.TryParse(timeCell.GetString(), out var parsedTime))
                {
                    time = parsedTime;
                }

                if (time == null)
                {
                    continue;
                }

                // Process data cells for this time row
                foreach (var dateEntry in dates)
                {
                    int col = dateEntry.Key;
                    var dataCell = worksheet.Cell(row, col);

                    if (dataCell.TryGetValue<double>(out var value) && value != 0)
                    {
                        var lineAdherence = new LineAdherence
                        {
                            ClientAbbr = clientAbbr,
                            RequiredDate = dateEntry.Value,
                            RequiredTime = time.Value,
                            RequiredHours = (int)value,
                            InsertedUtcDate = DateTime.UtcNow,
                            LastUpdatedDate = DateTime.UtcNow
                        };

                        lineAdherences.Add(lineAdherence);
                    }
                    else if (int.TryParse(dataCell.GetString(), out var intValue) && intValue != 0)
                    {
                        var lineAdherence = new LineAdherence
                        {
                            ClientAbbr = clientAbbr,
                            RequiredDate = dateEntry.Value,
                            RequiredTime = time.Value,
                            RequiredHours = intValue,
                            InsertedUtcDate = DateTime.UtcNow,
                            LastUpdatedDate = DateTime.UtcNow
                        };

                        lineAdherences.Add(lineAdherence);
                    }
                }
            }

            // Save all records using batch save
            if (lineAdherences.Count > 0)
            {
                lineAdherences = await SaveLineAdherencesBatchAsync(lineAdherences);
            }

            return lineAdherences;
        }
    }
}
