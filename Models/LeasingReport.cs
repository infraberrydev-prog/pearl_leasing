using System.ComponentModel.DataAnnotations.Schema;

namespace pearl_leasing.Models
{
    public class LeasingReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public decimal GrandTotalAmount { get; set; }

        public List<LeasingReportRow> Rows { get; set; } = new List<LeasingReportRow>();
    }

    public class LeasingReportRow
    {
        public int Id { get; set; }
        public int LeasingReportId { get; set; }
        [ForeignKey("LeasingReportId")]
        public LeasingReport? LeasingReport { get; set; }

        public int? BuildingId { get; set; }
        public int? StallUnitId { get; set; }

        [NotMapped]
        public string Building { get; set; } = string.Empty;

        [NotMapped]
        public string StallUnit { get; set; } = string.Empty;

        public string VendorNameOrCategory { get; set; } = string.Empty;
        public string NameOfLessee { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }

        public decimal MonthlyRental { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;

        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }

        public decimal? PenaltyTenPercent { get; set; }
        public decimal RowTotalAmount { get; set; }
    }
}
