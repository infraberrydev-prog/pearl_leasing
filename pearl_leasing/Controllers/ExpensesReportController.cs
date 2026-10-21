using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Models;
using System.Security.Claims;
using System.Text.Json;

namespace pearl_leasing.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/expenses-reports")]
    public class ExpensesReportController : ControllerBase
    {

        private readonly PearlLeasingDbContext _context;
        private const string ReportsCacheTag = "reports-cache-tag";
        public ExpensesReportController(PearlLeasingDbContext context)
        {
            _context = context;
        }

        [HttpPost("save-report")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SavingSuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateExpenseReport(
            [FromBody] CreateExpensesRequestDto request,
            [FromServices] IValidator<CreateExpensesRequestDto> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }


            decimal calculatedTotal = request.Electricity + request.Water + request.Payroll + request.MaintenanceExpenses;

            var newExpense = new ExpensesReport
            {
                ReportName = request.ReportName.Trim(),
                Electricity = request.Electricity,
                Payroll = request.Payroll,
                Water = request.Water,
                MaintenanceExpenses = request.MaintenanceExpenses,
                TotalAmount = calculatedTotal
            };

            try
            {
                _context.ExpenseReports.Add(newExpense);
                await _context.SaveChangesAsync();

                return Ok(new SavingSuccessfulResponse
                {
                    message = "Report successfully submitted!",
                    savedIds = new List<int> { newExpense.Id }

                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving the expense report.");
            }
        }

        [HttpGet("{id:int}/details")]
        [OutputCache(Duration = 300, Tags = [ReportsCacheTag])]
        [ProducesResponseType(typeof(ReportDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetExpenseReportDetails(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var expense = await _context.ExpenseReports
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null)
            {
                return NotFound($"Expense report with ID {id} does not exist.");
            }

            try
            {
                var response = new ReportExpensesDetailsResponse
                (
                    expense.Id.ToString(),
                    expense.ReportName,
                    expense.Electricity,
                    expense.Payroll,
                    expense.Water,
                    expense.MaintenanceExpenses,
                    expense.TotalAmount,
                    "Successfully retrieved expense report details."
                );

                return Ok(response);
            }

            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the expense report details.");
            }
        }

        [HttpGet("get-all-report")]
        [ProducesResponseType(typeof(GetReportsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllExpenses([FromQuery] string? search,
            [FromQuery] DateOnly? startDateCreated,
            [FromQuery] DateOnly? endDateCreated,
            [FromQuery] decimal? minRevenue,
            [FromQuery] decimal? maxRevenue,
            [FromQuery] string? lastUpdated = "ALL", // Short codes: TOD, L7D, L30D, TM, ALL
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "dateTimeCreated", // Column name
            [FromQuery] string? sortOrder = "DESC")
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            pageSize = Math.Min(pageSize, 100);

            try
            {
                var query = _context.ExpenseReports
                    .AsNoTracking()
                    .Where(r => !r.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(e => e.ReportName.ToLower().Contains(searchTerm));
                }

                if (startDateCreated.HasValue)
                {
                    query = query.Where(e => e.CreatedAt >= startDateCreated.Value.ToDateTime(TimeOnly.MinValue));
                }

                if (endDateCreated.HasValue)
                {
                    query = query.Where(e => e.CreatedAt <= endDateCreated.Value.ToDateTime(TimeOnly.MaxValue));
                }

                if (minRevenue.HasValue)
                {
                    query = query.Where(e => e.TotalAmount >= minRevenue.Value);
                }

                if (maxRevenue.HasValue)
                {
                    query = query.Where(e => e.TotalAmount <= maxRevenue.Value);
                }

                if (!string.IsNullOrWhiteSpace(lastUpdated) &&
                    !lastUpdated.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.Now;

                    query = lastUpdated.ToUpperInvariant() switch
                    {
                        "TOD" => query.Where(e => e.LastDateUpdated.HasValue && e.LastDateUpdated.Value.Date == now.Date),
                        "L7D" => query.Where(e => e.LastDateUpdated.HasValue && e.LastDateUpdated.Value >= now.AddDays(-7)),
                        "L30D" => query.Where(e => e.LastDateUpdated.HasValue && e.LastDateUpdated.Value >= now.AddDays(-30)),
                        "TM" => query.Where(e => e.LastDateUpdated.HasValue &&
                                                 e.LastDateUpdated.Value.Year == now.Year &&
                                                 e.LastDateUpdated.Value.Month == now.Month),
                        _ => query
                    };
                }

                int totalRecords = await query.CountAsync();
                bool isDescending = string.Equals(sortOrder, "DESC", StringComparison.OrdinalIgnoreCase);

                query = sortBy?.Trim().ToLowerInvariant() switch
                {
                    "expectedrevenue" or "revenue" or "totalamount" => isDescending
                        ? query.OrderByDescending(e => e.TotalAmount).ThenByDescending(e => e.Id)
                        : query.OrderBy(e => e.TotalAmount).ThenBy(e => e.Id),

                    "lastdateupdated" or "lastupdated" => isDescending
                        ? query.OrderByDescending(e => e.LastDateUpdated ?? e.CreatedAt).ThenByDescending(e => e.Id)
                        : query.OrderBy(e => e.LastDateUpdated ?? e.CreatedAt).ThenBy(e => e.Id),

                    "reportname" or "name" => isDescending
                        ? query.OrderByDescending(e => e.ReportName).ThenByDescending(e => e.Id)
                        : query.OrderBy(e => e.ReportName).ThenBy(e => e.Id),

                    _ => isDescending
                        ? query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
                        : query.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
                };

                var reports = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReportListDto(
                        r.Id,
                        $"EP{r.Id:D5}",
                        r.ReportName,
                        r.TotalAmount,
                        r.CreatedAt,
                        r.LastDateUpdated
                    ))
                    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return Ok(new GetReportsResponse(reports, totalRecords, page, totalPages == 0 ? 1 : totalPages, pageSize));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the paginated expense reports.");
            }
        }

        [HttpPost("update-report")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateExpenseReport(
            [FromBody] UpdateExpenseReportDto request,
            [FromServices] IValidator<UpdateExpenseReportDto> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            try
            {
                var dbReport = await _context.ExpenseReports
                    .FirstOrDefaultAsync(r => r.Id == request.Id);

                if (dbReport == null)
                {
                    return NotFound(new { code = "REPORT_NOT_FOUND", message = $"Expense report with ID {request.Id} does not exist." });
                }

                dbReport.ReportName = request.ReportName.Trim();
                dbReport.Electricity = request.Electricity;
                dbReport.Payroll = request.Electricity;
                dbReport.Water = request.Water;
                dbReport.MaintenanceExpenses = request.MaintenanceExpenses;

                dbReport.TotalAmount = request.Electricity + request.Water + request.MaintenanceExpenses + request.Payroll;
                dbReport.LastDateUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new SuccessfulResponse()
                {
                    message = "Report successfully updated!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Expense Update Error]: {ex.Message}");
                return StatusCode(500, new { code = "SERVER_ERROR", message = "An error occurred while updating the expense report." });
            }
        }

        [HttpPost("delete/{reportId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            var report = await _context.ExpenseReports
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
                    EntityName = nameof(ExpensesReport),
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

        [HttpPost("download")]
        [EnableRateLimiting("DownloadPolicy")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadExpenseReports(
            [FromBody] DownloadReportsRequest request,
            [FromServices] IValidator<DownloadReportsRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse(400, "VALIDATION_ERROR", string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            var reportIds = request.ReportIds.Distinct().ToList();
            var expenses = await _context.ExpenseReports
                .AsNoTracking()
                .Where(e => reportIds.Contains(e.Id) && !e.IsDeleted)
                .OrderBy(e => e.Id)
                .ToListAsync();

            if (expenses.Count == 0)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", "No matching active expense reports found."));
            }

            using var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(
                zipStream,
                System.IO.Compression.ZipArchiveMode.Create,
                true))
            {
                foreach (var expense in expenses)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Expense Breakdown");
                    var formattedReportId = $"EP{expense.Id:D5}";
                    var generatedAt = DateTime.Now;

                    worksheet.Cell("A1").Value = expense.ReportName;
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 14;
                    worksheet.Cell("A2").Value = generatedAt;
                    worksheet.Cell("A2").Style.NumberFormat.Format = "MMMM dd, yyyy";
                    worksheet.Cell("A3").Value = generatedAt;
                    worksheet.Cell("A3").Style.NumberFormat.Format = "HH:mm:ss";
                    worksheet.Cell("D1").Value = "Report ID:";
                    worksheet.Cell("E1").Value = formattedReportId;
                    worksheet.Range("D1:E1").Style.Font.Bold = true;

                    worksheet.Cell("A5").Value = "Category";
                    worksheet.Cell("B5").Value = "Expense Item";
                    worksheet.Cell("C5").Value = "Amount";
                    var headerRange = worksheet.Range("A5:C5");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#C0504D");
                    headerRange.Style.Font.FontColor = XLColor.White;

                    var rows = new[]
                    {
                        (Category: "Utilities", Item: "Electricity", Amount: expense.Electricity),
                        (Category: "Utilities", Item: "Payroll", Amount: expense.Payroll),
                        (Category: "Utilities", Item: "Water", Amount: expense.Water),
                        (Category: "Utilities", Item: "Other Utilities", Amount: expense.MaintenanceExpenses)
                    };

                    var currentRow = 6;
                    foreach (var item in rows)
                    {
                        worksheet.Cell(currentRow, 1).Value = item.Category;
                        worksheet.Cell(currentRow, 2).Value = item.Item;
                        worksheet.Cell(currentRow, 3).Value = item.Amount;
                        worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                        currentRow++;
                    }

                    worksheet.Cell(currentRow, 2).Value = "TOTAL EXPENSES:";
                    worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 3).FormulaA1 = $"=SUM(C6:C{currentRow - 1})";
                    worksheet.Cell(currentRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                    worksheet.Columns().AdjustToContents();

                    using var excelStream = new MemoryStream();
                    workbook.SaveAs(excelStream);
                    var safeName = string.Join("_", expense.ReportName.Split(Path.GetInvalidFileNameChars()));
                    var entry = archive.CreateEntry($"{formattedReportId}_{safeName}.xlsx", System.IO.Compression.CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    excelStream.Position = 0;
                    await excelStream.CopyToAsync(entryStream);
                }
            }

            return File(zipStream.ToArray(), "application/zip", "expense_reports.zip");
        }
    }
}
