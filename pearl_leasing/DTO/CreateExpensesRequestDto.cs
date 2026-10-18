using FluentValidation;

namespace pearl_leasing.DTO
{
    public class CreateExpensesRequestDto
    {
        public string ReportName { get; set; } = string.Empty;
        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }
    }

    public class CreateExpensesRequestDtoValidator : AbstractValidator<CreateExpensesRequestDto>
    {
        public CreateExpensesRequestDtoValidator()
        {
                        RuleFor(x => x.ReportName)
                .NotEmpty().WithMessage("Report Name is required.")
                .MaximumLength(150).WithMessage("Report Name cannot exceed 150 characters.");

                        RuleFor(x => x.Electricity)
                .GreaterThanOrEqualTo(0).WithMessage("Electricity amount cannot be negative.");

                        RuleFor(x => x.Water)
                .GreaterThanOrEqualTo(0).WithMessage("Water amount cannot be negative.");

                        RuleFor(x => x.OtherUtilities)
                .GreaterThanOrEqualTo(0).WithMessage("Other utilities amount cannot be negative.");

                        RuleFor(x => x)
                .Must(x => (x.Electricity + x.Water + x.OtherUtilities) > 0)
                .WithMessage("At least one expense category (Electricity, Water, or Others) must be greater than 0.");
        }
    }
}
