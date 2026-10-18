using pearl_leasing.Models;

namespace pearl_leasing.DTO
{
    public class UpdateReportRowDto
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public int StallUnitId { get; set; }
        public string Building { get; set; } = string.Empty;
        public string VendorNameOrCategory { get; set; } = string.Empty;
        public string StallUnit { get; set; } = string.Empty;
        public string NameOfLessee { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public decimal MonthlyRental { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }
        public decimal? CusaFee { get; set; }
        public decimal? PylonRental { get; set; }
        public decimal? PenaltyTenPercent { get; set; }
    }
}
