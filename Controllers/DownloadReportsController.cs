using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;

namespace pearl_leasing.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class DownloadReportsController : ControllerBase
    {
        private readonly PearlLeasingDbContext _context;
        private readonly IConfiguration _configuration;

        public DownloadReportsController(PearlLeasingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("{id}/download-lease-report")]
        public async Task<IActionResult> ExportReportToExcel(int id)
        {
            var report = await _context.LeasingReports
    .Include(r => r.Rows)
    .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound($"Report with ID {id} not found.");
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Leasing Report Summary");
                string formattedReportId = "RP" + report.Id.ToString().PadLeft(5, '0');

                worksheet.Cell("A1").Value = "PEARL PLACE LEASING MANAGEMENT";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;

                worksheet.Cell("A2").Value = $"Report ID: {formattedReportId}";
                worksheet.Cell("A3").Value = $"Report Name: {report.ReportName}";
                worksheet.Cell("A4").Value = $"Date Generated: {report.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";

                worksheet.Cell("A6").Value = "Stall No.";
                worksheet.Cell("B6").Value = "Building Type";
                worksheet.Cell("C6").Value = "Vendor/Client Name";
                worksheet.Cell("D6").Value = "Payment Status";
                worksheet.Cell("E6").Value = "Amount Paid";

                var headerRange = worksheet.Range("A6:E6");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                headerRange.Style.Font.FontColor = XLColor.White;

                int currentRow = 7;
                foreach (var row in report.Rows)
                {
                    worksheet.Cell(currentRow, 1).Value = row.StallUnitId;
                    worksheet.Cell(currentRow, 2).Value = row.BuildingId;
                    worksheet.Cell(currentRow, 3).Value = row.VendorNameOrCategory;
                    worksheet.Cell(currentRow, 4).Value = row.PaymentStatus;

                    worksheet.Cell(currentRow, 5).Value = row.RowTotalAmount;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "₱#,##0.00";

                    currentRow++;
                }

                currentRow++; worksheet.Cell(currentRow, 4).Value = "GRAND TOTAL:";
                worksheet.Cell(currentRow, 4).Style.Font.Bold = true;

                worksheet.Cell(currentRow, 5).FormulaA1 = $"=SUM(E7:E{currentRow - 2})";
                worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "₱#,##0.00";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string fileName = $"{formattedReportId}_{report.ReportName.Replace(" ", "_")}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }

        [HttpGet("{id}/download-expense-report")]
        public async Task<IActionResult> ExportExpenseToExcel(int id)
        {
            var expense = await _context.ExpenseReports.FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null)
            {
                return NotFound($"Expense report with ID {id} not found.");
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Expense Breakdown");
                string formattedReportId = "EP" + expense.Id.ToString().PadLeft(5, '0');

                worksheet.Cell("A1").Value = "PEARL PLACE OPERATIONAL EXPENSES";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;

                worksheet.Cell("A2").Value = $"Report ID: {formattedReportId}";
                worksheet.Cell("A3").Value = $"Report Name: {expense.ReportName}";
                worksheet.Cell("A4").Value = $"Date Logged: {expense.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";

                worksheet.Cell("A6").Value = "Category";
                worksheet.Cell("B6").Value = "Expense Item";
                worksheet.Cell("C6").Value = "Amount";

                var headerRange = worksheet.Range("A6:C6");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#C0504D"); headerRange.Style.Font.FontColor = XLColor.White;

                var rowsData = new List<(string Category, string Item, decimal Amt)>
                {
                    ("Utilities", "Electricity", expense.Electricity),
                    ("Utilities", "Water", expense.Water),
                    ("Utilities", "Other Utilities", expense.OtherUtilities)
                };

                int currentRow = 7;
                foreach (var data in rowsData)
                {
                    worksheet.Cell(currentRow, 1).Value = data.Category;
                    worksheet.Cell(currentRow, 2).Value = data.Item;
                    worksheet.Cell(currentRow, 3).Value = data.Amt;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                    currentRow++;
                }

                currentRow++; worksheet.Cell(currentRow, 2).Value = "TOTAL EXPENSES:";
                worksheet.Cell(currentRow, 2).Style.Font.Bold = true;

                worksheet.Cell(currentRow, 3).FormulaA1 = $"=SUM(C7:C{currentRow - 2})";
                worksheet.Cell(currentRow, 3).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "₱#,##0.00";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"EXP_{formattedReportId}_{expense.ReportName.Replace(" ", "_")}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }
    }
}
