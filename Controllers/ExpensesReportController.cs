using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Models;

namespace pearl_leasing.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/expense-reports")]
    public class ExpensesReportController : ControllerBase
    {

        private readonly PearlLeasingDbContext _context;
        public ExpensesReportController(PearlLeasingDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-expense-report")]
        public async Task<IActionResult> CreateExpenseReport(
    [FromBody] CreateExpensesRequestDto request,
    [FromServices] IValidator<CreateExpensesRequestDto> validator)
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            decimal calculatedTotal = request.Electricity + request.Water + request.OtherUtilities;

            var newExpense = new ExpensesReport
            {
                ReportName = request.ReportName.Trim(),
                Electricity = request.Electricity,
                Water = request.Water,
                OtherUtilities = request.OtherUtilities,
                TotalAmount = calculatedTotal
            };

            try
            {
                _context.ExpenseReports.Add(newExpense);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Expense report created successfully!",
                    id = newExpense.Id,
                    totalAmount = newExpense.TotalAmount,
                    data = newExpense
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving the expense report.");
            }
        }

        [HttpGet("{id}/details")]
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
                var allBreakdownItems = new List<object>
                {
                    new { Category = "Utilities", ItemName = "Electricity", Amount = expense.Electricity },
                    new { Category = "Utilities", ItemName = "Water", Amount = expense.Water },
                    new { Category = "Utilities", ItemName = "Other Utilities", Amount = expense.OtherUtilities }
                };

                int totalRows = allBreakdownItems.Count;

                var paginatedRows = allBreakdownItems
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                return Ok(new
                {
                    message = "Successfully retrieved expense report header with paginated breakdown.",
                    id = expense.Id,
                    reportId = "EP" + expense.Id.ToString().PadLeft(5, '0'),
                    reportName = expense.ReportName,
                    grandTotalAmount = expense.TotalAmount,
                    createdAt = expense.CreatedAt,

                    currentRowPage = pageNumber,
                    pageSize = pageSize,
                    totalRowsCount = totalRows,
                    totalRowPages = totalPages,
                    hasNextRowPage = pageNumber < totalPages,
                    hasPreviousRowPage = pageNumber > 1,

                    rows = paginatedRows
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the expense report details.");
            }
        }

        [HttpGet("get-all-expenses-report")]
        public async Task<IActionResult> GetAllExpenses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            try
            {
                int totalRecords = await _context.ExpenseReports.CountAsync();

                var expenses = await _context.ExpenseReports
    .OrderByDescending(e => e.CreatedAt).Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

                var formattedExpenses = expenses.Select(e => new
                {
                    Id = e.Id,
                    ReportId = "EP" + e.Id.ToString().PadLeft(5, '0'),
                    ReportName = e.ReportName,
                    Electricity = e.Electricity,
                    Water = e.Water,
                    OtherUtilities = e.OtherUtilities,
                    GrandTotalAmount = e.TotalAmount,
                    ReportDate = e.CreatedAt
                }).ToList();

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return Ok(new
                {
                    message = "Successfully retrieved expense reports.",
                    currentPage = pageNumber,
                    pageSize = pageSize,
                    totalRecords = totalRecords,
                    totalPages = totalPages,
                    hasNextPage = pageNumber < totalPages,
                    hasPreviousPage = pageNumber > 1,
                    data = formattedExpenses
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the paginated expense reports.");
            }
        }

        [HttpPost("update-expense-report")]
        public async Task<IActionResult> UpdateExpenseReport([FromBody] UpdateExpenseReportDto request)
        {
            if (request == null)
            {
                return BadRequest(new { code = "EMPTY_PAYLOAD", message = "Payload cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(request.ReportName))
            {
                return BadRequest(new { code = "REQUIRED_FIELD", message = "Report Name is required." });
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
                dbReport.Water = request.Water;
                dbReport.OtherUtilities = request.OtherUtilities;

                dbReport.TotalAmount = request.Electricity + request.Water + request.OtherUtilities;


                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Expense report updated successfully!",
                    id = dbReport.Id,
                    reportName = dbReport.ReportName,
                    totalAmount = dbReport.TotalAmount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Expense Update Error]: {ex.Message}");
                return StatusCode(500, new { code = "SERVER_ERROR", message = "An error occurred while updating the expense report." });
            }
        }

        [HttpGet("search-filter")]
        public async Task<IActionResult> SearchExpenseReports(
            [FromQuery] string? reportName,
            [FromQuery] decimal? minExpense,
            [FromQuery] decimal? maxExpense,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.ExpenseReports.AsQueryable();

            if (!string.IsNullOrWhiteSpace(reportName))
            {
                var searchTerm = reportName.Trim().ToLower();
                query = query.Where(e => e.ReportName.ToLower().Contains(searchTerm));
            }

            if (minExpense.HasValue)
            {
                query = query.Where(e => e.TotalAmount >= minExpense.Value);
            }
            if (maxExpense.HasValue)
            {
                query = query.Where(e => e.TotalAmount <= maxExpense.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(e => e.CreatedAt <= endOfDay);
            }

            try
            {
                var results = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                var formattedResults = results.Select(e => new
                {
                    Id = e.Id,
                    ReportId = "EP" + e.Id.ToString().PadLeft(5, '0'),
                    ReportName = e.ReportName,
                    Payroll = e.Payroll,
                    CashAdvance = e.CashAdvance,
                    Electricity = e.Electricity,
                    Water = e.Water,
                    OtherUtilities = e.OtherUtilities,
                    TotalAmount = e.TotalAmount,
                    CreatedAt = e.CreatedAt
                }).ToList();

                return Ok(new
                {
                    message = $"Successfully retrieved {results.Count} expense reports matching the criteria.",
                    count = results.Count,
                    data = formattedResults
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while searching and filtering the expense reports.");
            }
        }
    }
}
