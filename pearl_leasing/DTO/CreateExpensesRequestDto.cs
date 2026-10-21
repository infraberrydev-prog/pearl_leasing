

using FluentValidation;

namespace pearl_leasing.DTO
{
    public class CreateExpensesRequestDto
    {
        public string ReportName { get; set; } = string.Empty;
        public decimal Electricity { get; set; }
        public decimal Payroll { get; set; }
        public decimal Water { get; set; }
        public decimal MaintenanceExpenses { get; set; }
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

            RuleFor(x => x.Payroll)
                .GreaterThanOrEqualTo(0).WithMessage("Payroll amount cannot be negative.");

            RuleFor(x => x.Water)
                .GreaterThanOrEqualTo(0).WithMessage("Water amount cannot be negative.");

            RuleFor(x => x.MaintenanceExpenses)
                .GreaterThanOrEqualTo(0).WithMessage("Maintenance Expenses amount cannot be negative.");
        }
    }
}
