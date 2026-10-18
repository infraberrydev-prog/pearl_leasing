using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
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

                return Ok(new
                {
                    message = $"Successfully saved {savedReportsList.Count} reports.",
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
        [OutputCache(PolicyName = "ReportsListCache", Tags = [ReportsCacheTag])]
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

            var query = _context.LeasingReports.AsNoTracking();

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
                    ? query.OrderByDescending(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) ?? 0)
                    : query.OrderBy(r => r.Rows.Sum(row => (decimal?)(row.MonthlyRental + row.Electricity + row.Water + row.OtherUtilities + (row.CusaFee ?? 0m) + (row.PylonRental ?? 0m) + row.PenaltyTenPercent)) ?? 0),

                "lastDateUpdated" or "lastupdated" => isDesc
                    ? query.OrderByDescending(r => r.LastDateUpdated ?? r.CreatedAt)
                    : query.OrderBy(r => r.LastDateUpdated ?? r.CreatedAt),

                "reportName" or "name" => isDesc
                    ? query.OrderByDescending(r => r.ReportName)
                    : query.OrderBy(r => r.ReportName),

                _ => isDesc // Default Column: "createdAt"
                    ? query.OrderByDescending(r => r.CreatedAt)
                    : query.OrderBy(r => r.CreatedAt)
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
                                       dbRow.CusaFee ?? 0 +
                                       dbRow.PylonRental ?? 0 +
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

                return Ok(new SuccessfulResponse { message = $"Successfully updated row {rowIds}!" });
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
        public async Task<IActionResult> UpdateSingleRow(int rowId, [FromBody] UpdateChildRequest request)
        {
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

        [HttpPost("{reportId:int}/rows/add")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(AddSingleRowResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSingleRowToReport(int reportId, [FromBody] AddSingleRowRequest request)
        {
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
    }
}
