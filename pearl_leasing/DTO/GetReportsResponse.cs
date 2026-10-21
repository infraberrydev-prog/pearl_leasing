namespace pearl_leasing.DTO
{
    public record GetReportsResponse(
        List<ReportListDto> Data,
        int TotalRecords,
        int CurrentPage,
        int TotalPages,
        int PageSize
    );

    public record ReportListDto(
        int Id,
        string ReportId,
        string ReportName,
        decimal TotalExpenses,
        DateTime DateTimeCreated,
        DateTime? LastDateUpdated
    );

    public record ChildReportListDto(
        int Id,
        int LeasingReportId,
        string ReportId,
        string ReportName,
        string Building,
        string StallUnit,
        string VendorNameOrCategory,
        string NameOfLessee,
        string? MobileNumber,
        string? TinNo,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNumber,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent,
        decimal RowTotalAmount,
        DateTime DateTimeCreated,
        DateTime? LastDateUpdated
    );

    public record GetChildReportsResponse(
        List<ChildReportListDto> Data,
        int TotalRecords,
        int CurrentPage,
        int TotalPages,
        int PageSize
    );
}
