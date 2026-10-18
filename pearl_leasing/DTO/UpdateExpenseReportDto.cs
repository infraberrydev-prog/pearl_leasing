namespace pearl_leasing.DTO
{
    public class UpdateExpenseReportDto
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }
    }
}
