namespace pearl_leasing.DTO
{
    public record ReportDetailsResponse(
        string ReportId,
        string ReportName,
        decimal TotalRevenue,
        DateTime DateCreated,
        List<ReportRowDetailsDto> rowData
    );

    public record ReportExpensesDetailsResponse(
        string ReportId,
        string ReportName,
        decimal Electricity,
        decimal Payroll,
        decimal Water,
        decimal MaintenanceExpenses,
        decimal TotalAmount,
        string message
    );

    public record ReportRowDetailsDto(
        int Id,
        string BuildingType,
        string VendorNameOrCategory,
        string StallUnit,
        string LesseeName,
        string MobileNo,
        string TinNo,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNo,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent,
        decimal RowTotalAmount
    );
}
