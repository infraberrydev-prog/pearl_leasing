using System.ComponentModel.DataAnnotations.Schema;

namespace pearl_leasing.Models
{
    public class ExpensesReport
    {
        public int Id { get; set; }

        public string ReportName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Payroll { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashAdvance { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Electricity { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Water { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherUtilities { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
