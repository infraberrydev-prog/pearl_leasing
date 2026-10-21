using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pearl_leasing.Models
{
    public class Logs_DeletedReports
    {
        [Key]
        public int Id { get; set; }

        public int LeasingReportRowId { get; set; }
        public int LeasingReportId { get; set; }

        // Snapshot fields
        public int? BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public int? StallUnitId { get; set; }
        public string StallUnitNumber { get; set; } = string.Empty;

        public string VendorNameOrCategory { get; set; } = string.Empty;
        public string NameOfLessee { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string? TinNo { get; set; }

        [Column(TypeName = "numeric")]
        public decimal MonthlyRental { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;

        [Column(TypeName = "numeric")]
        public decimal Electricity { get; set; }
        [Column(TypeName = "numeric")]
        public decimal Water { get; set; }
        [Column(TypeName = "numeric")]
        public decimal OtherUtilities { get; set; }
        [Column(TypeName = "numeric")]
        public decimal? CusaFee { get; set; }
        [Column(TypeName = "numeric")]
        public decimal? PylonRental { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? PenaltyTenPercent { get; set; }
        [Column(TypeName = "numeric")]
        public decimal RowTotalAmount { get; set; }

        public string DeletedBy { get; set; } = "System";
        public DateTime DeletedAt { get; set; } = DateTime.Now;
    }
}
