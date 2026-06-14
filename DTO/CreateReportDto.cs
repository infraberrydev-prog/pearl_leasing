using FluentValidation;
using pearl_leasing.Models;
using System.Text.Json.Serialization;

namespace pearl_leasing.DTO
{
    public class CreateReportDto
    {
        public int Id { get; set; }

        public string ReportName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal GrandTotalAmount { get; set; }

        public string ReportIdFormatted => $"RP{Id}";

        public List<CreateReportRowDto> Rows { get; set; } = new List<CreateReportRowDto>();
    }

    public class CreateReportRowDto
    {
        public int BuildingId { get; set; }
        public int StallUnitId { get; set; }
        public string StallUnit { get; set; } = string.Empty;
        public string BuildingType { get; set; } = string.Empty;
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
    }

    public class CreateReportValidator : AbstractValidator<CreateReportDto>
    {
        public CreateReportValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.Rows)
                .NotEmpty().WithErrorCode("RE014").WithMessage("Report must contain at least one row.")
                .ForEach(row => row.SetValidator(new CreateReportRowValidator()));
        }
    }

    public class CreateReportRowValidator : AbstractValidator<CreateReportRowDto>
    {
        public CreateReportRowValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.BuildingId)
                .NotEmpty().WithErrorCode("RE002").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.VendorNameOrCategory)
                .NotEmpty().WithErrorCode("RE003").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.StallUnitId)
                .NotEmpty().WithErrorCode("RE004").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.NameOfLessee)
                .NotEmpty().WithErrorCode("RE005").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.MonthlyRental)
                .NotEmpty().WithErrorCode("RE006").WithMessage("{PropertyName} is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("RE007").WithMessage("{PropertyName} must be 0 or greater.");

            RuleFor(x => x.PaymentStatus)
                .NotEmpty().WithErrorCode("RE008").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithErrorCode("RE009").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.ReferenceNumber)
                .NotEmpty().WithErrorCode("RE010").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.Electricity)
                .GreaterThanOrEqualTo(0).WithErrorCode("RE011").WithMessage("{PropertyName} must be 0 or greater.");

            RuleFor(x => x.Water)
                .GreaterThanOrEqualTo(0).WithErrorCode("RE012").WithMessage("{PropertyName} must be 0 or greater.");

            RuleFor(x => x.OtherUtilities)
                .GreaterThanOrEqualTo(0).WithErrorCode("RE013").WithMessage("{PropertyName} must be 0 or greater.");
        }
    }
}
