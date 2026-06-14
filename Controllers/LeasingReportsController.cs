using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Models;

namespace pearl_leasing.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class LeasingReportsController : ControllerBase
    {
        private readonly PearlLeasingDbContext _context;
        public LeasingReportsController(PearlLeasingDbContext context)
        {
            _context = context;
        }

        [HttpPost("save-reports")]
        public async Task<IActionResult> SaveReports([FromBody] List<CreateReportDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new ErrorResponse(400, "INVALID_REPORTS", "No reports provided for saving."));
            }

            var validator = new CreateReportValidator();
            var savedReportsList = new List<LeasingReport>();

            var validationErrors = new List<string>();

            foreach (var reportRequest in requests)
            {
                var validationResult = await validator.ValidateAsync(reportRequest);
                if (!validationResult.IsValid)
                {
                    validationErrors.Add($"Report '{reportRequest.ReportName}': {validationResult.Errors.First().ErrorMessage}");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: "VALIDATION_ERROR",
                    message: string.Join(" | ", validationErrors)
                ));
            }

            foreach (var reportRequest in requests)
            {
                if (string.IsNullOrWhiteSpace(reportRequest.ReportName)) continue;

                var newReport = new LeasingReport
                {
                    ReportName = reportRequest.ReportName,
                    CreatedAt = DateTime.UtcNow,
                    GrandTotalAmount = 0
                };

                decimal grandTotal = 0;

                foreach (var r in reportRequest.Rows)
                {
                    decimal penaltyValue = r.PenaltyTenPercent ?? 0;
                    decimal rowTotal = r.MonthlyRental + r.Electricity + r.Water + r.OtherUtilities + penaltyValue;
                    grandTotal += rowTotal;

                    var dbRow = new LeasingReportRow
                    {
                        BuildingId = r.BuildingId,
                        StallUnitId = r.StallUnitId,
                        Building = r.BuildingType,
                        StallUnit = r.StallUnit,
                        VendorNameOrCategory = r.VendorNameOrCategory,
                        NameOfLessee = r.NameOfLessee,
                        MobileNumber = r.MobileNumber,
                        MonthlyRental = r.MonthlyRental,
                        PaymentStatus = r.PaymentStatus,
                        PaymentMethod = r.PaymentMethod,
                        ReferenceNumber = r.ReferenceNumber,
                        Electricity = r.Electricity,
                        Water = r.Water,
                        OtherUtilities = r.OtherUtilities,
                        PenaltyTenPercent = r.PenaltyTenPercent,
                        RowTotalAmount = rowTotal
                    };

                    newReport.Rows.Add(dbRow);
                }

                newReport.GrandTotalAmount = grandTotal;
                _context.LeasingReports.Add(newReport);
                savedReportsList.Add(newReport);
            }

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = savedReportsList.Count == 1
                        ? "Leasing report saved successfully!"
                        : $"Successfully saved {savedReportsList.Count} reports in bulk!",
                    savedIds = savedReportsList.Select(r => r.Id).ToList(),
                    totalReportsSaved = savedReportsList.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse(500, "DATABASE_ERROR", "An internal error occurred while saving the report(s)."));
            }
        }

        [HttpGet("get-all-reports")]
        public async Task<IActionResult> GetAllReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            try
            {
                int totalRecords = await _context.LeasingReports.CountAsync();

                var reports = await _context.LeasingReports
                    .Include(r => r.Rows)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        Id = r.Id,
                        ReportId = "RP" + r.Id.ToString().PadLeft(5, '0'),
                        ReportName = r.ReportName,
                        ReportDate = r.CreatedAt,
                        GrandTotalAmount = r.GrandTotalAmount,
                        IncludedBuildings = r.Rows.Select(row => row.BuildingId).Where(id => id.HasValue).Distinct().ToList(),
                        TotalRowsCount = r.Rows.Count
                    })
                    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return Ok(new
                {
                    message = "Successfully retrieved reports.",
                    currentPage = pageNumber,
                    pageSize = pageSize,
                    totalRecords = totalRecords,
                    totalPages = totalPages,
                    hasNextPage = pageNumber < totalPages,
                    hasPreviousPage = pageNumber > 1,
                    data = reports
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the paginated reports.");
            }
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetReportDetailsWithPagination(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var mainReport = await _context.LeasingReports
                .Select(r => new { r.Id, r.ReportName, r.GrandTotalAmount, r.CreatedAt })
                .FirstOrDefaultAsync(r => r.Id == id);

            if (mainReport == null)
            {
                return NotFound($"Report with ID {id} does not exist.");
            }

            try
            {
                int totalRows = await _context.LeasingReportRows
                    .CountAsync(row => row.LeasingReportId == id);

                var paginatedRows = await _context.LeasingReportRows
    .Where(row => row.LeasingReportId == id)
    .OrderBy(row => row.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                return Ok(new
                {
                    message = "Successfully retrieved report.",
                    id = mainReport.Id,
                    reportId = "RP" + mainReport.Id.ToString().PadLeft(5, '0'),
                    reportName = mainReport.ReportName,
                    grandTotalAmount = mainReport.GrandTotalAmount,
                    createdAt = mainReport.CreatedAt,

                    currentRowPage = pageNumber,
                    pageSize = pageSize,
                    totalRowsCount = totalRows,
                    totalRowPages = totalPages,
                    hasNextRowPage = pageNumber < totalPages,
                    hasPreviousRowPage = pageNumber > 1,
                    rows = paginatedRows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the report details: {ex.Message}");
            }
        }

        [HttpPost("update-report")]
        public async Task<IActionResult> UpdateReportRows([FromBody] List<UpdateReportRowDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new ErrorResponse(400, "EMPTY_PAYLOAD", "No rows provided for update."));
            }

            var rowIds = requests.Select(r => r.Id).ToList();

            var dbRows = await _context.LeasingReportRows
                .Include(r => r.LeasingReport)
                .Where(r => rowIds.Contains(r.Id))
                .ToListAsync();

            if (!dbRows.Any())
            {
                return NotFound(new ErrorResponse(404, "ROWS_NOT_FOUND", "None of the requested report rows were found."));
            }

            int? parentReportId = dbRows.First().LeasingReport?.Id;

            foreach (var request in requests)
            {
                var dbRow = dbRows.FirstOrDefault(r => r.Id == request.Id);
                if (dbRow == null) continue;

                dbRow.BuildingId = request.BuildingId;
                dbRow.StallUnitId = request.StallUnitId;
                dbRow.Building = request.Building;
                dbRow.StallUnit = request.StallUnit;
                dbRow.VendorNameOrCategory = request.VendorNameOrCategory;
                dbRow.NameOfLessee = request.NameOfLessee;
                dbRow.MobileNumber = request.MobileNumber;
                dbRow.MonthlyRental = request.MonthlyRental;
                dbRow.PaymentStatus = request.PaymentStatus;
                dbRow.PaymentMethod = request.PaymentMethod;
                dbRow.ReferenceNumber = request.ReferenceNumber;
                dbRow.Electricity = request.Electricity;
                dbRow.Water = request.Water;
                dbRow.OtherUtilities = request.OtherUtilities;
                dbRow.PenaltyTenPercent = request.PenaltyTenPercent;

                dbRow.PenaltyTenPercent = request.PenaltyTenPercent ?? 0.00m;

                dbRow.RowTotalAmount = dbRow.MonthlyRental +
                                       dbRow.Electricity +
                                       dbRow.Water +
                                       dbRow.OtherUtilities +
                                       dbRow.PenaltyTenPercent.Value;
            }

            try
            {
                await _context.SaveChangesAsync();

                decimal updatedGrandTotal = 0;
                if (parentReportId.HasValue)
                {
                    updatedGrandTotal = await _context.LeasingReportRows
                        .Where(r => r.LeasingReportId == parentReportId.Value)
                        .SumAsync(r => r.RowTotalAmount);

                    var parentReport = await _context.LeasingReports.FindAsync(parentReportId.Value);
                    if (parentReport != null)
                    {
                        parentReport.GrandTotalAmount = updatedGrandTotal;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new
                {
                    message = requests.Count == 1
                        ? "Report row updated successfully!"
                        : "All report rows updated successfully!",
                    affectedRows = dbRows.Count,
                    updatedGrandTotal = updatedGrandTotal
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse(500, "UPDATE_ERROR", "An error occurred during the update process."));
            }
        }

        [HttpGet("search-filter")]
        public async Task<IActionResult> SearchAndFilterReports(
            [FromQuery] string? reportName,
            [FromQuery] decimal? minRevenue,
            [FromQuery] decimal? maxRevenue,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? buildingId,
            [FromQuery] string? paymentStatus)
        {
            var query = _context.LeasingReports.AsQueryable();

            if (!string.IsNullOrWhiteSpace(reportName))
            {
                var searchTerm = reportName.Trim().ToLower();
                query = query.Where(r => r.ReportName.ToLower().Contains(searchTerm));
            }

            if (minRevenue.HasValue)
            {
                query = query.Where(r => r.GrandTotalAmount >= minRevenue.Value);
            }
            if (maxRevenue.HasValue)
            {
                query = query.Where(r => r.GrandTotalAmount <= maxRevenue.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreatedAt <= endOfDay);
            }

            if (buildingId.HasValue)
            {
                query = query.Where(r => r.Rows.Any(row => row.BuildingId == buildingId.Value));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                var targetStatus = paymentStatus.Trim();
                query = query.Where(r => r.Rows.Any(row => EF.Functions.Like(row.PaymentStatus, targetStatus)));
            }

            try
            {
                var results = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        ReportId = "RP" + r.Id.ToString().PadLeft(5, '0'),
                        ReportName = r.ReportName,
                        ReportDate = r.CreatedAt,
                        GrandTotalAmount = r.GrandTotalAmount,
                        IncludedBuildings = r.Rows.Where(row => row.BuildingId.HasValue).Select(row => row.BuildingId!.Value).Distinct().ToList(),
                        TotalRowsCount = r.Rows.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = $"Search filter execution success. Found {results.Count} matching leasing reports.",
                    count = results.Count,
                    data = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while executing the search filter: {ex.Message}");
            }
        }
    }
}
