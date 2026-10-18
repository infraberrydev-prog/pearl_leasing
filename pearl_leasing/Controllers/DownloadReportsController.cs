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

                // Report name at top-left
                worksheet.Cell("A1").Value = report.ReportName;
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;

                // Date and time generated below report name
                var generatedAt = report.CreatedAt.ToLocalTime();
                worksheet.Cell("A2").Value = generatedAt;
                worksheet.Cell("A2").Style.NumberFormat.Format = "MMMM dd, yyyy"; // Date: MMMM dd, yyyy

                worksheet.Cell("A3").Value = generatedAt;
                worksheet.Cell("A3").Style.NumberFormat.Format = "HH:mm:ss"; // Time: HH:mm:ss

                // Small metadata on the right
                worksheet.Cell("D1").Value = "Report ID:";
                worksheet.Cell("E1").Value = formattedReportId;
                worksheet.Range("D1:E1").Style.Font.Bold = true;

                // Header row (start at row 5)
                int headerRow = 5;
                var headers = new[] {
                    "#",
                    "Building",
                    "Stall Unit",
                    "Vendor/Category",
                    "Lessee",
                    "Mobile No",
                    "TIN No",
                    "Monthly Rental",
                    "Payment Status",
                    "Payment Method",
                    "Reference No",
                    "Electricity",
                    "Water",
                    "Other Utilities",
                    "CUSA Fee",
                    "Pylon Rental",
                    "Penalty 10%",
                    "Row Total"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(headerRow, i + 1).Value = headers[i];
                }

                var headerRange = worksheet.Range(headerRow, 1, headerRow, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                headerRange.Style.Font.FontColor = XLColor.White;

                // Data rows
                int currentRow = headerRow + 1;
                foreach (var row in report.Rows)
                {
                    // fallback to snapshot values; if empty try to resolve from master tables
                    string buildingName = row.Building;
                    if (string.IsNullOrWhiteSpace(buildingName) && row.BuildingId.HasValue)
                    {
                        var b = await _context.Buildings.FindAsync(row.BuildingId.Value);
                        if (b != null) buildingName = b.Name;
                    }

                    string stallNumber = row.StallUnit;
                    if (string.IsNullOrWhiteSpace(stallNumber) && row.StallUnitId.HasValue)
                    {
                        var s = await _context.StallUnits.FindAsync(row.StallUnitId.Value);
                        if (s != null) stallNumber = s.UnitNumber;
                    }

                    worksheet.Cell(currentRow, 1).Value = row.Id;
                    worksheet.Cell(currentRow, 2).Value = buildingName;
                    worksheet.Cell(currentRow, 3).Value = stallNumber;
                    worksheet.Cell(currentRow, 4).Value = row.VendorNameOrCategory;
                    worksheet.Cell(currentRow, 5).Value = row.NameOfLessee;
                    worksheet.Cell(currentRow, 6).Value = row.MobileNumber ?? string.Empty;
                    worksheet.Cell(currentRow, 7).Value = row.TinNo ?? string.Empty;

                    worksheet.Cell(currentRow, 8).Value = row.MonthlyRental;
                    worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 9).Value = row.PaymentStatus;
                    worksheet.Cell(currentRow, 10).Value = row.PaymentMethod;
                    worksheet.Cell(currentRow, 11).Value = row.ReferenceNumber;

                    worksheet.Cell(currentRow, 12).Value = row.Electricity;
                    worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 13).Value = row.Water;
                    worksheet.Cell(currentRow, 13).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 14).Value = row.OtherUtilities;
                    worksheet.Cell(currentRow, 14).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 15).Value = row.CusaFee ?? 0m;
                    worksheet.Cell(currentRow, 15).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 16).Value = row.PylonRental ?? 0m;
                    worksheet.Cell(currentRow, 16).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 17).Value = row.PenaltyTenPercent ?? 0m;
                    worksheet.Cell(currentRow, 17).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 18).Value = row.RowTotalAmount;
                    worksheet.Cell(currentRow, 18).Style.NumberFormat.Format = "₱#,##0.00";

                    currentRow++;
                }

                // Grand total row
                worksheet.Cell(currentRow, headers.Length - 0).Value = "GRAND TOTAL:";
                worksheet.Cell(currentRow, headers.Length - 0).Style.Font.Bold = true;

                // Sum the Row Total column (column 18 -> R)
                worksheet.Cell(currentRow, 18).FormulaA1 = $"=SUM(R{headerRow + 1}:R{currentRow - 1})";
                worksheet.Cell(currentRow, 18).Style.NumberFormat.Format = "₱#,##0.00";
                worksheet.Cell(currentRow, 18).Style.Font.Bold = true;

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
