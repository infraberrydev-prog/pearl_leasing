namespace pearl_leasing.DTO
{
    public class CreateExpensesRequestDto
    {
        public string ReportName { get; set; } = string.Empty;
        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }
    }
}
