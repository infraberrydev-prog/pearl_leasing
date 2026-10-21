using FluentValidation;

namespace pearl_leasing.DTO
{
    public class UpdateReportRowDto
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public int StallUnitId { get; set; }
        public string ReportName { get; set; }
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

    public class UpdateReportRowDtoValidator : AbstractValidator<UpdateReportRowDto>
    {
        public UpdateReportRowDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ReportName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.BuildingId).GreaterThan(0);
            RuleFor(x => x.StallUnitId).GreaterThan(0);
            RuleFor(x => x.VendorNameOrCategory).NotEmpty();
            RuleFor(x => x.NameOfLessee).NotEmpty();
            RuleFor(x => x.MonthlyRental).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Electricity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Water).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherUtilities).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CusaFee).GreaterThanOrEqualTo(0).When(x => x.CusaFee.HasValue);
            RuleFor(x => x.PylonRental).GreaterThanOrEqualTo(0).When(x => x.PylonRental.HasValue);
            RuleFor(x => x.PenaltyTenPercent).GreaterThanOrEqualTo(0).When(x => x.PenaltyTenPercent.HasValue);
        }
    }
}
