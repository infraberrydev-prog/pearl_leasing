namespace pearl_leasing.DTO
{
    public record UpdateChildRequest
    (
        int Id,
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
        string NameOfLessee,
        string? MobileNumber,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNumber,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent
    );

    // DTO para sa pagdagdag ng bagong row sa umiiral na Report
    public record AddReportRowRequest(
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
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
        decimal? PenaltyTenPercent
    );

    // Legacy DTO alias — align to AddReportRowRequest structure used by the controller
    public record AddSingleRowRequest(
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
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
        decimal? PenaltyTenPercent
    );

    public class AddSingleRowResponse
    {
        public string message { get; set; } = string.Empty;
        public int newRowId { get; set; }
    }
}