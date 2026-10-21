using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Office2010.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Models;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text.Json;

namespace pearl_leasing.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class LeasingReportsController : ControllerBase
    {
        private readonly PearlLeasingDbContext _context;

        // Visual tag identifier for clearing cache
        private const string ReportsCacheTag = "reports-cache-tag";
        public LeasingReportsController(PearlLeasingDbContext context)
        {
            _context = context;
        }

        [HttpPost("save-reports")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SavingSuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveReports(
            [FromBody] List<CreateReportDto> requests,
            [FromServices] IValidator<CreateReportDto> validator)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new ErrorResponse(400, "INVALID_REPORTS", "No reports provided for saving."));
            }

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
                    CreatedAt = DateTime.Now,
                    GrandTotalAmount = 0
                };

                decimal grandTotal = 0;

                foreach (var r in reportRequest.Rows)
                {
                    decimal penaltyValue = r.PenaltyTenPercent ?? 0;
                    decimal rowTotal = (r.MonthlyRental) + (r.Electricity) + (r.Water) + (r.OtherUtilities) + (r.CusaFee ?? 0) + (r.PylonRental ?? 0) + penaltyValue;
                    grandTotal += rowTotal;

                    // populate snapshot fields (Building name and StallUnit unit number) when ids are provided
                    string buildingName = string.Empty;
                    string stallUnitNumber = string.Empty;

                    if (r.BuildingId > 0)
                    {
                        var building = await _context.Buildings.FindAsync(r.BuildingId);
                        if (building != null) buildingName = building.Name;
                    }

                    if (r.StallUnitId > 0)
                    {
                        var stall = await _context.StallUnits.FindAsync(r.StallUnitId);
                        if (stall != null) stallUnitNumber = stall.UnitNumber;
                    }

                    var dbRow = new LeasingReportRow
                    {
                        BuildingId = r.BuildingId,
                        StallUnitId = r.StallUnitId,
                        Building = !string.IsNullOrWhiteSpace(buildingName) ? buildingName : r.BuildingType,
                        StallUnit = !string.IsNullOrWhiteSpace(stallUnitNumber) ? stallUnitNumber : r.StallUnit,
                        VendorNameOrCategory = r.VendorNameOrCategory,
                        NameOfLessee = r.NameOfLessee,
                        MobileNumber = r.MobileNumber,
                        TinNo = r.TinNo,
                        MonthlyRental = r.MonthlyRental,
                        PaymentStatus = r.PaymentStatus,
                        PaymentMethod = r.PaymentMethod,
                        ReferenceNumber = r.ReferenceNumber,
                        Electricity = r.Electricity,
                        Water = r.Water,
                        OtherUtilities = r.OtherUtilities,
                        CusaFee = r.CusaFee,
                        PylonRental = r.PylonRental,
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

                return Ok(new SavingSuccessfulResponse
                {
                    message = "Report successfully submitted!",
                    savedIds = savedReportsList.Select(r => r.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse(500, "DATABASE_ERROR", "An internal error occurred while saving the report(s)."));
            }
        }

        [HttpGet("get-all-reports")]
        [ProducesResponseType(typeof(GetReportsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetReportsResponse>> GetReports(
            [FromQuery] string? search,
            [FromQuery] DateOnly? startDateCreated,
            [FromQuery] DateOnly? endDateCreated,
            [FromQuery] decimal? minRevenue,
            [FromQuery] decimal? maxRevenue,
            [FromQuery] string? lastUpdated = "ALL", // Short codes: TOD, L7D, L30D, TM, ALL
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "dateTimeCreated", // Column name
            [FromQuery] string? sortOrder = "DESC")   // Direction: ASC, DESC
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.LeasingReports
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            // 1. Search Filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.ReportName.ToLower().Contains(search.ToLower()));
            }

            // 2. Date Created Range Filter
            if (startDateCreated.HasValue) query = query.Where(r => r.CreatedAt >= startDateCreated.Value.ToDateTime(TimeOnly.MinValue));
            if (endDateCreated.HasValue) query = query.Where(r => r.CreatedAt <= endDateCreated.Value.ToDateTime(TimeOnly.MaxValue));

            // 3. Min/Max Revenue Filter (use computed per-row total)
            if (minRevenue.HasValue) query = query.Where(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) >= minRevenue.Value);
            if (maxRevenue.HasValue) query = query.Where(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) <= maxRevenue.Value);


            // 4. Last Updated Filter (Direct Inline Code Gamit ang Short Codes)
            if (!string.IsNullOrWhiteSpace(lastUpdated) && !lastUpdated.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var nowUtc = DateTime.Now;

                query = lastUpdated.ToUpper() switch
                {
                    "TOD" => query.Where(r => r.LastDateUpdated.HasValue && r.LastDateUpdated.Value.Date == nowUtc.Date),
                    "L7D" => query.Where(r => r.LastDateUpdated.HasValue && r.LastDateUpdated.Value >= nowUtc.AddDays(-7)),
                    "L30D" => query.Where(r => r.LastDateUpdated.HasValue && r.LastDateUpdated.Value >= nowUtc.AddDays(-30)),
                    "TM" => query.Where(r => r.LastDateUpdated.HasValue &&
                                              r.LastDateUpdated.Value.Year == nowUtc.Year &&
                                              r.LastDateUpdated.Value.Month == nowUtc.Month),
                    _ => query
                };
            }

            int totalRecords = await query.CountAsync();
            // 5. Separate Column (sortBy) & Direction (sortOrder)
            bool isDesc = string.Equals(sortOrder, "DESC", StringComparison.OrdinalIgnoreCase);

            query = sortBy?.ToLower() switch
            {
                "expectedRevenue" or "revenue" => isDesc
                    ? query.OrderByDescending(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) ?? 0).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) ?? 0).ThenBy(r => r.Id),

                "lastDateUpdated" or "lastupdated" => isDesc
                    ? query.OrderByDescending(r => r.LastDateUpdated ?? r.CreatedAt).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.LastDateUpdated ?? r.CreatedAt).ThenBy(r => r.Id),

                "reportName" or "name" => isDesc
                    ? query.OrderByDescending(r => r.ReportName).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.ReportName).ThenBy(r => r.Id),

                _ => isDesc // Default Column: "createdAt"
                    ? query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            };

            try
            {
                var reports = await query
                    .Include(r => r.Rows)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReportListDto(
                        r.Id,
                        $"RP{r.Id:D5}",
                        r.ReportName,
                        r.GrandTotalAmount,
                        r.CreatedAt,
                        r.LastDateUpdated
                    ))
                    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return Ok(new GetReportsResponse(reports, totalRecords, page, totalPages == 0 ? 1 : totalPages, pageSize));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the paginated reports.");
            }
        }

        [HttpGet("{reportId:int}/sort-child-reports")]
        [ProducesResponseType(typeof(GetChildReportsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetChildReportsResponse>> SortChildReports(
            int reportId,
            [FromQuery] string? search,
            [FromQuery] DateOnly? startDateCreated,
            [FromQuery] DateOnly? endDateCreated,
            [FromQuery] decimal? minRevenue,
            [FromQuery] decimal? maxRevenue,
            [FromQuery] string? lastUpdated = "ALL", // Short codes: TOD, L7D, L30D, TM, ALL
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "dateTimeCreated", // Column name
            [FromQuery] string? sortOrder = "DESC")   // Direction: ASC, DESC
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            pageSize = Math.Min(pageSize, 100);

            var parentReportExists = await _context.LeasingReports
                .AsNoTracking()
                .AnyAsync(r => r.Id == reportId && !r.IsDeleted);

            if (!parentReportExists)
            {
                return NotFound(new ErrorResponse(
                    404,
                    "REPORT_NOT_FOUND",
                    $"The parent report with ID {reportId} was not found."));
            }

            var query = _context.LeasingReportRows
                .AsNoTracking()
                .Where(r => r.LeasingReportId == reportId &&
                            r.LeasingReport != null &&
                            !r.LeasingReport.IsDeleted);

            // 1. Search Filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(r =>
                    r.LeasingReport!.ReportName.ToLower().Contains(searchTerm) ||
                    r.Building.ToLower().Contains(searchTerm) ||
                    r.StallUnit.ToLower().Contains(searchTerm) ||
                    r.VendorNameOrCategory.ToLower().Contains(searchTerm) ||
                    r.NameOfLessee.ToLower().Contains(searchTerm));
            }

            // 2. Date Created Range Filter
            if (startDateCreated.HasValue) query = query.Where(r => r.LeasingReport!.CreatedAt >= startDateCreated.Value.ToDateTime(TimeOnly.MinValue));
            if (endDateCreated.HasValue) query = query.Where(r => r.LeasingReport!.CreatedAt <= endDateCreated.Value.ToDateTime(TimeOnly.MaxValue));

            // 3. Min/Max Revenue Filter (use computed per-row total)
            if (minRevenue.HasValue) query = query.Where(r => r.RowTotalAmount >= minRevenue.Value);
            if (maxRevenue.HasValue) query = query.Where(r => r.RowTotalAmount <= maxRevenue.Value);


            // 4. Last Updated Filter (Direct Inline Code Gamit ang Short Codes)
            if (!string.IsNullOrWhiteSpace(lastUpdated) && !lastUpdated.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var nowUtc = DateTime.Now;

                query = lastUpdated.ToUpper() switch
                {
                    "TOD" => query.Where(r => r.LeasingReport!.LastDateUpdated.HasValue && r.LeasingReport.LastDateUpdated.Value.Date == nowUtc.Date),
                    "L7D" => query.Where(r => r.LeasingReport!.LastDateUpdated.HasValue && r.LeasingReport.LastDateUpdated.Value >= nowUtc.AddDays(-7)),
                    "L30D" => query.Where(r => r.LeasingReport!.LastDateUpdated.HasValue && r.LeasingReport.LastDateUpdated.Value >= nowUtc.AddDays(-30)),
                    "TM" => query.Where(r => r.LeasingReport!.LastDateUpdated.HasValue &&
                                              r.LeasingReport.LastDateUpdated.Value.Year == nowUtc.Year &&
                                              r.LeasingReport.LastDateUpdated.Value.Month == nowUtc.Month),
                    _ => query
                };
            }

            int totalRecords = await query.CountAsync();
            // 5. Separate Column (sortBy) & Direction (sortOrder)
            bool isDesc = string.Equals(sortOrder, "DESC", StringComparison.OrdinalIgnoreCase);

            query = sortBy?.ToLower() switch
            {
                "expectedRevenue" or "revenue" or "rowTotalAmount" => isDesc
                    ? query.OrderByDescending(r => r.RowTotalAmount).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.RowTotalAmount).ThenBy(r => r.Id),

                "lastDateUpdated" or "lastupdated" => isDesc
                    ? query.OrderByDescending(r => r.LeasingReport!.LastDateUpdated ?? r.LeasingReport.CreatedAt).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.LeasingReport!.LastDateUpdated ?? r.LeasingReport.CreatedAt).ThenBy(r => r.Id),

                //"reportName" or "name" => isDesc
                //    ? query.OrderByDescending(r => r.LeasingReport!.ReportName)
                //    : query.OrderBy(r => r.LeasingReport!.ReportName),

                //"building" => isDesc ? query.OrderByDescending(r => r.Building) : query.OrderBy(r => r.Building),
                //"stallUnit" or "stall" => isDesc ? query.OrderByDescending(r => r.StallUnit) : query.OrderBy(r => r.StallUnit),
                //"lessee" or "nameOfLessee" => isDesc ? query.OrderByDescending(r => r.NameOfLessee) : query.OrderBy(r => r.NameOfLessee),
                //"paymentStatus" => isDesc ? query.OrderByDescending(r => r.PaymentStatus) : query.OrderBy(r => r.PaymentStatus),

                _ => isDesc // Default Column: "createdAt"
                    ? query.OrderByDescending(r => r.LeasingReport!.CreatedAt).ThenByDescending(r => r.Id)
                    : query.OrderBy(r => r.LeasingReport!.CreatedAt).ThenBy(r => r.Id)
            };

            try
            {
                var reports = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ChildReportListDto(
                        r.Id,
                        r.LeasingReportId,
                        $"RP{r.LeasingReportId:D5}",
                        r.LeasingReport!.ReportName,
                        r.Building,
                        r.StallUnit,
                        r.VendorNameOrCategory,
                        r.NameOfLessee,
                        r.MobileNumber,
                        r.TinNo,
                        r.MonthlyRental,
                        r.PaymentStatus,
                        r.PaymentMethod,
                        r.ReferenceNumber,
                        r.Electricity,
                        r.Water,
                        r.OtherUtilities,
                        r.CusaFee,
                        r.PylonRental,
                        r.PenaltyTenPercent,
                        r.RowTotalAmount,
                        r.LeasingReport.CreatedAt,
                        r.LeasingReport.LastDateUpdated
                    ))
                    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return Ok(new GetChildReportsResponse(reports, totalRecords, page, totalPages == 0 ? 1 : totalPages, pageSize));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the paginated reports.");
            }
        }

        [HttpGet("{id:int}/details")]
        [OutputCache(Duration = 300, Tags = [ReportsCacheTag])]
        [ProducesResponseType(typeof(ReportDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReportDetailsResponse>> GetReportById(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var mainReport = await _context.LeasingReports
                .Where(r => !r.IsDeleted)
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
                    .GroupJoin(_context.Buildings, row => row.BuildingId, b => b.Id, (row, bgrp) => new { row, bgrp })
                    .SelectMany(x => x.bgrp.DefaultIfEmpty(), (x, b) => new { x.row, b })
                    .GroupJoin(_context.StallUnits, x => x.row.StallUnitId, s => s.Id, (x, sgrp) => new { x.row, x.b, sgrp })
                    .SelectMany(x => x.sgrp.DefaultIfEmpty(), (x, s) => new { x.row, x.b, s })
                    .OrderBy(x => x.row.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new DTO.ReportRowDetailsDto(
                        x.row.Id,
                        x.b != null ? x.b.Name : x.row.Building,
                        x.row.VendorNameOrCategory,
                        x.s != null ? x.s.UnitNumber : x.row.StallUnit,
                        x.row.NameOfLessee,
                        x.row.MobileNumber ?? string.Empty,
                        x.row.TinNo ?? string.Empty,
                        x.row.MonthlyRental,
                        x.row.PaymentStatus,
                        x.row.PaymentMethod,
                        x.row.ReferenceNumber,
                        x.row.Electricity,
                        x.row.Water,
                        x.row.OtherUtilities,
                        x.row.CusaFee,
                        x.row.PylonRental,
                        x.row.PenaltyTenPercent,
                        x.row.RowTotalAmount
                    ))
                    .ToListAsync();

                // Build the response aligned to ReportDetailsResponse
                var reportIdFormatted = $"RP{mainReport.Id:D5}";
                var response = new ReportDetailsResponse(
                    reportIdFormatted,
                    mainReport.ReportName,
                    mainReport.GrandTotalAmount,
                    mainReport.CreatedAt,
                    paginatedRows
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the report details: {ex.Message}");
            }
        }

        [HttpPost("update-report")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReportRows(
            [FromBody] List<UpdateReportRowDto> requests,
            [FromServices] IValidator<UpdateReportRowDto> validator)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new ErrorResponse(400, "EMPTY_PAYLOAD", "No rows provided for update."));
            }

            var validationErrors = new List<string>();
            foreach (var request in requests)
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    validationErrors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationErrors)));
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

            var parentReport = dbRows.First().LeasingReport;
            if (parentReport == null)
            {
                return NotFound(new ErrorResponse(404, "REPORT_NOT_FOUND", "The parent report was not found."));
            }

            // ReportName belongs to the parent report. Use the first non-empty
            // value because the current request contains one ReportName per row.
            var reportName = requests
                .Select(r => r.ReportName?.Trim())
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            if (!string.IsNullOrWhiteSpace(reportName))
            {
                parentReport.ReportName = reportName;
            }

            parentReport.LastDateUpdated = DateTime.Now;

            foreach (var request in requests)
            {
                var dbRow = dbRows.FirstOrDefault(r => r.Id == request.Id);
                if (dbRow == null) continue;

                // populate snapshot fields (Building name and StallUnit unit number) when ids are provided
                string buildingName = string.Empty;
                string stallUnitNumber = string.Empty;

                if (request.BuildingId > 0)
                {
                    var building = await _context.Buildings.FindAsync(request.BuildingId);
                    if (building != null) buildingName = building.Name;
                }

                if (request.StallUnitId > 0)
                {
                    var stall = await _context.StallUnits.FindAsync(request.StallUnitId);
                    if (stall != null) stallUnitNumber = stall.UnitNumber;
                }

                dbRow.BuildingId = request.BuildingId;
                dbRow.StallUnitId = request.StallUnitId;
                dbRow.Building = !string.IsNullOrWhiteSpace(buildingName) ? buildingName : request.Building;
                dbRow.StallUnit = !string.IsNullOrWhiteSpace(stallUnitNumber) ? stallUnitNumber : request.StallUnit;
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
                dbRow.CusaFee = request.CusaFee;
                dbRow.PylonRental = request.PylonRental;
                dbRow.PenaltyTenPercent = request.PenaltyTenPercent;

                dbRow.PenaltyTenPercent = request.PenaltyTenPercent ?? 0.00m;

                dbRow.RowTotalAmount = dbRow.MonthlyRental +
                                       dbRow.Electricity +
                                       dbRow.Water +
                                       dbRow.OtherUtilities +
                                       (dbRow.CusaFee ?? 0m) +
                                       (dbRow.PylonRental ?? 0m) +
                                       (dbRow.PenaltyTenPercent ?? 0m);
            }

            try
            {
                await _context.SaveChangesAsync();

                decimal updatedGrandTotal = await _context.LeasingReportRows
                    .Where(r => r.LeasingReportId == parentReport.Id)
                    .SumAsync(r => r.RowTotalAmount);

                parentReport.GrandTotalAmount = updatedGrandTotal;
                parentReport.LastDateUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new SuccessfulResponse { message = "Report successfully updated!" });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse(500, "UPDATE_ERROR", "An error occurred during the update process."));
            }
        }

        [HttpPost("rows/update/{rowId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSingleRow(
            int rowId,
            [FromBody] UpdateChildRequest request,
            [FromServices] IValidator<UpdateChildRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            var row = await _context.LeasingReportRows.FindAsync(rowId);
            if (row == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"The report row with ID {rowId} was not found."));
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changedColumns = new List<string>();

            void TrackChange<T>(string propertyName, T oldValue, T newValue)
            {
                if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
                {
                    oldValues[propertyName] = oldValue;
                    newValues[propertyName] = newValue;
                    changedColumns.Add(propertyName);
                }
            }

            TrackChange(nameof(row.BuildingId), row.BuildingId, request.BuildingId);
            TrackChange(nameof(row.Building), row.Building, request.Building);
            TrackChange(nameof(row.VendorNameOrCategory), row.VendorNameOrCategory, request.VendorNameOrCategory);
            TrackChange(nameof(row.StallUnit), row.StallUnit, request.StallUnit);
            TrackChange(nameof(row.NameOfLessee), row.NameOfLessee, request.NameOfLessee);
            TrackChange(nameof(row.MobileNumber), row.MobileNumber, request.MobileNumber);
            TrackChange(nameof(row.MonthlyRental), row.MonthlyRental, request.MonthlyRental);
            TrackChange(nameof(row.PaymentStatus), row.PaymentStatus, request.PaymentStatus);
            TrackChange(nameof(row.PaymentMethod), row.PaymentMethod, request.PaymentMethod);
            TrackChange(nameof(row.ReferenceNumber), row.ReferenceNumber, request.ReferenceNumber);
            TrackChange(nameof(row.Electricity), row.Electricity, request.Electricity);
            TrackChange(nameof(row.Water), row.Water, request.Water);
            TrackChange(nameof(row.OtherUtilities), row.OtherUtilities, request.OtherUtilities);
            TrackChange(nameof(row.CusaFee), row.CusaFee, request.CusaFee);
            TrackChange(nameof(row.PylonRental), row.PylonRental, request.PylonRental);
            TrackChange(nameof(row.PenaltyTenPercent), row.PenaltyTenPercent, request.PenaltyTenPercent);

            // 2. I-apply ang mga bagong values sa entity
            row.BuildingId = request.BuildingId;
            row.StallUnitId = request.StallUnitId;
            row.VendorNameOrCategory = request.VendorNameOrCategory;
            row.StallUnit = request.StallUnit;
            row.NameOfLessee = request.NameOfLessee;
            row.MobileNumber = request.MobileNumber;
            row.MonthlyRental = request.MonthlyRental;
            row.PaymentStatus = request.PaymentStatus;
            row.PaymentMethod = request.PaymentMethod;
            row.ReferenceNumber = request.ReferenceNumber;
            row.Electricity = request.Electricity;
            row.Water = request.Water;
            row.OtherUtilities = request.OtherUtilities;
            row.CusaFee = request.CusaFee;
            row.PylonRental = request.PylonRental;
            row.PenaltyTenPercent = request.PenaltyTenPercent;

            // 3. Mag-save sa AuditLogs kung may tunay na pagbabago na nangyari
            if (changedColumns.Count > 0)
            {
                string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                       ?? User.Identity?.Name
                                       ?? "System";

                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };

                var auditLog = new AuditLogs
                {
                    EntityName = nameof(LeasingReportRow),
                    EntityId = row.Id,
                    Action = "UPDATE",
                    ModifiedBy = currentUserId,
                    Timestamp = DateTime.Now,
                    OldValues = JsonSerializer.Serialize(oldValues, jsonOptions),
                    NewValues = JsonSerializer.Serialize(newValues, jsonOptions),
                    ChangedColumns = string.Join(", ", changedColumns)
                };

                _context.AuditLogs.Add(auditLog);
            }

            await _context.SaveChangesAsync();

            //await RecalculateReportTotal(row.GymReportId);

            // Clear cache
            //await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = $"Successfully updated row {rowId}!" });
        }

        [HttpPost("rows/delete/{rowId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteChildRow(int rowId)
        {
            var row = await _context.LeasingReportRows.FindAsync(rowId);

            if (row == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"The report row with ID {rowId} was not found."));
            }

            int parentReportId = row.LeasingReportId;

            // Gamitin ang transaction para siguradong parehong magse-save ang log at delete
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Map current row properties to Logs_DeletedReports
                var deletedLog = new Logs_DeletedReports
                {
                    LeasingReportRowId = row.Id,
                    LeasingReportId = row.LeasingReportId,
                    BuildingId = row.BuildingId,
                    BuildingName = row.Building,
                    StallUnitId = row.StallUnitId,
                    StallUnitNumber = row.StallUnit,
                    VendorNameOrCategory = row.VendorNameOrCategory,
                    NameOfLessee = row.NameOfLessee,
                    MobileNumber = row.MobileNumber,
                    TinNo = row.TinNo,
                    MonthlyRental = row.MonthlyRental,
                    PaymentStatus = row.PaymentStatus,
                    PaymentMethod = row.PaymentMethod,
                    ReferenceNumber = row.ReferenceNumber,
                    Electricity = row.Electricity,
                    Water = row.Water,
                    OtherUtilities = row.OtherUtilities,
                    CusaFee = row.CusaFee,
                    PylonRental = row.PylonRental,
                    PenaltyTenPercent = row.PenaltyTenPercent,
                    RowTotalAmount = row.RowTotalAmount,
                    DeletedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "System",
                    DeletedAt = DateTime.Now
                };

                // 2. Add to logs and remove from main table
                _context.Logs_DeletedReports.Add(deletedLog);
                _context.LeasingReportRows.Remove(row);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            // 3. Post-deletion tasks
            //await RecalculateReportTotal(parentReportId);
            //await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = $"Successfully deleted row {rowId}!" });
        }

        [HttpPost("delete/{reportId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            var report = await _context.LeasingReports
                .FirstOrDefaultAsync(r => r.Id == reportId && !r.IsDeleted);

            if (report == null)
            {
                return NotFound(new ErrorResponse(
                    404,
                    "NOT_FOUND",
                    $"The master report with ID {reportId} was not found."));
            }

            var currentUserId = User.FindFirst(ClaimTypes.Name)?.Value
                                ?? User.Identity?.Name
                                ?? "System";

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                report.IsDeleted = true;
                report.DeletedAt = DateTime.Now;
                report.DeletedBy = currentUserId;
                report.LastDateUpdated = DateTime.Now;

                _context.AuditLogs.Add(new AuditLogs
                {
                    EntityName = nameof(LeasingReport),
                    EntityId = report.Id,
                    Action = "DELETE",
                    ModifiedBy = currentUserId,
                    Timestamp = DateTime.Now,
                    OldValues = JsonSerializer.Serialize(new
                    {
                        report.Id,
                        report.ReportName,
                        report.IsDeleted
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        report.Id,
                        report.ReportName,
                        report.IsDeleted,
                        report.DeletedAt,
                        report.DeletedBy
                    }),
                    ChangedColumns = "IsDeleted, DeletedAt, DeletedBy"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new SuccessfulResponse
                {
                    message = $"Successfully deleted report {reportId}."
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ErrorResponse(
                    500,
                    "DELETE_ERROR",
                    "An error occurred while deleting the report."));
            }
        }

        [HttpPost("{reportId:int}/rows/add")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(AddSingleRowResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSingleRowToReport(
            int reportId,
            [FromBody] AddSingleRowRequest request,
            [FromServices] IValidator<AddSingleRowRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            var parentReport = await _context.LeasingReports.FindAsync(reportId);
            if (parentReport == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"The master report with ID {reportId} was not found."));
            }

            // compute row total (handle nullable fees)
            decimal rowTotal = request.MonthlyRental
                               + request.Electricity
                               + request.Water
                               + request.OtherUtilities
                               + (request.CusaFee ?? 0m)
                               + (request.PylonRental ?? 0m)
                               + (request.PenaltyTenPercent ?? 0m);

            // resolve snapshot values if ids provided
            string buildingName = request.Building;
            string stallUnitNumber = request.StallUnit;

            if (request.BuildingId > 0)
            {
                var b = await _context.Buildings.FindAsync(request.BuildingId);
                if (b != null) buildingName = b.Name;
            }

            if (request.StallUnitId > 0)
            {
                var s = await _context.StallUnits.FindAsync(request.StallUnitId);
                if (s != null) stallUnitNumber = s.UnitNumber;
            }

            // create new row and add to report rows table
            var newRow = new LeasingReportRow
            {
                LeasingReportId = reportId,
                BuildingId = request.BuildingId,
                StallUnitId = request.StallUnitId,
                Building = buildingName,
                StallUnit = stallUnitNumber,
                VendorNameOrCategory = request.VendorNameOrCategory,
                NameOfLessee = request.NameOfLessee,
                MobileNumber = request.MobileNumber,
                TinNo = request.TinNo,
                MonthlyRental = request.MonthlyRental,
                PaymentStatus = request.PaymentStatus,
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber,
                Electricity = request.Electricity,
                Water = request.Water,
                OtherUtilities = request.OtherUtilities,
                CusaFee = request.CusaFee,
                PylonRental = request.PylonRental,
                PenaltyTenPercent = request.PenaltyTenPercent,
                RowTotalAmount = rowTotal
            };

            _context.LeasingReportRows.Add(newRow);

            // update parent report totals and timestamp
            parentReport.GrandTotalAmount = (parentReport.GrandTotalAmount) + rowTotal;
            parentReport.LastDateUpdated = DateTime.Now;

            // persist row and updated report
            await _context.SaveChangesAsync();

            // create audit log for the inserted row
            string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.Identity?.Name
                                   ?? "System";

            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            var auditLog = new AuditLogs
            {
                EntityName = nameof(LeasingReportRow),
                EntityId = newRow.Id,
                Action = "CREATE",
                ModifiedBy = currentUserId,
                Timestamp = DateTime.Now,
                OldValues = null,
                NewValues = JsonSerializer.Serialize(newRow, jsonOptions),
                ChangedColumns = "ALL"
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Ok(new AddSingleRowResponse { message = "Successfully added new report row!", newRowId = newRow.Id });
        }

        [HttpPost("download")]
        [EnableRateLimiting("DownloadPolicy")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportReportToExcel([FromBody] DownloadReportsRequest request)
        {
            if (request?.ReportIds == null || request.ReportIds.Count == 0)
            {
                return BadRequest(new ErrorResponse(400, "INVALID_REPORT_IDS", "At least one report ID is required."));
            }

            var reportIds = request.ReportIds.Distinct().ToList();
            var reports = await _context.LeasingReports
                .AsNoTracking()
                .Include(r => r.Rows)
                .Where(r => reportIds.Contains(r.Id) && !r.IsDeleted)
                .OrderBy(r => r.Id)
                .ToListAsync();

            if (reports.Count == 0)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", "No matching active reports found for the selected IDs."));
            }

            using var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                foreach (var report in reports)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Leasing Report Summary");
                    var formattedReportId = $"RP{report.Id:D5}";
                    var generatedAt = DateTime.Now;

                    worksheet.Cell("A1").Value = report.ReportName;
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 14;
                    worksheet.Cell("A2").Value = generatedAt;
                    worksheet.Cell("A2").Style.NumberFormat.Format = "MMMM dd, yyyy";
                    worksheet.Cell("A3").Value = generatedAt;
                    worksheet.Cell("A3").Style.NumberFormat.Format = "HH:mm:ss";
                    worksheet.Cell("D1").Value = "Report ID:";
                    worksheet.Cell("E1").Value = formattedReportId;
                    worksheet.Range("D1:E1").Style.Font.Bold = true;

                    var headers = new[] { "#", "Building", "Stall Unit", "Vendor/Category", "Lessee", "Mobile No", "TIN No", "Monthly Rental", "Payment Status", "Payment Method", "Reference No", "Electricity", "Water", "Other Utilities", "CUSA Fee", "Pylon Rental", "Penalty 10%", "Total Amount" };
                    const int headerRow = 5;
                    for (var i = 0; i < headers.Length; i++) worksheet.Cell(headerRow, i + 1).Value = headers[i];
                    var headerRange = worksheet.Range(headerRow, 1, headerRow, headers.Length);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                    headerRange.Style.Font.FontColor = XLColor.White;

                    var currencyColumns = new[] { 8, 12, 13, 14, 15, 16, 17, 18 };
                    var currentRow = headerRow + 1;
                    foreach (var row in report.Rows.OrderBy(r => r.Id))
                    {
                        var values = new object?[]
                        {
                            row.Id, row.Building, row.StallUnit, row.VendorNameOrCategory, row.NameOfLessee,
                            row.MobileNumber ?? string.Empty, row.TinNo ?? string.Empty, row.MonthlyRental,
                            row.PaymentStatus, row.PaymentMethod, row.ReferenceNumber, row.Electricity, row.Water,
                            row.OtherUtilities, row.CusaFee ?? 0m, row.PylonRental ?? 0m,
                            row.PenaltyTenPercent ?? 0m, row.RowTotalAmount
                        };

                        for (var column = 0; column < values.Length; column++)
                        {
                            worksheet.Cell(currentRow, column + 1).Value = XLCellValue.FromObject(values[column]);
                        }

                        foreach (var column in currencyColumns)
                            worksheet.Cell(currentRow, column).Style.NumberFormat.Format = "₱#,##0.00";

                        currentRow++;
                    }

                    worksheet.Cell(currentRow, 17).Value = "GRAND TOTAL:";
                    worksheet.Cell(currentRow, 17).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 18).FormulaA1 = $"=SUM(R{headerRow + 1}:R{currentRow - 1})";
                    worksheet.Cell(currentRow, 18).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 18).Style.NumberFormat.Format = "₱#,##0.00";
                    worksheet.Columns().AdjustToContents();

                    using var excelStream = new MemoryStream();
                    workbook.SaveAs(excelStream);
                    var safeName = string.Join("_", report.ReportName.Split(Path.GetInvalidFileNameChars()));
                    var entry = archive.CreateEntry($"{formattedReportId}_{safeName}.xlsx", System.IO.Compression.CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    excelStream.Position = 0;
                    await excelStream.CopyToAsync(entryStream);
                }
            }

            zipStream.Position = 0;
            return File(zipStream.ToArray(), "application/zip", "leasing_reports.zip");
        }
    }
}
