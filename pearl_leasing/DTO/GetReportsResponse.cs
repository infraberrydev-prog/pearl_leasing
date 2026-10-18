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
        decimal ExpectedCashRevenue,
        DateTime DateTimeCreated,
        DateTime? LastDateUpdated
    );
}
